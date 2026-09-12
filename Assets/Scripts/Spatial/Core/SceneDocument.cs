using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GeoX.Spatial
{
    /// <summary>Immutable scientific scene v2. Parsing never resolves assets or runs providers.</summary>
    public sealed class SceneDocument
    {
        public const int SchemaVersion = 2;
        private readonly JObject data;
        private readonly Dictionary<string, JObject> frames, operations, registrations, layers;
        public string Id => (string)data["id"];
        public string Revision => (string)data["revision"];
        public string SceneFrameId => (string)data["sceneFrameId"];

        private SceneDocument(JObject document)
        {
            SceneDocumentValidation.Validate(document);
            data = document;
            frames = SceneDocumentValidation.Table(data["frames"]);
            operations = SceneDocumentValidation.Table(data["operations"]);
            registrations = SceneDocumentValidation.Table(data["registrations"]);
            layers = SceneDocumentValidation.Table(data["layers"]);
        }

        /// <summary>Requires schema 2; use SceneMigration explicitly for schema 1.</summary>
        public static SceneDocument Read(string json) => new SceneDocument(PortableJson.Read(json));
        public string ToJson() => data.ToString(Formatting.None);
        public JObject ToJObject() => (JObject)data.DeepClone();

        /// <summary>Checks a proposed revision against its predecessor, without mutating either.</summary>
        public void ValidateRevisionOf(SceneDocument previous)
        {
            if (previous == null) throw new ArgumentNullException(nameof(previous));
            PortableJson.Require(Id == previous.Id, "scene_identity_changed");
            PortableJson.Require(Revision != previous.Revision || JToken.DeepEquals(data, previous.data),
                "scientific_change_requires_new_revision");
            ImmutableDefinitions(previous.frames, frames, null);
            ImmutableDefinitions(previous.operations, operations,
                new[] { "id", "kind", "version", "sourceFrameId", "targetFrameId", "payload" });
            ImmutableDefinitions(previous.registrations, registrations, null);
        }

        private static void ImmutableDefinitions(Dictionary<string, JObject> oldRecords,
            Dictionary<string, JObject> newRecords, string[] fields)
        {
            foreach (var prior in oldRecords)
            {
                if (!newRecords.TryGetValue(prior.Key, out JObject current)) continue;
                bool same = fields == null ? JToken.DeepEquals(prior.Value, current) :
                    fields.All(field => JToken.DeepEquals(prior.Value[field], current[field]));
                PortableJson.Require(same, "immutable_definition_changed: " + prior.Key);
            }
        }

        /// <summary>Returns a supported tuple map, or a reason it cannot be evaluated.
        /// Availability is engineering capability, never scientific accuracy or acceptance.</summary>
        public bool TryGetAffineOperation(string operationId, out AffineMap map, out string reason)
        {
            JObject op = Record(operations, operationId);
            map = null;
            reason = RequiredExtensionReason();
            if (reason != null) return false;
            if (!SceneDocumentValidation.IsAffine(op))
            {
                reason = (string)op["kind"] == "geodetic" && PortableJson.Version(op["version"], 1)
                    ? "qualified_geodetic_provider_required" : "unsupported_operation_kind_or_version";
                return false;
            }
            JObject source = frames[(string)op["sourceFrameId"]], target = frames[(string)op["targetFrameId"]];
            string sourceBody = (string)source["body"], targetBody = (string)target["body"];
            if (sourceBody != null && targetBody != null && sourceBody != targetBody)
                reason = "known_body_conflict";
            else if ((string)source["kind"] == "geographic" || (string)target["kind"] == "geographic")
                reason = "geographic_frame_requires_qualified_provider";
            else if ((string)source["definition"]["encoding"] != "opaque" ||
                (string)target["definition"]["encoding"] != "opaque")
                reason = "crs_interpretation_required";
            if (reason != null) return false;
            map = SceneDocumentValidation.Affine(op);
            return true;
        }

        public Vector3d EvaluateOperation(string operationId, Vector3d point, bool inverse = false)
        {
            if (!TryGetAffineOperation(operationId, out AffineMap map, out string reason))
                throw new InvalidOperationException(reason);
            return (inverse ? map.Inverse() : map).Apply(point);
        }

        public Vector3d SourceToScene(string layerId, Vector3d point, bool inverse = false)
        {
            string registrationId = (string)Record(layers, layerId)["activeRegistrationId"];
            if (registrationId == null) throw new InvalidOperationException("layer_not_registered");
            JObject common = frames[SceneFrameId];
            if ((string)common["kind"] != "cartesian" || common["cartesian"].Type == JTokenType.Null)
                throw new InvalidOperationException("common_frame_not_available_for_cartesian_evaluation");
            string op = (string)registrations[registrationId]["operationId"];
            return EvaluateOperation(op, point, inverse);
        }

        public Vector3d RepresentationToSource(string layerId, string representationId, Vector3d point, bool inverse = false)
        {
            JObject layer = Record(layers, layerId);
            JObject representation = Record(SceneDocumentValidation.Table(layer["representations"]), representationId);
            string reason = RequiredExtensionReason();
            if (reason != null) throw new InvalidOperationException(reason);
            string op = (string)representation["toSourceOperationId"];
            if (op != null) return EvaluateOperation(op, point, inverse);
            if ((string)representation["frameId"] != (string)layer["sourceFrameId"])
                throw new InvalidOperationException("representation_mapping_unresolved");
            return point; // Exact frame identity, without a redundant operation record.
        }

        private string RequiredExtensionReason() => ((JObject)data["extensions"]).Properties()
            .Any(p => (bool)p.Value["required"]) ? "unsupported_required_extension" : null;

        private static JObject Record(Dictionary<string, JObject> records, string id)
        {
            if (id == null || !records.TryGetValue(id, out JObject record))
                throw new ArgumentException("unknown_record_identity", nameof(id));
            return record;
        }
    }
}

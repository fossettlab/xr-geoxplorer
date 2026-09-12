using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GeoX.Spatial
{
    /// <summary>A detached, immutable preview of one explicitly supplied affine registration.</summary>
    public sealed class DeclaredRegistrationProposal
    {
        private readonly string baseJson;
        private readonly AffineMap mapping;
        private readonly JObject evidence;

        public string SceneId { get; }
        public string BaseRevision { get; }
        public string LayerId { get; }
        public string SourceFrameId { get; }
        public string TargetFrameId { get; }
        public string PriorRegistrationId { get; }
        public SceneDocument PreviewScene { get; }
        public AffineMap Mapping => new AffineMap(mapping.Linear, mapping.Translation);
        public JObject Evidence => (JObject)evidence.DeepClone();

        internal string BaseJson => baseJson;
        internal AffineMap StoredMapping => mapping;
        internal JObject StoredEvidence => evidence;

        internal DeclaredRegistrationProposal(SceneDocument scene, string layerId,
            string sourceFrameId, string targetFrameId, string priorRegistrationId,
            AffineMap map, JObject suppliedEvidence, SceneDocument previewScene)
        {
            baseJson = scene.ToJson();
            mapping = new AffineMap(map.Linear, map.Translation);
            evidence = (JObject)suppliedEvidence.DeepClone();
            SceneId = scene.Id;
            BaseRevision = scene.Revision;
            LayerId = layerId;
            SourceFrameId = sourceFrameId;
            TargetFrameId = targetFrameId;
            PriorRegistrationId = priorRegistrationId;
            PreviewScene = previewScene;
        }

        public Vector3d Evaluate(Vector3d sourcePoint) => mapping.Apply(sourcePoint);
    }

    /// <summary>
    /// Pure commands for a caller-supplied affine declaration. Apply always requires
    /// fresh record IDs and a fresh revision, including an otherwise identical reapply.
    /// </summary>
    public static class DeclaredRegistration
    {
        private const string PreviewOperationStem = "preview:declared-affine-operation";
        private const string PreviewRegistrationStem = "preview:declared-registration";

        public static DeclaredRegistrationProposal Begin(SceneDocument scene,
            string expectedRevision, string layerId, string targetFrameId,
            AffineMap mapping, JObject evidence)
        {
            if (scene == null) throw new ArgumentNullException(nameof(scene));
            RequireText(expectedRevision, nameof(expectedRevision));
            RequireText(layerId, nameof(layerId));
            RequireText(targetFrameId, nameof(targetFrameId));
            if (mapping == null) throw new ArgumentNullException(nameof(mapping));
            if (evidence == null) throw new ArgumentNullException(nameof(evidence));
            Require(scene.Revision == expectedRevision, "stale_scene_revision");
            Require(scene.SceneFrameId == targetFrameId, "target_is_not_scene_frame");

            JObject data = scene.ToJObject();
            JObject layer = Find(data, "layers", layerId, "unknown_layer_identity");
            JObject source = Find(data, "frames", (string)layer["sourceFrameId"],
                "unknown_source_frame_identity");
            JObject target = Find(data, "frames", targetFrameId,
                "unknown_target_frame_identity");
            ValidateEvaluationBoundary(data, source, target);

            JObject suppliedEvidence = (JObject)evidence.DeepClone();
            try
            {
                SceneDocumentValidation.Evidence(suppliedEvidence);
            }
            catch (JsonException error)
            {
                throw new ArgumentException("invalid_registration_evidence", nameof(evidence), error);
            }
            bool hasSuppliedDeclaration = ((JArray)suppliedEvidence["statements"])
                .OfType<JObject>().Any(statement =>
                    (string)statement["category"] == "declaration" &&
                    (string)statement["status"] == "supplied");
            Require(hasSuppliedDeclaration, "supplied_declaration_evidence_required");

            // Reconstructing defensively rechecks finite coefficients and invertibility.
            var checkedMap = new AffineMap(mapping.Linear, mapping.Translation);
            string operationId = UnusedId(data, "operations", PreviewOperationStem);
            string registrationId = UnusedId(data, "registrations", PreviewRegistrationStem);
            string previewRevision = scene.Revision + ":declared-registration-preview";
            string prior = (string)layer["activeRegistrationId"];
            SceneDocument preview = BuildRevision(scene, layerId, operationId,
                registrationId, previewRevision, checkedMap, suppliedEvidence, prior);
            return new DeclaredRegistrationProposal(scene, layerId,
                (string)layer["sourceFrameId"], targetFrameId, prior, checkedMap,
                suppliedEvidence, preview);
        }

        public static SceneDocument Apply(SceneDocument current,
            DeclaredRegistrationProposal proposal, string operationId,
            string registrationId, string newRevision)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (proposal == null) throw new ArgumentNullException(nameof(proposal));
            RequireText(operationId, nameof(operationId));
            RequireText(registrationId, nameof(registrationId));
            RequireText(newRevision, nameof(newRevision));
            Require(current.Id == proposal.SceneId &&
                current.Revision == proposal.BaseRevision &&
                current.ToJson() == proposal.BaseJson, "stale_registration_proposal");
            Require(newRevision != current.Revision, "new_scene_revision_required");

            JObject data = current.ToJObject();
            Require(!ContainsId(data, "operations", operationId), "operation_id_reused");
            Require(!ContainsId(data, "registrations", registrationId),
                "registration_id_reused");
            return BuildRevision(current, proposal.LayerId, operationId,
                registrationId, newRevision, proposal.StoredMapping,
                proposal.StoredEvidence, proposal.PriorRegistrationId);
        }

        /// <summary>Discards only the detached preview and returns the canonical scene unchanged.</summary>
        public static SceneDocument Cancel(SceneDocument canonical,
            DeclaredRegistrationProposal proposal)
        {
            if (canonical == null) throw new ArgumentNullException(nameof(canonical));
            if (proposal == null) throw new ArgumentNullException(nameof(proposal));
            Require(canonical.Id == proposal.SceneId, "scene_identity_changed");
            return canonical;
        }

        public static SceneDocument Undo(SceneDocument current,
            string expectedRevision, string layerId, string newRevision)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            RequireText(expectedRevision, nameof(expectedRevision));
            RequireText(layerId, nameof(layerId));
            RequireText(newRevision, nameof(newRevision));
            Require(current.Revision == expectedRevision, "stale_scene_revision");
            Require(newRevision != current.Revision, "new_scene_revision_required");

            JObject data = current.ToJObject();
            JObject layer = Find(data, "layers", layerId, "unknown_layer_identity");
            string active = (string)layer["activeRegistrationId"];
            Require(active != null, "layer_not_registered");
            JObject registration = Find(data, "registrations", active,
                "unknown_registration_identity");
            layer["activeRegistrationId"] = registration["supersedes"].DeepClone();
            data["revision"] = newRevision;
            SceneDocument undone = SceneDocument.Read(data.ToString(Formatting.None));
            undone.ValidateRevisionOf(current);
            return undone;
        }

        private static SceneDocument BuildRevision(SceneDocument current,
            string layerId, string operationId, string registrationId,
            string revision, AffineMap map, JObject evidence, string supersedes)
        {
            JObject data = current.ToJObject();
            JObject layer = Find(data, "layers", layerId, "unknown_layer_identity");
            ((JArray)data["operations"]).Add(new JObject
            {
                ["id"] = operationId,
                ["kind"] = "affine",
                ["version"] = 1,
                ["sourceFrameId"] = (string)layer["sourceFrameId"],
                ["targetFrameId"] = current.SceneFrameId,
                ["payload"] = new JObject
                {
                    ["linear"] = new JArray(map.Linear),
                    ["translation"] = new JArray(
                        map.Translation.X, map.Translation.Y, map.Translation.Z)
                },
                ["evidence"] = evidence.DeepClone()
            });
            ((JArray)data["registrations"]).Add(new JObject
            {
                ["id"] = registrationId,
                ["operationId"] = operationId,
                ["supersedes"] = supersedes == null ? JValue.CreateNull() : supersedes,
                ["evidence"] = EmptyEvidence()
            });
            layer["activeRegistrationId"] = registrationId;
            data["revision"] = revision;
            SceneDocument changed = SceneDocument.Read(data.ToString(Formatting.None));
            changed.ValidateRevisionOf(current);
            return changed;
        }

        private static void ValidateEvaluationBoundary(JObject scene, JObject source,
            JObject target)
        {
            bool requiredExtension = ((JObject)scene["extensions"]).Properties()
                .Any(property => (bool)property.Value["required"]);
            Require(!requiredExtension, "unsupported_required_extension");
            string sourceBody = (string)source["body"];
            string targetBody = (string)target["body"];
            Require(sourceBody == null || targetBody == null || sourceBody == targetBody,
                "known_body_conflict");
            Require((string)source["kind"] != "geographic" &&
                (string)target["kind"] != "geographic",
                "geographic_frame_requires_qualified_provider");
            Require((string)source["definition"]["encoding"] == "opaque" &&
                (string)target["definition"]["encoding"] == "opaque",
                "crs_interpretation_required");
            Require((string)target["kind"] == "cartesian" &&
                target["cartesian"].Type != JTokenType.Null,
                "common_frame_not_available_for_cartesian_evaluation");
        }

        private static JObject EmptyEvidence() => new JObject
        {
            ["origins"] = new JArray(),
            ["lineage"] = new JArray(),
            ["statements"] = new JArray()
        };

        private static JObject Find(JObject scene, string table, string id, string error)
        {
            JObject record = ((JArray)scene[table]).OfType<JObject>()
                .SingleOrDefault(item => (string)item["id"] == id);
            if (record == null) throw new ArgumentException(error, nameof(id));
            return record;
        }

        private static bool ContainsId(JObject scene, string table, string id) =>
            ((JArray)scene[table]).OfType<JObject>()
                .Any(item => (string)item["id"] == id);

        private static string UnusedId(JObject scene, string table, string stem)
        {
            string candidate = stem;
            while (ContainsId(scene, table, candidate)) candidate += ":collision";
            return candidate;
        }

        private static void RequireText(string value, string parameter)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("blank_identifier", parameter);
        }

        private static void Require(bool condition, string error)
        {
            if (!condition) throw new InvalidOperationException(error);
        }
    }
}

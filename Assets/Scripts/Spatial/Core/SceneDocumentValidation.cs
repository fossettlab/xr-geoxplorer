using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using static GeoX.Spatial.PortableJson;

namespace GeoX.Spatial
{
    internal static class SceneDocumentValidation
    {
        internal static void Validate(JObject scene)
        {
            Shape(scene, "schemaVersion", "id", "revision", "sceneFrameId", "frames", "assets",
                "operations", "registrations", "layers", "metadata", "extensions");
            Require(Version(scene["schemaVersion"], 2), "unsupported_scene_schema");
            Text(scene["id"]); Text(scene["revision"]); Object(scene["metadata"]);
            var frames = Table(scene["frames"], Frame);
            var assets = Table(scene["assets"], Asset);
            var operations = Table(scene["operations"], Operation);
            var registrations = Table(scene["registrations"], Registration);
            var layers = Table(scene["layers"], Layer);
            string common = Reference(scene["sceneFrameId"], frames);
            foreach (JProperty extension in Object(scene["extensions"]).Properties())
            {
                Require(!string.IsNullOrWhiteSpace(extension.Name), "blank_extension_namespace");
                Shape(extension.Value, "version", "required", "payload");
                PositiveInteger(extension.Value["version"]); Boolean(extension.Value["required"]);
            }
            foreach (JObject op in operations.Values)
            {
                string source = Reference(op["sourceFrameId"], frames);
                string target = Reference(op["targetFrameId"], frames);
                if (source == target && IsAffine(op))
                {
                    AffineMap map = Affine(op);
                    Require(map.Linear.SequenceEqual(AffineMap.Identity.Linear) &&
                        map.Translation.X == 0 && map.Translation.Y == 0 && map.Translation.Z == 0,
                        "same_frame_requires_identity");
                }
            }
            ValidateHistory(registrations, operations);
            foreach (JObject layer in layers.Values)
            {
                string source = Reference(layer["sourceFrameId"], frames);
                string registration = Reference(layer["activeRegistrationId"], registrations, true);
                if (registration != null)
                    Endpoints(operations[(string)registrations[registration]["operationId"]], source, common);
                foreach (JToken asset in Array(layer["sourceAssetIds"])) Reference(asset, assets);
                var representations = Table(layer["representations"], Representation);
                foreach (JObject representation in representations.Values)
                {
                    Reference(representation["assetId"], assets);
                    string frame = Reference(representation["frameId"], frames);
                    string op = Reference(representation["toSourceOperationId"], operations, true);
                    if (op != null) Endpoints(operations[op], frame, source);
                }
            }
        }

        internal static Dictionary<string, JObject> Table(JToken token, Action<JObject> validate = null)
        {
            var records = new Dictionary<string, JObject>(StringComparer.Ordinal);
            foreach (JToken item in Array(token))
            {
                JObject record = Object(item);
                string id = Text(record["id"]);
                Require(!records.ContainsKey(id), "duplicate_identity: " + token.Path + ": " + id);
                validate?.Invoke(record);
                records.Add(id, record);
            }
            return records;
        }

        internal static string Reference(JToken id, Dictionary<string, JObject> records, bool nullable = false)
        {
            string key = Text(id, nullable: nullable);
            Require(key == null || records.ContainsKey(key), "dangling_reference: " + id.Path);
            return key;
        }

        private static void Frame(JObject frame)
        {
            Shape(frame, "id", "body", "kind", "definition", "cartesian");
            Text(frame["body"], nullable: true);
            Choice(frame["kind"], "unknown", "cartesian", "geographic");
            Definition(frame["definition"]);
            if (frame["cartesian"].Type == JTokenType.Null) return;
            JObject cartesian = Shape(frame["cartesian"], "axisConvention", "metresPerUnit");
            Text(cartesian["axisConvention"]);
            if (cartesian["metresPerUnit"].Type != JTokenType.Null)
                Require(Number(cartesian["metresPerUnit"]) > 0, "non_positive_unit_factor");
        }

        private static void Definition(JToken token)
        {
            JObject definition = Shape(token, "encoding", "value");
            Choice(definition["encoding"], "opaque", "wkt2", "projjson");
            JToken value = definition["value"];
            Require(value.Type == JTokenType.String || value.Type == JTokenType.Object, "invalid_definition_value");
            if ((string)definition["encoding"] == "wkt2")
                Require(value.Type == JTokenType.String && ((string)value).Length > 0, "invalid_wkt2_value");
            if ((string)definition["encoding"] == "projjson") Object(value);
        }

        internal static void Evidence(JToken token)
        {
            JObject evidence = Shape(token, "origins", "lineage", "statements");
            UniqueStrings(evidence["origins"]);
            foreach (JToken origin in Array(evidence["origins"]))
                Choice(origin, "measured", "reconstructed", "interpreted", "generated", "mixed");
            foreach (JToken lineage in Array(evidence["lineage"])) Object(lineage);
            foreach (JToken statement in Array(evidence["statements"]))
            {
                Shape(statement, "category", "status", "details");
                Choice(statement["category"], "source-accuracy", "fit-residual", "operation-accuracy",
                    "tracking", "representation-error", "declaration", "legacy");
                Choice(statement["status"], "supplied", "derived", "independently-checked", "unknown");
                Object(statement["details"]);
            }
        }

        private static void Asset(JObject asset)
        {
            Shape(asset, "id", "contentHash", "format", "locators", "metadata", "evidence");
            Text(asset["format"], nullable: true); Object(asset["metadata"]); Evidence(asset["evidence"]);
            if (asset["contentHash"].Type != JTokenType.Null)
            {
                Shape(asset["contentHash"], "algorithm", "value");
                Text(asset["contentHash"]["algorithm"]); Text(asset["contentHash"]["value"]);
            }
            foreach (JToken locator in Array(asset["locators"])) Locator(Text(locator));
        }

        private static void Locator(string locator)
        {
            // Query strings are deliberately excluded: the parser cannot prove an
            // arbitrary query is unsigned. Private resolution belongs in access state.
            Require(!locator.Any(char.IsWhiteSpace) && !locator.Any(char.IsControl) &&
                locator.IndexOfAny(new[] { '\\', '?', '#' }) < 0, "unsafe_locator");
            if (locator.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                Require(Uri.TryCreate(locator, UriKind.Absolute, out Uri uri) &&
                    uri.Scheme == "https" && uri.Host.Length > 0 && uri.UserInfo.Length == 0,
                    "unsafe_https_locator");
                return;
            }
            Require(!locator.StartsWith("/", StringComparison.Ordinal) &&
                !locator.StartsWith("~", StringComparison.Ordinal) && !locator.Contains(":"), "unsafe_package_locator");
            // No percent escapes in package names: this eliminates encoded traversal
            // and ambiguous double decoding at the later resolver boundary.
            Require(!locator.Contains("%") && locator.Split('/').All(p => p.Length > 0 && p != "." && p != ".."),
                "unsafe_package_locator");
        }

        private static void Operation(JObject op)
        {
            Shape(op, "id", "kind", "version", "sourceFrameId", "targetFrameId", "payload", "evidence");
            Text(op["kind"]); PositiveInteger(op["version"]); Object(op["payload"]); Evidence(op["evidence"]);
            if (IsAffine(op))
            {
                Shape(op["payload"], "linear", "translation");
                try { Affine(op); }
                catch (ArgumentException error) { Require(false, "invalid_affine: " + error.Message); }
            }
            if ((string)op["kind"] == "geodetic" && Version(op["version"], 1))
            {
                JObject payload = Shape(op["payload"], "definition", "provider", "database", "resources", "domain", "inverseAvailable");
                Definition(payload["definition"]);
                Shape(payload["provider"], "name", "version");
                Text(payload["provider"]["name"]); Text(payload["provider"]["version"]);
                Object(payload["database"]); Object(payload["domain"]); Boolean(payload["inverseAvailable"]);
                foreach (JToken resource in Array(payload["resources"])) Object(resource);
            }
        }

        internal static bool IsAffine(JObject op) => (string)op["kind"] == "affine" && Version(op["version"], 1);

        internal static AffineMap Affine(JObject op)
        {
            JArray linear = Array(op["payload"]["linear"]), translation = Array(op["payload"]["translation"]);
            Require(linear.Count == 9 && translation.Count == 3, "invalid_affine_dimensions");
            return new AffineMap(linear.Select(Number).ToArray(),
                new Vector3d(Number(translation[0]), Number(translation[1]), Number(translation[2])));
        }

        private static void Registration(JObject record)
        {
            Shape(record, "id", "operationId", "supersedes", "evidence");
            Evidence(record["evidence"]);
        }

        private static void Layer(JObject layer)
        {
            Shape(layer, "id", "name", "sourceFrameId", "activeRegistrationId", "sourceAssetIds", "representations", "metadata", "evidence");
            Text(layer["name"], blankAllowed: true); Object(layer["metadata"]); Evidence(layer["evidence"]);
            UniqueStrings(layer["sourceAssetIds"]);
        }

        private static void Representation(JObject record) => Shape(record, "id", "assetId", "frameId", "toSourceOperationId");

        private static void UniqueStrings(JToken token)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (JToken item in Array(token)) Require(seen.Add(Text(item)), "duplicate_value: " + token.Path);
        }

        private static void Endpoints(JObject op, string source, string target) =>
            Require((string)op["sourceFrameId"] == source && (string)op["targetFrameId"] == target,
                "operation_endpoint_mismatch: " + op["id"]);

        private static void ValidateHistory(Dictionary<string, JObject> records, Dictionary<string, JObject> operations)
        {
            foreach (JObject record in records.Values)
            {
                Reference(record["operationId"], operations);
                Reference(record["supersedes"], records, true);
            }
            var completed = new HashSet<string>(StringComparer.Ordinal);
            foreach (string start in records.Keys)
            {
                var path = new HashSet<string>(StringComparer.Ordinal);
                string id = start;
                while (id != null && !completed.Contains(id))
                {
                    Require(path.Add(id), "cyclic_registration_history");
                    JObject record = records[id];
                    string prior = (string)record["supersedes"];
                    id = prior;
                }
                completed.UnionWith(path);
            }
        }
    }
}

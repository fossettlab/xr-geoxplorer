using System;
using System.Collections;
using System.IO;
using System.Linq;
using GeoX.Spatial;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace GeoX.Spatial.Tests
{
    internal static class SpatialFixtures
    {
        internal static string Read(string name)
        {
            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "ProjectSettings/ProjectVersion.txt")))
                directory = directory.Parent;
            if (directory == null) throw new DirectoryNotFoundException("GeoXplorer fixture root");
            return File.ReadAllText(Path.Combine(directory.FullName, "tests/fixtures/spatial", name));
        }

        internal static JObject Parse(string json)
        {
            using (var text = new StringReader(json))
            using (var reader = new JsonTextReader(text) { DateParseHandling = DateParseHandling.None })
                return JObject.Load(reader);
        }

        internal static JObject Affine() => Parse(Read("affine.json"));
        internal static SceneDocument ReadScene(JObject data) => SceneDocument.Read(data.ToString(Formatting.None));
        internal static void Equal(Vector3d actual, double x, double y, double z)
        {
            Assert.That(actual.X, Is.EqualTo(x)); Assert.That(actual.Y, Is.EqualTo(y)); Assert.That(actual.Z, Is.EqualTo(z));
        }
    }

    public class SceneDocumentTests
    {
        private static IEnumerable FixtureCases()
        {
            foreach (JObject item in JObject.Parse(SpatialFixtures.Read("cases.json"))["valid"])
                yield return new TestCaseData((string)item["file"]).SetName("PortableFixture_" + (string)item["file"]);
        }

        [TestCaseSource(nameof(FixtureCases))]
        public void SharedFixturesRoundTripWithExplicitAvailability(string file)
        {
            string json = SpatialFixtures.Read(file);
            SceneDocument scene = SceneDocument.Read(json);
            Assert.That(JToken.DeepEquals(SpatialFixtures.Parse(json), scene.ToJObject()), Is.True);
            Assert.That(SceneDocument.Read(scene.ToJson()).ToJson(), Is.EqualTo(scene.ToJson()));
            JObject expected = JObject.Parse(SpatialFixtures.Read("cases.json"))["valid"].OfType<JObject>()
                .Single(item => (string)item["file"] == file);
            bool available = scene.TryGetAffineOperation("declared-placement", out AffineMap map, out string reason);
            Assert.That(available, Is.EqualTo((bool)expected["available"]));
            if (available)
            {
                JArray p = (JArray)expected["point"], q = (JArray)expected["expected"];
                Vector3d result = scene.SourceToScene("model-layer", new Vector3d((double)p[0], (double)p[1], (double)p[2]));
                SpatialFixtures.Equal(result, (double)q[0], (double)q[1], (double)q[2]);
                SpatialFixtures.Equal(map.Inverse().Apply(result), (double)p[0], (double)p[1], (double)p[2]);
            }
            else
            {
                Assert.That(map, Is.Null); Assert.That(reason, Is.EqualTo((string)expected["reason"]));
                Assert.Throws<InvalidOperationException>(() => scene.EvaluateOperation("declared-placement", default));
            }
            Assert.That(scene.ToJObject()["metadata"]["largeInteger"].ToString(), Is.EqualTo("9007199254740993"));
            Assert.That(scene.ToJObject()["metadata"]["beyondInt64"].ToString(), Is.EqualTo("123456789012345678901234567890"));
            Assert.That(scene.ToJObject()["metadata"]["timestampText"].Type, Is.EqualTo(JTokenType.String));
        }

        [TestCase("Earth")]
        [TestCase("Moon")]
        [TestCase("Mars")]
        [TestCase("project:asteroid")]
        [TestCase(null)]
        public void ExplicitMappingNeverRewritesUnknownSourceBody(string body)
        {
            JObject data = SpatialFixtures.Affine(); data["frames"][0]["body"] = body;
            SceneDocument scene = SpatialFixtures.ReadScene(data);
            SpatialFixtures.Equal(scene.SourceToScene("model-layer", new Vector3d(2, 3, 4)), 14, 26, 46);
            Assert.That((string)scene.ToJObject()["frames"][1]["body"], Is.Null);
            Assert.That((string)scene.ToJObject()["frames"][0]["body"], Is.EqualTo(body));
        }

        [TestCase("missing-field")]
        [TestCase("extra-field")]
        [TestCase("wrong-version-type")]
        [TestCase("duplicate-frame")]
        [TestCase("duplicate-asset")]
        [TestCase("duplicate-operation")]
        [TestCase("duplicate-registration")]
        [TestCase("duplicate-layer")]
        [TestCase("duplicate-representation")]
        [TestCase("dangling-frame")]
        [TestCase("dangling-asset")]
        [TestCase("dangling-solution")]
        [TestCase("dangling-history")]
        [TestCase("cycle")]
        [TestCase("wrong-registration-endpoint")]
        [TestCase("wrong-representation-endpoint")]
        [TestCase("same-frame-offset")]
        [TestCase("singular")]
        [TestCase("ill-conditioned")]
        [TestCase("string-coordinate")]
        [TestCase("negative-unit")]
        [TestCase("unknown-evidence-status")]
        [TestCase("duplicate-origin")]
        [TestCase("numeric-name")]
        [TestCase("float-operation-version")]
        public void MalformedScienceIsRejected(string fault)
        {
            JObject data = SpatialFixtures.Affine();
            JToken op = data["operations"][0], layer = data["layers"][0], frame = data["frames"][0];
            switch (fault)
            {
                case "missing-field": data.Remove("revision"); break;
                case "extra-field": frame["implicitDatum"] = "Earth"; break;
                case "wrong-version-type": data["schemaVersion"] = "2"; break;
                case "duplicate-frame": ((JArray)data["frames"]).Add(frame.DeepClone()); break;
                case "duplicate-asset": Duplicate(data, "assets"); break;
                case "duplicate-operation": Duplicate(data, "operations"); break;
                case "duplicate-registration": Duplicate(data, "registrations"); break;
                case "duplicate-layer": Duplicate(data, "layers"); break;
                case "duplicate-representation": Duplicate((JObject)layer, "representations"); break;
                case "dangling-frame": op["targetFrameId"] = "absent"; break;
                case "dangling-asset": layer["sourceAssetIds"][0] = "absent"; break;
                case "dangling-solution": layer["activeRegistrationId"] = "absent"; break;
                case "dangling-history": data["registrations"][0]["supersedes"] = "absent"; break;
                case "cycle": data["registrations"][0]["supersedes"] = "declared-solution"; break;
                case "wrong-registration-endpoint": op["sourceFrameId"] = "local-study"; break;
                case "wrong-representation-endpoint": layer["representations"][0]["toSourceOperationId"] = "declared-placement"; break;
                case "same-frame-offset":
                    op["sourceFrameId"] = "local-study"; layer["sourceFrameId"] = "local-study"; break;
                case "singular": op["payload"]["linear"] = new JArray(new double[9]); break;
                case "ill-conditioned": op["payload"]["linear"] = new JArray(1, 0, 0, 0, 1, 0, 0, 0, 1e-18); break;
                case "string-coordinate": op["payload"]["translation"][0] = "10"; break;
                case "negative-unit": frame["cartesian"]["metresPerUnit"] = -1; break;
                case "unknown-evidence-status": op["evidence"]["statements"][0]["status"] = "trusted"; break;
                case "duplicate-origin": op["evidence"]["origins"] = new JArray("measured", "measured"); break;
                case "numeric-name": layer["name"] = 5; break;
                case "float-operation-version": op["version"] = 1.0; break;
            }
            Assert.Catch<JsonException>(() => SpatialFixtures.ReadScene(data));
        }

        private static void Duplicate(JObject data, string field) => ((JArray)data[field]).Add(data[field][0].DeepClone());

        [TestCase("duplicate", "{\"x\":1,\"x\":2}")]
        [TestCase("NaN", "{\"x\":NaN}")]
        [TestCase("Infinity", "{\"x\":Infinity}")]
        [TestCase("overflow", "{\"x\":1e999}")]
        [TestCase("comment", "{/* comment */\"x\":1}")]
        [TestCase("single-quote", "{'x':1}")]
        [TestCase("unquoted-keyword", "{true:1}")]
        [TestCase("trailing-comma", "{\"x\":1,}")]
        [TestCase("hex", "{\"x\":0x10}")]
        [TestCase("leading-zero", "{\"x\":01}")]
        [TestCase("raw-newline", "{\"x\":\"a\nb\"}")]
        public void NonJsonMetadataCannotHideInsideOtherwiseValidScene(string name, string metadata)
        {
            JObject data = SpatialFixtures.Affine(); data["metadata"] = "REPLACE_METADATA";
            string json = data.ToString(Formatting.None).Replace("\"REPLACE_METADATA\"", metadata);
            Assert.Catch<JsonException>(() => SceneDocument.Read(json), name);
        }

        [TestCase("../asset.glb")]
        [TestCase("/Users/someone/model.glb")]
        [TestCase("~/model.glb")]
        [TestCase("models/%2e%2e/model.glb")]
        [TestCase("models\\model.glb")]
        [TestCase("https://user:secret@example.org/model.glb")]
        [TestCase("https://example.org/model.glb?sig=private")]
        [TestCase("file:///model.glb")]
        public void PortableLocatorsRejectPrivateOrAmbiguousAccess(string locator)
        {
            JObject data = SpatialFixtures.Affine(); data["assets"][0]["locators"] = new JArray(locator);
            Assert.Catch<JsonException>(() => SpatialFixtures.ReadScene(data));
        }

        [Test]
        public void OptionalExtensionsAndUnresolvedAssetsRemainInert()
        {
            JObject data = SpatialFixtures.Affine();
            data["extensions"] = new JObject { ["example:optional"] = new JObject
            { ["version"] = 1, ["required"] = false, ["payload"] = new JArray("inert", null, true) } };
            data["assets"][0]["locators"] = new JArray("models/model.glb", "https://example.invalid/model.glb");
            SceneDocument scene = SpatialFixtures.ReadScene(data);
            Assert.That(scene.TryGetAffineOperation("declared-placement", out _, out _), Is.True);
            JObject copy = scene.ToJObject(); copy["frames"][0]["body"] = "changed copy";
            Assert.That((string)scene.ToJObject()["frames"][0]["body"], Is.Null);
        }

        [Test]
        public void NullMappingsNeverSilentlyRegisterLayers()
        {
            JObject data = SpatialFixtures.Affine();
            data["layers"][0]["activeRegistrationId"] = null;
            data["layers"][0]["sourceFrameId"] = "local-study";
            SceneDocument scene = SpatialFixtures.ReadScene(data);
            Assert.Throws<InvalidOperationException>(() => scene.SourceToScene("model-layer", default));
            Assert.Throws<InvalidOperationException>(() => scene.RepresentationToSource("model-layer", "model-representation", default));
            data["layers"][0]["representations"][0]["frameId"] = "local-study";
            SpatialFixtures.Equal(SpatialFixtures.ReadScene(data).RepresentationToSource("model-layer", "model-representation", new Vector3d(2, 3, 4)), 2, 3, 4);
        }

        [Test]
        public void RepresentationBasisIsAppliedExactlyOnceBeforeRegistration()
        {
            JObject data = SpatialFixtures.Affine();
            JObject frame = (JObject)data["frames"][1].DeepClone(); frame["id"] = "representation-frame";
            ((JArray)data["frames"]).Add(frame);
            JObject op = (JObject)data["operations"][0].DeepClone();
            op["id"] = "basis-map"; op["sourceFrameId"] = "representation-frame"; op["targetFrameId"] = "original-model";
            op["payload"] = new JObject { ["linear"] = new JArray(-1, 0, 0, 0, 1, 0, 0, 0, 1), ["translation"] = new JArray(0, 0, 0) };
            ((JArray)data["operations"]).Add(op);
            data["layers"][0]["representations"][0]["frameId"] = "representation-frame";
            data["layers"][0]["representations"][0]["toSourceOperationId"] = "basis-map";
            SceneDocument scene = SpatialFixtures.ReadScene(data);
            Vector3d source = scene.RepresentationToSource("model-layer", "model-representation", new Vector3d(-2, 3, 4));
            SpatialFixtures.Equal(scene.SourceToScene("model-layer", source), 14, 26, 46);
            SpatialFixtures.Equal(scene.RepresentationToSource("model-layer", "model-representation", source, true), -2, 3, 4);
        }

        [TestCase("metadata")]
        [TestCase("frame")]
        [TestCase("operation")]
        [TestCase("registration")]
        public void ScientificRevisionsProtectHistoricalDefinitions(string kind)
        {
            JObject data = SpatialFixtures.Affine(); SceneDocument previous = SpatialFixtures.ReadScene(data);
            if (kind == "metadata") data["metadata"]["edited"] = true;
            if (kind == "frame") data["frames"][0]["body"] = "Moon";
            if (kind == "operation") data["operations"][0]["payload"]["translation"][0] = 20;
            if (kind == "registration") data["registrations"][0]["evidence"]["origins"] = new JArray("measured");
            Assert.Catch<JsonException>(() => SpatialFixtures.ReadScene(data).ValidateRevisionOf(previous));
            data["revision"] = "authored-next-revision";
            if (kind == "metadata") Assert.DoesNotThrow(() => SpatialFixtures.ReadScene(data).ValidateRevisionOf(previous));
            else Assert.Catch<JsonException>(() => SpatialFixtures.ReadScene(data).ValidateRevisionOf(previous));
        }
        [TestCase("supplied")]
        [TestCase("derived")]
        [TestCase("independently-checked")]
        [TestCase("unknown")]
        public void EvidenceStatusRemainsDistinctAndUninterpreted(string status)
        {
            JObject data = SpatialFixtures.Affine();
            JToken statement = data["operations"][0]["evidence"]["statements"][0];
            statement["status"] = status; statement["category"] = "fit-residual";
            statement["details"] = new JObject { ["passed"] = true, ["value"] = 0 };
            SceneDocument scene = SpatialFixtures.ReadScene(data);
            Assert.That((string)scene.ToJObject()["operations"][0]["evidence"]["statements"][0]["status"], Is.EqualTo(status));
            Assert.That(JToken.DeepEquals(scene.ToJObject()["operations"][0]["evidence"], data["operations"][0]["evidence"]), Is.True);
        }

        [Test]
        public void NewCommonFrameUsesNewDefinitionsWithoutRewritingHistory()
        {
            JObject data = SpatialFixtures.Affine(); SceneDocument prior = SpatialFixtures.ReadScene(data);
            JObject frame = (JObject)data["frames"][0].DeepClone(); frame["id"] = "next-common-frame";
            ((JArray)data["frames"]).Add(frame); data["sceneFrameId"] = "next-common-frame";
            JObject op = (JObject)data["operations"][0].DeepClone();
            op["id"] = "next-operation"; op["targetFrameId"] = "next-common-frame";
            ((JArray)data["operations"]).Add(op);
            JObject registration = (JObject)data["registrations"][0].DeepClone();
            registration["id"] = "next-solution"; registration["operationId"] = "next-operation";
            registration["supersedes"] = "declared-solution";
            ((JArray)data["registrations"]).Add(registration);
            data["layers"][0]["activeRegistrationId"] = "next-solution"; data["revision"] = "next-revision";
            SceneDocument scene = SpatialFixtures.ReadScene(data); scene.ValidateRevisionOf(prior);
            SpatialFixtures.Equal(scene.SourceToScene("model-layer", new Vector3d(2, 3, 4)), 14, 26, 46);
            data["registrations"][0]["supersedes"] = "next-solution";
            Assert.Catch<JsonException>(() => SpatialFixtures.ReadScene(data));
        }
    }
}

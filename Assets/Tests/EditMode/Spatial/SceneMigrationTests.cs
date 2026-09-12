using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using GeoX.Spatial;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace GeoX.Spatial.Tests
{
    public class SceneMigrationTests
    {
        private static SceneState Legacy() => SceneJson.Read(SpatialFixtures.Read("schema1.json"));
        private static SceneMigrationResult Migrate(SceneState old) => SceneMigration.FromSchema1(SceneJson.Write(old), "explicit-migration-revision");

        [Test]
        public void MigrationKeepsScienceAndSeparatesViewSessionAndPrivateAccess()
        {
            SceneState old = Legacy(); string before = SceneJson.Write(old);
            SceneMigrationResult result = Migrate(old); JObject science = result.Scene.ToJObject();
            Assert.That(result.Scene.Id, Is.EqualTo(old.Id));
            Assert.That(result.Scene.Revision, Is.EqualTo("explicit-migration-revision"));
            Assert.That(JToken.DeepEquals(science["metadata"], old.Metadata), Is.True);
            Assert.That(science["Display"], Is.Null); Assert.That(science["display"], Is.Null);
            Assert.That(result.Scene.ToJson(), Does.Not.Contain("private-fixture-key"));
            Assert.That(science["layers"][0]["representations"].Count(), Is.Zero);
            Assert.That(science["layers"][0]["evidence"]["statements"][0]["status"].Value<string>(), Is.EqualTo("unknown"));
            Assert.That(JToken.DeepEquals(science["registrations"][0]["evidence"]["statements"][0]["details"]["ValidationEvidence"],
                old.Layers[0].Registration.ValidationEvidence), Is.True);
            Assert.That(science["operations"][0]["evidence"]["statements"][0]["status"].Value<string>(), Is.EqualTo("supplied"));
            Assert.That(JToken.DeepEquals(science["frames"][0]["definition"]["value"]["schema1"]["definition"], old.Frame.Definition), Is.True);
            Assert.That((bool)result.View["layers"][1]["visible"], Is.False);
            Assert.That(result.PrivateAccess["assets"].Count(), Is.EqualTo(2));
            Assert.That((string)science["assets"][0]["metadata"]["geox:schema1"], Is.EqualTo("existing user metadata"));
            Assert.That((string)science["assets"][0]["metadata"]["geox:schema1:legacy"]["ContentHash"], Is.EqualTo("unknown-algorithm-value"));
            Assert.That(science["assets"][0]["contentHash"].Type, Is.EqualTo(JTokenType.Null));
            Assert.That(SceneJson.Write(old), Is.EqualTo(before));
            Assert.That(Migrate(old).Scene.ToJson(), Is.EqualTo(result.Scene.ToJson()));
            Assert.That(Migrate(old).Report.ToString(), Is.EqualTo(result.Report.ToString()));
            JObject copy = result.View; copy["layers"][0]["visible"] = false;
            Assert.That((bool)result.View["layers"][0]["visible"], Is.True);
        }

        [TestCase("affine")]
        [TestCase("unknown-units")]
        [TestCase("large-origin")]
        public void MigrationPreservesCoordinateAndDisplayResults(string fixture)
        {
            SceneState old = Legacy(); Vector3d point = new Vector3d(2, 3, 4);
            if (fixture == "unknown-units")
            {
                old.Frame.MetresPerUnit = null;
                foreach (LayerState layer in old.Layers)
                {
                    layer.SourceFrame = old.Frame;
                    layer.Registration.SourceFrameId = old.Frame.Id;
                    layer.Registration.SourceToScene = AffineMap.Identity;
                }
            }
            if (fixture == "large-origin")
            {
                double origin = Math.Pow(2, 40);
                old.Frame.MetresPerUnit = 1;
                old.Display = new DisplayState { Origin = new Vector3d(origin, 0, 0) };
                foreach (LayerState layer in old.Layers) layer.Registration.SourceToScene = AffineMap.Identity;
                point = new Vector3d(origin + 0.125, 0, 0);
            }
            SceneMigrationResult result = Migrate(old);
            foreach (LayerState layer in old.Layers)
            {
                Vector3d expected = layer.Registration.SourceToScene.Apply(point);
                SpatialFixtures.Equal(result.Scene.SourceToScene(layer.Id, point), expected.X, expected.Y, expected.Z);
                expected = old.SourceToDisplay(layer.Id, point);
                SpatialFixtures.Equal(SceneSidecars.LegacySourceToDisplay(result.Scene, result.Session, layer.Id, point), expected.X, expected.Y, expected.Z);
                expected = old.Reset().SourceToDisplay(layer.Id, point);
                SpatialFixtures.Equal(SceneSidecars.LegacySourceToDisplay(result.Scene, result.Session, layer.Id, point, true), expected.X, expected.Y, expected.Z);
            }
            if (fixture == "large-origin")
                Assert.That((float)SceneSidecars.LegacySourceToDisplay(result.Scene, result.Session, "layer-0", point).X, Is.EqualTo(0.125f));
        }

        [Test]
        public void UnregisteredUnknownFrameKeepsEveryOriginalField()
        {
            SceneState old = Legacy(); old.Frame.Kind = CoordinateKind.Unknown;
            old.Frame.Body = null; old.Frame.AxisConvention = null; old.Frame.MetresPerUnit = 2;
            foreach (LayerState layer in old.Layers) layer.Registration = null;
            SceneMigrationResult result = Migrate(old);
            JToken frame = result.Scene.ToJObject()["frames"][0];
            Assert.That(frame["body"].Type, Is.EqualTo(JTokenType.Null));
            Assert.That(frame["cartesian"].Type, Is.EqualTo(JTokenType.Null));
            Assert.That((double)frame["definition"]["value"]["schema1"]["metresPerUnit"], Is.EqualTo(2));
            Assert.That(result.Scene.ToJObject()["layers"][0]["activeRegistrationId"].Type, Is.EqualTo(JTokenType.Null));
            Assert.Throws<InvalidOperationException>(() => result.Scene.SourceToScene("layer-0", default));
        }

        [Test]
        public void ConflictingInlineFramesAreRejectedEvenWithoutRegistration()
        {
            SceneState old = Legacy(); old.Layers[0].Registration = null;
            old.Layers[0].SourceFrame.Id = old.Frame.Id;
            old.Layers[0].SourceFrame.Definition["different"] = true;
            Assert.Catch<JsonException>(() => Migrate(old));
        }

        [Test]
        public void EqualAssetsDeduplicateButConflictsGetDeterministicCollisionCheckedIds()
        {
            SceneState old = Legacy();
            old.Layers[1].Assets[0] = old.Layers[0].Assets[0];
            Assert.That(Migrate(old).Scene.ToJObject()["assets"].Count(), Is.EqualTo(1));
            old = Legacy();
            string path = "/Layers/1/Assets/0";
            string input = new JArray(old.Id, path).ToString(Formatting.None);
            string digest;
            using (SHA256 sha = SHA256.Create())
                digest = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(input))).Replace("-", "").ToLowerInvariant();
            string reserved = "schema1:asset:" + digest;
            old.Layers[0].Assets.Add(new SourceAsset { Id = reserved, AccessReferenceId = "another-private-key" });
            JObject science = Migrate(old).Scene.ToJObject();
            Assert.That((string)science["layers"][1]["sourceAssetIds"][0], Is.EqualTo(reserved + ":collision"));
            Assert.That(science["assets"].Select(a => (string)a["id"]).Distinct().Count(), Is.EqualTo(3));
        }

        [Test]
        public void BlankLegacyLabelsRemainInspectableWithoutBecomingKnownIdentifiers()
        {
            SceneState old = Legacy(); old.Frame.Body = " ";
            foreach (LayerState layer in old.Layers) layer.SourceFrame.Body = " ";
            old.Layers[0].Assets[0].Format = "";
            JObject science = Migrate(old).Scene.ToJObject();
            Assert.That((string)science["frames"][0]["body"], Is.Null);
            Assert.That((string)science["frames"][0]["definition"]["value"]["schema1"]["body"], Is.EqualTo(" "));
            Assert.That((string)science["assets"][0]["metadata"]["geox:schema1:legacy"]["Format"], Is.EqualTo(""));
        }

        [Test]
        public void MigrationIsExplicitAndCannotOverwriteOriginalFiles()
        {
            string source = SpatialFixtures.Read("schema1.json");
            Assert.Catch<JsonException>(() => SceneDocument.Read(source));
            Assert.Catch<JsonException>(() => SceneMigration.FromSchema1(SpatialFixtures.Read("affine.json"), "revision"));
            SceneMigrationResult result = SceneMigration.FromSchema1(source, "explicit-revision");
            string parent = Path.Combine(Path.GetTempPath(), "geox-migration-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(parent);
            try
            {
                string original = Path.Combine(parent, "original.json"); File.WriteAllText(original, source);
                Assert.Throws<IOException>(() => result.WriteNewDirectory(original));
                string target = Path.Combine(parent, "migrated"); result.WriteNewDirectory(target);
                Assert.That(File.ReadAllText(original), Is.EqualTo(source));
                Assert.That(SceneDocument.Read(File.ReadAllText(Path.Combine(target, "scene.json"))).ToJson(), Is.EqualTo(result.Scene.ToJson()));
                Assert.Throws<IOException>(() => result.WriteNewDirectory(target));
                Assert.That(Directory.GetFiles(target).Length, Is.EqualTo(5));
            }
            finally { Directory.Delete(parent, true); }
        }

        [Test]
        public void ViewEditsHaveNoEffectOnScientificRevisionOrRegistration()
        {
            SceneMigrationResult migrated = Migrate(Legacy()); string science = migrated.Scene.ToJson();
            JObject view = migrated.View; view["revision"] = "view-only-revision"; view["layers"][0]["visible"] = false;
            SceneSidecars.ReadView(view.ToString(), migrated.Scene);
            Assert.That(migrated.Scene.ToJson(), Is.EqualTo(science));
            view["layers"][0]["selectedRepresentationId"] = "absent";
            Assert.Catch<JsonException>(() => SceneSidecars.ReadView(view.ToString(), migrated.Scene));
            JObject access = migrated.PrivateAccess; access["assets"]["absent"] = new JObject { ["resolverKeys"] = new JArray("private") };
            Assert.Catch<JsonException>(() => SceneSidecars.ReadPrivateAccess(access.ToString(), migrated.Scene));
            JObject session = migrated.Session; session["display"]["Scale"] = "2";
            Assert.Catch<JsonException>(() => SceneSidecars.ReadLegacyUnitySession(session.ToString(), migrated.Scene));
        }
    }
}

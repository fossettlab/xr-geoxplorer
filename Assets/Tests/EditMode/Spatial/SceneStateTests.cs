using System;
using GeoX.Spatial;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace GeoX.Spatial.Tests
{
    public class SceneStateTests
    {
        private static ReferenceFrame Frame(string body = "Mars", string id = "authored-local-frame") => new ReferenceFrame
        {
            Id = id, Body = body, Kind = CoordinateKind.Cartesian,
            AxisConvention = "X-right,Y-forward,Z-up", MetresPerUnit = 1,
            Definition = new JObject { ["source"] = "synthetic test fixture" }
        };

        private static AffineMap Translate(double x, double y, double z) =>
            new AffineMap(AffineMap.Identity.Linear, new Vector3d(x, y, z));

        private static LayerState Layer(string id, AffineMap mapping, string body) => new LayerState
        {
            Id = id, Name = id, SourceFrame = Frame(body, id + "-source-frame"),
            Registration = new Registration
            {
                SourceFrameId = id + "-source-frame", TargetFrameId = "authored-local-frame",
                SourceToScene = mapping,
                Provenance = new JObject { ["method"] = "supplied fixture transform" }
            },
            Assets = { new SourceAsset { Id = id + "-asset", AccessReferenceId = id + "-resolver-key" } },
            Metadata = new JObject { ["acquired"] = "2026-09-06T00:00:00Z", ["extension"] = new JArray(1, "unknown") }
        };

        private static SceneState Scene(string body = "Mars") => new SceneState
        {
            Id = "authored-scene", Frame = Frame(body),
            Layers = { Layer("a", AffineMap.Identity, body), Layer("b", Translate(-10, -20, -30), body) }
        };

        private static void Equal(Vector3d actual, double x, double y, double z)
        {
            Assert.That(actual.X, Is.EqualTo(x));
            Assert.That(actual.Y, Is.EqualTo(y));
            Assert.That(actual.Z, Is.EqualTo(z));
        }

        [TestCase(1, false)]
        [TestCase(2, false)]
        [TestCase(1, true)]
        [TestCase(2, true)]
        public void RegisteredLayersRemainCoincidentUnderDisplayChanges(double scale, bool rotate)
        {
            SceneState source = Scene();
            string original = SceneJson.Write(source);
            double[] rotation = rotate ? new double[] { 0, -1, 0, 1, 0, 0, 0, 0, 1 } : AffineMap.Identity.Linear;
            SceneState moved = source.WithDisplay(new DisplayState
            {
                Scale = scale, Pose = new AffineMap(rotation, new Vector3d(10, 20, 30))
            });
            Vector3d point = moved.SourceToDisplay("a", new Vector3d(2, 3, 4));
            Equal(moved.SourceToDisplay("b", new Vector3d(12, 23, 34)), point.X, point.Y, point.Z);
            Equal(point, 10 + scale * (rotate ? -3 : 2), 20 + scale * (rotate ? 2 : 3), 30 + scale * 4);
            Equal(moved.DisplayToSource("b", point), 12, 23, 34);
            Assert.That(SceneJson.Write(source), Is.EqualTo(original));
            Assert.That(SceneJson.Write(moved.Reset()), Is.EqualTo(original));
            moved.Layers[0].Metadata["extension"] = "changed copy";
            Assert.That(SceneJson.Write(source), Is.EqualTo(original));
        }

        [Test]
        public void RepresentationBasisUnitsAndDisplayComposeOnce()
        {
            SceneState scene = Scene();
            scene.Frame.MetresPerUnit = 2; // Fictitious authored unit, not a real unit definition.
            foreach (LayerState layer in scene.Layers) layer.SourceFrame.MetresPerUnit = 2;
            scene.Layers[0].Registration.SourceToScene = Translate(1, 0, 0);
            var importedToSource = new AffineMap(new double[] { -1, 0, 0, 0, 1, 0, 0, 0, 1 }, default);
            scene.Display = new DisplayState
            {
                Origin = new Vector3d(1, 1, 1), Scale = 2,
                AxisBridge = new AffineMap(new double[] { 1, 0, 0, 0, 0, 1, 0, 1, 0 }, default),
                Pose = Translate(10, 20, 30)
            };
            Vector3d source = importedToSource.Apply(new Vector3d(-2, 3, 4));
            Vector3d display = scene.SourceToDisplay("a", source);
            Equal(display, 18, 32, 38);
            Equal(importedToSource.Inverse().Apply(scene.DisplayToSource("a", display)), -2, 3, 4);
        }

        [TestCase("Earth")]
        [TestCase("Moon")]
        [TestCase("Mars")]
        [TestCase(null)]
        public void PersistenceKeepsBodyUnknownMetadataAndUnloadedLayerIdentity(string body)
        {
            SceneState scene = Scene(body);
            scene.Layers[0].Visible = false;
            scene.Layers[0].Registration.ValidationEvidence = new JObject { ["supplied"] = "fixture only" };
            string json = SceneJson.Write(scene);
            SceneState restored = SceneJson.Read(json);
            Assert.That(SceneJson.Write(restored), Is.EqualTo(json));
            Assert.That(restored.Frame.Body, Is.EqualTo(body));
            Assert.That(restored.Layers[0].Id, Is.EqualTo("a"));
            Assert.That(restored.Layers[0].Visible, Is.False);
            Assert.That(restored.Layers[0].Metadata["acquired"].Type, Is.EqualTo(JTokenType.String));
            Assert.That(restored.Layers[0].Assets[0].AccessReferenceId, Is.EqualTo("a-resolver-key"));
            // No renderer or resolved source is needed to retain this layer.
            Assert.That(restored.Layers.Count, Is.EqualTo(2));
        }

        [Test]
        public void UnknownUnregisteredInputPersistsWithoutAnInventedMetreOrBody()
        {
            SceneState scene = Scene(null);
            scene.Frame.Kind = CoordinateKind.Unknown;
            scene.Frame.AxisConvention = null;
            scene.Frame.MetresPerUnit = null;
            foreach (LayerState layer in scene.Layers) layer.Registration = null;
            SceneState restored = SceneJson.Read(SceneJson.Write(scene));
            Assert.That(restored.Frame.MetresPerUnit, Is.Null);
            Assert.That(restored.Layers[0].Registration, Is.Null);
            Assert.Throws<ArgumentException>(() => restored.SourceToDisplay("a", default));
        }

        [TestCase("body")]
        [TestCase("definition")]
        [TestCase("units")]
        [TestCase("target")]
        [TestCase("geographic")]
        [TestCase("duplicate")]
        [TestCase("same-frame-offset")]
        public void InvalidRegistrationIsNotSilentlyAccepted(string fault)
        {
            SceneState scene = Scene();
            switch (fault)
            {
                case "body": scene.Layers[0].SourceFrame.Body = "Moon"; break;
                case "definition":
                    scene.Layers[0].SourceFrame.Id = scene.Frame.Id;
                    scene.Layers[0].Registration.SourceFrameId = scene.Frame.Id;
                    scene.Layers[0].SourceFrame.Definition["epoch"] = "different";
                    break;
                case "units": scene.Layers[0].SourceFrame.MetresPerUnit = null; break;
                case "target": scene.Layers[0].Registration.TargetFrameId = "other"; break;
                case "geographic": scene.Layers[0].SourceFrame.Kind = CoordinateKind.Geographic; break;
                case "duplicate": scene.Layers[1].Id = "a"; break;
                case "same-frame-offset":
                    scene.Layers[1].SourceFrame.Id = scene.Frame.Id;
                    scene.Layers[1].Registration.SourceFrameId = scene.Frame.Id;
                    break;
            }
            Assert.Throws<ArgumentException>(() => SceneJson.Write(scene));
        }

        [Test]
        public void ShearReflectionAndNonuniformScaleArePreserved()
        {
            var map = new AffineMap(new double[] { -1, 2, 0, 0, 2, 0, 0, 0, 4 }, new Vector3d(10, 20, 30));
            Equal(map.Apply(new Vector3d(2, 3, 4)), 14, 26, 46);
            Equal(map.Inverse().Apply(new Vector3d(14, 26, 46)), 2, 3, 4);
            double[] copy = map.Linear;
            copy[0] = 999;
            Equal(map.Apply(new Vector3d(2, 3, 4)), 14, 26, 46);
            Assert.Throws<ArgumentException>(() => new DisplayState { Pose = map }.Validate());
        }

        [Test]
        public void InvalidNumericsFailExplicitly()
        {
            Assert.Throws<ArgumentException>(() => new Vector3d(double.NaN, 0, 0));
            Assert.Throws<ArgumentException>(() => new AffineMap(new double[9], default));
            Assert.Throws<ArgumentException>(() => new AffineMap(new double[] { 1, 0, 0, 0, 1, 0, 0, 0, 1e-18 }, default));
            Assert.Throws<ArgumentException>(() => new DisplayState { Scale = 0 }.Validate());
        }

        [Test]
        public void LargeOriginIsSubtractedBeforeFloatConversion()
        {
            SceneState scene = Scene();
            double origin = Math.Pow(2, 40);
            double offset = 0.125; // 2^-3: binary-exact; lost in float at 2^40.
            scene.Display.Origin = new Vector3d(origin, 0, 0);
            Vector3d display = scene.SourceToDisplay("a", new Vector3d(origin + offset, 0, 0));
            Assert.That((float)display.X, Is.EqualTo((float)offset));
            Assert.That((double)(float)(origin + offset) - origin, Is.Zero);
        }

        [TestCase("version")]
        [TestCase("missing")]
        [TestCase("coordinate")]
        [TestCase("extra")]
        [TestCase("string-number")]
        [TestCase("numeric-name")]
        [TestCase("string-bool")]
        public void MalformedDocumentsDoNotAcquireDefaultMeaning(string fault)
        {
            JObject document = JObject.Parse(SceneJson.Write(Scene()));
            switch (fault)
            {
                case "version": document["SchemaVersion"] = 999; break;
                case "missing": document.Remove("SchemaVersion"); break;
                case "coordinate": ((JObject)document["Display"]["Origin"]).Remove("X"); break;
                case "extra": document["UnexpectedSemanticField"] = true; break;
                case "string-number": document["Display"]["Scale"] = "2"; break;
                case "numeric-name": document["Id"] = 42; break;
                case "string-bool": document["Layers"][0]["Visible"] = "true"; break;
            }
            Assert.Throws<JsonSerializationException>(() => SceneJson.Read(document.ToString()));
        }

        [Test]
        public void DuplicatePropertiesAndTrailingContentFail()
        {
            string json = SceneJson.Write(Scene());
            Assert.Throws<JsonReaderException>(() => SceneJson.Read(json.Replace("\"SchemaVersion\":1", "\"SchemaVersion\":1,\"SchemaVersion\":1")));
            Assert.Throws<JsonReaderException>(() => SceneJson.Read(json + "{}"));
        }
    }
}

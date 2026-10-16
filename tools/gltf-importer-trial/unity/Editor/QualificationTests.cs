using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GLTFast;
using GLTFast.Loading;
using GLTFast.Logging;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace GeoX.GltfTrial
{
    public sealed class QualificationTests
    {
        // Arithmetic tolerance for synthetic float transforms, not scientific uncertainty.
        private const float FloatTolerance = 0.00001f;

        private sealed class DenyRequests : IDownloadProvider
        {
            public Task<IDownload> Request(Uri uri) => throw new InvalidOperationException("Unexpected companion request");
            public Task<ITextureDownload> RequestTexture(Uri uri, bool nonReadable) =>
                throw new InvalidOperationException("Unexpected companion texture request");
        }

        private sealed class CancelAtCheckpoint : IDeferAgent
        {
            public CancellationTokenSource Source;
            public int Hits;
            public bool ShouldDefer() => false;
            public bool ShouldDefer(float duration) => false;
            public Task BreakPoint(float duration) => BreakPoint();
            public async Task BreakPoint()
            {
                if (Source == null || Source.IsCancellationRequested) return;
                Hits++;
                await Task.Yield();
                Source.Cancel(); // Importer must observe its own token; this agent never throws.
            }
        }

        [UnityTest]
        public IEnumerator AuthoredGeometryHierarchyAndMaterialFidelity()
        {
            foreach (string name in new[] { "triangle.glb", "hierarchy.glb", "textured.glb" })
            {
                var task = Probe(name, null, true);
                while (!task.IsCompleted) yield return null;
                task.GetAwaiter().GetResult();
            }
        }

        [UnityTest]
        public IEnumerator CancelDuringLoadThenRetry()
        {
            var task = Probe("textured.glb", "load", false);
            while (!task.IsCompleted) yield return null;
            task.GetAwaiter().GetResult();
            task = Probe("textured.glb", null, true);
            while (!task.IsCompleted) yield return null;
            task.GetAwaiter().GetResult();
        }

        [UnityTest]
        public IEnumerator CancelDuringHierarchyCreationThenRetry()
        {
            var task = Probe("hierarchy.glb", "instantiate", false);
            while (!task.IsCompleted) yield return null;
            task.GetAwaiter().GetResult();
            task = Probe("hierarchy.glb", null, true);
            while (!task.IsCompleted) yield return null;
            task.GetAwaiter().GetResult();
        }

        private static async Task Probe(string name, string cancelStage, bool inspect)
        {
            var logger = new CollectingLogger();
            var defer = new CancelAtCheckpoint();
            using var cancellation = new CancellationTokenSource();
            var importer = new GltfImport(downloadProvider: new DenyRequests(), deferAgent: defer, logger: logger);
            GameObject root = null;
            var resources = new List<Object>();
            bool cancelled = false;
            try
            {
                if (cancelStage == "load") defer.Source = cancellation;
                var bytes = File.ReadAllBytes(Path.Combine(Application.streamingAssetsPath, "Fixtures", name + ".bytes"));
                Assert.That(await importer.Load(bytes, new Uri("https://fixture.invalid/model.glb"),
                    new ImportSettings { TexturesReadable = true }, cancellation.Token), Is.True);
                CaptureResources(importer, resources);
                if (cancelStage == "instantiate") defer.Source = cancellation;
                root = new GameObject("Qualification instance");
                Assert.That(await importer.InstantiateMainSceneAsync(root.transform, cancellation.Token), Is.True);
                if (inspect) AssertFidelity(name, root);
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
                Assert.That(cancelStage, Is.Not.Null, "Unexpected cancellation");
                if (cancelStage == "instantiate")
                    Assert.That(root.GetComponentsInChildren<Transform>(true).Length, Is.GreaterThan(1),
                        "Cancellation must occur after hierarchy creation started");
            }
            finally
            {
                CaptureResources(importer, resources);
                var nodes = root == null ? Array.Empty<Transform>() : root.GetComponentsInChildren<Transform>(true);
                // Cancellation may already have disposed some tracked objects.
                var aliveBeforeInstanceDestruction = resources.Select(x => x != null).ToArray();
                if (root != null) Object.DestroyImmediate(root);
                bool instancesGone = nodes.All(x => x == null);
                bool resourcesUnchanged = aliveBeforeInstanceDestruction.SequenceEqual(resources.Select(x => x != null));
                importer.Dispose();
                Assert.That(instancesGone, Is.True, "Instances must be destroyed before resource disposal");
                Assert.That(resourcesUnchanged, Is.True, "Instance destruction must not change shared import resources");
                Assert.That(resources.All(x => x == null), Is.True, "Tracked import resources survived Dispose");
                if (cancelStage != "load") Assert.That(resources.Count, Is.GreaterThan(0), "Non-vacuous ownership check");
            }
            Assert.That(cancelled, Is.EqualTo(cancelStage != null));
            if (cancelStage != null) Assert.That(defer.Hits, Is.EqualTo(1), "Cancelled at an actual defer checkpoint");
            Assert.That(logger.Items?.Any(x => x.Type == LogType.Error || x.Type == LogType.Exception) ?? false,
                Is.False, "Unexpected importer diagnostics");
        }

        private static void CaptureResources(GltfImport importer, List<Object> resources)
        {
            if (importer.Meshes != null) resources.AddRange(importer.Meshes.Where(x => !resources.Contains(x)));
            for (int i = 0; i < importer.MaterialCount; i++)
                if (!resources.Contains(importer.GetMaterial(i))) resources.Add(importer.GetMaterial(i));
            for (int i = 0; i < importer.TextureCount; i++)
                if (!resources.Contains(importer.GetTexture(i))) resources.Add(importer.GetTexture(i));
        }

        private static void AssertFidelity(string name, GameObject root)
        {
            // Authored values: scripts/generate_model_input_fixtures.py. glTFast reflects X
            // flips UV Y and reverses winding for Unity. This is representation mapping, not a CRS.
            var filter = root.GetComponentsInChildren<MeshFilter>().Single();
            var mesh = filter.sharedMesh;
            var expected = new[] { Vector3.zero, Vector3.left, Vector3.up };
            Assert.That(mesh.vertexCount, Is.EqualTo(expected.Length));
            for (int i = 0; i < expected.Length; i++)
            {
                Near(mesh.vertices[i], expected[i]);
                Near(mesh.normals[i], Vector3.forward);
            }
            Near(mesh.bounds.min, Vector3.left);
            Near(mesh.bounds.max, Vector3.up);
            var indices = mesh.triangles;
            Assert.That(indices.Length, Is.EqualTo(3));
            Assert.That(Vector3.Dot(Vector3.Cross(mesh.vertices[indices[1]] - mesh.vertices[indices[0]],
                mesh.vertices[indices[2]] - mesh.vertices[indices[0]]), Vector3.forward), Is.GreaterThan(0));
            CollectionAssert.AreEqual(new[] { Vector2.up, Vector2.one, Vector2.zero }, mesh.uv);
            if (name == "hierarchy.glb")
            {
                Near(filter.transform.TransformPoint(expected[0]), new Vector3(-5, 2, -3));
                Near(filter.transform.TransformPoint(expected[1]), new Vector3(-4, 2, -3));
                Near(filter.transform.TransformPoint(expected[2]), new Vector3(-5, 0, -3));
                Near(filter.transform.localScale, new Vector3(1, 2, 3));
            }
            var material = filter.GetComponent<Renderer>().sharedMaterial;
            Assert.That(material.shader.name, Is.EqualTo("glTF/PbrMetallicRoughness"));
            Assert.That(material.GetFloat("metallicFactor"), Is.EqualTo(0));
            Assert.That(material.GetFloat("roughnessFactor"), Is.EqualTo(1));
            Assert.That(material.GetFloat("_CullMode"), Is.EqualTo(0));
            if (name == "textured.glb")
            {
                var texture = material.GetTexture("baseColorTexture") as Texture2D;
                Assert.That(texture, Is.Not.Null);
                Assert.That(texture.width, Is.EqualTo(1));
                Assert.That(texture.height, Is.EqualTo(1));
                Assert.That(texture.GetPixels32().Single(), Is.EqualTo(new Color32(64, 128, 192, 255)));
            }
        }

        private static void Near(Vector3 actual, Vector3 expected) =>
            Assert.That(Vector3.Distance(actual, expected), Is.LessThan(FloatTolerance), $"{actual} != {expected}");
    }
}

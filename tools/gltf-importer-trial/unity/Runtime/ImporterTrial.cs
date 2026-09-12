using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GLTFast;
using GLTFast.Loading;
using GLTFast.Logging;
using UnityEngine;

namespace GeoX.GltfTrial
{
    // Curated importer qualification; not a production model-opening adapter.
    public static class ImporterTrial
    {
        [Serializable]
        public sealed class Result
        {
            public string fixture;
            public string caseId;
            public string stage;
            public int errorCount;
            public string cleanupException;
            public bool loaded;
            public bool instantiated;
            public bool cancelled;
            public int deniedRequests;
            public int meshes;
            public int vertices;
            public string[] diagnostics;
            public string[] shaders;
            public string exception;
            public bool passed;
        }

        private sealed class DeniedDownload : ITextureDownload
        {
            public bool Success => false;
            public string Error => "Companion fetching is disabled in this trial";
            public byte[] Data => null;
            public string Text => null;
            public bool? IsBinary => null;
            public Texture2D Texture => null;
            public void Dispose() { }
        }

        private sealed class RejectCompanions : IDownloadProvider
        {
            public int Count;
            public Task<IDownload> Request(Uri uri)
            {
                Count++;
                return Task.FromResult<IDownload>(new DeniedDownload());
            }
            public Task<ITextureDownload> RequestTexture(Uri uri, bool nonReadable)
            {
                Count++;
                return Task.FromResult<ITextureDownload>(new DeniedDownload());
            }
        }

        public static async Task<Result> Run(string name, byte[] bytes, bool cancelLoad = false,
            bool cancelInstantiation = false, bool allowPlayMode = false)
        {
            if (Application.isPlaying && !allowPlayMode)
                throw new InvalidOperationException("This harness is Edit Mode only");
            var result = new Result { fixture = name, caseId = name +
                (cancelLoad ? ":cancel-before-load" : cancelInstantiation ? ":cancel-before-instantiation" : ""),
                stage = "construct" };
            var logger = new CollectingLogger();
            var downloads = new RejectCompanions();
            GltfImport importer = null;
            GameObject root = null;
            using (var cancellation = new CancellationTokenSource())
            {
                try
                {
                    // Edit Mode cannot host the default persistent frame-budget GameObject.
                    // This curated correctness probe makes no scheduling/performance claim.
                    importer = new GltfImport(downloadProvider: downloads,
                        deferAgent: new UninterruptedDeferAgent(), logger: logger);
                    result.stage = "load";
                    if (cancelLoad) cancellation.Cancel();
                    result.loaded = await importer.Load(bytes, new Uri("https://fixture.invalid/model.glb"),
                        cancellationToken: cancellation.Token);
                    bool errors = logger.Items?.Any(x => x.Type == LogType.Error || x.Type == LogType.Exception) ?? false;
                    // The importer's bool alone is not permission to publish a partial scene.
                    if (result.loaded && !errors && downloads.Count == 0)
                    {
                        result.stage = "instantiate";
                        if (cancelInstantiation) cancellation.Cancel();
                        root = new GameObject("Importer trial instance");
                        result.instantiated = await importer.InstantiateMainSceneAsync(root.transform, cancellation.Token);
                        result.stage = "inspect";
                        var filters = root.GetComponentsInChildren<MeshFilter>();
                        result.meshes = filters.Length;
                        result.vertices = filters.Sum(x => x.sharedMesh.vertexCount);
                        result.shaders = root.GetComponentsInChildren<Renderer>()
                            .SelectMany(x => x.sharedMaterials).Select(x => x.shader.name).Distinct().ToArray();
                    }
                }
                catch (OperationCanceledException) { result.cancelled = true; }
                catch (Exception error) { result.exception = error.GetType().Name + ": " + error.Message; }
                finally
                {
                    try
                    {
                        if (root != null) UnityEngine.Object.DestroyImmediate(root);
                        importer?.Dispose();
                    }
                    catch (Exception error) { result.cleanupException = error.ToString(); }
                }
            }
            result.deniedRequests = downloads.Count;
            result.diagnostics = logger.Items?.Select(x => x.Type + ":" + x.Code).ToArray() ?? Array.Empty<string>();
            result.errorCount = logger.Items?.Count(x => x.Type == LogType.Error || x.Type == LogType.Exception) ?? 0;
            // Assert a particular reason for each rejection; an arbitrary failure is not a pass.
            bool expected = cancelLoad || cancelInstantiation
                ? result.cancelled && !result.instantiated && result.deniedRequests == 0 && result.errorCount == 0
                    && result.stage == (cancelLoad ? "load" : "instantiate") && result.loaded == cancelInstantiation
                : name switch
                {
                    "triangle.glb" or "hierarchy.glb" or "textured.glb" =>
                        result.loaded && result.instantiated && result.meshes == 1 && result.vertices == 3
                        && result.deniedRequests == 0 && result.errorCount == 0 && !result.cancelled
                        && result.shaders.SequenceEqual(new[] { "glTF/PbrMetallicRoughness" }),
                    "external-image.glb" or "missing-image.glb" =>
                        result.loaded && !result.instantiated && !result.cancelled && result.deniedRequests == 1
                        && result.errorCount == 2 && result.diagnostics.Contains("Error:TextureDownloadFailed")
                        && result.diagnostics.Contains("Error:TextureLoadFailed"),
                    "unsupported-required.glb" => !result.loaded && !result.instantiated
                        && !result.cancelled && result.deniedRequests == 0 && result.errorCount == 1
                        && result.diagnostics.Contains("Error:ExtensionUnsupported"),
                    "truncated.glb" => !result.loaded && !result.instantiated
                        && !result.cancelled && result.deniedRequests == 0 && result.errorCount == 1
                        && result.diagnostics.Contains("Error:UnexpectedEndOfContent"),
                    "bad-magic.glb" => !result.loaded && !result.instantiated
                        && !result.cancelled && result.deniedRequests == 0 && result.errorCount == 1
                        && result.diagnostics.Contains("Error:JsonParsingFailed"),
                    _ => false
                };
            result.passed = expected && result.exception == null && result.cleanupException == null;
            return result;
        }
    }
}

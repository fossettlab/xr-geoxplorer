using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace GeoX.GltfTrial
{
    // Isolated Android player. Local StreamingAssets only. Not product opening.
    public sealed class ImporterPlayer : MonoBehaviour
    {
        [Serializable]
        private sealed class Receipt
        {
            public string status;
            public string attemptId;
            public string startedUtc;
            public string updatedUtc;
            public string completedUtc;
            public string stage;
            public string activeCase;
            public int completedCases;
            public int totalCases;
            public bool success;
            public string unityVersion;
            public string platform;
            public string applicationIdentifier;
            public string applicationVersion;
            public string buildGuid;
            public string deviceModel;
            public string operatingSystem;
            public string processorType;
            public string graphicsDeviceName;
            public string graphicsDeviceType;
            public int reportedSystemMemoryMegabytes;
            public int reportedGraphicsMemoryMegabytes;
            public string[] includedShaders;
            public string[] observedShaders;
            public ImporterTrial.Result[] results;
            public string errorType;
            public string error;
        }

        private string status = "Preparing isolated importer qualification...";

        private IEnumerator Start()
        {
            string directory = Path.Combine(Application.persistentDataPath,
                "gltf-importer-trial");
            string attemptId = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ") + "-" +
                Guid.NewGuid().ToString("N");
            string attemptPath = Path.Combine(directory,
                "android-importer-attempt-" + attemptId + ".json");
            string canonicalPath = Path.Combine(directory,
                "android-importer-receipt.json");
            Receipt receipt;
            var results = new List<ImporterTrial.Result>();
            try
            {
                Directory.CreateDirectory(directory);
                receipt = NewReceipt(attemptId);
                Persist(receipt, results, canonicalPath, attemptPath,
                    "starting", null);
            }
            catch (Exception error)
            {
                status = "Importer player could not create its current attempt receipt.";
                Debug.LogError("GEOX_GLTF_TRIAL_START_FAILED " +
                    error.GetType().FullName + ": " + error.Message);
                yield break;
            }

            IEnumerator attempt = RunAttempt(receipt, results, directory,
                canonicalPath, attemptPath);
            while (true)
            {
                bool moved;
                object yielded = null;
                try
                {
                    moved = attempt.MoveNext();
                    if (moved) yielded = attempt.Current;
                }
                catch (Exception error)
                {
                    Fail(receipt, results, canonicalPath, attemptPath, error);
                    yield break;
                }
                if (!moved) yield break;
                yield return yielded;
            }
        }

        private IEnumerator RunAttempt(Receipt receipt,
            List<ImporterTrial.Result> results, string directory,
            string canonicalPath, string attemptPath)
        {
            var names = new[]
            {
                "triangle.glb", "hierarchy.glb", "textured.glb",
                "external-image.glb", "missing-image.glb", "unsupported-required.glb",
                "truncated.glb", "bad-magic.glb"
            };
            receipt.totalCases = names.Length + 2;
            Persist(receipt, results, canonicalPath, attemptPath,
                "fixture_copy", names[0]);
            foreach (string name in names)
            {
                Persist(receipt, results, canonicalPath, attemptPath,
                    "fixture_copy", name);
                string path = Path.Combine(directory, name);
                string source = Application.streamingAssetsPath + "/Fixtures/" + name + ".bytes";
                using (var request = UnityWebRequest.Get(source))
                {
                    yield return request.SendWebRequest();
                    if (request.result != UnityWebRequest.Result.Success)
                        throw new IOException("Packaged fixture unavailable: " +
                            name + ": " + request.error);
                    File.WriteAllBytes(path, request.downloadHandler.data);
                }
                Persist(receipt, results, canonicalPath, attemptPath,
                    "import_case", name);
                var task = ImporterTrial.Run(name, File.ReadAllBytes(path), allowPlayMode: true);
                while (!task.IsCompleted) yield return null;
                results.Add(Unwrap(task));
                receipt.completedCases = results.Count;
                Persist(receipt, results, canonicalPath, attemptPath,
                    "case_complete", name);
            }
            Persist(receipt, results, canonicalPath, attemptPath,
                "cancel_load_case", "triangle.glb");
            var cancelLoad = ImporterTrial.Run("triangle.glb", File.ReadAllBytes(
                Path.Combine(directory, "triangle.glb")), cancelLoad: true, allowPlayMode: true);
            while (!cancelLoad.IsCompleted) yield return null;
            results.Add(Unwrap(cancelLoad));
            receipt.completedCases = results.Count;
            Persist(receipt, results, canonicalPath, attemptPath,
                "case_complete", "triangle.glb:cancel_load");
            Persist(receipt, results, canonicalPath, attemptPath,
                "cancel_instantiation_case", "triangle.glb");
            var cancelInstantiate = ImporterTrial.Run("triangle.glb", File.ReadAllBytes(
                Path.Combine(directory, "triangle.glb")), cancelInstantiation: true, allowPlayMode: true);
            while (!cancelInstantiate.IsCompleted) yield return null;
            results.Add(Unwrap(cancelInstantiate));
            receipt.completedCases = results.Count;
            Persist(receipt, results, canonicalPath, attemptPath,
                "case_complete", "triangle.glb:cancel_instantiation");
            receipt.success = results.All(x => x.passed) && receipt.includedShaders.Length == 3;
            if (!receipt.success && receipt.error == null)
                receipt.error = "One or more isolated player cases failed";
            Finish(receipt, results, canonicalPath, attemptPath);
        }

        private static Receipt NewReceipt(string attemptId)
        {
            return new Receipt
            {
                status = "in_progress",
                attemptId = attemptId,
                startedUtc = DateTime.UtcNow.ToString("o"),
                stage = "starting",
                unityVersion = Application.unityVersion,
                platform = Application.platform.ToString(),
                applicationIdentifier = Application.identifier,
                applicationVersion = Application.version,
                buildGuid = Application.buildGUID,
                deviceModel = SystemInfo.deviceModel,
                operatingSystem = SystemInfo.operatingSystem,
                processorType = SystemInfo.processorType,
                graphicsDeviceName = SystemInfo.graphicsDeviceName,
                graphicsDeviceType = SystemInfo.graphicsDeviceType.ToString(),
                reportedSystemMemoryMegabytes = SystemInfo.systemMemorySize,
                reportedGraphicsMemoryMegabytes = SystemInfo.graphicsMemorySize,
                includedShaders = new[]
                {
                    "glTF/PbrMetallicRoughness",
                    "glTF/PbrSpecularGlossiness",
                    "glTF/Unlit"
                }.Where(name => Shader.Find(name) != null).ToArray(),
                observedShaders = Array.Empty<string>(),
                results = Array.Empty<ImporterTrial.Result>()
            };
        }

        private void Finish(Receipt receipt, List<ImporterTrial.Result> results,
            string canonicalPath, string attemptPath)
        {
            receipt.status = receipt.success ? "passed" : "failed";
            receipt.stage = "complete";
            receipt.activeCase = null;
            receipt.completedUtc = DateTime.UtcNow.ToString("o");
            Persist(receipt, results, canonicalPath, attemptPath,
                receipt.stage, receipt.activeCase);
            status = receipt.success
                ? "Importer player checks passed. Retrieve the receipt with adb."
                : "Importer player checks failed. Retrieve the receipt with adb.";
            Debug.Log("GEOX_GLTF_TRIAL " + JsonUtility.ToJson(receipt));
        }

        private void Fail(Receipt receipt, List<ImporterTrial.Result> results,
            string canonicalPath, string attemptPath, Exception error)
        {
            receipt.success = false;
            receipt.status = "failed";
            receipt.stage = "failed";
            receipt.completedUtc = DateTime.UtcNow.ToString("o");
            receipt.errorType = error.GetType().FullName;
            receipt.error = error.Message;
            try
            {
                Persist(receipt, results, canonicalPath, attemptPath,
                    receipt.stage, receipt.activeCase);
            }
            catch (Exception writeError)
            {
                Debug.LogError("GEOX_GLTF_TRIAL_RECEIPT_WRITE_FAILED " +
                    writeError.GetType().FullName + ": " + writeError.Message);
            }
            status = "Importer player checks failed. Retrieve the attempt receipt with adb.";
            Debug.LogError("GEOX_GLTF_TRIAL_FAILED " + JsonUtility.ToJson(receipt));
        }

        private static void Persist(Receipt receipt,
            List<ImporterTrial.Result> results, string canonicalPath,
            string attemptPath, string stage, string activeCase)
        {
            receipt.stage = stage;
            receipt.activeCase = activeCase;
            receipt.updatedUtc = DateTime.UtcNow.ToString("o");
            receipt.results = results.ToArray();
            receipt.observedShaders = results
                .Where(x => x.shaders != null)
                .SelectMany(x => x.shaders)
                .Distinct()
                .ToArray();
            string json = JsonUtility.ToJson(receipt, true);
            WriteCurrent(canonicalPath, json, receipt.attemptId);
            WriteCurrent(attemptPath, json, receipt.attemptId);
        }

        private static void WriteCurrent(string path, string json, string attemptId)
        {
            string temporary = path + ".tmp-" + attemptId;
            File.WriteAllText(temporary, json);
            if (File.Exists(path)) File.Delete(path);
            File.Move(temporary, path);
        }

        private static ImporterTrial.Result Unwrap(Task<ImporterTrial.Result> task)
        {
            return task.GetAwaiter().GetResult();
        }

        private void OnGUI()
        {
            GUILayout.Label(status);
        }
    }
}

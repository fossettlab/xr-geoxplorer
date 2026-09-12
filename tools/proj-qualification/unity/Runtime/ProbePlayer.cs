using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Networking;

namespace GeoX.ProjQualification
{
    public sealed class ProbePlayer : MonoBehaviour
    {
        [Serializable]
        private sealed class Receipt
        {
            public bool success;
            public string unityVersion;
            public string platform;
            public string databaseSha256;
            public string fixturesSha256;
            public int firstExitCode;
            public int missingDatabaseExitCode;
            public int recoveryExitCode;
            public string error;
        }

        private string status = "Preparing offline coordinate qualification...";

        private IEnumerator Start()
        {
            string directory = Path.Combine(Application.persistentDataPath, "proj-qualification");
            Directory.CreateDirectory(directory);
            var receipt = new Receipt { unityVersion = Application.unityVersion,
                platform = Application.platform.ToString() };
            foreach (string name in new[] { "proj.db", "fixtures.json" })
            {
                string source = Application.streamingAssetsPath + "/ProjQualification/" + name;
                // Android StreamingAssets is inside the APK; this reads a local jar,
                // not a remote URL. Copy to a real file for PROJ/SQLite.
                using (var request = UnityWebRequest.Get(source))
                {
                    yield return request.SendWebRequest();
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        receipt.error = "Packaged resource unavailable: " + name + ": " + request.error;
                        Finish(directory, receipt);
                        yield break;
                    }
                    File.WriteAllBytes(Path.Combine(directory, name), request.downloadHandler.data);
                }
            }
            try
            {
                string fixtures = Path.Combine(directory, "fixtures.json");
                receipt.databaseSha256 = Hash(Path.Combine(directory, "proj.db"));
                receipt.fixturesSha256 = Hash(fixtures);
                receipt.firstExitCode = ProbeBridge.geox_proj_probe_run(directory, fixtures,
                    Path.Combine(directory, "android-report.json"));
                receipt.missingDatabaseExitCode = ProbeBridge.geox_proj_probe_run(
                    Path.Combine(directory, "absent-bundle"), fixtures,
                    Path.Combine(directory, "android-missing-database-report.json"));
                receipt.recoveryExitCode = ProbeBridge.geox_proj_probe_run(directory, fixtures,
                    Path.Combine(directory, "android-recovery-report.json"));
                receipt.success = receipt.firstExitCode == 0 && receipt.missingDatabaseExitCode == 1 &&
                    receipt.recoveryExitCode == 0 && File.ReadAllText(
                    Path.Combine(directory, "android-missing-database-report.json"))
                    .Contains("database_unavailable");
            }
            catch (Exception exception)
            {
                receipt.error = exception.ToString();
            }
            Finish(directory, receipt);
        }

        private void Finish(string directory, Receipt receipt)
        {
            File.WriteAllText(Path.Combine(directory, "android-managed-receipt.json"),
                JsonUtility.ToJson(receipt, true));
            status = receipt.success ? "PROJ qualification checks passed. Retrieve the receipts with adb."
                : "PROJ qualification failed. Retrieve the receipts with adb.";
            Debug.Log("GEOX_PROJ_QUALIFICATION " + JsonUtility.ToJson(receipt));
        }

        private static string Hash(string path)
        {
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path)))
                    .Replace("-", "").ToLowerInvariant();
        }

        private void OnGUI()
        {
            GUILayout.Label(status);
        }
    }
}

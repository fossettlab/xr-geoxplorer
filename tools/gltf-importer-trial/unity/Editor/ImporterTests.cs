using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GeoX.GltfTrial
{
    public sealed class ImporterTests
    {
        [Serializable]
        private sealed class Receipt
        {
            public Dictionary<string, string> harnessSha256;
            public Dictionary<string, string> fixtureSha256;
            public ImporterTrial.Result[] results;
        }

        [UnityTest]
        public IEnumerator CuratedFixturesDiagnosticsCancellationAndRetry()
        {
            var harnessBefore = HashTree(Path.Combine(Application.dataPath, "Trial"));
            var fixturesBefore = HashTree(Path.Combine(Application.streamingAssetsPath, "Fixtures"));
            var results = new List<ImporterTrial.Result>();
            foreach (string name in new[] { "triangle.glb", "hierarchy.glb", "textured.glb",
                "external-image.glb", "missing-image.glb", "unsupported-required.glb",
                "truncated.glb", "bad-magic.glb", "triangle.glb" })
            {
                // The parser also emits this exact Console error for malformed JSON.
                if (name == "bad-magic.glb") LogAssert.Expect(LogType.Error, "JsonParsingFailed");
                var task = ImporterTrial.Run(name, Read(name));
                while (!task.IsCompleted) yield return null;
                results.Add(task.GetAwaiter().GetResult());
                results.Last().caseId += ":case-" + results.Count;
            }
            foreach (bool beforeLoad in new[] { true, false })
            {
                var task = ImporterTrial.Run("triangle.glb", Read("triangle.glb"),
                    cancelLoad: beforeLoad, cancelInstantiation: !beforeLoad);
                while (!task.IsCompleted) yield return null;
                results.Add(task.GetAwaiter().GetResult());
            }
            AssertHashesUnchanged(harnessBefore, HashTree(Path.Combine(Application.dataPath, "Trial")), "harness");
            AssertHashesUnchanged(fixturesBefore, HashTree(Path.Combine(Application.streamingAssetsPath, "Fixtures")), "fixtures");
            Directory.CreateDirectory("build");
            File.WriteAllText("build/importer-editor-report.json", JsonConvert.SerializeObject(new Receipt
            {
                harnessSha256 = harnessBefore,
                fixtureSha256 = fixturesBefore,
                results = results.ToArray()
            }, Formatting.Indented));
            Assert.That(results.All(x => x.passed), Is.True,
                string.Join(", ", results.Where(x => !x.passed).Select(x => JsonUtility.ToJson(x))));
        }

        private static byte[] Read(string name) => File.ReadAllBytes(
            Path.Combine(Application.streamingAssetsPath, "Fixtures", name + ".bytes"));

        private static Dictionary<string, string> HashTree(string directory)
        {
            if (!Directory.Exists(directory)) throw new DirectoryNotFoundException(directory);
            var root = directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                Path.DirectorySeparatorChar;
            var hashes = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string path in Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
                .Where(path => !path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.Ordinal))
            {
                using (var hash = SHA256.Create())
                using (var stream = File.OpenRead(path))
                    hashes.Add(path.Substring(root.Length).Replace('\\', '/'),
                        BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant());
            }
            return hashes;
        }

        private static void AssertHashesUnchanged(Dictionary<string, string> before,
            Dictionary<string, string> after, string name)
        {
            CollectionAssert.AreEquivalent(before, after, name + " changed during the curated test");
        }
    }
}

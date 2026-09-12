using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace GeoX.GltfTrial
{
    public static class ImporterBuild
    {
        public static void BuildAndroid()
        {
            if (!File.Exists("trial-project.marker"))
                throw new InvalidOperationException("This is not an importer trial project.");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel34;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,
                "edu.wustl.fossett.geoxgltftrial");
            PlayerSettings.productName = "GeoX glTF Trial";
            PlayerSettings.Android.forceInternetPermission = false;
            PlayerSettings.Android.useCustomKeystore = false;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
            IncludeGltfShaders();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Trial").AddComponent<ImporterPlayer>();
            new GameObject("Camera").AddComponent<Camera>();
            EditorSceneManager.SaveScene(scene, "Assets/TrialPlayer.unity");
            Directory.CreateDirectory("build");
            var harness = new SortedDictionary<string, string>();
            foreach (string path in Directory.GetFiles("Assets/Trial", "*", SearchOption.AllDirectories)
                .Where(path => !path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)))
            {
                using (var hash = SHA256.Create())
                    harness[path.Substring("Assets/Trial/".Length).Replace('\\', '/')] =
                        BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path)))
                            .Replace("-", "").ToLowerInvariant();
            }
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/TrialPlayer.unity" },
                target = BuildTarget.Android,
                locationPathName = "build/GeoX-GltfTrial.apk",
                options = BuildOptions.Development
            });
            using (var hash = SHA256.Create())
                File.WriteAllText("build/build-identity.json", JsonConvert.SerializeObject(new
                {
                    harnessSha256 = harness,
                    apkSha256 = File.Exists("build/GeoX-GltfTrial.apk")
                        ? BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes("build/GeoX-GltfTrial.apk")))
                            .Replace("-", "").ToLowerInvariant() : null
                }, Formatting.Indented));
            string[] messages = report.steps.SelectMany(step => step.messages)
                .Where(message => message.type != LogType.Log)
                .Select(message => message.type + ": " + message.content).ToArray();
            File.WriteAllText("build/build-result.txt", report.summary.result + "\nErrors: " +
                report.summary.totalErrors + "\nWarnings: " + report.summary.totalWarnings + "\n" +
                string.Join("\n", messages));
            File.WriteAllText("build/included-shaders.txt",
                string.Join("\n", AlwaysIncludedShaderNames()));
            EditorApplication.Exit(report.summary.result == BuildResult.Succeeded &&
                report.summary.totalErrors == 0 ? 0 : 1);
        }

        private static void IncludeGltfShaders()
        {
            var names = new[]
            {
                "glTF/PbrMetallicRoughness",
                "glTF/PbrSpecularGlossiness",
                "glTF/Unlit"
            };
            var found = names.Select(Shader.Find).Where(shader => shader != null).ToArray();
            if (found.Length != names.Length)
                throw new InvalidOperationException("One or more required glTFast shaders were not found.");
            var settings = GraphicsSettings.GetGraphicsSettings();
            var serialized = new SerializedObject(settings);
            var property = serialized.FindProperty("m_AlwaysIncludedShaders");
            foreach (Shader shader in found)
            {
                bool present = false;
                for (int i = 0; i < property.arraySize; i++)
                {
                    if (property.GetArrayElementAtIndex(i).objectReferenceValue == shader)
                        present = true;
                }
                if (present) continue;
                property.InsertArrayElementAtIndex(property.arraySize);
                property.GetArrayElementAtIndex(property.arraySize - 1).objectReferenceValue = shader;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static string[] AlwaysIncludedShaderNames()
        {
            var serialized = new SerializedObject(GraphicsSettings.GetGraphicsSettings());
            var property = serialized.FindProperty("m_AlwaysIncludedShaders");
            return Enumerable.Range(0, property.arraySize)
                .Select(i => property.GetArrayElementAtIndex(i).objectReferenceValue as Shader)
                .Where(shader => shader != null)
                .Select(shader => shader.name)
                .ToArray();
        }
    }
}

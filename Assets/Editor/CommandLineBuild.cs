using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GeoXEditor
{
    /// <summary>
    /// Batchmode entry points for scripts/unity.sh (local Android builds).
    /// </summary>
    public static class CommandLineBuild
    {
        private const string MainScene = "Assets/Scenes/GeoXShared.unity";
        private const string OutputApk = "build/GeoXplorer.apk";

        public static void BuildAndroid()
        {
            string projectRoot = Directory.GetCurrentDirectory();
            string outputPath = Path.Combine(projectRoot, OutputApk);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { MainScene },
                locationPathName = outputPath,
                target = BuildTarget.Android,
                // An explicit target with the default subtarget resets the ASTC override.
                // https://docs.unity3d.com/6000.4/Documentation/ScriptReference/BuildPlayerOptions-subtarget.html
                subtarget = (int)MobileTextureSubtarget.ASTC,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            File.WriteAllText(Path.ChangeExtension(outputPath, ".build.json"),
                Newtonsoft.Json.JsonConvert.SerializeObject(new
                {
                    result = summary.result.ToString(),
                    platform = summary.platform.ToString(),
                    textureSubtarget = MobileTextureSubtarget.ASTC.ToString(),
                    unityVersion = Application.unityVersion,
                    scene = MainScene,
                    outputPath,
                    recordedAtUtc = DateTime.UtcNow.ToString("o"),
                    buildTimeMs = summary.totalTime.TotalMilliseconds,
                    summary.totalErrors,
                    summary.totalWarnings,
                    messages = report.steps.Where(step => step.messages != null)
                        .SelectMany(step => step.messages)
                        .Where(message => message.type != LogType.Log)
                        .Select(message => new { type = message.type.ToString(), message.content })
                }, Newtonsoft.Json.Formatting.Indented));

            if (summary.result != BuildResult.Succeeded || summary.totalErrors != 0)
            {
                Debug.LogError(
                    $"Android build failed: {summary.result} " +
                    $"({summary.totalErrors} errors, {summary.totalWarnings} warnings)");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log($"Android build succeeded: {outputPath}");
            EditorApplication.Exit(0);
        }
    }
}

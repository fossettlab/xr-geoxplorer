using System;
using System.IO;
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
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result != BuildResult.Succeeded)
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

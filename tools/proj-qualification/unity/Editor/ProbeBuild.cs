using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GeoX.ProjQualification
{
    public static class ProbeBuild
    {
        public static void BuildAndroid()
        {
            // This method runs only in the generated, isolated qualification project.
            if (!File.Exists("qualification-project.marker"))
                throw new InvalidOperationException("This is not a qualification project.");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel34;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,
                "edu.wustl.fossett.geoxprojqualification");
            PlayerSettings.productName = "GeoX PROJ Qualification";
            PlayerSettings.Android.forceInternetPermission = false;
            PlayerSettings.Android.useCustomKeystore = false;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Qualification").AddComponent<ProbePlayer>();
            new GameObject("Camera").AddComponent<Camera>();
            EditorSceneManager.SaveScene(scene, "Assets/Qualification.unity");
            Directory.CreateDirectory("build");
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Qualification.unity" },
                target = BuildTarget.Android,
                locationPathName = "build/GeoX-ProjQualification.apk",
                options = BuildOptions.Development
            });
            string[] messages = report.steps.SelectMany(step => step.messages)
                .Where(message => message.type != LogType.Log)
                .Select(message => message.type + ": " + message.content).ToArray();
            File.WriteAllText("build/build-result.txt", report.summary.result + "\nErrors: " +
                report.summary.totalErrors + "\nWarnings: " + report.summary.totalWarnings + "\n" +
                string.Join("\n", messages));
            EditorApplication.Exit(report.summary.result == BuildResult.Succeeded &&
                report.summary.totalErrors == 0 ? 0 : 1);
        }
    }
}

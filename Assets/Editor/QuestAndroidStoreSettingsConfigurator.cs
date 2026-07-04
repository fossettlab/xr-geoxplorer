using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

public static class QuestAndroidStoreSettingsConfigurator
{
    public const string PackageName = "edu.wustl.fossettlab.xrgeoxplorer";

    private const string AndroidManifestPath = "Assets/Plugins/Android/AndroidManifest.xml";
    private const string MainGradleTemplatePath = "Assets/Plugins/Android/mainTemplate.gradle";
    private const string GradlePropertiesTemplatePath = "Assets/Plugins/Android/gradleTemplate.properties";
    private const string GradleSettingsTemplatePath = "Assets/Plugins/Android/settingsTemplate.gradle";
    private const string UseCustomMainManifestProperty = "useCustomMainManifest";
    private const string UseCustomMainGradleTemplateProperty = "useCustomMainGradleTemplate";
    private const string UseCustomGradlePropertiesTemplateProperty = "useCustomGradlePropertiesTemplate";
    private const string UseCustomGradleSettingsTemplateProperty = "useCustomGradleSettingsTemplate";
    private const string ActiveInputHandlerProperty = "activeInputHandler";
    private const int ActiveInputHandlingBoth = 2;

    private static readonly string[] RequiredAndroidFeatureIds =
    {
        "com.unity.openxr.feature.metaquest",
        "com.unity.openxr.feature.input.oculustouch",
        "com.unity.openxr.feature.input.metaquestplus",
        "com.unity.openxr.feature.input.metahandtrackingaim"
    };

    [MenuItem("GeoXplorer/XR/Configure Quest Android Store Settings")]
    public static void ConfigureQuestAndroidStoreSettings()
    {
        OpenXRMigrationConfigurator.ConfigureOpenXRMigration();
        ApplyQuestStorePlayerSettings();
        ConfigureAndroidOpenXRSettings();
        WriteAndroidManifestTemplate();
        WriteGradlePropertiesTemplate();

        AssetDatabase.ImportAsset(AndroidManifestPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(GradlePropertiesTemplatePath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        ValidateQuestAndroidStoreSettings();
    }

    [MenuItem("GeoXplorer/XR/Validate Quest Android Store Settings")]
    public static void ValidateQuestAndroidStoreSettings()
    {
        List<string> failures = CollectValidationFailures();
        if (failures.Count > 0)
        {
            foreach (string failure in failures)
            {
                Debug.LogError("Quest Android store settings: " + failure);
            }

            throw new InvalidOperationException("Quest Android store settings validation failed.");
        }

        Debug.Log("Quest Android store settings validation passed.");
    }

    internal static void ApplyCoreAndroidPlayerSettings()
    {
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Android, ApiCompatibilityLevel.NET_Standard);
        PlayerSettings.colorSpace = ColorSpace.Linear;
        SetPlayerSettingsInt(ActiveInputHandlerProperty, ActiveInputHandlingBoth);
    }

    private static void ApplyQuestStorePlayerSettings()
    {
        ApplyCoreAndroidPlayerSettings();

        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel34;
        PlayerSettings.Android.forceInternetPermission = true;
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, PackageName);
        SetPlayerSettingsBool(UseCustomMainManifestProperty, true);
        SetPlayerSettingsBool(UseCustomMainGradleTemplateProperty, true);
        SetPlayerSettingsBool(UseCustomGradlePropertiesTemplateProperty, true);
        SetPlayerSettingsBool(UseCustomGradleSettingsTemplateProperty, true);
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
        EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
    }

    private static void ConfigureAndroidOpenXRSettings()
    {
        OpenXRSettings openXRSettings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
        if (openXRSettings == null)
        {
            Debug.LogWarning("OpenXR Android settings were unavailable. Install Android Build Support and rerun this configurator.");
            return;
        }

        openXRSettings.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
        openXRSettings.depthSubmissionMode = OpenXRSettings.DepthSubmissionMode.Depth16Bit;
        EditorUtility.SetDirty(openXRSettings);
    }

    private static List<string> CollectValidationFailures()
    {
        List<string> failures = new List<string>();

        Require(PlayerSettings.Android.minSdkVersion == AndroidSdkVersions.AndroidApiLevel29, failures, "minimum API level must be Android 10/API 29");
        Require(PlayerSettings.Android.targetSdkVersion == AndroidSdkVersions.AndroidApiLevel34, failures, "target API level must be Android 14/API 34");
        Require(PlayerSettings.Android.targetArchitectures == AndroidArchitecture.ARM64, failures, "target architecture must be ARM64 only");
        Require(PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) == ScriptingImplementation.IL2CPP, failures, "Android scripting backend must be IL2CPP");
        Require(PlayerSettings.GetApiCompatibilityLevel(BuildTargetGroup.Android) == ApiCompatibilityLevel.NET_Standard, failures, "API compatibility must be .NET Standard");
        Require(PlayerSettings.colorSpace == ColorSpace.Linear, failures, "color space must be Linear");
        Require(GetPlayerSettingsInt(ActiveInputHandlerProperty) == ActiveInputHandlingBoth, failures, "Active Input Handling must be Both");
        Require(PlayerSettings.Android.forceInternetPermission, failures, "Internet Access must be Require");
        Require(PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android) == PackageName, failures, "Android package name must be " + PackageName);
        Require(GetPlayerSettingsBool(UseCustomMainManifestProperty), failures, "custom main manifest must be enabled");
        Require(GetPlayerSettingsBool(UseCustomMainGradleTemplateProperty), failures, "custom main Gradle template must be enabled");
        Require(GetPlayerSettingsBool(UseCustomGradlePropertiesTemplateProperty), failures, "custom Gradle properties template must be enabled");
        Require(GetPlayerSettingsBool(UseCustomGradleSettingsTemplateProperty), failures, "custom Gradle settings template must be enabled");

        GraphicsDeviceType[] graphicsApis = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
        Require(!PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android), failures, "Android graphics APIs must be explicit");
        Require(graphicsApis.Length == 1 && graphicsApis[0] == GraphicsDeviceType.Vulkan, failures, "Android graphics APIs must be Vulkan only");
        Require(EditorUserBuildSettings.androidBuildSubtarget == MobileTextureSubtarget.ASTC, failures, "Android texture compression must be ASTC");

        ValidateOpenXRSettings(failures);
        ValidateOpenXRFeatures(failures);
        ValidateFileContains(AndroidManifestPath, failures, RequiredManifestSnippets);
        ValidateFileContains(MainGradleTemplatePath, failures, RequiredMainGradleSnippets);
        ValidateFileContains(GradlePropertiesTemplatePath, failures, RequiredGradlePropertiesSnippets);
        ValidateFileContains(GradleSettingsTemplatePath, failures, RequiredGradleSettingsSnippets);

        return failures;
    }

    private static void ValidateOpenXRSettings(List<string> failures)
    {
        OpenXRSettings openXRSettings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
        Require(openXRSettings != null, failures, "OpenXR Android settings must exist");
        if (openXRSettings == null)
        {
            return;
        }

        Require(openXRSettings.renderMode == OpenXRSettings.RenderMode.SinglePassInstanced, failures, "OpenXR Android render mode must be Single Pass Instanced / Multi-view");
        Require(openXRSettings.depthSubmissionMode == OpenXRSettings.DepthSubmissionMode.Depth16Bit, failures, "OpenXR Android depth submission mode must be 16-bit");
    }

    private static void ValidateOpenXRFeatures(List<string> failures)
    {
        foreach (string featureId in RequiredAndroidFeatureIds)
        {
            OpenXRFeature feature = FeatureHelpers.GetFeatureWithIdForBuildTarget(BuildTargetGroup.Android, featureId);
            Require(feature != null && feature.enabled, failures, "OpenXR feature must be enabled for Android: " + featureId);
        }
    }

    private static void ValidateFileContains(string path, List<string> failures, string[] snippets)
    {
        Require(File.Exists(path), failures, path + " must exist");
        if (!File.Exists(path))
        {
            return;
        }

        string contents = File.ReadAllText(path);
        foreach (string snippet in snippets)
        {
            Require(contents.Contains(snippet), failures, path + " must contain " + snippet);
        }
    }

    private static void Require(bool condition, List<string> failures, string message)
    {
        if (!condition)
        {
            failures.Add(message);
        }
    }

    private static void SetPlayerSettingsBool(string propertyName, bool value)
    {
        SerializedObject serializedPlayerSettings = GetSerializedPlayerSettings();
        SerializedProperty property = serializedPlayerSettings.FindProperty(propertyName);
        if (property == null)
        {
            throw new InvalidOperationException("PlayerSettings property was not found: " + propertyName);
        }

        property.boolValue = value;
        serializedPlayerSettings.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetPlayerSettingsInt(string propertyName, int value)
    {
        SerializedObject serializedPlayerSettings = GetSerializedPlayerSettings();
        SerializedProperty property = serializedPlayerSettings.FindProperty(propertyName);
        if (property == null)
        {
            throw new InvalidOperationException("PlayerSettings property was not found: " + propertyName);
        }

        property.intValue = value;
        serializedPlayerSettings.ApplyModifiedPropertiesWithoutUndo();
    }

    private static bool GetPlayerSettingsBool(string propertyName)
    {
        SerializedObject serializedPlayerSettings = GetSerializedPlayerSettings();
        SerializedProperty property = serializedPlayerSettings.FindProperty(propertyName);
        if (property == null)
        {
            throw new InvalidOperationException("PlayerSettings property was not found: " + propertyName);
        }

        return property.boolValue;
    }

    private static int GetPlayerSettingsInt(string propertyName)
    {
        SerializedObject serializedPlayerSettings = GetSerializedPlayerSettings();
        SerializedProperty property = serializedPlayerSettings.FindProperty(propertyName);
        if (property == null)
        {
            throw new InvalidOperationException("PlayerSettings property was not found: " + propertyName);
        }

        return property.intValue;
    }

    private static SerializedObject GetSerializedPlayerSettings()
    {
        UnityEngine.Object playerSettings = Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings");
        return new SerializedObject(playerSettings);
    }

    private static readonly string[] RequiredManifestSnippets =
    {
        "android.permission.RECORD_AUDIO",
        "android.permission.INTERNET",
        "com.oculus.permission.USE_ANCHOR_API",
        "com.oculus.permission.USE_SCENE",
        "com.oculus.permission.HAND_TRACKING",
        "oculus.software.handtracking",
        "com.oculus.feature.PASSTHROUGH",
        "com.oculus.intent.category.VR",
        "com.oculus.supportedDevices"
    };

    private static readonly string[] RequiredGradlePropertiesSnippets =
    {
        "org.gradle.jvmargs",
        "unityStreamingAssets",
        "**ADDITIONAL_PROPERTIES**"
    };

    private static readonly string[] RequiredMainGradleSnippets =
    {
        "com.android.library",
        "spatialanchors_ndk",
        "**TARGETSDKVERSION**"
    };

    private static readonly string[] RequiredGradleSettingsSnippets =
    {
        "include ':launcher', ':unityLibrary'",
        "google()",
        "mavenCentral()"
    };

    private static void WriteAndroidManifestTemplate()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(AndroidManifestPath));
        File.WriteAllText(AndroidManifestPath, NormalizeNewlines(AndroidManifestContents));
    }

    private static void WriteGradlePropertiesTemplate()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(GradlePropertiesTemplatePath));
        File.WriteAllText(GradlePropertiesTemplatePath, NormalizeNewlines(GradlePropertiesContents));
    }

    // The template constants are verbatim string literals, so their newline
    // bytes follow this source file's line endings (CRLF). The committed
    // manifest and Gradle properties are LF, so normalize before writing to
    // keep re-running the configurator idempotent instead of churning endings.
    private static string NormalizeNewlines(string text)
    {
        return text.Replace("\r\n", "\n").Replace("\r", "\n");
    }

    private const string AndroidManifestContents = @"<?xml version=""1.0"" encoding=""utf-8""?>
<manifest xmlns:android=""http://schemas.android.com/apk/res/android""
          xmlns:tools=""http://schemas.android.com/tools"">
    <uses-permission android:name=""android.permission.INTERNET"" />
    <uses-permission android:name=""android.permission.RECORD_AUDIO"" />
    <uses-permission android:name=""com.oculus.permission.USE_ANCHOR_API"" />
    <uses-permission android:name=""com.oculus.permission.USE_SCENE"" />
    <uses-permission android:name=""com.oculus.permission.HAND_TRACKING"" />

    <uses-feature android:name=""android.hardware.microphone"" android:required=""false"" />
    <uses-feature android:name=""android.hardware.vr.headtracking"" android:required=""true"" android:version=""1"" />
    <uses-feature android:name=""oculus.software.handtracking"" android:required=""false"" />
    <uses-feature android:name=""com.oculus.feature.PASSTHROUGH"" android:required=""true"" />

    <application>
        <meta-data android:name=""com.oculus.supportedDevices"" android:value=""quest3"" />
        <activity android:name=""com.unity3d.player.UnityPlayerActivity""
                  android:theme=""@style/UnityThemeSelector""
                  android:exported=""true"">
            <intent-filter>
                <action android:name=""android.intent.action.MAIN"" />
                <category android:name=""android.intent.category.LAUNCHER"" />
                <category android:name=""com.oculus.intent.category.VR"" />
            </intent-filter>
            <meta-data android:name=""unityplayer.UnityActivity"" android:value=""true"" />
        </activity>
    </application>
</manifest>
";

    private const string GradlePropertiesContents = @"org.gradle.jvmargs=-Xmx**JVM_HEAP_SIZE**M
org.gradle.parallel=true
unityStreamingAssets=**STREAMING_ASSETS**
**ADDITIONAL_PROPERTIES**
";
}

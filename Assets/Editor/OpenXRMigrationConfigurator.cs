using System.IO;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

public static class OpenXRMigrationConfigurator
{
    private const string OpenXRLoaderTypeName = "UnityEngine.XR.OpenXR.OpenXRLoader";

    private static readonly string[] AndroidOpenXRFeatures =
    {
        "com.unity.openxr.feature.metaquest",
        "com.unity.openxr.feature.input.oculustouch",
        "com.unity.openxr.feature.input.metaquestplus",
        "com.unity.openxr.feature.input.handtracking",
        "com.unity.openxr.feature.input.metahandtrackingaim"
    };

    [MenuItem("GeoXplorer/XR/Configure OpenXR Migration")]
    public static void ConfigureOpenXRMigration()
    {
        ConfigureAndroidOpenXRPlayerSettings();
        ConfigureBuildTarget(BuildTargetGroup.Android, AndroidOpenXRFeatures);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void ConfigureAndroidOpenXRPlayerSettings()
    {
        QuestAndroidStoreSettingsConfigurator.ApplyCoreAndroidPlayerSettings();
    }

    private static void ConfigureBuildTarget(BuildTargetGroup buildTargetGroup, string[] featureIds)
    {
        XRGeneralSettingsPerBuildTarget buildTargetSettings = GetOrCreateBuildTargetSettings();

        if (!buildTargetSettings.HasSettingsForBuildTarget(buildTargetGroup))
        {
            buildTargetSettings.CreateDefaultSettingsForBuildTarget(buildTargetGroup);
        }

        if (!buildTargetSettings.HasManagerSettingsForBuildTarget(buildTargetGroup))
        {
            buildTargetSettings.CreateDefaultManagerSettingsForBuildTarget(buildTargetGroup);
        }

        XRGeneralSettings generalSettings = buildTargetSettings.SettingsForBuildTarget(buildTargetGroup);
        generalSettings.InitManagerOnStart = true;
        EditorUtility.SetDirty(generalSettings);

        XRPackageMetadataStore.AssignLoader(generalSettings.AssignedSettings, OpenXRLoaderTypeName, buildTargetGroup);

        FeatureHelpers.RefreshFeatures(buildTargetGroup);
        OpenXRSettings openXRSettings = OpenXRSettings.GetSettingsForBuildTargetGroup(buildTargetGroup);
        if (openXRSettings == null)
        {
            Debug.LogWarningFormat(
                "OpenXR settings for {0} were not created. Install that Unity build support module, then rerun GeoXplorer/XR/Configure OpenXR Migration.",
                buildTargetGroup);
            return;
        }

        foreach (string featureId in featureIds)
        {
            EnableFeature(buildTargetGroup, featureId);
        }
    }

    private static XRGeneralSettingsPerBuildTarget GetOrCreateBuildTargetSettings()
    {
        XRGeneralSettingsPerBuildTarget buildTargetSettings = null;
        EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out buildTargetSettings);
        if (buildTargetSettings != null)
        {
            return buildTargetSettings;
        }

        string[] guids = AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            buildTargetSettings = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(path);
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, buildTargetSettings, true);
            return buildTargetSettings;
        }

        const string settingsFolder = "Assets/XR/Settings";
        Directory.CreateDirectory(settingsFolder);

        buildTargetSettings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
        AssetDatabase.CreateAsset(buildTargetSettings, settingsFolder + "/XRGeneralSettingsPerBuildTarget.asset");
        EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, buildTargetSettings, true);
        return buildTargetSettings;
    }

    private static void EnableFeature(BuildTargetGroup buildTargetGroup, string featureId)
    {
        OpenXRFeature feature = FeatureHelpers.GetFeatureWithIdForBuildTarget(buildTargetGroup, featureId);
        if (feature == null)
        {
            Debug.LogWarningFormat("OpenXR feature '{0}' was not available for {1}.", featureId, buildTargetGroup);
            return;
        }

        feature.enabled = true;
        EditorUtility.SetDirty(feature);
    }
}

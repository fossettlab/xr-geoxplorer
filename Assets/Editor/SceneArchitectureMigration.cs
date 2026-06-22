using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneArchitectureMigration
{
    private const string PrefabFolder = "Assets/Prefabs/PlatformRoot";
    private const string BasePrefabPath = PrefabFolder + "/PlatformRoot.prefab";
    private const string QuestPrefabPath = PrefabFolder + "/PlatformRoot.Quest3.prefab";
    private const string MobilePrefabPath = PrefabFolder + "/PlatformRoot.Mobile.prefab";
    private const string GeoXSharedScenePath = "Assets/Scenes/GeoXShared.unity";
    private const string MobileScenePath = "Assets/Scenes/_legacy/MobileMRTK.unity";

    private static readonly HashSet<string> GeneratedMrtkServiceRootNames = new HashSet<string>
    {
        "DefaultRaycastProvider",
        "FocusProvider",
        "HandJointService",
        "InputPlaybackService",
        "InputRecordingService",
        "InputSimulationService",
        "MixedRealityBoundarySystem",
        "MixedRealityCameraSystem",
        "MixedRealityDiagnosticsSystem",
        "MixedRealityInputSystem",
        "MixedRealitySpatialAwarenessSystem",
        "MixedRealityTeleportSystem",
        "OpenVRDeviceManager",
        "UnityJoystickManager",
        "UnityTouchDeviceManager",
        "WindowsDictationInputProvider",
        "WindowsMixedRealityDeviceManager",
        "WindowsMixedRealityEyeGazeDataProvider",
        "WindowsSpeechInputProvider"
    };

    [MenuItem("GeoXplorer/Scene Architecture/Rebuild PlatformRoot Prefabs")]
    public static void RebuildPlatformRootPrefabs()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        EnsureBasePrefab();

        RebuildVariantFromScene(MobilePrefabPath, MobileScenePath, "PlatformRoot.Mobile");

        // Quest starts from the mobile AR hierarchy until later Quest/OpenXR tickets replace the runtime rig.
        RebuildVariantFromScene(QuestPrefabPath, MobileScenePath, "PlatformRoot.Quest3");

        RebuildCanonicalScene();
        ConfigureBuildSettings();
        ValidateSceneArchitecture();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(GeoXSharedScenePath, OpenSceneMode.Single);
        Debug.Log("PlatformRoot prefab variants rebuilt from legacy reference scenes.");
    }

    [MenuItem("GeoXplorer/Scene Architecture/Validate Scene Architecture")]
    public static void ValidateSceneArchitecture()
    {
        GameObject basePrefab = LoadRequiredPrefab(BasePrefabPath);
        GameObject questPrefab = LoadRequiredPrefab(QuestPrefabPath);
        GameObject mobilePrefab = LoadRequiredPrefab(MobilePrefabPath);

        ValidatePrefabVariant(questPrefab, basePrefab, QuestPrefabPath);
        ValidatePrefabVariant(mobilePrefab, basePrefab, MobilePrefabPath);
        ValidateNoGeneratedMrtkServices(questPrefab, QuestPrefabPath);
        ValidateNoGeneratedMrtkServices(mobilePrefab, MobilePrefabPath);

        Scene scene = EditorSceneManager.OpenScene(GeoXSharedScenePath, OpenSceneMode.Single);
        GameObject[] sceneRoots = scene.GetRootGameObjects();
        if (sceneRoots.Length != 1 || sceneRoots[0].name != "PlatformBootstrapper")
        {
            throw new System.InvalidOperationException(
                "GeoXShared.unity must contain only the PlatformBootstrapper root after migration.");
        }

        PlatformBootstrapper bootstrapper = sceneRoots[0].GetComponent<PlatformBootstrapper>();
        if (bootstrapper == null)
        {
            throw new System.InvalidOperationException("PlatformBootstrapper component is missing from GeoXShared.unity.");
        }

        SerializedObject serializedBootstrapper = new SerializedObject(bootstrapper);
        ValidateObjectReference(serializedBootstrapper, "quest3Prefab", QuestPrefabPath);
        ValidateObjectReference(serializedBootstrapper, "mobilePrefab", MobilePrefabPath);

        if (EditorBuildSettings.scenes.Length != 1 ||
            EditorBuildSettings.scenes[0].path != GeoXSharedScenePath ||
            !EditorBuildSettings.scenes[0].enabled)
        {
            throw new System.InvalidOperationException("Build settings must contain only enabled GeoXShared.unity.");
        }

        Debug.Log("Scene architecture validation passed.");
    }

    private static void EnsureBasePrefab()
    {
        if (!Directory.Exists(PrefabFolder))
        {
            Directory.CreateDirectory(PrefabFolder);
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(BasePrefabPath) != null)
        {
            return;
        }

        GameObject root = new GameObject("PlatformRoot");
        PrefabUtility.SaveAsPrefabAsset(root, BasePrefabPath);
        Object.DestroyImmediate(root);
    }

    private static void RebuildVariantFromScene(string prefabPath, string scenePath, string rootName)
    {
        GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BasePrefabPath);
        if (basePrefab == null)
        {
            Debug.LogError("Missing base PlatformRoot prefab at " + BasePrefabPath);
            return;
        }

        if (!File.Exists(scenePath))
        {
            Debug.LogError("Missing legacy scene at " + scenePath);
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        GameObject variantRoot = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
        variantRoot.name = rootName;

        foreach (GameObject sourceRoot in scene.GetRootGameObjects())
        {
            if (ShouldSkipPlatformRootCopy(sourceRoot))
            {
                continue;
            }

            if (sourceRoot == variantRoot)
            {
                continue;
            }

            GameObject copy = Object.Instantiate(sourceRoot, variantRoot.transform);
            copy.name = sourceRoot.name;
        }

        DisableNestedDontDestroyOnLoadFlags(variantRoot);

        if (File.Exists(prefabPath))
        {
            File.Delete(prefabPath);
        }

        PrefabUtility.SaveAsPrefabAsset(variantRoot, prefabPath);
        AssetDatabase.ImportAsset(prefabPath, ImportAssetOptions.ForceSynchronousImport);
        Object.DestroyImmediate(variantRoot);
    }

    private static bool ShouldSkipPlatformRootCopy(GameObject sourceRoot)
    {
        if (sourceRoot.name == "MixedRealityToolkit")
        {
            return true;
        }

        return GeneratedMrtkServiceRootNames.Contains(sourceRoot.name);
    }

    private static void DisableNestedDontDestroyOnLoadFlags(GameObject variantRoot)
    {
        MonoBehaviour[] behaviours = variantRoot.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null)
            {
                continue;
            }

            SerializedObject serializedBehaviour = new SerializedObject(behaviour);
            SerializedProperty applyDontDestroyOnLoad = serializedBehaviour.FindProperty("ApplyDontDestroyOnLoad");
            if (applyDontDestroyOnLoad != null && applyDontDestroyOnLoad.propertyType == SerializedPropertyType.Boolean)
            {
                applyDontDestroyOnLoad.boolValue = false;
                serializedBehaviour.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }

    private static void RebuildCanonicalScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject bootstrapperObject = new GameObject("PlatformBootstrapper");
        PlatformBootstrapper bootstrapper = bootstrapperObject.AddComponent<PlatformBootstrapper>();

        SerializedObject serializedBootstrapper = new SerializedObject(bootstrapper);
        serializedBootstrapper.FindProperty("platformOverride").enumValueIndex = 0;
        serializedBootstrapper.FindProperty("quest3Prefab").objectReferenceValue = LoadRequiredPrefab(QuestPrefabPath);
        serializedBootstrapper.FindProperty("mobilePrefab").objectReferenceValue = LoadRequiredPrefab(MobilePrefabPath);
        serializedBootstrapper.FindProperty("platformRootParent").objectReferenceValue = bootstrapperObject.transform;
        serializedBootstrapper.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, GeoXSharedScenePath);
    }

    private static void ConfigureBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(GeoXSharedScenePath, true)
        };
    }

    private static GameObject LoadRequiredPrefab(string prefabPath)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            throw new System.InvalidOperationException("Missing required prefab at " + prefabPath);
        }

        return prefab;
    }

    private static void ValidatePrefabVariant(GameObject variantPrefab, GameObject basePrefab, string prefabPath)
    {
        if (PrefabUtility.GetPrefabAssetType(variantPrefab) != PrefabAssetType.Variant)
        {
            throw new System.InvalidOperationException(prefabPath + " must be a prefab variant.");
        }

        GameObject sourcePrefab = PrefabUtility.GetCorrespondingObjectFromSource(variantPrefab);
        if (sourcePrefab != basePrefab)
        {
            throw new System.InvalidOperationException(prefabPath + " must inherit from " + BasePrefabPath);
        }
    }

    private static void ValidateNoGeneratedMrtkServices(GameObject variantPrefab, string prefabPath)
    {
        Transform[] transforms = variantPrefab.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in transforms)
        {
            if (GeneratedMrtkServiceRootNames.Contains(child.name))
            {
                throw new System.InvalidOperationException(
                    prefabPath + " must not serialize legacy MRTK service root " + child.name +
                    ". Those services register in edit mode and dirty GeoXShared before Play.");
            }
        }
    }

    private static void ValidateObjectReference(SerializedObject serializedObject, string propertyName, string expectedPath)
    {
        Object objectReference = serializedObject.FindProperty(propertyName).objectReferenceValue;
        if (objectReference == null || AssetDatabase.GetAssetPath(objectReference) != expectedPath)
        {
            throw new System.InvalidOperationException(
                "PlatformBootstrapper." + propertyName + " must reference " + expectedPath);
        }
    }
}

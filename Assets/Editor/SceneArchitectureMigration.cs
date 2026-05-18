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
    private const string HoloLensPrefabPath = PrefabFolder + "/PlatformRoot.HoloLens2.prefab";
    private const string MobilePrefabPath = PrefabFolder + "/PlatformRoot.Mobile.prefab";
    private const string GeoXSharedScenePath = "Assets/Scenes/GeoXShared.unity";
    private const string HoloLensScenePath = "Assets/Scenes/_legacy/HoloLens.unity";
    private const string MobileScenePath = "Assets/Scenes/_legacy/MobileMRTK.unity";

    [MenuItem("GeoXplorer/Scene Architecture/Rebuild PlatformRoot Prefabs")]
    public static void RebuildPlatformRootPrefabs()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        EnsureBasePrefab();

        RebuildVariantFromScene(MobilePrefabPath, MobileScenePath, "PlatformRoot.Mobile");
        RebuildVariantFromScene(HoloLensPrefabPath, HoloLensScenePath, "PlatformRoot.HoloLens2");

        // Quest starts from the mobile AR hierarchy until later Quest/OpenXR tickets replace the runtime rig.
        RebuildVariantFromScene(QuestPrefabPath, MobileScenePath, "PlatformRoot.Quest3");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(GeoXSharedScenePath, OpenSceneMode.Single);
        Debug.Log("PlatformRoot prefab variants rebuilt from legacy reference scenes.");
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
            if (sourceRoot == variantRoot)
            {
                continue;
            }

            GameObject copy = Object.Instantiate(sourceRoot, variantRoot.transform);
            copy.name = sourceRoot.name;
        }

        PrefabUtility.SaveAsPrefabAsset(variantRoot, prefabPath);
        Object.DestroyImmediate(variantRoot);
        EditorSceneManager.CloseScene(scene, true);
    }
}

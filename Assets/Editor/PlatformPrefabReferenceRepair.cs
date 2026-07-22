using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Repairs the PlatformBootstrapper prefab references in GeoXShared.
///
/// The platform roots (Quest3 / Mobile / HoloLens2) are prefab VARIANTS of
/// PlatformRoot.prefab. A previous manual fileID renumbering of their internal
/// PrefabInstance desynced the scene's serialized references: they half-resolve
/// in the Editor but come back null in a player build, so PlatformBootstrapper
/// logs "no prefab assigned" and never spawns the platform root.
///
/// Re-assigning the fields through Unity's AssetDatabase rewrites correct,
/// build-safe references.
/// </summary>
public static class PlatformPrefabReferenceRepair
{
    private const string ScenePath = "Assets/Scenes/GeoXShared.unity";
    private const string Quest3PrefabPath = "Assets/Resources/PlatformRoot/PlatformRoot.Quest3.prefab";
    private const string HoloLens2PrefabPath = "Assets/Resources/PlatformRoot/PlatformRoot.HoloLens2.prefab";
    private const string MobilePrefabPath = "Assets/Resources/PlatformRoot/PlatformRoot.Mobile.prefab";

    [MenuItem("GeoX/Repair Platform Prefab References")]
    public static void Repair()
    {
        GameObject quest3 = AssetDatabase.LoadAssetAtPath<GameObject>(Quest3PrefabPath);
        GameObject holoLens2 = AssetDatabase.LoadAssetAtPath<GameObject>(HoloLens2PrefabPath);
        GameObject mobile = AssetDatabase.LoadAssetAtPath<GameObject>(MobilePrefabPath);

        if (quest3 == null || holoLens2 == null || mobile == null)
        {
            Debug.LogErrorFormat(
                "PlatformPrefabReferenceRepair could not load one or more prefabs (quest3={0}, holoLens2={1}, mobile={2}).",
                quest3 != null,
                holoLens2 != null,
                mobile != null);
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        bool openedScene = false;
        if (!scene.isLoaded || scene.path != ScenePath)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            openedScene = true;
        }

        PlatformBootstrapper bootstrapper = FindBootstrapper(scene);
        if (bootstrapper == null)
        {
            Debug.LogError("PlatformPrefabReferenceRepair could not find a PlatformBootstrapper in the scene.");
            return;
        }

        SerializedObject serialized = new SerializedObject(bootstrapper);
        AssignReference(serialized, "quest3Prefab", quest3);
        AssignReference(serialized, "holoLens2Prefab", holoLens2);
        AssignReference(serialized, "mobilePrefab", mobile);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(bootstrapper);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.LogFormat(
            "PlatformPrefabReferenceRepair reassigned platform prefab references and saved {0}{1}.",
            ScenePath,
            openedScene ? " (scene was opened by the repair tool)" : string.Empty);
    }

    private static void AssignReference(SerializedObject serialized, string propertyName, GameObject prefab)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogWarningFormat("PlatformPrefabReferenceRepair: property '{0}' not found on PlatformBootstrapper.", propertyName);
            return;
        }

        property.objectReferenceValue = prefab;
    }

    private static PlatformBootstrapper FindBootstrapper(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            PlatformBootstrapper found = root.GetComponentInChildren<PlatformBootstrapper>(true);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}

using UnityEngine;
using UnityEngine.XR;

public class PlatformBootstrapper : MonoBehaviour
{
    public enum PlatformVariant
    {
        Auto,
        Quest3,
        Mobile
    }

    [SerializeField]
    private PlatformVariant platformOverride = PlatformVariant.Auto;

    [SerializeField]
    private GameObject quest3Prefab;

    [SerializeField]
    private GameObject mobilePrefab;

    [SerializeField]
    private Transform platformRootParent;

    private GameObject instantiatedPlatformRoot;

    public PlatformVariant ActiveVariant { get; private set; }

    public GameObject InstantiatedPlatformRoot => instantiatedPlatformRoot;

    private void Awake()
    {
        ActiveVariant = platformOverride == PlatformVariant.Auto
            ? ResolvePlatformVariant()
            : platformOverride;

        GameObject prefab = ResolvePrefab(ActiveVariant);
        if (prefab == null)
        {
            Debug.LogWarningFormat(
                this,
                "PlatformBootstrapper has no prefab assigned for {0}. Scene platform root was not instantiated.",
                ActiveVariant);
            return;
        }

        // Instantiate under a temporary inactive holder so nested MixedRealityToolkit
        // Awake (and its "must be root" assert) does not run before we remove it.
        // GeoXShared already has a scene-root MixedRealityToolkit.
        GameObject spawnHolder = new GameObject("PlatformSpawnHolder");
        spawnHolder.SetActive(false);
        instantiatedPlatformRoot = Instantiate(prefab, spawnHolder.transform);
        instantiatedPlatformRoot.name = "PlatformRoot." + ActiveVariant;
        RemoveNestedMixedRealityToolkit(instantiatedPlatformRoot.transform);
        instantiatedPlatformRoot.transform.SetParent(null, true);
        Destroy(spawnHolder);
        instantiatedPlatformRoot.SetActive(true);

        Debug.LogFormat(
            this,
            "PlatformBootstrapper spawned {0} (variant={1}, device='{2}', xrDevice='{3}').",
            instantiatedPlatformRoot.name,
            ActiveVariant,
            SystemInfo.deviceModel,
            XRSettings.loadedDeviceName);
    }

    /// <summary>
    /// GeoXShared already has a scene-root MixedRealityToolkit. The platform prefab
    /// also embeds one (often named "MixedRealityToolkit (Inactive)") nested under
    /// PlatformRoot, which trips MRTK's "should not be parented" assert. Drop it.
    /// </summary>
    private static void RemoveNestedMixedRealityToolkit(Transform platformRoot)
    {
        if (platformRoot == null)
        {
            return;
        }

        // Collect first — destroying while iterating children is unsafe.
        var toDestroy = new System.Collections.Generic.List<GameObject>();
        CollectNestedMixedRealityToolkit(platformRoot, platformRoot, toDestroy);
        for (int i = 0; i < toDestroy.Count; i++)
        {
            DestroyImmediate(toDestroy[i]);
        }
    }

    private static void CollectNestedMixedRealityToolkit(
        Transform platformRoot,
        Transform current,
        System.Collections.Generic.List<GameObject> toDestroy)
    {
        if (current != platformRoot &&
            current.name.StartsWith("MixedRealityToolkit") &&
            current.parent != null)
        {
            toDestroy.Add(current.gameObject);
            return;
        }

        for (int i = 0; i < current.childCount; i++)
        {
            CollectNestedMixedRealityToolkit(platformRoot, current.GetChild(i), toDestroy);
        }
    }

    private PlatformVariant ResolvePlatformVariant()
    {
        switch (Platform.Current)
        {
            case PlatformId.Quest:
                return PlatformVariant.Quest3;
            case PlatformId.Editor:
            case PlatformId.LegacyHoloLens2:
            case PlatformId.Mobile:
            case PlatformId.Other:
            default:
                return PlatformVariant.Mobile;
        }
    }

    private GameObject GetPrefabForVariant(PlatformVariant variant)
    {
        switch (variant)
        {
            case PlatformVariant.Quest3:
                return quest3Prefab;
            case PlatformVariant.Mobile:
                return mobilePrefab;
            default:
                return null;
        }
    }

    /// <summary>
    /// The platform roots are prefab variants whose serialized scene references have
    /// proven fragile (they resolve to null in player builds). Loading from Resources
    /// by path is reference-stable, so prefer it and fall back to the inspector slot.
    /// </summary>
    private GameObject ResolvePrefab(PlatformVariant variant)
    {
        string resourcePath = GetResourcePathForVariant(variant);
        if (!string.IsNullOrEmpty(resourcePath))
        {
            GameObject fromResources = Resources.Load<GameObject>(resourcePath);
            if (fromResources != null)
            {
                return fromResources;
            }

            Debug.LogWarningFormat(
                this,
                "PlatformBootstrapper could not load '{0}' from Resources; falling back to inspector reference.",
                resourcePath);
        }

        return GetPrefabForVariant(variant);
    }

    private static string GetResourcePathForVariant(PlatformVariant variant)
    {
        switch (variant)
        {
            case PlatformVariant.Quest3:
                return "PlatformRoot/PlatformRoot.Quest3";
            case PlatformVariant.Mobile:
                return "PlatformRoot/PlatformRoot.Mobile";
            default:
                return null;
        }
    }
}

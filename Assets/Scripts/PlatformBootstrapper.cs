using UnityEngine;

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

    private void Awake()
    {
        RemoteConfigLoader.EnsureLoaded();

        ActiveVariant = platformOverride == PlatformVariant.Auto
            ? ResolvePlatformVariant()
            : platformOverride;

        GameObject prefab = GetPrefabForVariant(ActiveVariant);
        if (prefab == null)
        {
            Debug.LogWarningFormat(
                this,
                "PlatformBootstrapper has no prefab assigned for {0}. Scene platform root was not instantiated.",
                ActiveVariant);
            return;
        }

        Transform parent = platformRootParent != null ? platformRootParent : transform;
        instantiatedPlatformRoot = Instantiate(prefab, parent);
        instantiatedPlatformRoot.name = prefab.name;
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
}

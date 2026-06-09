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

    private void Awake()
    {
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
        RuntimePlatform platform = Application.platform;

        if (platform == RuntimePlatform.Android)
        {
            if (IsQuestRuntime())
            {
                return PlatformVariant.Quest3;
            }

            return PlatformVariant.Mobile;
        }

        if (platform == RuntimePlatform.IPhonePlayer)
        {
            return PlatformVariant.Mobile;
        }

        return PlatformVariant.Mobile;
    }

    private bool IsQuestRuntime()
    {
        string deviceModel = SystemInfo.deviceModel.ToLowerInvariant();
        if (deviceModel.Contains("quest") ||
            deviceModel.Contains("oculus") ||
            deviceModel.Contains("meta"))
        {
            return true;
        }

        string loadedDeviceName = XRSettings.loadedDeviceName;
        return !string.IsNullOrEmpty(loadedDeviceName) &&
               loadedDeviceName != "None" &&
               loadedDeviceName.ToLowerInvariant().Contains("oculus");
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

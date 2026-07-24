using UnityEngine;

/// <summary>
/// Selects Dev / Staging / Prod <see cref="RemoteConfig"/> from scripting defines
/// GEOX_DEV / GEOX_STAGING / GEOX_PROD (Editor default: Dev).
/// </summary>
public static class RemoteConfigLoader
{
    private const string CatalogResourcePath = "RemoteConfig/RemoteConfigCatalog";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureLoaded();
    }

    public static RemoteConfig EnsureLoaded()
    {
        if (RemoteConfig.Loaded != null)
        {
            return RemoteConfig.Loaded;
        }

        RemoteConfigCatalog catalog = Resources.Load<RemoteConfigCatalog>(CatalogResourcePath);
        if (catalog == null)
        {
            Debug.LogError("RemoteConfigLoader could not load Resources/" + CatalogResourcePath + ".");
            return null;
        }

        RemoteConfig selected = SelectFromCatalog(catalog);
        if (selected == null)
        {
            Debug.LogError("RemoteConfigLoader selected a null RemoteConfig for the active environment.");
            return null;
        }

        RemoteConfig.Current = selected;
        Debug.Log("RemoteConfigLoader active environment: " + selected.EnvironmentName);
        return selected;
    }

    private static RemoteConfig SelectFromCatalog(RemoteConfigCatalog catalog)
    {
#if GEOX_PROD
#if UNITY_EDITOR
        RemoteConfig localProd = UnityEditor.AssetDatabase.LoadAssetAtPath<RemoteConfig>(
            "Assets/Settings/Config/RemoteConfig.Prod.local.asset");
        if (localProd != null)
        {
            return localProd;
        }
#endif
        return catalog.Prod != null ? catalog.Prod : catalog.Staging;
#elif GEOX_STAGING
        return catalog.Staging != null ? catalog.Staging : catalog.Dev;
#elif GEOX_DEV
        return catalog.Dev;
#else
        // Editor / unmarked player builds default to Dev.
        return catalog.Dev != null ? catalog.Dev : catalog.Staging;
#endif
    }
}

using UnityEngine;

/// <summary>
/// Per-environment URL and endpoint routing for GeoXplorer.
/// This is environment routing / friction, not cryptographic auth.
/// Values ship in the client and are extractable from the APK.
/// </summary>
[CreateAssetMenu(fileName = "RemoteConfig", menuName = "GeoXplorer/Remote Config")]
public class RemoteConfig : ScriptableObject
{
    [SerializeField] private string environmentName = "dev";
    [SerializeField] private string assetBundleBaseUrl = "";
    [SerializeField] private string thumbnailsBaseUrl = "";
    [SerializeField] private string featuredModelsConfigUrl = "";
    [SerializeField] private string storageAccountName = "";
    [SerializeField] private string sasEndpointBaseUrl = "";
    [SerializeField] private string sasApiKey = "";
    [SerializeField] private string straboSpotSearchUrl = "";
    [SerializeField] private string firebaseAnchorsUrl = "";

    public string EnvironmentName => environmentName;
    public string AssetBundleBaseUrl => assetBundleBaseUrl;
    public string ThumbnailsBaseUrl => thumbnailsBaseUrl;
    public string FeaturedModelsConfigUrl => featuredModelsConfigUrl;
    public string StorageAccountName => storageAccountName;
    public string SasEndpointBaseUrl => sasEndpointBaseUrl;
    public string SasApiKey => sasApiKey;
    public string StraboSpotSearchUrl => straboSpotSearchUrl;
    public string FirebaseAnchorsUrl => firebaseAnchorsUrl;

    private static RemoteConfig current;

    /// <summary>Active config selected by <see cref="RemoteConfigLoader"/>.</summary>
    public static RemoteConfig Current
    {
        get
        {
            if (current == null)
            {
                RemoteConfigLoader.EnsureLoaded();
            }

            return current;
        }
        internal set { current = value; }
    }

    internal static RemoteConfig Loaded => current;

    public string BuildContainerListUrl(string platformContainer, string prefix)
    {
        return TrimSlash(assetBundleBaseUrl) + "/" + platformContainer
            + "?restype=container&comp=list&include=metadata&prefix=" + prefix;
    }

    public string BuildAssetBundleUri(string platformContainer, string bundleName)
    {
        return TrimSlash(assetBundleBaseUrl) + "/" + platformContainer + "/" + bundleName;
    }

    public string BuildThumbnailUrl(string relativePath)
    {
        return TrimSlash(thumbnailsBaseUrl) + "/" + relativePath.TrimStart('/');
    }

    private static string TrimSlash(string url)
    {
        return string.IsNullOrEmpty(url) ? string.Empty : url.TrimEnd('/');
    }
}

using NUnit.Framework;
using UnityEngine;

// EditMode tests for RemoteConfig's URL builders. These lock in the exact
// endpoints the #25 URL-centralization was verified to preserve, so a future
// change that alters a produced URL fails CI without needing a headset.
public class RemoteConfigTests
{
    const string BlobRoot = "https://haringerverdiag.blob.core.windows.net";
    const string ThumbRoot = "https://haringerverdiag.blob.core.windows.net/thumbnails";

    static RemoteConfig Make(string bundleBase, string thumbBase)
    {
        var cfg = ScriptableObject.CreateInstance<RemoteConfig>();
        // Fields are [SerializeField] private; JsonUtility overwrites them by name.
        JsonUtility.FromJsonOverwrite(
            "{\"assetBundleBaseUrl\":\"" + bundleBase + "\",\"thumbnailsBaseUrl\":\"" + thumbBase + "\"}",
            cfg);
        return cfg;
    }

    [Test]
    public void BuildAssetBundleUri_composes_platform_and_bundle()
    {
        var cfg = Make(BlobRoot, "");
        Assert.AreEqual(BlobRoot + "/android/model.bundle",
            cfg.BuildAssetBundleUri("android", "model.bundle"));
        Object.DestroyImmediate(cfg);
    }

    [Test]
    public void BuildAssetBundleUri_trims_trailing_slash_on_base()
    {
        var cfg = Make(BlobRoot + "/", "");
        Assert.AreEqual(BlobRoot + "/android/model.bundle",
            cfg.BuildAssetBundleUri("android", "model.bundle"));
        Object.DestroyImmediate(cfg);
    }

    [Test]
    public void BuildContainerListUrl_matches_azure_list_query()
    {
        var cfg = Make(BlobRoot, "");
        Assert.AreEqual(
            BlobRoot + "/android?restype=container&comp=list&include=metadata&prefix=geoxplorer-outcrop",
            cfg.BuildContainerListUrl("android", "geoxplorer-outcrop"));
        Object.DestroyImmediate(cfg);
    }

    [Test]
    public void BuildThumbnailUrl_joins_thumbnails_root_and_path()
    {
        var cfg = Make("", ThumbRoot);
        Assert.AreEqual(ThumbRoot + "/outcrop/a.png",
            cfg.BuildThumbnailUrl("outcrop/a.png"));
        Object.DestroyImmediate(cfg);
    }

    [Test]
    public void BuildThumbnailUrl_trims_leading_slash_on_path()
    {
        var cfg = Make("", ThumbRoot);
        Assert.AreEqual(ThumbRoot + "/outcrop/a.png",
            cfg.BuildThumbnailUrl("/outcrop/a.png"));
        Object.DestroyImmediate(cfg);
    }
}

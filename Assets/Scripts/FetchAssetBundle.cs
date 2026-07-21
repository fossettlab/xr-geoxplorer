using System.Collections;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class FetchAssetBundle : MonoBehaviour
{
    /// <summary>
    /// Handles the download of the AssetBundle from Azure Storage. Works on all platforms. The model is also scaled to a default extent of 1m. Original scales can be applied by the tooltip menu that is attached to the downloaded asset.
    /// </summary>


    //Public variables
    public string storageAccountName;
    public string containerName;
    public string prefabName;
    public string bundleName;
    public string modelName;
    public Material mobileMaterial;
    public GameObject downloadIndicatorText;
    public GameObject newPrefab;

    public Vector3 resetPosition;
    public Vector3 resetEulerAngles;
    public Vector3 resetScale;

    //Private variables
    bool requestStarted = false;
    bool loadingStarted = false;
    string buttonName;
    Transform objectStage;
    UnityWebRequest request;
    LobbyManager lobbyManager;

    //Wrapper method to download assetbundles
    //public void FetchBundle()
    void Start()
    {
        objectStage = this.transform;
        lobbyManager = LobbyManager.Instance;
        StartCoroutine(DownloadAssetBundle());
    }

    //Coroutine to download from Azure
    IEnumerator DownloadAssetBundle()
    {
        if (storageAccountName == "")
        {
            object[] pV = GetComponent<PhotonView>().InstantiationData;
            storageAccountName = pV[0].ToString();
            containerName = pV[1].ToString();
            prefabName = pV[2].ToString();
            bundleName = pV[3].ToString();
        }

        if (!this.gameObject.name.Contains("_"))
        {
            this.gameObject.name = "AssetBundleLoader_" + prefabName;
        }

#if UNITY_IOS
        string extensionName = "ios";
#elif UNITY_ANDROID
        string extensionName = "android";
#else
        string extensionName = "android";
#endif


        string uri = "https://" + storageAccountName + ".blob.core.windows.net/" + extensionName + "/" + bundleName;  // + containerName + "/" + bundleName;
        print(uri);
        lobbyManager.ShowDownloadState();
        requestStarted = true;
        request = UnityWebRequestAssetBundle.GetAssetBundle(uri, 0);
        yield return request.SendWebRequest();
        requestStarted = false;
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Asset bundle download failed: {request.error} ({uri})");
            lobbyManager.HideDownloadState();
            yield break;
        }

        print(prefabName);
        loadingStarted = true;
        AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(request);
        if (bundle == null)
        {
            Debug.LogError($"Asset bundle content was null after download ({uri})");
            loadingStarted = false;
            lobbyManager.HideDownloadState();
            yield break;
        }
        var assetLoadRequest = bundle.LoadAssetAsync<GameObject>(prefabName);
        yield return assetLoadRequest;
        loadingStarted = false;
        lobbyManager.HideDownloadState();

        GameObject prefab = assetLoadRequest.asset as GameObject;
        if (prefab == null)
        {
            Debug.LogError($"Prefab '{prefabName}' was not found in asset bundle ({uri})");
            yield break;
        }

        newPrefab = Instantiate(prefab, objectStage);
        newPrefab.name = prefabName;
        newPrefab.tag = Tags.AssetBundle;


        if (prefabName.Contains(".IMG.blend"))
        {
            newPrefab.GetComponent<Renderer>().material.shader = Shader.Find("Custom/AlphaBlendTransition");
            Texture2D satelliteTexture = bundle.LoadAsset<Texture2D>(prefabName.Replace("IMG.blend","sb.jpg"));
            Texture2D colorAltimetryTexture = bundle.LoadAsset<Texture2D>(prefabName.Replace("IMG.blend", "cb.jpg"));
            newPrefab.GetComponent<Renderer>().material.SetTexture("_BaseTexture",satelliteTexture);
            newPrefab.GetComponent<Renderer>().material.SetTexture("_OverlayTexture", colorAltimetryTexture);
#if UNITY_IOS || UNITY_ANDROID
            TableAnchor.instance.GetComponent<PlanetManager>().geoSlider.SetActive(true);
            SceneQueries.ByName("GeoText (TMP)").GetComponent<TextMeshProUGUI>().text = "Color Altimetry";
#endif
        }

        //Normalize the scale of the model
        if (this.GetComponent<PhotonView>().IsMine)
        {
            this.transform.localEulerAngles = Vector3.zero;   //set the rotation to be zero so that if the model is correctly oriented it will match with the map tiles.


            Bounds newBounds = GetChildRendererBounds(this.gameObject);
            float dominantExtent = Mathf.Max(newBounds.size.x, newBounds.size.z);
            if (dominantExtent > Mathf.Epsilon)
            {
                this.transform.localScale /= (dominantExtent / 2f);
            }
            else
            {
                Debug.LogWarning($"Skipping scale normalization for {prefabName}: model has no measurable renderer bounds.");
            }

            resetPosition = this.transform.localPosition;
            resetEulerAngles = this.transform.localEulerAngles;
            resetScale = this.transform.localScale;
        }
        bundle.Unload(false);
    }

    //Delete the downloaded bundle
    public void DeleteBundle()
    {
        Destroy(SceneQueries.ByName(prefabName));
    }

    //UI to show download progress on the toggle button
    private void Update()
    {
        if (requestStarted)
        {
            lobbyManager.downloadIndicatorText.GetComponent<TextMeshProUGUI>().text = (request.downloadProgress * 100).ToString("F0") + "%";
        }

        if (loadingStarted)
        {
            lobbyManager.downloadIndicatorText.GetComponent<TextMeshProUGUI>().text = "Loading Model";
        }
    }


    Bounds GetChildRendererBounds(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();

        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1, ni = renderers.Length; i < ni; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            return bounds;
        }
        else
        {
            return new Bounds();
        }
    }
}

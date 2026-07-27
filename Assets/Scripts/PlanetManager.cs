using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using Photon;
using Photon.Pun;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;

public class PlanetManager : MonoBehaviourPun, IMixedRealityPointerHandler , IPunObservable
{
    /// <summary>
    /// Controls the interactions pertaining to downloaded assets. This includes the creation of shared tooltip-type menus on both MobileAR and HoloLens. Menus are created through PunRPC which creates a menu on each connected device. However only the instantiator of the model can delete or manipulate the model. It perhaps may be better UX to have some menus be only viewable on one device at a time?
    ///
    /// TODO could be made a Singleton
    /// </summary>


    public GameObject tileStage;
    public GameObject activePlanet;
    public GameObject GoToTooltip;
    public GameObject backButton;
    public GameObject hideTilesButton;
    public GameObject geoSlider;
    public GameObject ZoomInButton;
    public GameObject ZoomOutButton;

    public List<ObjectCoordinates> objectCoordinates = new List<ObjectCoordinates>();
    public List<StraboDatasetFeature> spotCoordinates = new List<StraboDatasetFeature>();

    RaycastHit hit;
    public float hitLat;
    public float hitLon;

    bool tapped;
    bool tilesActive;

    public void ListenForCLicks()
    {
        CoreServices.InputSystem?.RegisterHandler<IMixedRealityPointerHandler>(this);
    }

    public void StopListenForClicks()
    {
        CoreServices.InputSystem?.UnregisterHandler<IMixedRealityPointerHandler>(this);
    }


    // Start is called before the first frame update
    void Start()
    {
        tapped = false;
        tilesActive = true;
    }

    // Update is called once per frame
    void Update()
    {


#if UNITY_EDITOR || UNITY_IOS || UNITY_ANDROID

#if UNITY_EDITOR
        bool pointerAction = GeoXInput.PrimaryPointerPressedThisFrame;
#elif UNITY_IOS || UNITY_ANDROID
        bool pointerAction = GeoXInput.PrimaryTouchReleasedThisFrame;
#endif

        if (pointerAction && !EventSystem.current.IsPointerOverGameObject())
        {
            Ray ray = Camera.main.ScreenPointToRay(GeoXInput.PointerPosition);


            if (Physics.Raycast(ray, out hit) && hit.collider.gameObject.tag == Tags.Tappable)
            {
                Vector3 hitPosition = hit.transform.InverseTransformPoint(hit.point);
                hitLon = -Mathf.Atan2(hitPosition.x, hitPosition.z) * Mathf.Rad2Deg;
                hitLat = (Mathf.Acos(hitPosition.y) - (Mathf.PI / 2)) * -Mathf.Rad2Deg;
                //CreateTooltipAtLoc(hitLat, hitLon);
#if UNITY_EDITOR
                PhotonView photonView = PhotonView.Get(this);
                this.photonView.RPC("CreateTooltipAtLoc", RpcTarget.All, hitLat, hitLon, hitPosition, hit.transform.InverseTransformPoint(hit.normal));
#elif UNITY_IOS || UNITY_ANDROID
                PhotonView photonView = PhotonView.Get(this);
                this.photonView.RPC("CreateTooltipAtLoc", RpcTarget.All, hitLat, hitLon, hitPosition, hit.transform.InverseTransformPoint(hit.normal));
#endif
            }
            else if (Physics.Raycast(ray, out hit) && hit.collider.gameObject.tag == Tags.TooltipInteraction)
            {
#if UNITY_EDITOR
                PhotonView photonView = PhotonView.Get(this);
                this.photonView.RPC("GoToTiles", RpcTarget.All, hitLat, hitLon, 9);
#elif UNITY_IOS || UNITY_ANDROID
                PhotonView photonView = PhotonView.Get(this);
                this.photonView.RPC("GoToTiles", RpcTarget.All, hitLat, hitLon, 9);
#endif
            }
            else if (Physics.Raycast(ray, out hit) && hit.collider.gameObject.name == "MainInteractable")
            {
#if UNITY_EDITOR
                hit.collider.gameObject.GetComponentInParent<SpatialTooltipManager>().MenuSwitcher();
#elif UNITY_IOS || UNITY_ANDROID
                hit.collider.gameObject.GetComponentInParent<SpatialTooltipManager>().MenuSwitcher();
#endif

            }
            else if (Physics.Raycast(ray, out hit) && hit.collider.gameObject.name == "RecenterInteractable")
            {
#if UNITY_EDITOR
                PhotonView photonView = PhotonView.Get(this);
                hitLat = hit.collider.gameObject.GetComponentInParent<SpatialTooltipManager>().lat;
                hitLon = hit.collider.gameObject.GetComponentInParent<SpatialTooltipManager>().lon;
                this.photonView.RPC("GoToTiles", RpcTarget.All, hit.collider.gameObject.GetComponentInParent<SpatialTooltipManager>().lat, hit.collider.gameObject.GetComponentInParent<SpatialTooltipManager>().lon, 100);  //100 means that no zooming takes place
#elif UNITY_IOS || UNITY_ANDROID
                PhotonView photonView = PhotonView.Get(this);
                hitLat = hit.collider.gameObject.GetComponentInParent<SpatialTooltipManager>().lat;
                hitLon = hit.collider.gameObject.GetComponentInParent<SpatialTooltipManager>().lon;
                this.photonView.RPC("GoToTiles", RpcTarget.All, hit.collider.gameObject.GetComponentInParent<SpatialTooltipManager>().lat, hit.collider.gameObject.GetComponentInParent<SpatialTooltipManager>().lon, 100);  //100 means that no zooming takes place
#endif
            }
            else if (Physics.Raycast(ray, out hit) && hit.collider.gameObject.name == "GoToInteractable")
            {
#if UNITY_EDITOR
                DownloadButtonInteraction assetBundleDownloader = hit.collider.gameObject.GetComponentInParent<DownloadButtonInteraction>();
                LobbyManager.Instance.CreateInteractableObjects(assetBundleDownloader.storageAccountName, assetBundleDownloader.containerName, assetBundleDownloader.prefabName, assetBundleDownloader.bundleName, assetBundleDownloader.modelName);
#elif UNITY_IOS || UNITY_ANDROID
                DownloadButtonInteraction assetBundleDownloader = hit.collider.gameObject.GetComponentInParent<DownloadButtonInteraction>();
                LobbyManager.Instance.CreateInteractableObjects(assetBundleDownloader.storageAccountName, assetBundleDownloader.containerName, assetBundleDownloader.prefabName, assetBundleDownloader.bundleName, assetBundleDownloader.modelName);
#endif
            }
        }
#endif
    }

    public void OnBack()
    {
        tileStage.SetActive(false);
        activePlanet.SetActive(true);
        GameObject[] toolTips = SceneQueries.WithTag(Tags.GoToTooltip);
        if (toolTips.Length > 0)
        {
            foreach (var tool in toolTips)
            {
                Destroy(tool);
            }
        }

        GameObject[] flags = SceneQueries.WithTag(Tags.Flag);
        if (flags.Length > 0)
        {
            foreach (var flag in flags)
            {
                Destroy(flag);
            }
        }

        GameObject[] primeflags = SceneQueries.WithTag(Tags.FlagPrime);
        if (primeflags.Length > 0)
        {
            foreach (var flag in primeflags)
            {
                Destroy(flag);
            }
        }

        backButton.SetActive(false);
        hideTilesButton.SetActive(false);
        geoSlider.GetComponent<ControlTextureAlpha>().FindTileObjects(0);
        geoSlider.SetActive(false);
        ZoomInButton.SetActive(false);
        ZoomOutButton.SetActive(false);
    }

    [PunRPC]
    public void CreateTooltipAtLoc(float locationHitLat, float locationHitLon, Vector3 localHitPoint, Vector3 localHitNormal)
    {
        GameObject[] toolTips = SceneQueries.WithTag(Tags.GoToTooltip);
        if (toolTips.Length > 0)
        {
            foreach (var tool in toolTips)
            {
                Destroy(tool);
            }
        }

        GameObject newTooltip = Instantiate(GoToTooltip);
        newTooltip.transform.parent = activePlanet.transform;
        newTooltip.transform.localPosition = localHitPoint;

        //newTooltip.transform.localEulerAngles = new Vector3(-(90 - locationHitLat), 180, 0);
        newTooltip.transform.localRotation = Quaternion.FromToRotation(transform.up, localHitNormal) * transform.rotation;
        string locationString = string.Format("Go to:\nLat: {0}\nLon: {1}", locationHitLat.ToString(), locationHitLon.ToString());
        newTooltip.GetComponentInChildren<TextMeshPro>().text = locationString;
        hitLat = locationHitLat;
        hitLon = locationHitLon;
    }

    [PunRPC]
    public void GoToTiles(float locationHitLat, float locationHitLon, int zoomLevel)
    {
        activePlanet.SetActive(false);
        tileStage.SetActive(true);
        backButton.SetActive(true);
        hideTilesButton.SetActive(true);
        geoSlider.SetActive(true);
#if UNITY_IOS || UNITY_ANDROID
        SceneQueries.ByName("GeoText (TMP)").GetComponent<TextMeshProUGUI>().text = "Geology";
#endif
        ZoomInButton.SetActive(true);
        ZoomOutButton.SetActive(true);

        if (zoomLevel != 100)
        {
            tileStage.GetComponent<MapBuilder>().ZoomLevel = zoomLevel;
        }
        tileStage.GetComponent<MapBuilder>().Latitude = locationHitLat;
        tileStage.GetComponent<MapBuilder>().Longitude = locationHitLon;
        tileStage.GetComponent<MapBuilder>().targetLatitude = locationHitLat;
        tileStage.GetComponent<MapBuilder>().targetLongitude = locationHitLon;
        tileStage.GetComponent<MapBuilder>().ShowMap();
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        //throw new NotImplementedException();
    }

    public void HideTiles()
    {
        if (!tilesActive)
        {
            tileStage.SetActive(true);

            hideTilesButton.GetComponentInChildren<TextMeshProUGUI>().text = "Hide Map";
            tilesActive = true;
        }
        else
        {
            tileStage.SetActive(false);

            hideTilesButton.GetComponentInChildren<TextMeshProUGUI>().text = "Show Map";
            tilesActive = false;
        }

    }


    public void ZoomIn()
    {
        if (tileStage.GetComponent<TileStageOrganizer>().mapTilesLoaded == 0)
        {
            GameObject[] flagMarker = SceneQueries.WithTag(Tags.Flag);
            if (flagMarker != null)
            {
                foreach (var item in flagMarker)
                {
                    Destroy(item);
                }
            }

            GameObject[] infoMarker = SceneQueries.WithTag(Tags.InfoMarker);
            if (infoMarker != null)
            {
                foreach (var item in infoMarker)
                {
                    Destroy(item);
                }
            }

            int oldZoomLevel = tileStage.GetComponent<MapBuilder>().ZoomLevel;
            PhotonView photonView = PhotonView.Get(this);
            this.photonView.RPC("GoToTiles", RpcTarget.All, hitLat, hitLon, oldZoomLevel + 1);

            //modelStage.GetComponent<MapBuilder>().ZoomLevel = oldZoomLevel + 1;
            //modelStage.GetComponent<MapBuilder>().ShowMap();
        }
        else
        {
            Debug.Log("Please wait for tiles to finish loading...");
        }

    }

    public void ZoomOut()
    {
        if (tileStage.GetComponent<TileStageOrganizer>().mapTilesLoaded == 0)
        {
            GameObject[] flagMarker = SceneQueries.WithTag(Tags.Flag);
            if (flagMarker != null)
            {
                foreach (var item in flagMarker)
                {
                    Destroy(item);
                }
            }

            GameObject[] infoMarker = SceneQueries.WithTag(Tags.InfoMarker);
            if (infoMarker != null)
            {
                foreach (var item in infoMarker)
                {
                    Destroy(item);
                }
            }

            int oldZoomLevel = tileStage.GetComponent<MapBuilder>().ZoomLevel;
            PhotonView photonView = PhotonView.Get(this);
            this.photonView.RPC("GoToTiles", RpcTarget.All, hitLat, hitLon, oldZoomLevel - 1);

            //tileStage.GetComponent<MapBuilder>().ZoomLevel = oldZoomLevel - 1;
            //tileStage.GetComponent<MapBuilder>().ShowMap();
        }
        else
        {
            Debug.Log("Please wait for tiles to finish loading...");
        }
    }

    public void OnPointerDown(MixedRealityPointerEventData eventData)
    {

    }

    public void OnPointerDragged(MixedRealityPointerEventData eventData)
    {

    }

    public void OnPointerUp(MixedRealityPointerEventData eventData)
    {


    }

    public void OnPointerClicked(MixedRealityPointerEventData eventData)
    {
    }

    //This is a lousy hack to stop the pointer firing twice on certain objects (not sure why)
    IEnumerator WaitForTapToFinish()
    {

        yield return new WaitForSeconds(0.1f);
        tapped = false;
    }

}

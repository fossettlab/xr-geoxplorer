using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using TMPro;

public class AssetBundleInteraction : MonoBehaviour, IMixedRealityPointerHandler, IPunObservable
{
    public GameObject outcropTooltip;
    public GameObject outcropFlag;
    RaycastHit hit;
    LobbyManager lobbymanager;
    string hitObjectName;
    Vector3 hitObjectPosition;
    Vector3 hitObjectNormal;
    bool moving;

    // Start is called before the first frame update
    void Start()
    {
        lobbymanager = FindObjectOfType<LobbyManager>();
        moving = false;
    }

    // Update is called once per frame
    void Update()
    {
#if UNITY_ANDROID
        // Quest uses MRTK controller pointers in OnPointerClicked, not touch input.
        if (Platform.IsQuest)
        {
            return;
        }
#endif

#if UNITY_EDITOR || UNITY_IOS || UNITY_ANDROID


#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
#elif UNITY_IOS || UNITY_ANDROID
        if (Input.touchCount == 1 && !EventSystem.current.IsPointerOverGameObject())
#endif
        {

#if UNITY_EDITOR
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
#elif UNITY_IOS || UNITY_ANDROID
            Touch touch = Input.GetTouch(0);
            Ray ray = Camera.main.ScreenPointToRay(touch.position);

#endif

            FetchAssetBundle assetBundleLoader = Physics.Raycast(ray, out hit) ? hit.collider.gameObject.GetComponentInParent<FetchAssetBundle>() : null;
            if (assetBundleLoader != null && assetBundleLoader.gameObject.tag == "AssetBundleLoader" && !GameObject.FindGameObjectWithTag("OutcropTooltip"))
            {
                Vector3 hitPosition = hit.transform.InverseTransformPoint(hit.point);
                //Vector3 hitPosition = hit.point;
                Vector3 hitNormal = hit.transform.InverseTransformPoint(hit.normal);
                //Vector3 hitNormal = hit.normal;
                //CreateABTooltipAtLoc(hitLat, hitLon);

#if UNITY_EDITOR
                //PhotonView photonView = PhotonView.Get(this);
                PhotonView photonView = hit.collider.gameObject.GetComponentInParent<FetchAssetBundle>().gameObject.GetComponent<PhotonView>();
                photonView.RPC("CreateABTooltipAtLoc", RpcTarget.All, hitPosition, hitNormal, hit.transform.gameObject.name, photonView.ViewID);

#elif UNITY_IOS || UNITY_ANDROID
                if (touch.phase == TouchPhase.Ended)
                {
                    //PhotonView photonView = PhotonView.Get(this);
                    PhotonView photonView = hit.collider.gameObject.GetComponentInParent<FetchAssetBundle>().gameObject.GetComponent<PhotonView>();
                    photonView.RPC("CreateABTooltipAtLoc", RpcTarget.All, hitPosition, hitNormal, hit.transform.gameObject.name, photonView.ViewID);
                }
#endif
            }
            else if (Physics.Raycast(ray, out hit) && hit.collider.gameObject.name == "TooltipHideButton")
            {
#if UNITY_EDITOR
                PhotonView photonView = PhotonView.Get(this);
                photonView.RPC("OnHideTooltip", RpcTarget.All);
#elif UNITY_IOS || UNITY_ANDROID
                if (touch.phase == TouchPhase.Ended)
                {
                    PhotonView photonView = PhotonView.Get(this);
                    photonView.RPC("OnHideTooltip", RpcTarget.All);
                }
#endif
            }
            else if (Physics.Raycast(ray, out hit) && hit.collider.gameObject.name == "TooltipScaleButton")
            {
#if UNITY_EDITOR
                PhotonView photonView = PhotonView.Get(this);
                photonView.RPC("OnMakeFullScale", RpcTarget.All);
#elif UNITY_IOS || UNITY_ANDROID
                    if (touch.phase == TouchPhase.Ended)
                {
                    PhotonView photonView = PhotonView.Get(this);
                    photonView.RPC("OnMakeFullScale", RpcTarget.All);
                }
#endif
            }
            else if (Physics.Raycast(ray, out hit) && hit.collider.gameObject.name == "TooltipDeleteButton")
            {
#if UNITY_EDITOR
                if (hit.collider.gameObject.GetComponentInParent<FetchAssetBundle>().gameObject.GetComponent<PhotonView>() == PhotonView.Get(this))
                {
                    PhotonView photonView = PhotonView.Get(this);
                    photonView.RPC("OnDelete", RpcTarget.All);
                }
#elif UNITY_IOS || UNITY_ANDROID
                if (touch.phase == TouchPhase.Ended)
                {
                    if (hit.collider.gameObject.GetComponentInParent<FetchAssetBundle>().gameObject.GetComponent<PhotonView>() == PhotonView.Get(this))
                    {
                        PhotonView photonView = PhotonView.Get(this);
                        photonView.RPC("OnDelete", RpcTarget.All);
                    }
                }
#endif
            }
            else if (Physics.Raycast(ray, out hit) && hit.collider.gameObject.name == "TooltipResetButton")
            {
#if UNITY_EDITOR
                PhotonView photonView = PhotonView.Get(this);
                photonView.RPC("OnReset", RpcTarget.All);
#elif UNITY_IOS || UNITY_ANDROID
                if (touch.phase == TouchPhase.Ended)
                {
                    PhotonView photonView = PhotonView.Get(this);
                    photonView.RPC("OnReset", RpcTarget.All);
                }
#endif
            }
            else if (Physics.Raycast(ray, out hit) && hit.collider.gameObject.name == "TooltipFlagButton")
            {
#if UNITY_EDITOR
                //PhotonView photonView = PhotonView.Get(this);
                PhotonView photonView = hit.collider.gameObject.GetComponentInParent<FetchAssetBundle>().gameObject.GetComponent<PhotonView>();
                photonView.RPC("OnFlagCreate", RpcTarget.All, hitObjectPosition, hitObjectNormal, hitObjectName, photonView.ViewID);
#elif UNITY_IOS || UNITY_ANDROID
                if (touch.phase == TouchPhase.Ended)
                {
                    PhotonView photonView = PhotonView.Get(this);
                    photonView.RPC("OnFlagCreate", RpcTarget.All, hitObjectPosition, hitObjectNormal, hitObjectName, photonView.ViewID);
                }
#endif
            }
            else if (Physics.Raycast(ray, out hit) && hit.collider.gameObject.name == "TooltipMoveButton")
            {
#if UNITY_EDITOR
                //PhotonView photonView = PhotonView.Get(this);
                PhotonView photonView = hit.collider.gameObject.GetComponentInParent<FetchAssetBundle>().gameObject.GetComponent<PhotonView>();
                if (photonView.IsMine)
                {
                    if (!moving)
                    {
                        //BoundingBox bbox = gameObject.AddComponent<BoundingBox>();
                        //gameObject.GetComponent<BoxCollider>().enabled = false;


                        gameObject.AddComponent<ObjectManipulator>();
                        gameObject.AddComponent<NearInteractionGrabbable>();
                        gameObject.GetComponent<ObjectManipulator>().OneHandRotationModeFar = ObjectManipulator.RotateInOneHandType.RotateAboutGrabPoint;
                        gameObject.AddComponent<RotationAxisConstraint>().ConstraintOnRotation = Microsoft.MixedReality.Toolkit.Utilities.AxisFlags.XAxis;
                        gameObject.AddComponent<RotationAxisConstraint>().ConstraintOnRotation = Microsoft.MixedReality.Toolkit.Utilities.AxisFlags.ZAxis;
                        //gameObject.AddComponent<FixedRotationToWorldConstraint>();

                        //gameObject.AddComponent<ManipulationHandler>();
                        //gameObject.GetComponent<ManipulationHandler>().OneHandRotationModeFar = ManipulationHandler.RotateInOneHandType.RotateAboutObjectCenter;
                        //gameObject.GetComponent<ManipulationHandler>().ConstraintOnRotation = Microsoft.MixedReality.Toolkit.Utilities.RotationConstraintType.YAxisOnly;
                        hit.collider.gameObject.GetComponent<Renderer>().material.color = Color.gray;
                        hit.collider.gameObject.GetComponentInChildren<TextMeshPro>().text = "Stop manipulation";
                        moving = true;
                    }
                    else
                    {

                        Destroy(gameObject.GetComponent<ObjectManipulator>());
                        Destroy(gameObject.GetComponent<RotationAxisConstraint>());
                        //Destroy(gameObject.GetComponent<ManipulationHandler>());
                        //Destroy(gameObject.GetComponent<BoundingBox>());
                        Destroy(gameObject.GetComponent<BoxCollider>());
                        hit.collider.gameObject.GetComponent<Renderer>().material.color = Color.white;
                        hit.collider.gameObject.GetComponentInChildren<TextMeshPro>().text = "Move";
                        moving = false;
                    }

                }
#elif UNITY_IOS || UNITY_ANDROID
                if (touch.phase == TouchPhase.Ended)
                {
                    //PhotonView photonView = PhotonView.Get(this);
                    PhotonView photonView = hit.collider.gameObject.GetComponentInParent<FetchAssetBundle>().gameObject.GetComponent<PhotonView>();
                    if (photonView.IsMine)
                    {
                        if (!moving)
                        {
                            
                            gameObject.AddComponent<ObjectManipulator>();
                            gameObject.AddComponent<NearInteractionGrabbable>();
                            gameObject.GetComponent<ObjectManipulator>().OneHandRotationModeFar = ObjectManipulator.RotateInOneHandType.RotateAboutGrabPoint;
                            gameObject.AddComponent<RotationAxisConstraint>().ConstraintOnRotation = Microsoft.MixedReality.Toolkit.Utilities.AxisFlags.XAxis;
                            gameObject.AddComponent<RotationAxisConstraint>().ConstraintOnRotation = Microsoft.MixedReality.Toolkit.Utilities.AxisFlags.ZAxis;
                            //gameObject.AddComponent<FixedRotationToWorldConstraint>();

                            //gameObject.AddComponent<ManipulationHandler>();
                            //gameObject.GetComponent<ManipulationHandler>().OneHandRotationModeFar = ManipulationHandler.RotateInOneHandType.RotateAboutObjectCenter;
                            //gameObject.GetComponent<ManipulationHandler>().ConstraintOnRotation = Microsoft.MixedReality.Toolkit.Utilities.RotationConstraintType.YAxisOnly;
                            hit.collider.gameObject.GetComponent<Renderer>().material.color = Color.gray;
                            hit.collider.gameObject.GetComponentInChildren<TextMeshPro>().text = "Stop manipulation";
                            moving = true;
                        }
                        else
                        {
                            
                            Destroy(gameObject.GetComponent<ObjectManipulator>());
                            Destroy(gameObject.GetComponent<RotationAxisConstraint>());
                            //Destroy(gameObject.GetComponent<ManipulationHandler>());
                            hit.collider.gameObject.GetComponent<Renderer>().material.color = Color.white;
                            hit.collider.gameObject.GetComponentInChildren<TextMeshPro>().text = "Move";
                            moving = false;
                        }

                    }
                }
#endif
            }
        }
#endif
    }  
    
    [PunRPC]
    private void CreateABTooltipAtLoc(Vector3 localHitPoint, Vector3 localHitNormal, string hitObject, int PVid)
    {
        GameObject[] toolTips = GameObject.FindGameObjectsWithTag("OutcropTooltip");
        if (toolTips.Length > 0)
        {
            foreach (var tool in toolTips)
            {
                Destroy(tool);
            }
        }
        
        print("Creating AB Tooltop");

        GameObject newTooltip = Instantiate(outcropTooltip);
        
        newTooltip.transform.parent = PhotonNetwork.GetPhotonView(PVid).gameObject.FindInChildren(hitObject);
        newTooltip.transform.localPosition = localHitPoint;
        //newTooltip.transform.position = localHitPoint;
        newTooltip.transform.localRotation = Quaternion.FromToRotation(transform.up, localHitNormal) * transform.rotation;
        hitObjectName = hitObject;
        hitObjectPosition = localHitPoint;
        hitObjectNormal = localHitNormal;
    }

    [PunRPC]
    private void OnHideTooltip()
    {
        GameObject[] toolTips = GameObject.FindGameObjectsWithTag("OutcropTooltip");
        if (toolTips.Length > 0)
        {
            foreach (var tool in toolTips)
            {
                Destroy(tool);
            }
        }
    }

    [PunRPC]
    private void OnMakeFullScale()
    {
        PhotonView PV = this.GetComponent<PhotonView>();
        if (PV.IsMine)
        {
            GameObject[] toolTips = GameObject.FindGameObjectsWithTag("OutcropTooltip");
            if (toolTips.Length > 0)
            {
                foreach (var tool in toolTips)
                {
                    Destroy(tool);
                }
            }

            this.transform.localScale = Vector3.one;
        }
    }

    [PunRPC]
    private void OnFlagCreate(Vector3 localHitPoint, Vector3 localHitNormal, string hitObject, int PVid)
    {
        GameObject[] toolTips = GameObject.FindGameObjectsWithTag("OutcropTooltip");
        if (toolTips.Length > 0)
        {
            foreach (var tool in toolTips)
            {
                Destroy(tool);
            }
        }

        GameObject newFlag = Instantiate(outcropFlag);
        newFlag.transform.parent = PhotonNetwork.GetPhotonView(PVid).gameObject.FindInChildren(hitObject);
        newFlag.transform.localPosition = localHitPoint;
        //newFlag.transform.position = localHitPoint;
        newFlag.transform.localRotation = Quaternion.FromToRotation(transform.up, localHitNormal) * transform.rotation;
    }

    [PunRPC]
    public void OnReset()
    {
        PhotonView PV = this.GetComponent<PhotonView>();
        if (PV.IsMine)
        {
            GameObject[] toolTips = GameObject.FindGameObjectsWithTag("OutcropTooltip");
            if (toolTips.Length > 0)
            {
                foreach (var tool in toolTips)
                {
                    Destroy(tool);
                }
            }

            transform.localPosition = this.GetComponentInParent<FetchAssetBundle>().resetPosition;
            transform.localEulerAngles = this.GetComponentInParent<FetchAssetBundle>().resetEulerAngles;
            transform.localScale = this.GetComponentInParent<FetchAssetBundle>().resetScale;
        }
    }

    [PunRPC]
    private void OnDelete()
    {
        PhotonView PV = this.GetComponent<PhotonView>();
        if (PV.IsMine)
        {
            GameObject[] toolTips = GameObject.FindGameObjectsWithTag("OutcropTooltip");
            if (toolTips.Length > 0)
            {
                foreach (var tool in toolTips)
                {
                    Destroy(tool);
                }
            }
            lobbymanager.DeleteAssetBundle(this.gameObject);
        }
    }

    [PunRPC]
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
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
        if (eventData?.Pointer?.Result == null)
        {
            return;
        }

        GameObject hitObject = eventData.Pointer.Result.Details.Object;
        if (hitObject == null)
        {
            return;
        }

        Vector3 hitPosition = eventData.Pointer.Result.Details.PointLocalSpace;
        Vector3 hitNormal = eventData.Pointer.Result.Details.NormalLocalSpace;
        if (hitPosition == Vector3.zero && eventData.Pointer.Result.Details.Point != Vector3.zero)
        {
            hitPosition = hitObject.transform.InverseTransformPoint(eventData.Pointer.Result.Details.Point);
        }
        if (hitNormal == Vector3.zero && eventData.Pointer.Result.Details.Normal != Vector3.zero)
        {
            hitNormal = hitObject.transform.InverseTransformDirection(eventData.Pointer.Result.Details.Normal);
        }

        FetchAssetBundle assetBundleLoader = hitObject.GetComponentInParent<FetchAssetBundle>();
        if (assetBundleLoader != null && assetBundleLoader.gameObject.tag == "AssetBundleLoader" && !GameObject.FindGameObjectWithTag("OutcropTooltip"))
        {
            PhotonView photonView = assetBundleLoader.gameObject.GetComponent<PhotonView>();
            if (photonView != null)
            {
                photonView.RPC("CreateABTooltipAtLoc", RpcTarget.All, hitPosition, hitNormal, hitObject.name, photonView.ViewID);
            }
        }
        else if (hitObject.name == "TooltipHideButton")
        {
            PhotonView photonView = PhotonView.Get(this);
            photonView.RPC("OnHideTooltip", RpcTarget.All);
        }
        else if (hitObject.name == "TooltipScaleButton")
        {
            PhotonView photonView = PhotonView.Get(this);
            photonView.RPC("OnMakeFullScale", RpcTarget.All);
        }
        else if (hitObject.name == "TooltipDeleteButton")
        {
            FetchAssetBundle parentLoader = hitObject.GetComponentInParent<FetchAssetBundle>();
            if (parentLoader != null && parentLoader.gameObject.GetComponent<PhotonView>() == PhotonView.Get(this))
            {
                PhotonView photonView = PhotonView.Get(this);
                photonView.RPC("OnDelete", RpcTarget.All);
            }
        }
        else if (hitObject.name == "TooltipResetButton")
        {
            PhotonView photonView = PhotonView.Get(this);
            photonView.RPC("OnReset", RpcTarget.All);
        }
        else if (hitObject.name == "TooltipFlagButton")
        {
            PhotonView photonView = hitObject.GetComponentInParent<FetchAssetBundle>()?.gameObject.GetComponent<PhotonView>();
            if (photonView != null)
            {
                // Flag the asset-surface hit captured when the tooltip was created, not the button itself.
                photonView.RPC("OnFlagCreate", RpcTarget.All, hitObjectPosition, hitObjectNormal, hitObjectName, photonView.ViewID);
            }
        }
        else if (hitObject.name == "TooltipMoveButton")
        {
            PhotonView photonView = hitObject.GetComponentInParent<FetchAssetBundle>()?.gameObject.GetComponent<PhotonView>();
            if (photonView != null && photonView.IsMine)
            {
                if (!moving)
                {
                    ResetQuestMoveComponents();
                    ObjectManipulator manipulator = gameObject.AddComponent<ObjectManipulator>();
                    gameObject.AddComponent<NearInteractionGrabbable>();
                    manipulator.OneHandRotationModeFar = ObjectManipulator.RotateInOneHandType.RotateAboutGrabPoint;
                    gameObject.AddComponent<RotationAxisConstraint>().ConstraintOnRotation = Microsoft.MixedReality.Toolkit.Utilities.AxisFlags.XAxis;
                    gameObject.AddComponent<RotationAxisConstraint>().ConstraintOnRotation = Microsoft.MixedReality.Toolkit.Utilities.AxisFlags.ZAxis;
                    TMP_Text moveLabel = hitObject.GetComponentInChildren<TMP_Text>();
                    if (moveLabel != null)
                    {
                        moveLabel.text = "Stop manipulation";
                    }
                    Renderer moveRenderer = hitObject.GetComponent<Renderer>();
                    if (moveRenderer != null)
                    {
                        moveRenderer.material.color = Color.gray;
                    }
                    moving = true;
                }
                else
                {
                    ResetQuestMoveComponents();
                    TMP_Text moveLabel = hitObject.GetComponentInChildren<TMP_Text>();
                    if (moveLabel != null)
                    {
                        moveLabel.text = "Move";
                    }
                    Renderer moveRenderer = hitObject.GetComponent<Renderer>();
                    if (moveRenderer != null)
                    {
                        moveRenderer.material.color = Color.white;
                    }
                    moving = false;
                }
            }
        }
        else if (hitObject.name == "TooltipInspectButton")
        {
            InspectorModelObject[] modelInspector = Resources.FindObjectsOfTypeAll<InspectorModelObject>();
            if (modelInspector.Length == 0)
            {
                Debug.LogError("InspectorModelObject not found in scene; cannot open model inspector.");
                return;
            }

            Transform modelInspectorTransform = modelInspector[0].transform;
            foreach (Transform child in modelInspectorTransform)
            {
                Destroy(child.gameObject);
            }

            FetchAssetBundle fetchAssetBundle = GetComponent<FetchAssetBundle>();
            if (fetchAssetBundle == null || fetchAssetBundle.newPrefab == null)
            {
                Debug.LogError("Cannot open model inspector because the loaded prefab is missing.");
                return;
            }

            inspectorPrefab = Instantiate(fetchAssetBundle.newPrefab);

            GameObject[] toolTips = GameObject.FindGameObjectsWithTag("OutcropTooltip");
            if (toolTips.Length > 0)
            {
                foreach (var tool in toolTips)
                {
                    Destroy(tool);
                }
            }

            inspectorPrefab.transform.parent = modelInspectorTransform;
            Bounds newBounds = GetChildRendererBounds(inspectorPrefab.gameObject);
            float largestFootprint = Mathf.Max(newBounds.size.x, newBounds.size.z);
            if (largestFootprint > 0f)
            {
                inspectorPrefab.transform.localScale /= (largestFootprint / 0.2f);
            }

            inspectorPrefab.transform.localPosition = Vector3.zero;
            inspectorPrefab.transform.localEulerAngles = Vector3.zero;
        }
    }
    GameObject inspectorPrefab;

    private void ResetQuestMoveComponents()
    {
        ObjectManipulator manipulator = gameObject.GetComponent<ObjectManipulator>();
        if (manipulator != null)
        {
            Destroy(manipulator);
        }

        NearInteractionGrabbable grabbable = gameObject.GetComponent<NearInteractionGrabbable>();
        if (grabbable != null)
        {
            Destroy(grabbable);
        }

        foreach (RotationAxisConstraint constraint in gameObject.GetComponents<RotationAxisConstraint>())
        {
            Destroy(constraint);
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

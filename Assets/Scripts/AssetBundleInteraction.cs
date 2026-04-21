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
#if UNITY_EDITOR || UNITY_IOS


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

            if (Physics.Raycast(ray, out hit) && hit.collider.gameObject.GetComponentInParent<FetchAssetBundle>().gameObject.tag == "AssetBundleLoader" && !GameObject.FindGameObjectWithTag("OutcropTooltip"))
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
#if UNITY_WSA

        Vector3 hitPos;
        Vector3 hitNorm;
        GameObject hitObject;

        if (Application.platform == RuntimePlatform.WSAPlayerARM)
        {
            hitPos = eventData.Pointer.Result.Details.Point;
            hitNorm = eventData.Pointer.Result.Details.Normal;
            hitObject = eventData.Pointer.Result.Details.Object;
        }
        else
        {
            hitPos = Camera.main.GetComponent<GazeProvider>().HitPosition;
            hitNorm = Camera.main.GetComponent<GazeProvider>().HitNormal;
            hitObject = Camera.main.GetComponent<GazeProvider>().HitInfo.transform.gameObject;
        }


        if (hitObject.GetComponentInParent<FetchAssetBundle>().gameObject.tag == "AssetBundleLoader" && !GameObject.FindGameObjectWithTag("OutcropTooltip"))
        {
            Vector3 hitPosition = hitObject.transform.InverseTransformPoint(hitPos);
            Vector3 hitNormal = hitObject.transform.InverseTransformPoint(hitNorm);

            //PhotonView photonView = PhotonView.Get(this);
            PhotonView photonView = hitObject.GetComponentInParent<FetchAssetBundle>().gameObject.GetComponent<PhotonView>();
            photonView.RPC("CreateABTooltipAtLoc", RpcTarget.All, hitPosition, hitNormal, hitObject.name, photonView.ViewID);
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
            if (hitObject.GetComponentInParent<FetchAssetBundle>().gameObject.GetComponent<PhotonView>() == PhotonView.Get(this))
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
            //PhotonView photonView = PhotonView.Get(this);
            PhotonView photonView = hitObject.GetComponentInParent<FetchAssetBundle>().gameObject.GetComponent<PhotonView>();
            photonView.RPC("OnFlagCreate", RpcTarget.All, hitObjectPosition, hitObjectNormal, hitObjectName, photonView.ViewID);
        }
        else if (hitObject.name == "TooltipMoveButton")
        {
            //PhotonView photonView = PhotonView.Get(this);
            PhotonView photonView = hitObject.GetComponentInParent<FetchAssetBundle>().gameObject.GetComponent<PhotonView>();
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
                    hitObject.GetComponent<Renderer>().material.color = Color.gray;
                    hitObject.GetComponentInChildren<TextMeshPro>().text = "Stop manipulation";
                    moving = true;
                }
                else
                {
                    Destroy(gameObject.GetComponent<ObjectManipulator>());
                    Destroy(gameObject.GetComponent<RotationAxisConstraint>());
                    //Destroy(gameObject.GetComponent<ManipulationHandler>());
                    //Destroy(gameObject.GetComponent<BoundingBox>());
                    hitObject.GetComponent<Renderer>().material.color = Color.white;
                    hitObject.GetComponentInChildren<TextMeshPro>().text = "Move";
                    moving = false;
                }

            }
        }
        else if (hitObject.name == "TooltipInspectButton")
        {
            InspectorModelObject[] modelInspector = Resources.FindObjectsOfTypeAll<InspectorModelObject>();
            Transform modelInspectorTransform = modelInspector[0].transform;
            foreach (Transform child in modelInspectorTransform)
            {
                Destroy(child.gameObject);
            }

            inspectorPrefab = Instantiate(this.GetComponent<FetchAssetBundle>().newPrefab);

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
            if (newBounds.size.x > newBounds.size.z)
            {
                inspectorPrefab.transform.localScale /= (newBounds.size.x / 0.2f);
            }
            else
            {
                inspectorPrefab.transform.localScale /= (newBounds.size.z / 0.2f);
            }

            inspectorPrefab.transform.localPosition = Vector3.zero;
            inspectorPrefab.transform.localEulerAngles = Vector3.zero;
        }
#endif
    }
    GameObject inspectorPrefab;

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

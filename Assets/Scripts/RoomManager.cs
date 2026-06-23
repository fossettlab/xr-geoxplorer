using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit;
using UnityEngine.XR.ARFoundation;
using Microsoft.Azure.SpatialAnchors.Unity;
using UnityEngine.UI;

public class RoomManager : MonoBehaviour, IMixedRealityPointerHandler
{

    /// <summary>
    /// Controls the Room set up system. After the user enters a room, they are presented with an option to create an Azure Spatial Anchor or manually place an anchor at a particular location. UI interactions for both MobileAR and HoloLens included.
    /// 
    /// </summary>

    public TextMeshProUGUI directionText;
    public Image panelImage;
    public LobbyManager lobbyManager;
    public GameObject createAnchorButton;
    public GameObject findAnchorButton;
    public GameObject continueButton;
    public GameObject createInputTextBox;
    public GameObject startCreatingAnchorButton;
    public GameObject findInputTextBox;
    public GameObject startFindingAnchorButton;
    public GameObject roomBackButton;

    public GameObject anchorObject;
    public Pose anchorPose;
    public string anchorNumber { get; set; }


    ARRaycastManager raycastManager;
    GameObject newAnchorObject;

    bool isPlacing;
    bool anchorValid;
    bool creatingASA;
    bool arPlacementAvailable;


    public void ListenForClicks()
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
        creatingASA = false;
#if UNITY_IOS || UNITY_ANDROID
        raycastManager = FindObjectOfType<ARRaycastManager>();
        arPlacementAvailable = raycastManager != null;
#endif
    }

    // Update is called once per frame
    void Update()
    {
        if (isPlacing)
        {
#if UNITY_IOS || UNITY_ANDROID
            if (!arPlacementAvailable || raycastManager == null)
            {
                return;
            }

            Camera arCamera = Camera.main;
            if (arCamera == null)
            {
                return;
            }

            var screenCenter = arCamera.ViewportToScreenPoint(new Vector3(0.5f, 0.5f));
            var hits = new List<ARRaycastHit>();
            raycastManager.Raycast(screenCenter, hits, UnityEngine.XR.ARSubsystems.TrackableType.PlaneWithinPolygon);

            anchorValid = hits.Count > 0;
            if (anchorValid)
            {
                anchorPose = hits[0].pose;
                
                newAnchorObject.GetComponentInChildren<Renderer>().enabled = true;

                var cameraForward = arCamera.transform.forward;
                var cameraBearing = new Vector3(cameraForward.x, 0, cameraForward.z).normalized;
                anchorPose.rotation = Quaternion.LookRotation(cameraBearing);

                newAnchorObject.transform.SetPositionAndRotation(anchorPose.position, anchorPose.rotation);

                if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
                {
                    isPlacing = false;
                    newAnchorObject.GetComponentInChildren<Renderer>().material.color = Color.green;

                    if (creatingASA)
                    {
                        newAnchorObject.GetComponent<SpatialAnchorManager>().enabled = true;
                        newAnchorObject.AddComponent<CreateASA>();
                        newAnchorObject.GetComponent<CreateASA>().feedback = directionText;
                        creatingASA = false;
                    }
                    else
                    {
                        lobbyManager.OnAnchorSuccessful(newAnchorObject);
                    }
                }
            }
            else
            {
                newAnchorObject.GetComponentInChildren<Renderer>().enabled = false;
            }

#endif

        }
    }

    
    public void OnCreateSelected()
    {
        createAnchorButton.SetActive(false);
        findAnchorButton.SetActive(false);
        continueButton.SetActive(false);

        directionText.text = "Enter anchor name:";
        createInputTextBox.SetActive(true);
        startCreatingAnchorButton.SetActive(true);
        roomBackButton.SetActive(true);
    }

    public void OnStartCreatingSelected()
    {
        StartCoroutine(OnStartCreatingSelectedRoutine());
    }

    IEnumerator OnStartCreatingSelectedRoutine()
    {
        FirebaseExchanger firebase = GetComponent<FirebaseExchanger>();
        while (!firebase.AnchorsLoaded)
        {
            yield return null;
        }

        yield return firebase.RefreshAnchorsFromServer();
        if (!firebase.LastFetchSucceeded)
        {
            directionText.text = "Could not load anchor list; try again.";
            yield break;
        }

        if (firebase.CheckForNameConflict(firebase.anchorName))
        {
            directionText.text = "Anchor name is already used, please pick another";
        }
        else
        {
#if UNITY_IOS || UNITY_ANDROID
            if (!arPlacementAvailable)
            {
                directionText.text = "AR plane placement is not available on this device. Use Find Anchor instead.";
                yield break;
            }

            panelImage.enabled = false;
#endif

            createInputTextBox.SetActive(false);
            startCreatingAnchorButton.SetActive(false);
            roomBackButton.SetActive(false);
            directionText.text = "Place anchor location...";

            isPlacing = true;
            anchorValid = false;
            creatingASA = true;
            newAnchorObject = Instantiate(anchorObject);
        }
    }

    public void OnFindSelected()
    {
        createAnchorButton.SetActive(false);
        findAnchorButton.SetActive(false);
        continueButton.SetActive(false);

        directionText.text = "Enter anchor name:";
        findInputTextBox.SetActive(true);
        startFindingAnchorButton.SetActive(true);
        roomBackButton.SetActive(true);
    }

    public void OnStartFindingSelected()
    {
        StartCoroutine(OnStartFindingSelectedRoutine());
    }

    IEnumerator OnStartFindingSelectedRoutine()
    {
        FirebaseExchanger firebase = GetComponent<FirebaseExchanger>();
        while (!firebase.AnchorsLoaded)
        {
            yield return null;
        }

        yield return firebase.RefreshAnchorsFromServer();
        if (!firebase.LastFetchSucceeded)
        {
            directionText.text = "Could not load anchor list; try again.";
            yield break;
        }

        if (firebase.CheckIfNameExists(firebase.anchorName))
        {
#if UNITY_IOS || UNITY_ANDROID
            panelImage.enabled = false;
#endif

            findInputTextBox.SetActive(false);
            startFindingAnchorButton.SetActive(false);
            roomBackButton.SetActive(false);

            print("finding anchor: " + firebase.anchorName);

            newAnchorObject = Instantiate(anchorObject);
            newAnchorObject.AddComponent<CloudNativeAnchor>();
            newAnchorObject.GetComponentInChildren<Renderer>().enabled = false;
            newAnchorObject.GetComponent<SpatialAnchorManager>().enabled = true;
            newAnchorObject.AddComponent<FindASA>();
            newAnchorObject.GetComponent<FindASA>().anchorName = firebase.anchorName;
            newAnchorObject.GetComponent<FindASA>().feedback = directionText;
        }
        else
        {
            directionText.text = "Anchor Name " + firebase.anchorName + " Not Found!";
        }
    }

    public void OnContinueSelected()
    {
#if UNITY_IOS || UNITY_ANDROID
        if (!arPlacementAvailable)
        {
            directionText.text = "Manual placement requires AR plane detection (not available on this device).";
            return;
        }

        panelImage.enabled = false;
#endif
        createAnchorButton.SetActive(false);
        findAnchorButton.SetActive(false);
        continueButton.SetActive(false);
        directionText.text = "Place menu location...";
        isPlacing = true;
        anchorValid = false;

        newAnchorObject = Instantiate(anchorObject);
    }


    public void OnBack()
    {
        isPlacing = false;
        creatingASA = false;
        anchorValid = false;
        StopListenForClicks();

        if (newAnchorObject != null)
        {
            Destroy(newAnchorObject);
            newAnchorObject = null;
        }

#if UNITY_IOS || UNITY_ANDROID
        panelImage.enabled = true;
#endif

        createAnchorButton.SetActive(true);
        findAnchorButton.SetActive(true);
        continueButton.SetActive(true);

        createInputTextBox.SetActive(false);
        startCreatingAnchorButton.SetActive(false);
        findInputTextBox.SetActive(false);
        startFindingAnchorButton.SetActive(false);
        roomBackButton.SetActive(false);
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

}

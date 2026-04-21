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

#if UNITY_WSA
    public TextMeshPro directionText;
#elif UNITY_IOS || UNITY_ANDROID
    public TextMeshProUGUI directionText;
    public Image panelImage;
#endif
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
#endif
    }

    // Update is called once per frame
    void Update()
    {
        if (isPlacing)
        {
#if UNITY_IOS || UNITY_ANDROID

            var screenCenter = Camera.current.ViewportToScreenPoint(new Vector3(0.5f, 0.5f));
            var hits = new List<ARRaycastHit>();
            raycastManager.Raycast(screenCenter, hits, UnityEngine.XR.ARSubsystems.TrackableType.PlaneWithinPolygon);

            anchorValid = hits.Count > 0;
            if (anchorValid)
            {
                anchorPose = hits[0].pose;
                
                newAnchorObject.GetComponentInChildren<Renderer>().enabled = true;

                var cameraForward = Camera.current.transform.forward;
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

            

#elif UNITY_WSA
            if (Camera.main.GetComponent<GazeProvider>().HitPosition.magnitude > 0)
            {
                ListenForClicks();

                anchorValid = true;
                newAnchorObject.GetComponentInChildren<Renderer>().enabled = true;

                var cameraForward = Camera.main.transform.forward;
                var cameraBearing = new Vector3(cameraForward.x, 0, cameraForward.z).normalized;
                anchorPose.rotation = Quaternion.LookRotation(cameraBearing);
                anchorPose.position = Camera.main.GetComponent<GazeProvider>().HitPosition;

                newAnchorObject.transform.SetPositionAndRotation(anchorPose.position, anchorPose.rotation);
            }
            else
            {
                StopListenForClicks();
                anchorValid = false;
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
        if (this.GetComponent<FirebaseExchanger>().CheckForNameConflict(this.GetComponent<FirebaseExchanger>().anchorName))
        {
            directionText.text = "Anchor name is already used, please pick another";
        }
        else
        {
#if UNITY_IOS || UNITY_ANDROID
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


        if (this.GetComponent<FirebaseExchanger>().CheckIfNameExists(this.GetComponent<FirebaseExchanger>().anchorName))
        {

#if UNITY_IOS || UNITY_ANDROID
            panelImage.enabled = false;
#endif

            findInputTextBox.SetActive(false);
            startFindingAnchorButton.SetActive(false);
            roomBackButton.SetActive(false);

            print("finding anchor: " + this.GetComponent<FirebaseExchanger>().anchorName);

            newAnchorObject = Instantiate(anchorObject);
            newAnchorObject.AddComponent<CloudNativeAnchor>();
            newAnchorObject.GetComponentInChildren<Renderer>().enabled = false;
            newAnchorObject.GetComponent<SpatialAnchorManager>().enabled = true;
            newAnchorObject.AddComponent<FindASA>();
            newAnchorObject.GetComponent<FindASA>().anchorName = this.GetComponent<FirebaseExchanger>().anchorName;
            newAnchorObject.GetComponent<FindASA>().feedback = directionText;
        }
        else
        {
            directionText.text = "Anchor Name " + this.GetComponent<FirebaseExchanger>().anchorName + " Not Found!";
        }
    }

    public void OnContinueSelected()
    {
#if UNITY_IOS || UNITY_ANDROID
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
#if UNITY_WSA
        if (anchorValid)
        {
            isPlacing = false;

            newAnchorObject.GetComponentInChildren<Renderer>().material.color = Color.green;

            if (creatingASA)
            {
                newAnchorObject.GetComponent<SpatialAnchorManager>().enabled = true;
                newAnchorObject.AddComponent<CreateASA>();
                newAnchorObject.GetComponent<CreateASA>().feedback = directionText;
                creatingASA = false;
                StopListenForClicks();
            }
            else
            {
                StopListenForClicks();
                lobbyManager.OnAnchorSuccessful(newAnchorObject);
            }


            //set the table anchor instance to it's new position and rotation
            //TableAnchor.instance.transform.SetPositionAndRotation(newAnchorObject.transform.position, newAnchorObject.transform.rotation);
            
            //lobbyManager.OnAnchorSuccessful(newAnchorObject);

        }
#endif
    }

}
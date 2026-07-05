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

    const float QuestPlacementRayDistance = 10f;


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
    }

    // Update is called once per frame
    void Update()
    {
        if (!isPlacing || newAnchorObject == null)
        {
            return;
        }

#if UNITY_IOS || UNITY_ANDROID
        if (RequiresArPlanePlacement())
        {
            UpdateArPlanePlacement();
        }
        else if (UsesQuestPointerPlacement())
        {
            UpdateQuestPointerPlacement();
        }
#endif
    }

#if UNITY_IOS || UNITY_ANDROID
    ARRaycastManager EnsureRaycastManager()
    {
        if (raycastManager == null)
        {
            raycastManager = FindObjectOfType<ARRaycastManager>();
        }

        return raycastManager;
    }

    bool IsArPlacementAvailable()
    {
        ARRaycastManager activeRaycastManager = EnsureRaycastManager();
        return activeRaycastManager != null && activeRaycastManager.isActiveAndEnabled;
    }

    bool RequiresArPlanePlacement()
    {
#if UNITY_IOS
        return true;
#elif UNITY_ANDROID
        return !Platform.IsQuest;
#else
        return false;
#endif
    }

    bool UsesQuestPointerPlacement()
    {
#if UNITY_ANDROID
        return Platform.IsQuest;
#else
        return false;
#endif
    }

    void UpdateArPlanePlacement()
    {
        ARRaycastManager activeRaycastManager = EnsureRaycastManager();
        if (activeRaycastManager == null || !activeRaycastManager.isActiveAndEnabled)
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
        activeRaycastManager.Raycast(screenCenter, hits, UnityEngine.XR.ARSubsystems.TrackableType.PlaneWithinPolygon);

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
                ConfirmPlacement();
            }
        }
        else
        {
            newAnchorObject.GetComponentInChildren<Renderer>().enabled = false;
        }
    }

    void UpdateQuestPointerPlacement()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        Vector3 hitPosition = Vector3.zero;
        bool hasHit = false;

        GazeProvider gazeProvider = mainCamera.GetComponent<GazeProvider>();
        if (gazeProvider != null && gazeProvider.HitPosition.magnitude > 0f)
        {
            hasHit = true;
            hitPosition = gazeProvider.HitPosition;
        }
        else
        {
            Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit physicsHit, QuestPlacementRayDistance))
            {
                hasHit = true;
                hitPosition = physicsHit.point;
            }
        }

        if (hasHit)
        {
            ListenForClicks();

            anchorValid = true;
            newAnchorObject.GetComponentInChildren<Renderer>().enabled = true;

            var cameraForward = mainCamera.transform.forward;
            var cameraBearing = new Vector3(cameraForward.x, 0, cameraForward.z).normalized;
            anchorPose.rotation = Quaternion.LookRotation(cameraBearing);
            anchorPose.position = hitPosition;

            newAnchorObject.transform.SetPositionAndRotation(anchorPose.position, anchorPose.rotation);
        }
        else
        {
            StopListenForClicks();
            anchorValid = false;
            newAnchorObject.GetComponentInChildren<Renderer>().enabled = false;
        }
    }
#endif

    void ConfirmPlacement()
    {
        isPlacing = false;
        newAnchorObject.GetComponentInChildren<Renderer>().material.color = Color.green;
        StopListenForClicks();

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
            if (RequiresArPlanePlacement() && !IsArPlacementAvailable())
            {
                directionText.text = "AR plane detection is not available on this device.";
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
        if (RequiresArPlanePlacement() && !IsArPlacementAvailable())
        {
            directionText.text = "AR plane detection is not available on this device.";
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
#if UNITY_ANDROID
        if (UsesQuestPointerPlacement() && isPlacing && anchorValid)
        {
            ConfirmPlacement();
        }
#endif
    }

}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.SpatialAnchors;
using Microsoft.Azure.SpatialAnchors.Unity;
using TMPro;
using UnityEngine;

public class FindASA : MonoBehaviour
{
    public TextMeshProUGUI feedback;

    CloudSpatialAnchor currentCloudAnchor;
    //AnchorExchanger anchorExchanger = new AnchorExchanger();
    public string anchorName;
    protected AnchorLocateCriteria anchorLocateCriteria = null;
    CloudSpatialAnchorWatcher currentWatcher;
    bool anchorLocatedAndPlaced;

    void Start()
    {
        anchorLocatedAndPlaced = false;
        //anchorExchanger.WatchKeys("https://flsharingservice.azurewebsites.net/api/anchors");

        GetComponent<SpatialAnchorManager>().AnchorLocated += CloudAnchor_Located;
        anchorLocateCriteria = new AnchorLocateCriteria();

        _ = RunInitializeAsync();
    }

    private async Task RunInitializeAsync()
    {
        try
        {
            await Initialize();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            if (feedback != null)
            {
                feedback.text = "Anchor lookup failed: " + ex.Message;
            }
        }
    }

    public async Task Initialize()
    {
        if (!GetComponent<SpatialAnchorManager>().enabled)
        {
            feedback.text = "Spatial Anchor Manager not enabled";
        }
        else
        {
            await GetComponent<SpatialAnchorManager>().CreateSessionAsync();
            feedback.text = "Created Session";
            await GetComponent<SpatialAnchorManager>().StartSessionAsync();
            feedback.text = "Started Session";

            FirebaseExchanger firebase = FirebaseExchanger.Instance;
            while (!firebase.AnchorsLoaded)
            {
                await Task.Delay(100);
            }

            bool? refreshResult = null;
            firebase.StartCoroutine(firebase.RefreshAnchorsAndWait(succeeded => refreshResult = succeeded));
            while (refreshResult == null)
            {
                await Task.Delay(50);
            }

            if (refreshResult != true)
            {
                feedback.text = "Could not load anchor list; try again.";
                GetComponent<SpatialAnchorManager>().StopSession();
                return;
            }

            string _anchorKeyToFind = firebase.FindAnchorByName();
            if (_anchorKeyToFind == null)
            {
                feedback.text = "Anchor Number Not Found!";
                GetComponent<SpatialAnchorManager>().StopSession();
                return;
            }

            List<string> anchorsToFind = new List<string>();
            List<string> anchorIdsToLocate = new List<string>();
            anchorsToFind.Add(_anchorKeyToFind);
            anchorIdsToLocate.AddRange(anchorsToFind);

            anchorLocateCriteria.Identifiers = new string[0];
            anchorLocateCriteria.Identifiers = anchorIdsToLocate.ToArray();

            feedback.text = "Anchor key to find: " + _anchorKeyToFind;

            GetComponent<SpatialAnchorManager>().Session.CreateWatcher(anchorLocateCriteria);
            feedback.text = "Watcher started...";
        }

    }

    private void Update()
    {
        if (anchorLocatedAndPlaced)
        {
            anchorLocatedAndPlaced = false;
            GetComponent<SpatialAnchorManager>().StopSession();
            feedback.text = "Stopped Session";

            LobbyManager.Instance.OnAnchorSuccessful(this.gameObject);
        }
    }

    private void CloudAnchor_Located(object sender, AnchorLocatedEventArgs args)
    {
        //feedback.text = "Anchor " + anchorName + " located";
        currentCloudAnchor = args.Anchor;


        UnityDispatcher.InvokeOnAppThread(() =>
        {
            Pose anchorPose = Pose.identity;

#if UNITY_IOS || UNITY_ANDROID
        anchorPose = currentCloudAnchor.GetPose();
        this.transform.SetPositionAndRotation(anchorPose.position, anchorPose.rotation);
#endif

            // If a cloud anchor is passed, apply it to the native anchor
            if (currentCloudAnchor != null)
            {
                this.GetComponent<CloudNativeAnchor>().CloudToNative(currentCloudAnchor);
            }
            this.GetComponentInChildren<Renderer>().enabled = true;
            this.GetComponentInChildren<Renderer>().material.color = Color.green;
            TMP_Text anchorLabel = GetComponentInChildren<TMP_Text>();
            if (anchorLabel != null)
            {
                anchorLabel.text = anchorName;
            }

            anchorLocatedAndPlaced = true;
        });

    }
}

using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.SpatialAnchors;
using Microsoft.Azure.SpatialAnchors.Unity;
using TMPro;
using UnityEngine;

public class FindASA : MonoBehaviour
{

#if UNITY_WSA
    public TextMeshPro feedback;
#else
    public TextMeshProUGUI feedback;
#endif

    CloudSpatialAnchor currentCloudAnchor;
    //AnchorExchanger anchorExchanger = new AnchorExchanger();
    public string anchorName;
    protected AnchorLocateCriteria anchorLocateCriteria = null;
    CloudSpatialAnchorWatcher currentWatcher;
    bool anchorLocatedAndPlaced;

    // Start is called before the first frame update
    async void Start()
    {
        anchorLocatedAndPlaced = false;
        //anchorExchanger.WatchKeys("https://flsharingservice.azurewebsites.net/api/anchors");

        GetComponent<SpatialAnchorManager>().AnchorLocated += CloudAnchor_Located;
        anchorLocateCriteria = new AnchorLocateCriteria();

        await Initialize();
        
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

            FirebaseExchanger firebase = FindObjectOfType<FirebaseExchanger>();
            while (!firebase.AnchorsLoaded)
            {
                await Task.Delay(100);
            }

            if (!firebase.AnchorsFetchSucceeded)
            {
                feedback.text = "Could not load anchor list; try again.";
                return;
            }

            string _anchorKeyToFind = firebase.FindAnchorByName();
            if (_anchorKeyToFind == null)
            {
                feedback.text = "Anchor Number Not Found!";
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

            GameObject.FindGameObjectWithTag("NetworkRoom").GetComponent<LobbyManager>().OnAnchorSuccessful(this.gameObject);
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
            this.GetComponentInChildren<TextMeshPro>().text = anchorName;

            anchorLocatedAndPlaced = true;
        });

    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.Azure.SpatialAnchors;
using Microsoft.Azure.SpatialAnchors.Unity;
using System;
using System.Threading.Tasks;
using TMPro;

public class CreateASA : MonoBehaviour
{
    public TextMeshProUGUI feedback;

    CloudSpatialAnchor currentCloudAnchor;
    CloudSpatialAnchorSession cloudSpatialAnchorSession;

    void Start()
    {
        this.gameObject.AddComponent<CloudNativeAnchor>();
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
                feedback.text = "Anchor creation failed: " + ex.Message;
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
            //feedback.text = string.Format("{0}\n{1}", feedback.text, "Started Session");

            bool saveSucceeded = await SaveCurrentObjectAnchorToCloudAsync();

            GetComponent<SpatialAnchorManager>().StopSession();
            feedback.text = "Stopped Session";

            if (!saveSucceeded)
            {
                return;
            }

            TMP_Text anchorLabel = GetComponentInChildren<TMP_Text>();
            if (anchorLabel != null)
            {
                anchorLabel.text = FirebaseExchanger.Instance.anchorName;
            }

            LobbyManager.Instance.OnAnchorSuccessful(this.gameObject);
        }
    }


    protected virtual async Task<bool> SaveCurrentObjectAnchorToCloudAsync()
    {
        CloudNativeAnchor nativeAnchor = this.GetComponent<CloudNativeAnchor>();
        nativeAnchor.SetPose(this.transform.position, this.transform.rotation);

        // If the cloud portion of the anchor hasn't been created yet, create it
        if (nativeAnchor.CloudAnchor == null) { nativeAnchor.NativeToCloud(); }

        CloudSpatialAnchor cloudAnchor = nativeAnchor.CloudAnchor;

        cloudAnchor.Expiration = DateTimeOffset.Now.AddHours(24);

        feedback.text = "Created cloud anchor";


        while (!GetComponent<SpatialAnchorManager>().IsReadyForCreate)
        {
            await Task.Delay(330);
            float createProgress = GetComponent<SpatialAnchorManager>().SessionStatus.RecommendedForCreateProgress;
            feedback.text = $"Move your device to capture more environment data: {createProgress:0%}";
        }

        feedback.text = "Saving...";

#if UNITY_IOS || UNITY_ANDROID
        Pose anchorPose = cloudAnchor.GetPose();
        feedback.text = "Anchor Position: " + anchorPose.position + " Rotation: " + anchorPose.rotation;
#endif

        try
        {
            // Actually save
            await GetComponent<SpatialAnchorManager>().CreateAnchorAsync(cloudAnchor);
            feedback.text = string.Format("{0}\n{1}", feedback.text, "Saved: " + cloudAnchor.Identifier);
            // Store
            currentCloudAnchor = cloudAnchor;

            FirebaseExchanger firebase = FirebaseExchanger.Instance;
            bool? uploadResult = null;
            firebase.StartCoroutine(
                firebase.PutAnchorsAndWait(
                    currentCloudAnchor.Identifier,
                    DateTime.Now.AddHours(24),
                    succeeded => uploadResult = succeeded));
            while (uploadResult == null)
            {
                await Task.Delay(50);
            }

            if (uploadResult != true)
            {
                feedback.text = "Cloud anchor saved but Firebase registration failed. Try again.";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            feedback.text = ex.ToString();
            return false;
        }
    }
}

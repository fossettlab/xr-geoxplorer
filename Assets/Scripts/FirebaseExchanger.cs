using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Newtonsoft.Json;
using System.Text;
using UnityEngine.Networking;
using TMPro;

public class FirebaseExchanger : MonoBehaviour
{
    /// <summary>
    /// Gets current stored anchor information, downloads, checks for expiration.
    /// On creating a new ID, checks for conflicting name and uploads to Firebase.
    /// </summary>

    //Private variables
    List<AzureSpatialAnchorObject> anchorObjects = new List<AzureSpatialAnchorObject>();
    bool conflictFound;
    bool anchorsLoaded;

    //Public variables
    public string anchorName { get; set; } //this is set by the UI Input Field
#if UNITY_WSA
    public TextMeshPro feedback;
#elif UNITY_IOS || UNITY_ANDROID
    public TextMeshProUGUI feedback;
#endif

    // Initial settings
    void Start()
    {
        conflictFound = false;
        feedback.text = "Initializing Firebase Exchanger";
        StartCoroutine(FetchCurrentAnchors());
    }

    //Script to put (overwrite) the anchor selection on Firebase
    public void PutAnchors(string anchorIdentifier, DateTime expiration)
    {
        StartCoroutine(PutAnchorsRoutine(anchorIdentifier, expiration));
    }

    IEnumerator PutAnchorsRoutine(string anchorIdentifier, DateTime expiration)
    {
        while (!anchorsLoaded)
        {
            yield return null;
        }

        AzureSpatialAnchorObject anchorObject = new AzureSpatialAnchorObject();
        anchorObject.name = anchorName;
        anchorObject.identifier = anchorIdentifier;
        anchorObject.dateCreated = DateTime.Now;
        anchorObject.dateExpired = expiration;

        anchorObjects.Add(anchorObject);
        var json = JsonConvert.SerializeObject(anchorObjects);
        byte[] buffer = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest uwr = UnityWebRequest.Put("https://flasasharing.firebaseio.com/anchors.json", buffer))
        {
            uwr.SetRequestHeader("Content-Type", "application/json");
            yield return uwr.SendWebRequest();
            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to upload anchors to Firebase: {uwr.error}");
            }
        }
    }

    //Finds anchor name in stored anchor list
    public string FindAnchorByName()
    {
        string anchorToFind = null;
        foreach (var anchor in anchorObjects)
        {
            if (anchor.name == anchorName)
            {
                Debug.Log("Found " + anchor.name + ": " + anchor.identifier);
                anchorToFind = anchor.identifier;   
            }
        }
        return anchorToFind;
    }

    //Fetches the current list of anchor information on Firebase
    public IEnumerator FetchCurrentAnchors()
    {
        conflictFound = false;
        feedback.text = string.Format("{0}\n{1}", feedback.text, "Downloading from https://flasasharing.firebaseio.com/anchors.json");
        try
        {
            using (UnityWebRequest uwr = UnityWebRequest.Get("https://flasasharing.firebaseio.com/anchors.json"))
            {
                yield return uwr.SendWebRequest();

                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Failed to download anchors from Firebase: {uwr.error}");
                    yield break;
                }

                string responseText = uwr.downloadHandler.text;
                if (!string.IsNullOrEmpty(responseText) && responseText != "null")
                {
                    List<AzureSpatialAnchorObject> downloadedAnchors = JsonConvert.DeserializeObject<List<AzureSpatialAnchorObject>>(responseText);
                    if (downloadedAnchors != null)
                    {
                        foreach (var anchor in downloadedAnchors)
                        {
                            if (anchor.dateExpired > DateTime.Now)
                            {
                                anchorObjects.Add(anchor);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to parse anchors from Firebase: {ex.Message}");
        }
        finally
        {
            anchorsLoaded = true;
        }
    }

    public bool CheckForNameConflict(string potentialName)
    {
        conflictFound = false;

        foreach (var anchor in anchorObjects)
        {
            if (anchor.name == potentialName)
            {
                conflictFound = true;
            }
        }

        return conflictFound;
    }

    public bool CheckIfNameExists(string inputName)
    {
        bool nameExists = false;
        foreach (var anchor in anchorObjects)
        {
            if (anchor.name == inputName)
            {
                nameExists = true;
            }
        }

        return nameExists;
    }

	//Anchor class
	public class AzureSpatialAnchorObject
	{
		public string name { get; set; }
		public string identifier { get; set; }
		public DateTime dateCreated { get; set; }
		public DateTime dateExpired { get; set; }
	}
}

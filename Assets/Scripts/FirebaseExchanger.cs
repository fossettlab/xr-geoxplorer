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
    bool anchorsFetchSucceeded;
    bool lastFetchSucceeded;

    public bool AnchorsLoaded => anchorsLoaded;
    public bool AnchorsFetchSucceeded => anchorsFetchSucceeded;
    public bool LastFetchSucceeded => lastFetchSucceeded;

    //Public variables
    public string anchorName { get; set; } //this is set by the UI Input Field
    public TextMeshProUGUI feedback;

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

    public IEnumerator PutAnchorsAndWait(string anchorIdentifier, DateTime expiration, Action<bool> onComplete)
    {
        yield return PutAnchorsRoutine(anchorIdentifier, expiration, onComplete);
    }

    IEnumerator PutAnchorsRoutine(string anchorIdentifier, DateTime expiration, Action<bool> onComplete = null)
    {
        while (!anchorsLoaded)
        {
            yield return null;
        }

        yield return FetchAnchorsFromServer();
        if (!lastFetchSucceeded)
        {
            Debug.LogError("Refusing to upload anchors: pre-upload Firebase refresh failed.");
            onComplete?.Invoke(false);
            yield break;
        }

        if (CheckForNameConflict(anchorName))
        {
            Debug.LogError($"Refusing to upload anchor: name '{anchorName}' already exists in Firebase.");
            onComplete?.Invoke(false);
            yield break;
        }

        AzureSpatialAnchorObject anchorObject = new AzureSpatialAnchorObject();
        anchorObject.name = anchorName;
        anchorObject.identifier = anchorIdentifier;
        anchorObject.dateCreated = DateTime.Now;
        anchorObject.dateExpired = expiration;

        var uploadList = new List<AzureSpatialAnchorObject>(anchorObjects) { anchorObject };
        var json = JsonConvert.SerializeObject(uploadList);
        byte[] buffer = Encoding.UTF8.GetBytes(json);

        bool uploadSucceeded = false;
        using (UnityWebRequest uwr = UnityWebRequest.Put("https://flasasharing.firebaseio.com/anchors.json", buffer))
        {
            uwr.SetRequestHeader("Content-Type", "application/json");
            yield return uwr.SendWebRequest();
            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to upload anchors to Firebase: {uwr.error}");
            }
            else
            {
                uploadSucceeded = true;
            }
        }

        if (uploadSucceeded)
        {
            anchorObjects.Add(anchorObject);
        }

        onComplete?.Invoke(uploadSucceeded);
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
        yield return FetchAnchorsFromServer();
        anchorsLoaded = true;
    }

    public IEnumerator RefreshAnchorsFromServer()
    {
        yield return FetchAnchorsFromServer();
    }

    public IEnumerator RefreshAnchorsAndWait(Action<bool> onComplete)
    {
        yield return FetchAnchorsFromServer();
        onComplete?.Invoke(lastFetchSucceeded);
    }

    bool fetchInProgress;

    IEnumerator FetchAnchorsFromServer()
    {
        while (fetchInProgress)
        {
            yield return null;
        }

        fetchInProgress = true;
        try
        {
            conflictFound = false;
            lastFetchSucceeded = false;
            var fetchedAnchors = new List<AzureSpatialAnchorObject>();
            bool fetchSucceeded = false;

            feedback.text = string.Format("{0}\n{1}", feedback.text, "Downloading from https://flasasharing.firebaseio.com/anchors.json");
            using (UnityWebRequest uwr = UnityWebRequest.Get("https://flasasharing.firebaseio.com/anchors.json"))
            {
                yield return uwr.SendWebRequest();

                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Failed to download anchors from Firebase: {uwr.error}");
                }
                else
                {
                    string responseText = uwr.downloadHandler.text;
                    if (!string.IsNullOrEmpty(responseText) && responseText != "null")
                    {
                        try
                        {
                            List<AzureSpatialAnchorObject> downloadedAnchors =
                                JsonConvert.DeserializeObject<List<AzureSpatialAnchorObject>>(responseText);
                            if (downloadedAnchors != null)
                            {
                                foreach (var anchor in downloadedAnchors)
                                {
                                    if (anchor.dateExpired > DateTime.Now)
                                    {
                                        fetchedAnchors.Add(anchor);
                                    }
                                }

                                fetchSucceeded = true;
                            }
                            else
                            {
                                Debug.LogError("Firebase anchors response was not a JSON array; refusing to upload.");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"Failed to parse anchors from Firebase: {ex.Message}");
                        }
                    }
                    else
                    {
                        fetchSucceeded = true;
                    }
                }
            }

            lastFetchSucceeded = fetchSucceeded;
            if (fetchSucceeded)
            {
                anchorObjects.Clear();
                anchorObjects.AddRange(fetchedAnchors);
                anchorsFetchSucceeded = true;
            }
        }
        finally
        {
            fetchInProgress = false;
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

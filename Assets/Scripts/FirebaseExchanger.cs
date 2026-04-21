using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Newtonsoft.Json;
using System.Net;
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
        AzureSpatialAnchorObject anchorObject = new AzureSpatialAnchorObject();
        anchorObject.name = anchorName;
        anchorObject.identifier = anchorIdentifier; //Guid.NewGuid().ToString(); //This will be the ASA identifier
        anchorObject.dateCreated = DateTime.Now;
        anchorObject.dateExpired = expiration; //this is the time set by the ASA expiration

        anchorObjects.Add(anchorObject);
        var json = JsonConvert.SerializeObject(anchorObjects);

        var request = WebRequest.CreateHttp("https://flasasharing.firebaseio.com/anchors.json");
        request.Method = "PUT";
        request.ContentType = "application/json";
        var buffer = Encoding.UTF8.GetBytes(json);
        request.ContentLength = buffer.Length;
        request.GetRequestStream().Write(buffer, 0, buffer.Length);
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
        using (UnityWebRequest uwr = UnityWebRequest.Get("https://flasasharing.firebaseio.com/anchors.json"))
        {
            yield return uwr.SendWebRequest();

            //feedback.text = string.Format("{0}\n{1}", feedback.text, "Downloaded anchor data");
            //feedback.text = string.Format("{0}\n{1}", feedback.text, uwr.downloadHandler.text);

            //Continue if there are anchors stored, otherwise there's no point doing any more
            if (uwr.downloadHandler.text != "null")
            {
                List<AzureSpatialAnchorObject> downloadedAnchors = JsonConvert.DeserializeObject<List<AzureSpatialAnchorObject>>(uwr.downloadHandler.text);
                //feedback.text = string.Format("{0}\n{1}", feedback.text, downloadedAnchors.Count + " Anchors Stored on Firebase:");
                foreach (var anchor in downloadedAnchors)
                {
                    //Check if anchor has expired - if it has it's not added to the anchorObjects list and so when a new list is uploaded it won't be included
                    if (anchor.dateExpired > DateTime.Now)
                    {
                        anchorObjects.Add(anchor);
                        //feedback.text = string.Format("{0}\n{1}", feedback.text, anchor.name);
                    }
                }
            }
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

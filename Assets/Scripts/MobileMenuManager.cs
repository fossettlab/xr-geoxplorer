using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Threading.Tasks;
using TMPro;
using UnityEngine.UI;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Net;
using System.IO;
using System.Xml.Linq;
using System.Linq;

public class MobileMenuManager : MonoBehaviour
{
    public GameObject menuButton;
    public GameObject level1;
    public GameObject level2;
    public GameObject level3;
    public GameObject scrollContainer;
    public GameObject infoText;
    public GameObject minElevText;
    public GameObject maxElevText;
    public GameObject colorScale;
    public GameObject fetchButton;
    public Sprite placeholderSprite;

    public string apiKey;
    public string storageAccountName;
    public string azureContainerName;

    double numberOfRows;
    int numberOfPages;
    int numberOfModels;
    OutcropObject[] geoxoutcropModel;
    DEMObject[] geoxdemModel;
    CrystalLatticeObject[] geoxclModel;
    HandSampleObject[] geoxhsModel;
    string indexType;
    string newAzureContainerName;
    List<GameObject> itemButtons = new List<GameObject>();
    
#if UNITY_IOS
    string platformType = "ios";
#else
    string platformType = "android";
#endif

    public void OnMetaButtonSelect(string metaTypeIndex)
    {
        geoxoutcropModel = null;
        geoxdemModel = null;
        geoxclModel = null;
        geoxhsModel = null;

        level1.SetActive(false);
        level2.SetActive(true);
        indexType = metaTypeIndex;
        newAzureContainerName = azureContainerName + metaTypeIndex;
        StartCoroutine(FetchMetadata());
    }

    public void OnFeatureButtonSelect()
    {
        geoxoutcropModel = null;
        geoxdemModel = null;
        geoxclModel = null;
        geoxhsModel = null;

        level1.SetActive(false);
        level2.SetActive(true);
        StartCoroutine(FetchFeatured());
    }

    public void OnBackLevel1()
    {
        if (itemButtons.Count != 0)
        {
            foreach (var item in itemButtons)
            {
                Destroy(item);
            }
            itemButtons.Clear();
            Resources.UnloadUnusedAssets();
        }

#if UNITY_IOS || UNITY_ANDROID
        scrollContainer.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 300);
#endif
        level2.SetActive(false);
        level1.SetActive(true);
    }

    public void OnBackLevel2()
    {
        level3.SetActive(false);
        level2.SetActive(true);
    }

    IEnumerator FetchFeatured()
    {
        string url = RemoteConfig.Current.BuildContainerListUrl(platformType, "featured");
        print(url);

        List<OutcropObject> outcropModels = new List<OutcropObject>();
        List<DEMObject> demModels = new List<DEMObject>();
        List<HandSampleObject> hsModels = new List<HandSampleObject>();
        List<CrystalLatticeObject> clModels = new List<CrystalLatticeObject>();

        UnityWebRequest uwr = UnityWebRequest.Get(url);
        yield return uwr.SendWebRequest();
        if (uwr.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"{uwr.error} ({uwr.url})");
            yield break;
        }
        else
        {
            MemoryStream stream = new MemoryStream(uwr.downloadHandler.data);
            using (StreamReader reader = new StreamReader(stream))
            {
                XElement x = XElement.Parse(reader.ReadToEnd());
                IEnumerable<XElement> bloba = x.Element("Blobs").Elements("Blob");

                for (int i = 0; i < bloba.Count(); i++)
                {
                    var blob = bloba.ElementAt(i).Element("Metadata");
                    if (bloba.ElementAt(i).Element("Name").Value.Contains("outcrop"))
                    {
                        OutcropObject outcropObject = new OutcropObject();
                        outcropObject.author = (string)blob.Element("author");
                        outcropObject.country = (string)blob.Element("country");
                        outcropObject.dateAcquired = (string)blob.Element("dateAcquired");
                        outcropObject.description = (string)blob.Element("description");
                        outcropObject.geoDescription = (string)blob.Element("geoDescription");
                        outcropObject.isAssetBundle = (string)blob.Element("isAssetBundle");
                        outcropObject.latitude = (string)blob.Element("latitude");
                        outcropObject.lithologiesPresent = (string)blob.Element("lithologiesPresent");
                        outcropObject.locAccuracy = (string)blob.Element("locAccuracy");
                        outcropObject.locDescription = (string)blob.Element("locDescription");
                        outcropObject.longitude = (string)blob.Element("longitude");
                        outcropObject.modelName = (string)blob.Element("modelName");
                        outcropObject.prefabName = (string)blob.Element("prefabName");
                        outcropObject.structuresPresent = (string)blob.Element("structuresPresent");
                        outcropObject.timePeriod = (string)blob.Element("timePeriod");
                        outcropModels.Add(outcropObject);
                    }
                    else if (bloba.ElementAt(i).Element("Name").Value.Contains("dem"))
                    {
                        DEMObject demObject = new DEMObject();
                        demObject.author = (string)blob.Element("author");
                        demObject.description = (string)blob.Element("description");
                        demObject.geoDescription = (string)blob.Element("geoDescription");
                        demObject.isAssetBundle = (string)blob.Element("isAssetBundle");
                        demObject.latitude = (string)blob.Element("latitude");
                        demObject.longitude = (string)blob.Element("longitude");
                        demObject.modelName = (string)blob.Element("modelName");
                        demObject.prefabName = (string)blob.Element("prefabName");
                        demObject.planetaryBody = (string)blob.Element("planetaryBody");
                        demObject.elevMin = (string)blob.Element("elevMin");
                        demObject.elevMax = (string)blob.Element("elevMax");
                        demModels.Add(demObject);
                    }
                    else if (bloba.ElementAt(i).Element("Name").Value.Contains("handsample"))
                    {
                        HandSampleObject handSampleObject = new HandSampleObject();
                        handSampleObject.modelName = (string)blob.Element("modelName");
                        handSampleObject.author = (string)blob.Element("author");
                        handSampleObject.description = (string)blob.Element("description");
                        handSampleObject.isAssetBundle = (string)blob.Element("isAssetBundle");
                        handSampleObject.mineralGroup = (string)blob.Element("mineralGroup");
                        handSampleObject.locationOfCollection = (string)blob.Element("locationOfCollection");
                        handSampleObject.prefabName = (string)blob.Element("prefabName");
                        hsModels.Add(handSampleObject);
                    }
                    else if (bloba.ElementAt(i).Element("Name").Value.Contains("crystallattice"))
                    {
                        CrystalLatticeObject crystalLatticeObject = new CrystalLatticeObject();
                        crystalLatticeObject.author = (string)blob.Element("author");
                        crystalLatticeObject.description = (string)blob.Element("description");
                        crystalLatticeObject.isAssetBundle = (string)blob.Element("isAssetBundle");
                        crystalLatticeObject.mineralGroup = (string)blob.Element("mineralGroup");
                        crystalLatticeObject.prefabName = (string)blob.Element("prefabName");
                        crystalLatticeObject.modelName = (string)blob.Element("modelName");
                        crystalLatticeObject.symmetry = (string)blob.Element("symmetry");
                        clModels.Add(crystalLatticeObject);
                    }
                }
            }
        }

        geoxoutcropModel = outcropModels.ToArray();
        geoxdemModel = demModels.ToArray();
        geoxhsModel = hsModels.ToArray();
        geoxclModel = clModels.ToArray();
#if UNITY_IOS || UNITY_ANDROID
        numberOfModels = geoxoutcropModel.Length + geoxdemModel.Length + geoxclModel.Length + geoxhsModel.Length;
        numberOfRows = Math.Round((double)numberOfModels / 2, MidpointRounding.AwayFromZero);
        numberOfPages = Mathf.CeilToInt((float)numberOfRows / 5);
        scrollContainer.GetComponent<RectTransform>().sizeDelta = new Vector2(0, (float)numberOfRows * 550f);
#endif

        for (int i = 0; i < geoxoutcropModel.Length; i++)
        {
#if UNITY_IOS || UNITY_ANDROID
            GameObject newButton;
            newButton = Instantiate(menuButton, scrollContainer.transform);
            newButton.name = geoxoutcropModel[i].modelName + "-Button";
            newButton.transform.localPosition = new Vector3(0, i * -200, 0);
            newButton.GetComponentInChildren<TextMeshProUGUI>().text = geoxoutcropModel[i].modelName;
            int temp_i = i; //needs a dummy int to overwrite
            newButton.GetComponent<Button>().onClick.AddListener(() => OnItemButtonClicked(temp_i));
            itemButtons.Add(newButton);
#endif
        }

        for (int i = 0; i < geoxdemModel.Length; i++)
        {
#if UNITY_IOS || UNITY_ANDROID
            GameObject newButton;
            newButton = Instantiate(menuButton, scrollContainer.transform);
            newButton.name = geoxdemModel[i].modelName + "-Button";
            newButton.GetComponentInChildren<TextMeshProUGUI>().text = geoxdemModel[i].modelName;
            int temp_i = i; //needs a dummy int to overwrite
            newButton.GetComponent<Button>().onClick.AddListener(() => OnItemButtonClicked(temp_i));
            itemButtons.Add(newButton);
#endif
        }

        for (int i = 0; i < geoxclModel.Length; i++)
        {
#if UNITY_IOS || UNITY_ANDROID
            GameObject newButton;
            newButton = Instantiate(menuButton, scrollContainer.transform);
            newButton.name = geoxclModel[i].modelName + "-Button";
            newButton.transform.localPosition = new Vector3(0, i * -200, 0);
            newButton.GetComponentInChildren<TextMeshProUGUI>().text = geoxclModel[i].modelName;
            int temp_i = i; //needs a dummy int to overwrite
            newButton.GetComponent<Button>().onClick.AddListener(() => OnItemButtonClicked(temp_i));
            itemButtons.Add(newButton);
#endif
        }

        for (int i = 0; i < geoxhsModel.Length; i++)
        {
#if UNITY_IOS || UNITY_ANDROID
            GameObject newButton;
            newButton = Instantiate(menuButton, scrollContainer.transform);
            newButton.name = geoxhsModel[i].modelName + "-Button";
            newButton.transform.localPosition = new Vector3(0, i * -200, 0);
            newButton.GetComponentInChildren<TextMeshProUGUI>().text = geoxhsModel[i].modelName;
            int temp_i = i; //needs a dummy int to overwrite
            newButton.GetComponent<Button>().onClick.AddListener(() => OnItemButtonClicked(temp_i));
            itemButtons.Add(newButton);
#endif
        }

#if UNITY_IOS || UNITY_ANDROID
        pageIncrement = 5 / (float)numberOfRows;
        lowerPageIncrement = 0;
        upperPageIncrement = pageIncrement;
        pageNumber = 1;
        FetchThumbnailWrapper(0, 20);
#endif
    }



    IEnumerator FetchMetadata()
    {
        string url = RemoteConfig.Current.BuildContainerListUrl(platformType, "geoxplorer-" + indexType);
        print(url);
        UnityWebRequest uwr = UnityWebRequest.Get(url);
        yield return uwr.SendWebRequest();
        if (uwr.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"{uwr.error} ({uwr.url})");
            yield break;
        }
        else
        {
            MemoryStream stream = new MemoryStream(uwr.downloadHandler.data);
            using (StreamReader reader = new StreamReader(stream))
            {
                XElement x = XElement.Parse(reader.ReadToEnd());
                IEnumerable<XElement> bloba = x.Element("Blobs").Elements("Blob");

                if (indexType == "outcrop")
                {
                    //geoxoutcropModel = JsonConvert.DeserializeObject<OutcropObject>(request.downloadHandler.text);
                    List<OutcropObject> outcropModels = bloba.Elements("Metadata").Select(sv => new OutcropObject()
                    {
                        author = (string)sv.Element("author"),
                        country = (string)sv.Element("country"),
                        dateAcquired = (string)sv.Element("dateAcquired"),
                        description = (string)sv.Element("description"),
                        geoDescription = (string)sv.Element("geoDescription"),
                        isAssetBundle = (string)sv.Element("isAssetBundle"),
                        latitude = (string)sv.Element("latitude"),
                        lithologiesPresent = (string)sv.Element("lithologiesPresent"),
                        locAccuracy = (string)sv.Element("locAccuracy"),
                        locDescription = (string)sv.Element("locDescription"),
                        longitude = (string)sv.Element("longitude"),
                        modelName = (string)sv.Element("modelName"),
                        prefabName = (string)sv.Element("prefabName"),
                        structuresPresent = (string)sv.Element("structuresPresent"),
                        timePeriod = (string)sv.Element("timePeriod")
                    }).ToList();

                    for (int i = 0; i < bloba.Count(); i++)
                    {
                        var blob = bloba.ElementAt(i);
                        outcropModels[i].bundleName = blob.Element("Name").Value;
                    }

                    geoxoutcropModel = outcropModels.ToArray();
#if UNITY_IOS || UNITY_ANDROID
                    numberOfModels = geoxoutcropModel.Length;
                    numberOfRows = Math.Round((double)numberOfModels / 2, MidpointRounding.AwayFromZero);
                    numberOfPages = Mathf.CeilToInt((float)numberOfRows / 5);
                    scrollContainer.GetComponent<RectTransform>().sizeDelta = new Vector2(0, (float)numberOfRows * 550f);
#endif
                    //Array.Sort(geoxoutcropModel, delegate (OutcropObject x, OutcropObject y) { return string.Compare(x.modelName, y.modelName, StringComparison.Ordinal); });
                    for (int i = 0; i < geoxoutcropModel.Length; i++)
                    {
#if UNITY_IOS || UNITY_ANDROID
                        GameObject newButton;
                        newButton = Instantiate(menuButton, scrollContainer.transform);
                        newButton.name = geoxoutcropModel[i].modelName + "-Button";
                        newButton.transform.localPosition = new Vector3(0, i * -200, 0);
                        newButton.GetComponentInChildren<TextMeshProUGUI>().text = geoxoutcropModel[i].modelName;
                        int temp_i = i; //needs a dummy int to overwrite
                        newButton.GetComponent<Button>().onClick.AddListener(() => OnItemButtonClicked(temp_i));
                        itemButtons.Add(newButton);
#endif
                    }
#if UNITY_IOS || UNITY_ANDROID
                    pageIncrement = 5 / (float)numberOfRows;
                    lowerPageIncrement = 0;
                    upperPageIncrement = pageIncrement;
                    pageNumber = 1;
                    FetchThumbnailWrapper(0, 20);
#endif
                }
                else if (indexType == "dem")
                {
                    //geoxdemModel = JsonConvert.DeserializeObject<DEMObject>(request.downloadHandler.text);
                    List<DEMObject> demModels = bloba.Elements("Metadata").Select(sv => new DEMObject()
                    {
                        author = (string)sv.Element("author"),
                        description = (string)sv.Element("description"),
                        geoDescription = (string)sv.Element("geoDescription"),
                        isAssetBundle = (string)sv.Element("isAssetBundle"),
                        latitude = (string)sv.Element("latitude"),
                        longitude = (string)sv.Element("longitude"),
                        modelName = (string)sv.Element("modelName"),
                        prefabName = (string)sv.Element("prefabName"),
                        planetaryBody = (string)sv.Element("planetaryBody"),
                        elevMin = (string)sv.Element("elevMin"),
                        elevMax = (string)sv.Element("elevMax")
                    }).ToList();

                    for (int i = 0; i < bloba.Count(); i++)
                    {
                        var blob = bloba.ElementAt(i);
                        demModels[i].bundleName = blob.Element("Name").Value;
                    }

                    geoxdemModel = demModels.ToArray();

#if UNITY_IOS || UNITY_ANDROID
                    numberOfModels = geoxdemModel.Length;
                    numberOfRows = Math.Round((double)numberOfModels / 2, MidpointRounding.AwayFromZero);
                    numberOfPages = Mathf.CeilToInt((float)numberOfRows / 5);
                    scrollContainer.GetComponent<RectTransform>().sizeDelta = new Vector2(0, (float)numberOfRows * 550f);
#endif
                    //Array.Sort(geoxdemModel, delegate (DEMObject x, DEMObject y) { return string.Compare(x.modelName, y.modelName, StringComparison.Ordinal); });
                    for (int i = 0; i < geoxdemModel.Length; i++)
                    {
#if UNITY_IOS || UNITY_ANDROID
                        GameObject newButton;
                        newButton = Instantiate(menuButton, scrollContainer.transform);
                        newButton.name = geoxdemModel[i].modelName + "-Button";
                        newButton.GetComponentInChildren<TextMeshProUGUI>().text = geoxdemModel[i].modelName;
                        int temp_i = i; //needs a dummy int to overwrite
                        newButton.GetComponent<Button>().onClick.AddListener(() => OnItemButtonClicked(temp_i));
                        itemButtons.Add(newButton);
#endif
                    }

#if UNITY_IOS || UNITY_ANDROID
                    pageIncrement = 5 / (float)numberOfRows;
                    lowerPageIncrement = 0;
                    upperPageIncrement = pageIncrement;
                    pageNumber = 1;
                    FetchThumbnailWrapper(0, 20);
#endif
                }
                else if (indexType == "crystallattice")
                {
                    //geoxclModel = JsonConvert.DeserializeObject<CrystalLatticeObject>(request.downloadHandler.text);
                    List<CrystalLatticeObject> clModels = bloba.Elements("Metadata").Select(sv => new CrystalLatticeObject()
                    {
                        author = (string)sv.Element("author"),
                        description = (string)sv.Element("description"),
                        isAssetBundle = (string)sv.Element("isAssetBundle"),
                        mineralGroup = (string)sv.Element("mineralGroup"),
                        prefabName = (string)sv.Element("prefabName"),
                        modelName = (string)sv.Element("modelName"),
                        symmetry = (string)sv.Element("symmetry")
                    }).ToList();

                    for (int i = 0; i < bloba.Count(); i++)
                    {
                        var blob = bloba.ElementAt(i);
                        clModels[i].bundleName = blob.Element("Name").Value;
                    }

                    geoxclModel = clModels.ToArray();

#if UNITY_IOS || UNITY_ANDROID
                    numberOfModels = geoxclModel.Length;
                    numberOfRows = Math.Round((double)numberOfModels / 2, MidpointRounding.AwayFromZero);
                    numberOfPages = Mathf.CeilToInt((float)numberOfRows / 5);
                    scrollContainer.GetComponent<RectTransform>().sizeDelta = new Vector2(0, (float)numberOfRows * 550f);
#endif
                    //Array.Sort(geoxclModel, delegate (CrystalLatticeObject x, CrystalLatticeObject y) { return string.Compare(x.modelName, y.modelName, StringComparison.Ordinal); });
                    for (int i = 0; i < geoxclModel.Length; i++)
                    {
#if UNITY_IOS || UNITY_ANDROID
                        GameObject newButton;
                        newButton = Instantiate(menuButton, scrollContainer.transform);
                        newButton.name = geoxclModel[i].modelName + "-Button";
                        newButton.transform.localPosition = new Vector3(0, i * -200, 0);
                        newButton.GetComponentInChildren<TextMeshProUGUI>().text = geoxclModel[i].modelName;
                        int temp_i = i; //needs a dummy int to overwrite
                        newButton.GetComponent<Button>().onClick.AddListener(() => OnItemButtonClicked(temp_i));
                        itemButtons.Add(newButton);
#endif
                    }
#if UNITY_IOS || UNITY_ANDROID
                    pageIncrement = 5 / (float)numberOfRows;
                    lowerPageIncrement = 0;
                    upperPageIncrement = pageIncrement;
                    pageNumber = 1;
                    FetchThumbnailWrapper(0, 20);
#endif
                }
                else if (indexType == "handsample")
                {
                    //geoxhsModel = JsonConvert.DeserializeObject<HandSampleObject>(request.downloadHandler.text);
                    List<HandSampleObject> hsModels = bloba.Elements("Metadata").Select(sv => new HandSampleObject()
                    {
                        modelName = (string)sv.Element("modelName"),
                        author = (string)sv.Element("author"),
                        description = (string)sv.Element("description"),
                        isAssetBundle = (string)sv.Element("isAssetBundle"),
                        mineralGroup = (string)sv.Element("mineralGroup"),
                        locationOfCollection = (string)sv.Element("locationOfCollection"),
                        prefabName = (string)sv.Element("prefabName"),
                    }).ToList();

                    for (int i = 0; i < bloba.Count(); i++)
                    {
                        var blob = bloba.ElementAt(i);
                        hsModels[i].bundleName = blob.Element("Name").Value;
                    }

                    geoxhsModel = hsModels.ToArray();

#if UNITY_IOS || UNITY_ANDROID
                    numberOfModels = geoxhsModel.Length;
                    numberOfRows = Math.Round((double)numberOfModels / 2, MidpointRounding.AwayFromZero);
                    numberOfPages = Mathf.CeilToInt((float)numberOfRows / 5);
                    scrollContainer.GetComponent<RectTransform>().sizeDelta = new Vector2(0, (float)numberOfRows * 550f);
#endif
                    //Array.Sort(geoxhsModel, delegate (HandSampleObject x, HandSampleObject y) { return string.Compare(x.modelName, y.modelName, StringComparison.Ordinal); });
                    for (int i = 0; i < geoxhsModel.Length; i++)
                    {
#if UNITY_IOS || UNITY_ANDROID
                        GameObject newButton;
                        newButton = Instantiate(menuButton, scrollContainer.transform);
                        newButton.name = geoxhsModel[i].modelName + "-Button";
                        newButton.transform.localPosition = new Vector3(0, i * -200, 0);
                        newButton.GetComponentInChildren<TextMeshProUGUI>().text = geoxhsModel[i].modelName;
                        int temp_i = i; //needs a dummy int to overwrite
                        newButton.GetComponent<Button>().onClick.AddListener(() => OnItemButtonClicked(temp_i));
                        itemButtons.Add(newButton);
#endif
                    }
#if UNITY_IOS || UNITY_ANDROID
                    pageIncrement = 5 / (float)numberOfRows;
                    lowerPageIncrement = 0;
                    upperPageIncrement = pageIncrement;
                    pageNumber = 1;
                    FetchThumbnailWrapper(0, 20);
#endif
                }
            }
        }
    }

    public void OnItemButtonClicked(int buttonNumber)
    {
        level2.SetActive(false);
        level3.SetActive(true);

        print("here");

        if (indexType == "outcrop")
        {
            OutcropObject selectedModel = geoxoutcropModel[buttonNumber];
            string infoString = string.Format("{0}\nBy\n{1}\n\nLat:{2}  Lon:{3}\n\n{4}", selectedModel.modelName,selectedModel.author,selectedModel.latitude,selectedModel.longitude, selectedModel.description);
            infoText.GetComponent<TextMeshProUGUI>().text = infoString;
            fetchButton.GetComponent<DownloadButtonInteraction>().storageAccountName = storageAccountName;
            fetchButton.GetComponent<DownloadButtonInteraction>().containerName = newAzureContainerName;
            fetchButton.GetComponent<DownloadButtonInteraction>().prefabName = selectedModel.prefabName;
            fetchButton.GetComponent<DownloadButtonInteraction>().bundleName = selectedModel.bundleName.Replace("geoxplorer-outcrop/","");
        }
        else if (indexType == "dem")
        {
            DEMObject selectedModel = geoxdemModel[buttonNumber];
            string infoString = string.Format("{0}\nBy\n{1}\n\nLat:{2}  Lon:{3}\n\n{4}", selectedModel.modelName, selectedModel.author, selectedModel.latitude, selectedModel.longitude, selectedModel.description);
            infoText.GetComponent<TextMeshProUGUI>().text = infoString;
            minElevText.GetComponent<TextMeshProUGUI>().text = selectedModel.elevMin;
            maxElevText.GetComponent<TextMeshProUGUI>().text = selectedModel.elevMax;
            if (selectedModel.prefabName.Contains("IMG.blend"))
            {
                colorScale.SetActive(true);
            }
            else
            {
                colorScale.SetActive(false);
            }
            fetchButton.GetComponent<DownloadButtonInteraction>().storageAccountName = storageAccountName;
            fetchButton.GetComponent<DownloadButtonInteraction>().containerName = newAzureContainerName;
            fetchButton.GetComponent<DownloadButtonInteraction>().prefabName = selectedModel.prefabName;
            fetchButton.GetComponent<DownloadButtonInteraction>().bundleName = selectedModel.bundleName.Replace("geoxplorer-dem/", "");
        }
        else if (indexType == "crystallattice")
        {
            CrystalLatticeObject selectedModel = geoxclModel[buttonNumber];
            string infoString = string.Format("{0}\nBy\n{1}\n\n{2}\n\n{3}", selectedModel.modelName, selectedModel.author, selectedModel.mineralGroup, selectedModel.symmetry);
            infoText.GetComponent<TextMeshProUGUI>().text = infoString;
            fetchButton.GetComponent<DownloadButtonInteraction>().storageAccountName = storageAccountName;
            fetchButton.GetComponent<DownloadButtonInteraction>().containerName = newAzureContainerName;
            fetchButton.GetComponent<DownloadButtonInteraction>().prefabName = selectedModel.prefabName;
            fetchButton.GetComponent<DownloadButtonInteraction>().bundleName = selectedModel.bundleName.Replace("geoxplorer-crystallattice/", "");
        } else if (indexType == "handsample")
        {
            HandSampleObject selectedModel = geoxhsModel[buttonNumber];
            string infoString = string.Format("{0}\nBy\n{1}\n\n{2}\n\n{3}", selectedModel.modelName, selectedModel.author, selectedModel.mineralGroup, selectedModel.locationOfCollection);
            infoText.GetComponent<TextMeshProUGUI>().text = infoString;
            fetchButton.GetComponent<DownloadButtonInteraction>().storageAccountName = storageAccountName;
            fetchButton.GetComponent<DownloadButtonInteraction>().containerName = newAzureContainerName;
            fetchButton.GetComponent<DownloadButtonInteraction>().prefabName = selectedModel.prefabName;
            fetchButton.GetComponent<DownloadButtonInteraction>().bundleName = selectedModel.bundleName.Replace("geoxplorer-handsample/", "");
        }
    }

    float pageIncrement;
    float lowerPageIncrement;
    float upperPageIncrement;
    float scrollIncrement;
    float pageNumber;
    public void ScrollUpdate(float eventData)
    {
        scrollIncrement = 1 - eventData;
        if (scrollIncrement > upperPageIncrement)
        {
            if (pageNumber < numberOfPages - 1)
            {
                lowerPageIncrement = pageIncrement * pageNumber;
                pageNumber++;
                upperPageIncrement = pageIncrement * pageNumber;

                //print(lowerPageIncrement + " " + pageNumber + " " + upperPageIncrement);

                if (pageNumber - 2 > 0)
                {
                    int deleteLowerValue = (int)((pageNumber - 3) * 10);
                    int deleteUpperValue = (int)(((pageNumber - 3) * 10) + 10);
                    DestroyThumbnails(deleteLowerValue, deleteUpperValue);
                    //print("deleted: " + deleteLowerValue.ToString() + " to " + deleteUpperValue.ToString());
                }

                int lowerValue = (int)((pageNumber) * 10);
                int upperValue = (int)(((pageNumber) * 10) + 10);

                FetchThumbnailWrapper(lowerValue, upperValue);
                //print("loaded: " + lowerValue.ToString() + " to " + upperValue.ToString());
            }

        }

        if (scrollIncrement < lowerPageIncrement)
        {
            if (pageNumber > 2)
            {
                pageNumber--;
                upperPageIncrement = pageIncrement * pageNumber;
                lowerPageIncrement = pageIncrement * (pageNumber - 1);

                //print(lowerPageIncrement + " " + pageNumber + " " + upperPageIncrement);

                if (pageNumber + 2 < numberOfRows)
                {
                    int deleteLowerValue = (int)((pageNumber + 1) * 10);
                    int deleteUpperValue = (int)(((pageNumber + 1) * 10) + 10);
                    DestroyThumbnails(deleteLowerValue, deleteUpperValue);
                    //print("deleted: " + deleteLowerValue.ToString() + " to " + deleteUpperValue.ToString());
                }

                int lowerValue = (int)((pageNumber - 2) * 10);
                int upperValue = (int)(((pageNumber - 2) * 10) + 10);
                FetchThumbnailWrapper(lowerValue, upperValue);
                //print("loaded: " + lowerValue.ToString() + " to " + upperValue.ToString());
            }
        }
    }

    public void DestroyThumbnails(int firstEntry, int lastEntry)
    {
        for (int i = firstEntry; i < lastEntry; i++)
        {
            if (i < numberOfModels)
            {
                Sprite menuSprite = itemButtons[i].GetComponent<Image>().sprite;
                if (menuSprite != null)
                {
                    Destroy(menuSprite.texture);
                    Destroy(itemButtons[i].GetComponent<Image>().sprite);
                }
            }
        }
    }

    public void FetchThumbnailWrapper(int firstEntry, int lastEntry)
    {
        int tempCounter = 0;

        if (geoxoutcropModel != null)
        {
            for (int i = firstEntry; i < lastEntry; i++)
            {
                if (i < geoxoutcropModel.Length)
                {
                    indexType = "outcrop";
                    StartCoroutine(FetchThumbnail(geoxoutcropModel[i].prefabName, itemButtons[i + tempCounter]));
                }
                
            }
            tempCounter = tempCounter + geoxoutcropModel.Length;
        }

        if (geoxdemModel != null)
        {
            for (int i = firstEntry; i < lastEntry; i++)
            {
                if (i < geoxdemModel.Length)
                {
                    indexType = "dem";
                    StartCoroutine(FetchThumbnail(geoxdemModel[i].prefabName, itemButtons[i + tempCounter]));
                }
            }
            tempCounter = tempCounter + geoxdemModel.Length;
        }

        if (geoxhsModel != null)
        {
            for (int i = firstEntry; i < lastEntry; i++)
            {
                if (i < geoxhsModel.Length)
                {
                    indexType = "handsample";
                    StartCoroutine(FetchThumbnail(geoxhsModel[i].prefabName, itemButtons[i + tempCounter]));
                }
            }
            tempCounter = tempCounter + geoxhsModel.Length;
        }

        if (geoxclModel != null)
        {
            for (int i = firstEntry; i < lastEntry; i++)
            { 
                if (i < geoxclModel.Length)
                {
                    indexType = "crystallattice";
                    StartCoroutine(FetchThumbnail(geoxclModel[i].prefabName, itemButtons[i + tempCounter]));
                }
            }
            tempCounter = tempCounter + geoxclModel.Length;
        }
    }

    public void FetchThumbnailWrapper(int entry)
    {
        if (geoxoutcropModel != null)
        {
            indexType = "outcrop";
            StartCoroutine(FetchThumbnail(geoxoutcropModel[entry].prefabName, itemButtons[entry]));
        }

        if (geoxdemModel != null)
        {
            indexType = "dem";
            StartCoroutine(FetchThumbnail(geoxdemModel[entry].prefabName, itemButtons[entry]));
        }

        if (geoxhsModel != null)
        {
            indexType = "handsample";
            StartCoroutine(FetchThumbnail(geoxhsModel[entry].prefabName, itemButtons[entry]));
        }

        if (geoxclModel != null)
        {
            indexType = "crystallattice";
            StartCoroutine(FetchThumbnail(geoxclModel[entry].prefabName, itemButtons[entry]));
        }

    }

    IEnumerator FetchThumbnail(string prefabName, GameObject buttonObject)
    {
        UnityWebRequest uwrt = UnityWebRequestTexture.GetTexture(RemoteConfig.Current.BuildThumbnailUrl(indexType + "/" + prefabName + ".png"));
        yield return uwrt.SendWebRequest();
        if (uwrt.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"{uwrt.error} ({uwrt.url})");
            //buttonObject.GetComponent<Image>().sprite = placeholderSprite;
        }
        else
        {
            // Get downloaded asset bundle
            var texture = DownloadHandlerTexture.GetContent(uwrt);
            buttonObject.GetComponent<Image>().sprite = Sprite.Create(texture, new Rect(0, 0, 512, 512), Vector2.zero);
        }
    }

    int oldInputStringLength;
    public void SearchMetadata(string inputString)
    {
        if (inputString.Length > 2)
        {
            int counter = 0;
            foreach (var item in itemButtons)
            {
                item.SetActive(false);
                //Sprite menuSprite = item.GetComponent<Image>().sprite;
                //if (menuSprite != null)
                //{
                //    Destroy(menuSprite.texture);
                //}
            }

            Resources.UnloadUnusedAssets();

            for (int i = 0; i < itemButtons.Count; i++)
            {
                
                string itemUpper = itemButtons[i].name.ToUpper();
                string inputStringUpper = inputString.ToUpper();

                bool nameGood = itemUpper.Contains(inputStringUpper);
                if (nameGood)
                {
                    itemButtons[i].SetActive(true);
                    FetchThumbnailWrapper(i);
                    counter++;
                }
            }

            numberOfRows = Math.Round((double)counter / 2, MidpointRounding.AwayFromZero);
            numberOfPages = Mathf.CeilToInt((float)numberOfRows / 5);
            scrollContainer.GetComponent<RectTransform>().sizeDelta = new Vector2(0, (float)numberOfRows * 550f);

            pageIncrement = 5 / (float)numberOfRows;
            lowerPageIncrement = 0;
            upperPageIncrement = pageIncrement;
            pageNumber = 1;

            oldInputStringLength = inputString.Length;
        }
        else
        {

            if (oldInputStringLength == 3)
            {

                foreach (var item in itemButtons)
                {
                    item.SetActive(false);
                }
            }

            foreach (var item in itemButtons)
            {
                item.SetActive(true);
            }


            if (oldInputStringLength == 3)
            {
                Resources.UnloadUnusedAssets();
                numberOfRows = Math.Round((double)itemButtons.Count / 2, MidpointRounding.AwayFromZero);
                numberOfPages = Mathf.CeilToInt((float)numberOfRows / 5);
                scrollContainer.GetComponent<RectTransform>().sizeDelta = new Vector2(0, (float)numberOfRows * 550f);
                pageIncrement = 5 / (float)numberOfRows;
                lowerPageIncrement = 0;
                upperPageIncrement = pageIncrement;
                pageNumber = 1;
                FetchThumbnailWrapper(0, 20);
            }

            oldInputStringLength = inputString.Length;
        }
    }
}

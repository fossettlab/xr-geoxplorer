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

public class MenuManager : MonoBehaviour
{

    /// <summary>
    /// Controls the menu system after the user has entered the room. Covers both MobileAR and HoloLens UI interactions.
    /// 
    /// </summary>

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
    public string storageAccountName;
    public string azureContainerName;


#if UNITY_WSA
    public TextMeshPro pageIndicatorText;
    public int WSAPageNumber;
    public int numberOfWSAPages;
    public GameObject nextPageButton;
    public GameObject previousPageButton;
    string platformType = "wsa";
#elif UNITY_IOS
    string platformType = "ios";
#elif UNITY_ANDROID
    string platformTupe = "android";
#endif

    double numberOfRows;
    int numberOfPages;
    int numberOfModels;
    string indexType;
    float pageIncrement;
    float lowerPageIncrement;
    float upperPageIncrement;
    float scrollIncrement;
    float pageNumber;
    string newAzureContainerName;
    List<GameObject> itemButtons = new List<GameObject>();
    List<GameObject> searchedButtons = new List<GameObject>();
    int oldInputStringLength;
    int allWSApages;

    public void OnBackLevel1()
    {
        if (itemButtons.Count != 0)
        {
            foreach (var item in itemButtons)
            {
                Destroy(item);
            }
            itemButtons.Clear();
            searchedButtons.Clear();
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

    public void OnMetaButtonSelect(string metaTypeIndex)
    {
        level1.SetActive(false);
        level2.SetActive(true);
        indexType = metaTypeIndex;
        newAzureContainerName = azureContainerName + metaTypeIndex;
        StartCoroutine(FetchMetadata());
    }

    IEnumerator FetchMetadata()
    {
        string url = "https://haringerverdiag.blob.core.windows.net/" + platformType + "?restype=container&comp=list&include=metadata&prefix=geoxplorer-" + indexType;
        print(url);
        UnityWebRequest uwr = UnityWebRequest.Get(url);
        yield return uwr.SendWebRequest();
        if (uwr.isNetworkError)
        {
            print("Error while sending: " + uwr.error);
        }
        else
        {
            MemoryStream stream = new MemoryStream(uwr.downloadHandler.data);
            using (StreamReader reader = new StreamReader(stream))
            {
                XElement x = XElement.Parse(reader.ReadToEnd());
                IEnumerable<XElement> bloba = x.Element("Blobs").Elements("Blob");

#if UNITY_IOS || UNITY_ANDROID
                numberOfModels = bloba.Count();
                numberOfRows = Math.Round((double)numberOfModels / 2, MidpointRounding.AwayFromZero);
                numberOfPages = Mathf.CeilToInt((float)numberOfRows / 5);
                scrollContainer.GetComponent<RectTransform>().sizeDelta = new Vector2(0, (float)numberOfRows * 550f);
#endif


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
                        outcropObject.bundleName = bloba.ElementAt(i).Element("Name").Value;

#if UNITY_IOS || UNITY_ANDROID
                        GameObject newButton;
                        newButton = Instantiate(menuButton, scrollContainer.transform);
                        newButton.name = outcropObject.modelName + "-Button";
                        newButton.transform.localPosition = new Vector3(0, i * -200, 0);
                        newButton.GetComponentInChildren<TextMeshProUGUI>().text = outcropObject.modelName;
                        int temp_i = i; //needs a dummy int to overwrite
                        newButton.GetComponent<Button>().onClick.AddListener(() => OnItemButtonClicked(temp_i));
                        newButton.GetComponent<ButtonObjectType>().outcropObject = outcropObject;
                        itemButtons.Add(newButton);
#elif UNITY_WSA
                        GameObject newButton;
                        newButton = Instantiate(menuButton, scrollContainer.transform);
                        newButton.name = outcropObject.modelName + "-Button";
                        newButton.GetComponentInChildren<TextMeshPro>().text = outcropObject.modelName;
                        int temp_i = i; //needs a dummy int to overwrite
                        newButton.GetComponent<Interactable>().OnClick.AddListener(() => OnItemButtonClicked(temp_i));
                        newButton.SetActive(false);
                        newButton.GetComponent<ButtonObjectType>().outcropObject = outcropObject;
                        itemButtons.Add(newButton);
                        scrollContainer.GetComponent<GridObjectCollection>().UpdateCollection();
#endif
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
                        demObject.bundleName = bloba.ElementAt(i).Element("Name").Value;

#if UNITY_IOS || UNITY_ANDROID
                        GameObject newButton;
                        newButton = Instantiate(menuButton, scrollContainer.transform);
                        newButton.name = demObject.modelName + "-Button";
                        newButton.transform.localPosition = new Vector3(0, i * -200, 0);
                        newButton.GetComponentInChildren<TextMeshProUGUI>().text = demObject.modelName;
                        int temp_i = i; //needs a dummy int to overwrite
                        newButton.GetComponent<Button>().onClick.AddListener(() => OnItemButtonClicked(temp_i));
                        newButton.GetComponent<ButtonObjectType>().demObject = demObject;
                        itemButtons.Add(newButton);
#elif UNITY_WSA
                        GameObject newButton;
                        newButton = Instantiate(menuButton, scrollContainer.transform);
                        newButton.name = demObject.modelName + "-Button";
                        newButton.GetComponentInChildren<TextMeshPro>().text = demObject.modelName;
                        int temp_i = i; //needs a dummy int to overwrite
                        newButton.GetComponent<Interactable>().OnClick.AddListener(() => OnItemButtonClicked(temp_i));
                        newButton.GetComponent<ButtonObjectType>().demObject = demObject;
                        newButton.SetActive(false);
                        itemButtons.Add(newButton);
                        scrollContainer.GetComponent<GridObjectCollection>().UpdateCollection();
#endif
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
                        handSampleObject.bundleName = bloba.ElementAt(i).Element("Name").Value;

#if UNITY_IOS || UNITY_ANDROID
                        GameObject newButton;
                        newButton = Instantiate(menuButton, scrollContainer.transform);
                        newButton.name = handSampleObject.modelName + "-Button";
                        newButton.transform.localPosition = new Vector3(0, i * -200, 0);
                        newButton.GetComponentInChildren<TextMeshProUGUI>().text = handSampleObject.modelName;
                        int temp_i = i; //needs a dummy int to overwrite
                        newButton.GetComponent<Button>().onClick.AddListener(() => OnItemButtonClicked(temp_i));
                        newButton.GetComponent<ButtonObjectType>().handSampleObject = handSampleObject;
                        itemButtons.Add(newButton);
#elif UNITY_WSA
                        GameObject newButton;
                        newButton = Instantiate(menuButton, scrollContainer.transform);
                        newButton.name = handSampleObject.modelName + "-Button";
                        newButton.GetComponentInChildren<TextMeshPro>().text = handSampleObject.modelName;
                        int temp_i = i; //needs a dummy int to overwrite
                        newButton.GetComponent<Interactable>().OnClick.AddListener(() => OnItemButtonClicked(temp_i));
                        newButton.GetComponent<ButtonObjectType>().handSampleObject = handSampleObject;
                        newButton.SetActive(false);
                        itemButtons.Add(newButton);
                        scrollContainer.GetComponent<GridObjectCollection>().UpdateCollection();
#endif
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
                        crystalLatticeObject.bundleName = bloba.ElementAt(i).Element("Name").Value;

#if UNITY_IOS || UNITY_ANDROID
                        GameObject newButton;
                        newButton = Instantiate(menuButton, scrollContainer.transform);
                        newButton.name = crystalLatticeObject.modelName + "-Button";
                        newButton.transform.localPosition = new Vector3(0, i * -200, 0);
                        newButton.GetComponentInChildren<TextMeshProUGUI>().text = crystalLatticeObject.modelName;
                        int temp_i = i; //needs a dummy int to overwrite
                        newButton.GetComponent<Button>().onClick.AddListener(() => OnItemButtonClicked(temp_i));
                        newButton.GetComponent<ButtonObjectType>().crystalLatticeObject = crystalLatticeObject;
                        itemButtons.Add(newButton);
#elif UNITY_WSA
                        GameObject newButton;
                        newButton = Instantiate(menuButton, scrollContainer.transform);
                        newButton.name = crystalLatticeObject.modelName + "-Button";
                        newButton.GetComponentInChildren<TextMeshPro>().text = crystalLatticeObject.modelName;
                        int temp_i = i; //needs a dummy int to overwrite
                        newButton.GetComponent<Interactable>().OnClick.AddListener(() => OnItemButtonClicked(temp_i));
                        newButton.GetComponent<ButtonObjectType>().crystalLatticeObject = crystalLatticeObject;
                        newButton.SetActive(false);
                        itemButtons.Add(newButton);
                        scrollContainer.GetComponent<GridObjectCollection>().UpdateCollection();
#endif
                    }
#if UNITY_WSA
                    pageIndicatorText.text = "Loading menu - " + (int)(((float)i / (float)bloba.Count())*100) + "%";
#endif
                    yield return null;

                }
            }
        }


#if UNITY_WSA
        searchedButtons = itemButtons;
        numberOfWSAPages = Mathf.CeilToInt(itemButtons.Count / 15f);
        allWSApages = numberOfWSAPages;
        WSAPageNumber = 1;
        DisplayWSAPage(WSAPageNumber);

#elif UNITY_IOS || UNITY_ANDROID
        pageIncrement = 5 / (float)numberOfRows;
        lowerPageIncrement = 0;
        upperPageIncrement = pageIncrement;
        pageNumber = 1;
        FetchThumbnailWrapper(0, 20);
#endif
    }


#if UNITY_WSA
    public void DisplayWSAPage(int WSAPageNo)
    {
        if (WSAPageNo == numberOfWSAPages)
        {
            nextPageButton.GetComponent<Interactable>().enabled = false;
        }
        else
        {
            nextPageButton.GetComponent<Interactable>().enabled = true;
        }

        if (WSAPageNo == 1)
        {
            previousPageButton.GetComponent<Interactable>().enabled = false;
        }
        else
        {
            previousPageButton.GetComponent<Interactable>().enabled = true;
        }

        int lowerButtonNumber = (15 * WSAPageNo) - 15;
        int upperButtonNumber = (15 * WSAPageNo) - 1;

        foreach (var item in itemButtons)
        {
            item.SetActive(false);
        }

        if (upperButtonNumber >= searchedButtons.Count)
        {
            upperButtonNumber = searchedButtons.Count - 1;
        }

        for (int i = lowerButtonNumber; i <= upperButtonNumber; i++)
        {
            searchedButtons[i].SetActive(true);
        }

        scrollContainer.GetComponent<GridObjectCollection>().UpdateCollection();
        WSAPageNumber = WSAPageNo;
        pageIndicatorText.text = "Page " + WSAPageNo + " of " + numberOfWSAPages;
    }

    public void nextWSAPage()
    {
        DisplayWSAPage(WSAPageNumber + 1);
    }

    public void previousWSAPage()
    {
        DisplayWSAPage(WSAPageNumber - 1);
    }
#endif

    public void OnItemButtonClicked(int buttonNumber)
    {
        level2.SetActive(false);
        level3.SetActive(true);

        if (itemButtons[buttonNumber].GetComponent<ButtonObjectType>().outcropObject.modelName != null)
        {
            OutcropObject selectedModel = itemButtons[buttonNumber].GetComponent<ButtonObjectType>().outcropObject;
            string infoString = string.Format("{0}\nBy\n{1}\n\nLat:{2}  Lon:{3}\n\n{4}", selectedModel.modelName, selectedModel.author, selectedModel.latitude, selectedModel.longitude, selectedModel.description);
#if UNITY_WSA
            infoText.GetComponent<TextMeshPro>().text = infoString;
#elif UNITY_IOS || UNITY_ANDROID
            infoText.GetComponent<TextMeshProUGUI>().text = infoString;
#endif
            fetchButton.GetComponent<DownloadButtonInteraction>().storageAccountName = storageAccountName;
            fetchButton.GetComponent<DownloadButtonInteraction>().containerName = newAzureContainerName;
            fetchButton.GetComponent<DownloadButtonInteraction>().prefabName = selectedModel.prefabName;
            fetchButton.GetComponent<DownloadButtonInteraction>().bundleName = selectedModel.bundleName;
            fetchButton.GetComponent<DownloadButtonInteraction>().modelName = selectedModel.modelName;
        }
        else if (itemButtons[buttonNumber].GetComponent<ButtonObjectType>().demObject.modelName != null)
        {
            DEMObject selectedModel = itemButtons[buttonNumber].GetComponent<ButtonObjectType>().demObject;
            string infoString = string.Format("{0}\nBy\n{1}\n\nLat:{2}  Lon:{3}\n\n{4}", selectedModel.modelName, selectedModel.author, selectedModel.latitude, selectedModel.longitude, selectedModel.description);
#if UNITY_WSA
            infoText.GetComponent<TextMeshPro>().text = infoString;
#elif UNITY_IOS || UNITY_ANDROID
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
#endif
            fetchButton.GetComponent<DownloadButtonInteraction>().storageAccountName = storageAccountName;
            fetchButton.GetComponent<DownloadButtonInteraction>().containerName = newAzureContainerName;
            fetchButton.GetComponent<DownloadButtonInteraction>().prefabName = selectedModel.prefabName;
            fetchButton.GetComponent<DownloadButtonInteraction>().bundleName = selectedModel.bundleName;
            fetchButton.GetComponent<DownloadButtonInteraction>().modelName = selectedModel.modelName;
        }
        else if (itemButtons[buttonNumber].GetComponent<ButtonObjectType>().handSampleObject.modelName != null)
        {
            HandSampleObject selectedModel = itemButtons[buttonNumber].GetComponent<ButtonObjectType>().handSampleObject;
            string infoString = string.Format("{0}\nBy\n{1}\n\n{2}\n\n{3}", selectedModel.modelName, selectedModel.author, selectedModel.mineralGroup, selectedModel.locationOfCollection);
#if UNITY_WSA
            infoText.GetComponent<TextMeshPro>().text = infoString;
#elif UNITY_IOS || UNITY_ANDROID
            infoText.GetComponent<TextMeshProUGUI>().text = infoString;
#endif
            fetchButton.GetComponent<DownloadButtonInteraction>().storageAccountName = storageAccountName;
            fetchButton.GetComponent<DownloadButtonInteraction>().containerName = newAzureContainerName;
            fetchButton.GetComponent<DownloadButtonInteraction>().prefabName = selectedModel.prefabName;
            fetchButton.GetComponent<DownloadButtonInteraction>().bundleName = selectedModel.bundleName;
            fetchButton.GetComponent<DownloadButtonInteraction>().modelName = selectedModel.modelName;
        }
        else if (itemButtons[buttonNumber].GetComponent<ButtonObjectType>().crystalLatticeObject.modelName != null)
        {
            CrystalLatticeObject selectedModel = itemButtons[buttonNumber].GetComponent<ButtonObjectType>().crystalLatticeObject;
            string infoString = string.Format("{0}\nBy\n{1}\n\n{2}\n\n{3}", selectedModel.modelName, selectedModel.author, selectedModel.mineralGroup, selectedModel.symmetry);
#if UNITY_WSA
            infoText.GetComponent<TextMeshPro>().text = infoString;
#elif UNITY_IOS || UNITY_ANDROID
            infoText.GetComponent<TextMeshProUGUI>().text = infoString;
#endif
            fetchButton.GetComponent<DownloadButtonInteraction>().storageAccountName = storageAccountName;
            fetchButton.GetComponent<DownloadButtonInteraction>().containerName = newAzureContainerName;
            fetchButton.GetComponent<DownloadButtonInteraction>().prefabName = selectedModel.prefabName;
            fetchButton.GetComponent<DownloadButtonInteraction>().bundleName = selectedModel.bundleName;
            fetchButton.GetComponent<DownloadButtonInteraction>().modelName = selectedModel.modelName;
        }
    }

    public void FetchThumbnailWrapper(int firstEntry, int lastEntry)
    {
        for (int i = firstEntry; i < lastEntry; i++)
        {
            if (i < itemButtons.Count())
            {
                StartCoroutine(FetchThumbnail(itemButtons[i]));
            }
        }
    }

    public void FetchThumbnailWrapper(int entry)
    {
        StartCoroutine(FetchThumbnail(itemButtons[entry]));
    }

    IEnumerator FetchThumbnail(GameObject buttonObject)
    {
        UnityWebRequest uwrt;
        if (buttonObject.GetComponent<ButtonObjectType>().outcropObject.modelName != null)
        {
            uwrt = UnityWebRequestTexture.GetTexture("https://haringerverdiag.blob.core.windows.net/thumbnails/outcrop/" + buttonObject.GetComponent<ButtonObjectType>().outcropObject.prefabName + ".png");
            yield return uwrt.SendWebRequest();
            if (uwrt.isNetworkError || uwrt.isHttpError)
            {
                //buttonObject.GetComponent<Image>().sprite = placeholderSprite;
            }
            else
            {
                // Get downloaded asset bundle
                var texture = DownloadHandlerTexture.GetContent(uwrt);
                buttonObject.GetComponent<Image>().sprite = Sprite.Create(texture, new Rect(0, 0, 512, 512), Vector2.zero);
            }
        }
        else if (buttonObject.GetComponent<ButtonObjectType>().demObject.modelName != null)
        {
            uwrt = UnityWebRequestTexture.GetTexture("https://haringerverdiag.blob.core.windows.net/thumbnails/dem/" + buttonObject.GetComponent<ButtonObjectType>().demObject.prefabName + ".png");
            yield return uwrt.SendWebRequest();
            if (uwrt.isNetworkError || uwrt.isHttpError)
            {
                //buttonObject.GetComponent<Image>().sprite = placeholderSprite;
            }
            else
            {
                // Get downloaded asset bundle
                var texture = DownloadHandlerTexture.GetContent(uwrt);
                buttonObject.GetComponent<Image>().sprite = Sprite.Create(texture, new Rect(0, 0, 512, 512), Vector2.zero);
            }
        }
        else if (buttonObject.GetComponent<ButtonObjectType>().handSampleObject.modelName != null)
        {
            uwrt = UnityWebRequestTexture.GetTexture("https://haringerverdiag.blob.core.windows.net/thumbnails/handsample/" + buttonObject.GetComponent<ButtonObjectType>().handSampleObject.prefabName + ".png");
            yield return uwrt.SendWebRequest();
            if (uwrt.isNetworkError || uwrt.isHttpError)
            {
                //buttonObject.GetComponent<Image>().sprite = placeholderSprite;
            }
            else
            {
                // Get downloaded asset bundle
                var texture = DownloadHandlerTexture.GetContent(uwrt);
                buttonObject.GetComponent<Image>().sprite = Sprite.Create(texture, new Rect(0, 0, 512, 512), Vector2.zero);
            }
        }
        else if (buttonObject.GetComponent<ButtonObjectType>().crystalLatticeObject.modelName != null)
        {
            uwrt = UnityWebRequestTexture.GetTexture("https://haringerverdiag.blob.core.windows.net/thumbnails/crystallattice/" + buttonObject.GetComponent<ButtonObjectType>().crystalLatticeObject.prefabName + ".png");
            yield return uwrt.SendWebRequest();
            if (uwrt.isNetworkError || uwrt.isHttpError)
            {
                //buttonObject.GetComponent<Image>().sprite = placeholderSprite;
            }
            else
            {
                // Get downloaded asset bundle
                var texture = DownloadHandlerTexture.GetContent(uwrt);
                buttonObject.GetComponent<Image>().sprite = Sprite.Create(texture, new Rect(0, 0, 512, 512), Vector2.zero);
            }
        }
    }

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

                if (pageNumber - 2 > 0)
                {
                    int deleteLowerValue = (int)((pageNumber - 3) * 10);
                    int deleteUpperValue = (int)(((pageNumber - 3) * 10) + 10);
                    DestroyThumbnails(deleteLowerValue, deleteUpperValue);
                }

                int lowerValue = (int)((pageNumber) * 10);
                int upperValue = (int)(((pageNumber) * 10) + 10);

                FetchThumbnailWrapper(lowerValue, upperValue);
            }

        }

        if (scrollIncrement < lowerPageIncrement)
        {
            if (pageNumber > 2)
            {
                pageNumber--;
                upperPageIncrement = pageIncrement * pageNumber;
                lowerPageIncrement = pageIncrement * (pageNumber - 1);

                if (pageNumber + 2 < numberOfRows)
                {
                    int deleteLowerValue = (int)((pageNumber + 1) * 10);
                    int deleteUpperValue = (int)(((pageNumber + 1) * 10) + 10);
                    DestroyThumbnails(deleteLowerValue, deleteUpperValue);
                }

                int lowerValue = (int)((pageNumber - 2) * 10);
                int upperValue = (int)(((pageNumber - 2) * 10) + 10);
                FetchThumbnailWrapper(lowerValue, upperValue);
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

    public void SearchMetadata(string inputString)
    {
        
        if (inputString.Length > 2)
        {
            searchedButtons = new List<GameObject>();
            int counter = 0;

#if UNITY_IOS || UNITY_ANDROID
            foreach (var item in itemButtons)
            {
                item.SetActive(false);
            }
#endif
            Resources.UnloadUnusedAssets();
            

            for (int i = 0; i < itemButtons.Count; i++)
            {
                string itemUpper = itemButtons[i].name.ToUpper();
                string inputStringUpper = inputString.ToUpper();

                bool nameGood = itemUpper.Contains(inputStringUpper);
                if (nameGood)
                {
#if UNITY_WSA
                    searchedButtons.Add(itemButtons[i]);
#elif UNITY_IOS || UNITY_ANDROID
                    itemButtons[i].SetActive(true);
                    FetchThumbnailWrapper(i);
#endif
                    counter++;
                }
            }

#if UNITY_WSA
            numberOfWSAPages = Mathf.CeilToInt(counter / 15f);
            WSAPageNumber = 1;
            DisplayWSAPage(WSAPageNumber);
#elif UNITY_IOS || UNITY_ANDROID
            numberOfRows = Math.Round((double)counter / 2, MidpointRounding.AwayFromZero);
            numberOfPages = Mathf.CeilToInt((float)numberOfRows / 5);
            scrollContainer.GetComponent<RectTransform>().sizeDelta = new Vector2(0, (float)numberOfRows * 550f);

            pageIncrement = 5 / (float)numberOfRows;
            lowerPageIncrement = 0;
            upperPageIncrement = pageIncrement;
            pageNumber = 1;
#endif

            oldInputStringLength = inputString.Length;
        }
        else
        {
#if UNITY_WSA
            searchedButtons = itemButtons;
#endif

            if (oldInputStringLength == 3)
            {

                foreach (var item in itemButtons)
                {
                    item.SetActive(false);
                }
            }

            foreach (var item in itemButtons)
            {
#if UNITY_WSA
                numberOfWSAPages = allWSApages;
                WSAPageNumber = 1;
                DisplayWSAPage(WSAPageNumber);
#elif UNITY_IOS || UNITY_ANDROID
                item.SetActive(true);
#endif
            }


            if (oldInputStringLength == 3)
            {
                Resources.UnloadUnusedAssets();
#if UNITY_IOS || UNITY_ANDROID
                numberOfRows = Math.Round((double)itemButtons.Count / 2, MidpointRounding.AwayFromZero);
                numberOfPages = Mathf.CeilToInt((float)numberOfRows / 5);
                scrollContainer.GetComponent<RectTransform>().sizeDelta = new Vector2(0, (float)numberOfRows * 550f);
                pageIncrement = 5 / (float)numberOfRows;
                lowerPageIncrement = 0;
                upperPageIncrement = pageIncrement;
                pageNumber = 1;
                FetchThumbnailWrapper(0, 20);
#endif
            }

            oldInputStringLength = inputString.Length;
        }
    }
}

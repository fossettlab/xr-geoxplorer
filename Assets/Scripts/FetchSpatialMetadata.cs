using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System;
using System.Net;
using System.IO;
using System.Xml.Linq;
using System.Linq;

public class FetchSpatialMetadata : MonoBehaviour
{
    public string[] indexType;
    public string apiKey;

    public GameObject modelIcon;
    string platformType;
    PlanetManager planetManager;
    ObjectCoordinates newOC;

    // Start is called before the first frame update
    void Start()
    {
        planetManager = TableAnchor.instance.GetComponent<PlanetManager>();
        planetManager.objectCoordinates.Clear();
#if UNITY_WSA
        platformType = "x86";
#elif UNITY_IOS
        platformType = "ios";
#elif UNITY_ANDROID
        platformType = "android";
#endif
        for (int i = 0; i < indexType.Length; i++)
        {
            StartCoroutine(FetchMetadata(indexType[i]));
        }

        //StartCoroutine(FetchStraboDataset());
        
    }

    IEnumerator FetchMetadata(string type)
    {

        
        //string url = "https://geobase.search.windows.net/indexes/geox" + type + "-" + platformType + "-index/docs?api-version=2019-05-06&api-key=" + apiKey + "&search=*&$top=1000";
        string url = "https://haringerverdiag.blob.core.windows.net/" + platformType + "?restype=container&comp=list&include=metadata&prefix=geoxplorer-" + type;
        //UnityWebRequest request = UnityWebRequest.Get(url);
        //yield return request.SendWebRequest();
        print(url);
        
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        request.Method = "GET";
        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        yield return response;

        if (response.StatusCode == HttpStatusCode.OK)
        {
            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                XElement x = XElement.Parse(reader.ReadToEnd());
                IEnumerable<XElement> bloba = x.Element("Blobs").Elements("Blob");

                if (type == "outcrop")
                {
                    //OutcropObject geoxoutcropModel = JsonConvert.DeserializeObject<OutcropObject>(request.downloadHandler.text);
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

                    OutcropObject[] geoxoutcropModel = outcropModels.ToArray();
                    
                    for (int i = 0; i < geoxoutcropModel.Length; i++)
                    {
                        GameObject newIcon;
                        newIcon = Instantiate(modelIcon);
                        newIcon.transform.parent = this.transform;
                        newIcon.name = geoxoutcropModel[i].modelName;
                        float latitude = float.Parse(geoxoutcropModel[i].latitude);
                        float longitude = float.Parse(geoxoutcropModel[i].longitude);

                        newOC = new ObjectCoordinates();
                        newOC.modelName = geoxoutcropModel[i].modelName;
                        newOC.latitude = latitude;
                        newOC.longitude = longitude;
                        newOC.prefabName = geoxoutcropModel[i].prefabName;
                        newOC.storageAccountName = "haringerverdiag";
                        newOC.containerName = "geoxplorer-" + type;
                        newOC.bundleName = geoxoutcropModel[i].bundleName.Replace("geoxplorer-outcrop/","");

                        planetManager.objectCoordinates.Add(newOC);

                        float zpos = Mathf.Cos(latitude * Mathf.Deg2Rad) * Mathf.Sin((longitude - 90) * Mathf.Deg2Rad) * -1;
                        float xpos = Mathf.Cos(latitude * Mathf.Deg2Rad) * Mathf.Cos((longitude - 90) * Mathf.Deg2Rad) * -1;
                        float ypos = Mathf.Sin(latitude * Mathf.Deg2Rad);

                        newIcon.transform.localPosition = new Vector3(xpos, ypos, zpos);
                    }
                }
                else if (type == "dem")
                {
                    //DEMObject geoxdemModel = JsonConvert.DeserializeObject<DEMObject>(request.downloadHandler.text);
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

                    DEMObject[] geoxdemModel = demModels.ToArray();

                    for (int i = 0; i < geoxdemModel.Length; i++)
                    {
                        if (geoxdemModel[i].planetaryBody == gameObject.name)
                        {
                            GameObject newIcon;
                            newIcon = Instantiate(modelIcon);
                            newIcon.transform.parent = this.transform;
                            newIcon.name = geoxdemModel[i].modelName;
                            float latitude = float.Parse(geoxdemModel[i].latitude);
                            float longitude = float.Parse(geoxdemModel[i].longitude);

                            newOC = new ObjectCoordinates();
                            newOC.modelName = geoxdemModel[i].modelName;
                            newOC.latitude = latitude;
                            newOC.longitude = longitude;
                            newOC.prefabName = geoxdemModel[i].prefabName;
                            newOC.storageAccountName = "haringerverdiag";
                            newOC.containerName = "geoxplorer-" + type;
                            newOC.bundleName = geoxdemModel[i].bundleName.Replace("geoxplorer-outcrop","");

                            planetManager.objectCoordinates.Add(newOC);

                            float zpos = Mathf.Cos(latitude * Mathf.Deg2Rad) * Mathf.Sin((longitude - 90) * Mathf.Deg2Rad) * -1;
                            float xpos = Mathf.Cos(latitude * Mathf.Deg2Rad) * Mathf.Cos((longitude - 90) * Mathf.Deg2Rad) * -1;
                            float ypos = Mathf.Sin(latitude * Mathf.Deg2Rad);

                            newIcon.transform.localPosition = new Vector3(xpos, ypos, zpos);
                        }
                    }
                }
            }

        }
    }

    IEnumerator FetchStraboDataset()
    {
        string url = "https://strabospot.org/search/datasets.json";
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        StraboDataset straboDataset = JsonConvert.DeserializeObject<StraboDataset>(request.downloadHandler.text);

        for (int i = 0; i < straboDataset.features.Length; i++)
        {
            GameObject newIcon;
            newIcon = Instantiate(modelIcon);
            newIcon.transform.parent = this.transform;
            newIcon.name = straboDataset.features[i].type;

            float latitude = straboDataset.features[i].geometry.coordinates[1];
            float longitude = straboDataset.features[i].geometry.coordinates[0];

            planetManager.spotCoordinates.Add(straboDataset.features[i]);

            float zpos = Mathf.Cos(latitude * Mathf.Deg2Rad) * Mathf.Sin((longitude - 90) * Mathf.Deg2Rad) * -1;
            float xpos = Mathf.Cos(latitude * Mathf.Deg2Rad) * Mathf.Cos((longitude - 90) * Mathf.Deg2Rad) * -1;
            float ypos = Mathf.Sin(latitude * Mathf.Deg2Rad);

            newIcon.transform.localPosition = new Vector3(xpos, ypos, zpos);
            newIcon.GetComponent<Renderer>().material.color = Color.red;

        }
    }
}


public class StraboDataset
{
    public string type { get; set; }
    public StraboDatasetFeature[] features { get; set; }
}

public class StraboDatasetFeature
{
    public string type { get; set; }
    public StraboDatasetGeometry geometry { get; set; }
    public StraboDatasetProperties properties { get; set; }
}

public class StraboDatasetGeometry
{
    public string type { get; set; }
    public float[] coordinates { get; set; }
}

public class StraboDatasetProperties
{
    public string name { get; set; }
    public string projectname { get; set; }
    public long id { get; set; }
    public int count { get; set; }
    public string owner { get; set; }
}
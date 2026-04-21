using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class BuildGlobe : MonoBehaviour {

    public GameObject planeSegement;
    public string defaultInstrument;
    public string defaultResolution;
    public string defaulImageType;
    DateTime dateNow = DateTime.Now;
    public Material planetTileMaterial;

	// Use this for initialization
	void Start () {
        dateNow = DateTime.Now;
        string defaultDate = dateNow.AddDays(-1).ToString("yyyy-MM-dd");
        GenerateGlobe(defaultInstrument, defaultResolution, defaulImageType, defaultDate);
	}

    private void GenerateGlobe(string instrument, string resolution, string imageType, string imageDate)
    {
        float deltaLat = 36;
        float deltaLon = 36;
        float startLat = 90;
        float startLon = 0;

        GameObject[] tileplanes = GameObject.FindGameObjectsWithTag("TilePlane");
        if (tileplanes.Length > 0)
        {
            foreach (var go in tileplanes)
            {
                Destroy(go);
            }
        }


        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                GameObject newPlaneObject = Instantiate(planeSegement);
                newPlaneObject.transform.parent = this.transform;
                newPlaneObject.GetComponent<RestructurePlane>().maxLat = startLat - (i * deltaLat);
                newPlaneObject.GetComponent<RestructurePlane>().minLat = startLat - ((i + 1) * deltaLat);
                newPlaneObject.GetComponent<RestructurePlane>().minLon = startLon + (j * deltaLon);
                newPlaneObject.GetComponent<RestructurePlane>().maxLon = startLon + ((j + 1) * deltaLon);


                StartCoroutine(AddTexture(newPlaneObject, i, j, instrument, resolution, imageType, imageDate));

                if(instrument == "Coastlines")
                {
                    newPlaneObject.tag = "Coastlines";
                }
                
            }
            
        }
        
        this.transform.eulerAngles = new Vector3(0, -90, 0);
        
    }

    private IEnumerator AddTexture(GameObject newPlaneObject, int i, int j, string instrument, string resolution, string imageType, string imageDate)
    {
        string url = "https://gibs.earthdata.nasa.gov/wmts/epsg4326/best/" + instrument + "/default/" + imageDate + "/" + resolution + "/3/" + i + "/" + j + "." + imageType;
        
        Texture2D tex;
        tex = new Texture2D(4, 4, TextureFormat.DXT1, false);
        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
        {
            yield return uwr.SendWebRequest();

            if (uwr.isNetworkError || uwr.isHttpError)
            {
                Debug.Log(uwr.error);
            }
            else
            {
                // Get downloaded texture
                var texture = DownloadHandlerTexture.GetContent(uwr);
                texture.Apply();
                newPlaneObject.GetComponent<Renderer>().material.mainTexture = texture;

            }

            
        }
        
    }

}

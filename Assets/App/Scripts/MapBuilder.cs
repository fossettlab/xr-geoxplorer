using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class MapBuilder : MonoBehaviour
{
    public int ZoomLevel = 12;

    public float MapTileSize = 0.5f;

    public float Latitude = 47.642567f;
    public float Longitude = -122.136919f;
    public float targetLatitude = 47.642567f;
    public float targetLongitude = -122.136919f;

    public GameObject MapTilePrefab;
    public GameObject targetLocation;
    public GameObject spotTarget;
#if UNITY_WSA
    public TextMeshPro LatLabel;
    public TextMeshPro LonLabel;
#else
    public TextMeshProUGUI LatLabel;
    public TextMeshProUGUI LonLabel;
#endif


    public float MapSize = 12;


    private TileInfo _centerTile;
    private List<MapTile> _mapTiles;

    void Start()
    {
        _mapTiles = new List<MapTile>();
        //ShowMap();
    }

    public void ShowMap()
    {
        //_mapTiles = new List<MapTile>();
        GameObject[] flags = GameObject.FindGameObjectsWithTag("flag");
        if (flags.Length > 0)
        {
            foreach (var flag in flags)
            {
                Destroy(flag);
            }
        }

        GameObject[] primeFlags = GameObject.FindGameObjectsWithTag("flagPrime");
        if (primeFlags.Length > 0)
        {
            foreach (var flag in primeFlags)
            {
                Destroy(flag);
            }
        }


        LatLabel.text = "Latitude: " + Latitude.ToString();
        LonLabel.text = "Longitude: " + Longitude.ToString();
        _centerTile = new TileInfo(new WorldCoordinate { Lat = Latitude, Lon = Longitude }, 
            ZoomLevel, MapTileSize);
        LoadTiles();
        
    }

    private void LoadTiles(bool forceReload = false)
    {
        var size = (int)(MapSize / 2);

        var tileIndex = 0;
        for (var x = -size; x <= size; x++)
        {
            for (var y = -size; y <= size; y++)
            {
                var tile = GetOrCreateTile(x, y, tileIndex++);
                tile.SetTileData(new TileInfo(_centerTile.X - x, _centerTile.Y + y, ZoomLevel, MapTileSize),
                    forceReload);
                tile.gameObject.name = string.Format("({0},{1}) - {2},{3}", x, y, tile.TileData.X,
                    tile.TileData.Y);
                WorldCoordinate ne = tile.TileData.GetNorthEast();
                WorldCoordinate sw = tile.TileData.GetSouthWest();
                /*
                if (targetLatitude > sw.Lat && targetLatitude < ne.Lat && targetLongitude > sw.Lon && targetLongitude < ne.Lon)
                {
                    float relLat = (targetLatitude - sw.Lat) / (ne.Lat - sw.Lat);
                    float relLon = (targetLongitude - sw.Lon) / (ne.Lon - sw.Lon);
                    GameObject target = Instantiate(targetLocation);
                    target.transform.parent = tile.transform;
                    target.GetComponentInChildren<TextMeshPro>().text = string.Format("{0}\n{1}", targetLatitude, targetLongitude);
                    target.transform.localPosition = new Vector3((relLon * -10f) + 5f, 0, (relLat * -10f) + 5f);
                    target.transform.localEulerAngles = Vector3.zero;
                    target.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                    target.GetComponentInChildren<Collider>().enabled = false;
                }
                */
                foreach (var item in TableAnchor.instance.GetComponent<PlanetManager>().objectCoordinates)
                {
                    if (item.longitude > 180)
                    {
                        item.longitude = -360 + item.longitude;
                    }
                    if (item.latitude > sw.Lat && item.latitude < ne.Lat && item.longitude > sw.Lon && item.longitude < ne.Lon)
                    {
                        float relLat = (item.latitude - sw.Lat) / (ne.Lat - sw.Lat);
                        float relLon = (item.longitude - sw.Lon) / (ne.Lon - sw.Lon);
                        GameObject newIcon = Instantiate(targetLocation);
                        newIcon.transform.parent = tile.transform;
                        newIcon.GetComponentInChildren<TextMeshPro>().text = item.modelName;
                        newIcon.transform.localPosition = new Vector3((relLon * -10f) + 5f, 0, (relLat * -10f) + 5f);
                        newIcon.transform.localEulerAngles = Vector3.zero;
                        newIcon.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                        newIcon.GetComponent<Renderer>().material.color = Color.white;
                        newIcon.GetComponent<DownloadButtonInteraction>().storageAccountName = item.storageAccountName;
                        newIcon.GetComponent<DownloadButtonInteraction>().containerName = item.containerName;
                        newIcon.GetComponent<DownloadButtonInteraction>().bundleName = item.bundleName;
                        newIcon.GetComponent<DownloadButtonInteraction>().prefabName = item.prefabName;
                        newIcon.GetComponent<DownloadButtonInteraction>().modelName = item.modelName;
                        newIcon.GetComponent<SpatialTooltipManager>().lat = item.latitude;
                        newIcon.GetComponent<SpatialTooltipManager>().lon = item.longitude;
                    }
                }

                //foreach (var item in TableAnchor.instance.GetComponent<PlanetManager>().spotCoordinates)
                //{
                //    if (item.geometry.coordinates[0] > 180)
                //    {
                //        item.geometry.coordinates[0] = -360 + item.geometry.coordinates[0];
                //    }
                //    if (item.geometry.coordinates[1] > sw.Lat && item.geometry.coordinates[1] < ne.Lat && item.geometry.coordinates[0] > sw.Lon && item.geometry.coordinates[0] < ne.Lon)
                //    {
                //        float relLat = (item.geometry.coordinates[1] - sw.Lat) / (ne.Lat - sw.Lat);
                //        float relLon = (item.geometry.coordinates[0] - sw.Lon) / (ne.Lon - sw.Lon);
                //        GameObject newIcon = Instantiate(spotTarget);
                //        newIcon.transform.localScale = new Vector3(0.04f, 0.04f, 0.04f);
                //        newIcon.transform.parent = tile.transform;

                //        newIcon.transform.localPosition = new Vector3((relLon * -10f) + 5f, 100, (relLat * -10f) + 5f);
                //        newIcon.GetComponent<SpotInteraction>().spotFeature = item;
                //    }
                //}
            }
        }
        
    }

    private MapTile GetOrCreateTile(int x, int y, int i)
    {
        if (_mapTiles.Any() && _mapTiles.Count > i)
        {
            return _mapTiles[i];
        }



        var mapTile = Instantiate(MapTilePrefab, transform);
        Renderer[] rends = mapTile.GetComponentsInChildren<Renderer>();

        
        int tileNumberInt = Mathf.FloorToInt(MapSize / 2);

        for (int j = 1; j < 16; j++)
        {

            //SouthSide
            if (j == 1 || j == 1 + 8)
            {
                if (y == tileNumberInt)
                {
                    rends[j].enabled = true;
                }
                else
                {
                    rends[j].enabled = false;
                }
            }

            //Westside
            if (j == 2 || j == 2 + 8)
            {
                if (x == tileNumberInt)
                {
                    rends[j].enabled = true;
                }
                else
                {
                    rends[j].enabled = false;
                }
            }

            //SouthEast Corner
            if (j == 3 || j == 3 + 8)
            {
                if (x == -tileNumberInt && y == tileNumberInt)
                {
                    rends[j].enabled = true;
                }
                else
                {
                    rends[j].enabled = false;
                }
            }

            //Eastside
            if (j == 4 || j == 4 + 8)
            {
                if (x == -tileNumberInt)
                {
                    rends[j].enabled = true;
                }
                else
                {
                    rends[j].enabled = false;
                }
            }

            //Northside
            if (j == 5 || j == 5 + 8)
            {
                if (y == -tileNumberInt)
                {
                    rends[j].enabled = true;
                }
                else
                {
                    rends[j].enabled = false;
                }
            }

            //Northwest corner
            if (j == 6 || j == 6 + 8)
            {
                if (x == tileNumberInt && y == -tileNumberInt)
                {
                    rends[j].enabled = true;
                }
                else
                {
                    rends[j].enabled = false;
                }
            }

            //Northeast Corner
            if (j == 7 || j == 7 + 8 || j == 8 || j == 8 + 8)
            {
                if (x == -tileNumberInt && y == -tileNumberInt)
                {
                    rends[j].enabled = true;
                }
                else
                {
                    rends[j].enabled = false;
                }
            }
        }
        
            //rends[4].enabled = true;
            //rends[4 + 8].enabled = true;
        
       
            //rends[1].enabled = true;
            //rends[1 + 8].enabled = true;
        
       
            //rends[5].enabled = true;
            //rends[5 + 8].enabled = true;
        
       
            //rends[6].enabled = true;
            //rends[6 + 8].enabled = true;
        
        
            //rends[3].enabled = true;
            //rends[3 + 8].enabled = true;
        
        
            //rends[7].enabled = true;
            //rends[8].enabled = true;
            //rends[7 + 8].enabled = true;
            //rends[8 + 8].enabled = true;
        


        mapTile.transform.localPosition = new Vector3(MapTileSize * x - MapTileSize / 2, 0, MapTileSize * y + MapTileSize / 2);
        mapTile.transform.localRotation = Quaternion.identity;
        var tile = mapTile.GetComponent<MapTile>();
        _mapTiles.Add(tile);
        return tile;
    }
}

public class ObjectCoordinates
{
    public string modelName { get; set; }
    public float latitude { get; set; }
    public float longitude { get; set; }
    public string prefabName { get; set; }
    public string storageAccountName { get; set; }
    public string containerName { get; set; }
    public string bundleName { get; set; }
}

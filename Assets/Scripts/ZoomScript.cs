using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ZoomScript : MonoBehaviour {

    public GameObject modelStage;

    public void ZoomIn()
    {
        if (modelStage.GetComponent<TileStageOrganizer>().mapTilesLoaded == 0)
        {
            GameObject[] flagMarker = GameObject.FindGameObjectsWithTag("flag");
            if (flagMarker != null)
            {
                foreach (var item in flagMarker)
                {
                    Destroy(item);
                }
            }

            GameObject[] primeFlagMarker = GameObject.FindGameObjectsWithTag("flagPrime");
            if (primeFlagMarker != null)
            {
                foreach (var item in flagMarker)
                {
                    Destroy(item);
                }
            }

            GameObject[] infoMarker = GameObject.FindGameObjectsWithTag("InfoMarker");
            if (infoMarker != null)
            {
                foreach (var item in infoMarker)
                {
                    Destroy(item);
                }
            }

            int oldZoomLevel = modelStage.GetComponent<MapBuilder>().ZoomLevel;
            modelStage.GetComponent<MapBuilder>().ZoomLevel = oldZoomLevel + 1;
            modelStage.GetComponent<MapBuilder>().ShowMap();
        }
        else
        {
            Debug.Log("Please wait for tiles to finish loading...");
        }
        
    }

    public void ZoomOut()
    {
        if (modelStage.GetComponent<TileStageOrganizer>().mapTilesLoaded == 0)
        {
            GameObject[] flagMarker = GameObject.FindGameObjectsWithTag("flag");
            if (flagMarker != null)
            {
                foreach (var item in flagMarker)
                {
                    Destroy(item);
                }
            }

            GameObject[] primeFlagMarker = GameObject.FindGameObjectsWithTag("flagPrime");
            if (primeFlagMarker != null)
            {
                foreach (var item in flagMarker)
                {
                    Destroy(item);
                }
            }

            GameObject[] infoMarker = GameObject.FindGameObjectsWithTag("InfoMarker");
            if (infoMarker != null)
            {
                foreach (var item in infoMarker)
                {
                    Destroy(item);
                }
            }

            int oldZoomLevel = modelStage.GetComponent<MapBuilder>().ZoomLevel;
            modelStage.GetComponent<MapBuilder>().ZoomLevel = oldZoomLevel - 1;
            modelStage.GetComponent<MapBuilder>().ShowMap();
        }
        else
        {
            Debug.Log("Please wait for tiles to finish loading...");
        }
    }
}

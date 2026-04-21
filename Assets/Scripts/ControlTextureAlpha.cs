using Microsoft.MixedReality.Toolkit.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ControlTextureAlpha : MonoBehaviour {

    GameObject[] tiles;

    public void FindTileObjects(float eventData)
    {
        tiles = GameObject.FindGameObjectsWithTag("MapTile");
        if (tiles.Length == 0)
        {
            tiles = GameObject.FindGameObjectsWithTag("AssetBundle");
        }
        
        Renderer[] rends = new Renderer[tiles.Length];
#if UNITY_WSA
        eventData = GetComponent<PinchSlider>().SliderValue;
#elif UNITY_IOS || UNITY_ANDROID
        eventData = GetComponent<Slider>().value;
#endif

        //print(eventData);
        for (int i = 0; i < tiles.Length; i++)
        {
            rends[i] = tiles[i].GetComponent<Renderer>();
        }

        foreach (var rend in rends)
        {
            if (rend !=null)
            {
                rend.material.SetFloat("_Blend", eventData);
            }
            
        }
    }
}

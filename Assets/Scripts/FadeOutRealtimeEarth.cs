using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeOutRealtimeEarth : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        float objectDistance = Vector3.Distance(transform.position, Camera.main.transform.position);
        if (objectDistance < 1.5f && objectDistance > 0.5f)
        {
            GameObject[] tileObjects = GameObject.FindGameObjectsWithTag("TilePlane");
            foreach (var tile in tileObjects)
            {
                tile.GetComponent<Renderer>().material.color = new Color(tile.GetComponent<Renderer>().material.color.r, tile.GetComponent<Renderer>().material.color.g, tile.GetComponent<Renderer>().material.color.b, objectDistance - 0.5f);
            }
        }
    }
}

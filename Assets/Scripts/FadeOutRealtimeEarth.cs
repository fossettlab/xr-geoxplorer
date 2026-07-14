using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeOutRealtimeEarth : MonoBehaviour
{
    GameObject[] cachedTilePlanes;

    void OnEnable()
    {
        // Cache once; TilePlane set is static for the fade lifetime (avoid per-frame Find*).
        cachedTilePlanes = SceneQueries.WithTag("TilePlane");
    }

    // Update is called once per frame
    void Update()
    {
        float objectDistance = Vector3.Distance(transform.position, Camera.main.transform.position);
        if (objectDistance < 1.5f && objectDistance > 0.5f)
        {
            if (cachedTilePlanes == null || cachedTilePlanes.Length == 0)
            {
                cachedTilePlanes = SceneQueries.WithTag("TilePlane");
            }

            foreach (var tile in cachedTilePlanes)
            {
                if (tile == null)
                {
                    continue;
                }

                Renderer renderer = tile.GetComponent<Renderer>();
                if (renderer == null)
                {
                    continue;
                }

                Color color = renderer.material.color;
                renderer.material.color = new Color(color.r, color.g, color.b, objectDistance - 0.5f);
            }
        }
    }
}

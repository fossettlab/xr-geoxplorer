using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileStageOrganizer : MonoBehaviour
{
    public int numberOfTiles;
    public int mapTilesLoaded = 0;
    public float tileVerticalOffset;
    Vector3 tileStageInitialPosition;

    // Start is called before the first frame update
    void Start()
    {
        numberOfTiles = (int)(this.GetComponent<MapBuilder>().MapSize * this.GetComponent<MapBuilder>().MapSize);
        tileStageInitialPosition = this.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
		if (mapTilesLoaded == numberOfTiles)
		{
            OrganizeTileStage();
            mapTilesLoaded = 0;
		}
    }

    public void OrganizeTileStage()
    {
        Bounds tileStageBounds = GetChildRendererBounds(this.gameObject);
        //GameObject[] gos = GameObject.FindGameObjectsWithTag("flagPrime");
        //foreach (var go in gos)
        //{
        //    RaycastHit hit;
        //    if (Physics.Raycast(new Vector3(go.transform.position.x, 10, go.transform.position.z), Vector3.down, out hit, Mathf.Infinity))
        //    {
        //        go.transform.position = hit.point;
        //        //go.transform.localPosition = new Vector3(go.transform.localPosition.x, go.transform.localPosition.y - tileVerticalOffset, go.transform.localPosition.z);
        //    }
        //}

        //this.transform.localPosition = new Vector3(tileStageInitialPosition.x, -tileStageBounds.center.y + tileVerticalOffset, tileStageInitialPosition.z);
    }

    Bounds GetChildRendererBounds(GameObject go)
    {
        MeshFilter[] renderers = go.GetComponentsInChildren<MeshFilter>();
        

        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].mesh.bounds;
            for (int i = 1, ni = renderers.Length; i < ni; i++)
            {
                if (renderers[i].tag == Tags.MapTile )
                {
                    bounds.Encapsulate(renderers[i].mesh.bounds);
                }
            }
            return bounds;
        }
        else
        {
            return new Bounds();
        }
    }
}

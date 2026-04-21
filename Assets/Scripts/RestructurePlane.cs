using UnityEngine;

public class RestructurePlane : MonoBehaviour {

    public float minLat;
    public float maxLat;
    public float minLon;
    public float maxLon;

    // Use this for initialization
    void Start () {

        Vector3 anchorPosition = TableAnchor.instance.transform.position;
        Vector3 anchorRotation = TableAnchor.instance.transform.eulerAngles;

        float deltaLat = (maxLat - minLat) / 10;
        float deltaLon = (maxLon - minLon) / 10;
        float rad = 0.5f;
        int c = 0;

        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] verts = mesh.vertices;

        for (int m = 0; m < 11; m++)
        {
            for (int n = 0; n < 11; n++)
            {
                Vector3 xyz = new Vector3(minLon + (n * deltaLon), 0, minLat + (m * deltaLat));

                float cosLat = Mathf.Cos(xyz[2] * Mathf.PI / 180f);
                float sinLat = Mathf.Sin(xyz[2] * Mathf.PI / 180f);
                float cosLon = Mathf.Cos((xyz[0] - (2 * anchorRotation.y)) * Mathf.PI / 180f);
                float sinLon = Mathf.Sin((xyz[0] - (2 * anchorRotation.y)) * Mathf.PI / 180f);
                verts[c] = new Vector3((rad * cosLat * cosLon) + anchorPosition.x, (rad * sinLat) + anchorPosition.y, (rad * cosLat * sinLon) + anchorPosition.z);

                c++;
            }
        }

        mesh.vertices = verts;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
	}
	
}

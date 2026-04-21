using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ScaleMonitor : MonoBehaviour
{
    Transform assetbundleParent;

    // Start is called before the first frame update
    void Start()
    {
        assetbundleParent = this.GetComponentInParent<FetchAssetBundle>().transform;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        this.GetComponent<TextMeshPro>().text = "1:" + (1 / assetbundleParent.localScale.x).ToString(".0#");
    }
}

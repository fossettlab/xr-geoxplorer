using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.Input;
using Photon.Pun;
using TMPro;
using UnityEngine;

public class SpotInteraction : MonoBehaviourPun, IMixedRealityPointerHandler
{
    public StraboDatasetFeature spotFeature;
    public TextMeshPro infoText;
    public GameObject closeButton;
    public GameObject gotoButton;
    bool selected;

    public void OnPointerClicked(MixedRealityPointerEventData eventData)
    {
        
    }

    public void OnPointerDown(MixedRealityPointerEventData eventData)
    {
        
    }

    public void OnPointerDragged(MixedRealityPointerEventData eventData)
    {
        
    }

    public void OnPointerUp(MixedRealityPointerEventData eventData)
    {
        if (!selected)
        {
            //infoText.gameObject.GetComponent<RectTransform>().localPosition = new Vector3(0, 3, 0);
            infoText.gameObject.GetComponent<Renderer>().enabled = true;
            closeButton.SetActive(true);
            gotoButton.SetActive(true);
            selected = true;
        }
        else
        {
            if (eventData.Pointer.Result.CurrentPointerTarget.name == "CloseButton")
            {
                infoText.gameObject.GetComponent<Renderer>().enabled = false;
                closeButton.SetActive(false);
                gotoButton.SetActive(false);
                selected = false;
            }
            else if (eventData.Pointer.Result.CurrentPointerTarget.name == "GoToButton")
            {
                TableAnchor.instance.GetComponent<PlanetManager>().hitLat = spotFeature.geometry.coordinates[1];
                TableAnchor.instance.GetComponent<PlanetManager>().hitLon = spotFeature.geometry.coordinates[0];
                TableAnchor.instance.GetComponent<PhotonView>().RPC("GoToTiles", RpcTarget.All, spotFeature.geometry.coordinates[1], spotFeature.geometry.coordinates[0], 15);  //100 means that no zooming takes place
            }
        }
        
    }


    // Start is called before the first frame update
    void Start()
    {
        string labelString = string.Format("{0}\n{1}\n{2}\n{3}", spotFeature.properties.name, spotFeature.properties.projectname, spotFeature.properties.count, spotFeature.properties.owner);
        infoText.text = labelString;
        
    }


}

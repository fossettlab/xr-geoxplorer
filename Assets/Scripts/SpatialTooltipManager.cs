using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.EventSystems;

public class SpatialTooltipManager : MonoBehaviour
{

    public GameObject menuButtons;
    public float lat;
    public float lon;

    bool menuOn;


    // Start is called before the first frame update
    void Start()
    {
        menuButtons = this.GetComponentInChildren<MenuButtons>().gameObject;
        menuOn = false;
    }

    public void MenuSwitcher()
    {
        if (!menuOn)
        {
            Renderer[] rends = menuButtons.GetComponentsInChildren<Renderer>();
            Collider[] cols = menuButtons.GetComponentsInChildren<Collider>();
            foreach (var rend in rends)
            {
                rend.enabled = true;
            }
            foreach (var col in cols)
            {
                col.enabled = true;
            }
            menuOn = true;
        }
        else
        {
            Renderer[] rends = menuButtons.GetComponentsInChildren<Renderer>();
            Collider[] cols = menuButtons.GetComponentsInChildren<Collider>();
            foreach (var rend in rends)
            {
                rend.enabled = false;
            }
            foreach (var col in cols)
            {
                col.enabled = false;
            }
            menuOn = false;
        }
    }
}

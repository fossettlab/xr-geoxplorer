using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OutcropTooltipManager : MonoBehaviour
{
    public GameObject hL2Object;

    // Start is called before the first frame update
    void Start()
    {
        if (Application.platform == RuntimePlatform.WSAPlayerARM)
        {
            hL2Object.SetActive(true);
        }
    }

}

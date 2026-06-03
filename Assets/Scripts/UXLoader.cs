using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UXLoader : MonoBehaviour
{

    public GameObject UXStack;

    // Start is called before the first frame update
    void Start()
    {
        Instantiate(UXStack);
    }
}

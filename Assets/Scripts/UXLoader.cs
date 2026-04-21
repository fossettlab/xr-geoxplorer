using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UXLoader : MonoBehaviour
{

#if UNITY_WSA
    public GameObject UXStack;
#elif UNITY_IOS || UNITY_ANDROID
    public GameObject UXStack;
#endif

    // Start is called before the first frame update
    void Start()
    {
        Instantiate(UXStack);
    }
}

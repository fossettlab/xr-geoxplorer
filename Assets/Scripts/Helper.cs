using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Helper
{

    public static Transform FindInChildren(this GameObject go, string name)
    {
        foreach (Transform x in go.GetComponentsInChildren<Transform>())
            if (x.gameObject.name == name)
                return x;
        throw new System.Exception
                        ("Technically the old version throws an exception if none are found, so I'll do the same here!");
    }
}

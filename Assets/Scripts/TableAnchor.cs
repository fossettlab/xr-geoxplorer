using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TableAnchor : MonoBehaviour
{
    public static TableAnchor instance;
    // Start is called before the first frame update
    void Start()
    {
      
        if (TableAnchor.instance == null)
        {
            TableAnchor.instance = this;
        }
        else
        {
            if (TableAnchor.instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
        }
        Debug.Log("Table Created");
        GameObject persistentRoot = FindSharedContentPersistenceRoot();
        if (persistentRoot.transform.parent != null)
        {
            persistentRoot.transform.SetParent(null, true);
        }

        DontDestroyOnLoad(persistentRoot);
    }

    private GameObject FindSharedContentPersistenceRoot()
    {
        Transform current = transform;
        while (current.parent != null)
        {
            if (IsPlatformRootName(current.parent.name))
            {
                // Persist shared spawned content across Photon scene reloads, but let the active platform rig rebuild.
                return current.gameObject;
            }

            current = current.parent;
        }

        return current.gameObject;
    }

    private static bool IsPlatformRootName(string objectName)
    {
        return objectName == "PlatformRoot" || objectName.StartsWith("PlatformRoot.");
    }
}

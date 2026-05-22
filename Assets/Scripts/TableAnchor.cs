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
        GameObject persistentRoot = FindPersistentRoot();
        if (persistentRoot.transform.parent != null)
        {
            persistentRoot.transform.SetParent(null, true);
        }

        DontDestroyOnLoad(persistentRoot);
    }

    private GameObject FindPersistentRoot()
    {
        Transform current = transform;
        while (current.parent != null && !current.parent.name.StartsWith("PlatformRoot."))
        {
            current = current.parent;
        }

        return current.gameObject;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InspectorModelObject : MonoBehaviour
{

    private void Update()
    {
        this.transform.Rotate(new Vector3(0,0.5f,0));
    }

}

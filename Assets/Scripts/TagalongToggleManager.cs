using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TagalongToggleManager : MonoBehaviour
{
    public void TagalongSelected()
    {
        GetComponentInChildren<TextMesh>().text = "Tagalong Off";
    }

    public void TagalongDeselected()
    {
        GetComponentInChildren<TextMesh>().text = "Tagalong On";
    }
}

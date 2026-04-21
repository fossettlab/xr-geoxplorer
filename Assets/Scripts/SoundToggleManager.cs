using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundToggleManager : MonoBehaviour
{
    public void PhotonVoiceSelected()
    {
        GetComponentInChildren<TextMesh>().text = "Mic On";
    }

    public void PhotonVoiceDeselected()
    {
        GetComponentInChildren<TextMesh>().text = "Mic Off";
    }
}

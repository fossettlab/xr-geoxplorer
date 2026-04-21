using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using TMPro;
using UnityEngine;

public class DisplayUserName : MonoBehaviour
{
    public TextMeshPro userNameText;

    // Start is called before the first frame update
    void Start()
    {
        PhotonView PV = this.GetComponent<PhotonView>();
        if (!PV.IsMine)
        {
            userNameText.gameObject.GetComponent<Renderer>().enabled = true;
            object[] gObject = PV.InstantiationData;
            userNameText.text = gObject[0].ToString();
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;

public class ConnectionStatus : MonoBehaviour
{
    private readonly string connectionStatusMessage = "Connection Status: ";

    [Header("UI References")]
    public TextMeshProUGUI ConnectionStatusText;

    #region UNITY

    public void Update()
    {
        ConnectionStatusText.text = connectionStatusMessage + PhotonNetwork.NetworkClientState;
    }

    #endregion
}

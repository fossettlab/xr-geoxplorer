using Photon.Pun;
using Photon.Pun.UtilityScripts;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Microsoft.MixedReality.Toolkit.UI;

public class PlayerListEntry : MonoBehaviour
{
    [Header("UI References")]

    public GameObject PlayerColorImage;
#if UNITY_WSA
    public TextMeshPro PlayerNameText;
    public Interactable PlayerReadyButton;
#elif UNITY_IOS || UNITY_ANDROID
    public TextMeshProUGUI PlayerNameText;
    public Button PlayerReadyButton;
#endif
    //public Image PlayerReadyImage;

    private int ownerId;
    private bool isPlayerReady;

#region UNITY

    public void OnEnable()
    {
        PlayerNumbering.OnPlayerNumberingChanged += OnPlayerNumberingChanged;
    }   

    public void Start()
    {
        if (PhotonNetwork.LocalPlayer.ActorNumber != ownerId)
        {
            PlayerReadyButton.gameObject.SetActive(false);
        }
        else
        {
            ExitGames.Client.Photon.Hashtable initialProps = new ExitGames.Client.Photon.Hashtable() { { GeoXSession.PLAYER_READY, isPlayerReady }, { GeoXSession.PLAYER_LIVES, GeoXSession.PLAYER_MAX_LIVES } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(initialProps);
            PhotonNetwork.LocalPlayer.SetScore(0);

#if UNITY_IOS || UNITY_ANDROID
            PlayerReadyButton.onClick.AddListener(() =>
#elif UNITY_WSA
            PlayerReadyButton.OnClick.AddListener(() =>
#endif
            {
                isPlayerReady = !isPlayerReady;
                SetPlayerReady(isPlayerReady);

                ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable() { { GeoXSession.PLAYER_READY, isPlayerReady } };
                PhotonNetwork.LocalPlayer.SetCustomProperties(props);

                if (PhotonNetwork.IsMasterClient)
                {
                    FindObjectOfType<LobbyManager>().LocalPlayerPropertiesUpdated();
                }
            });
        }
    }

    public void OnDisable()
    {
        PlayerNumbering.OnPlayerNumberingChanged -= OnPlayerNumberingChanged;
    }

#endregion

    public void Initialize(int playerId, string playerName)
    {
        ownerId = playerId;
        PlayerNameText.text = playerName;
    }

    private void OnPlayerNumberingChanged()
    {
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.ActorNumber == ownerId)
            {
#if UNITY_WSA
                PlayerColorImage.GetComponent<Renderer>().material.color = GeoXSession.GetColor(p.GetPlayerNumber());
#elif UNITY_IOS || UNITY_ANDROID
                PlayerColorImage.GetComponent<Image>().color = GeoXSession.GetColor(p.GetPlayerNumber());
#endif
            }
        }
    }

    public void SetPlayerReady(bool playerReady)
    {
#if UNITY_WSA
        PlayerReadyButton.GetComponentInChildren<TextMesh>().text = playerReady ? "Ready!" : "Ready?";
#elif UNITY_IOS || UNITY_ANDROID
        PlayerReadyButton.GetComponentInChildren<TextMeshProUGUI>().text = playerReady ? "Ready!" : "Ready?";
#endif
        //PlayerReadyImage.enabled = playerReady;
    }
}

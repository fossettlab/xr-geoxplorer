using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Microsoft.MixedReality.Toolkit.UI;

public class RoomListEntry : MonoBehaviour
{
#if UNITY_WSA
    public TextMeshPro RoomNameText;
    public TextMeshPro RoomPlayersText;
    public Interactable JoinRoomButton;
#elif UNITY_IOS || UNITY_ANDROID
    public TextMeshProUGUI RoomNameText;
    public TextMeshProUGUI RoomPlayersText;
    public Button JoinRoomButton;
#endif

    private string roomName;

    public void Start()
    {
#if UNITY_WSA
        JoinRoomButton.OnClick.AddListener(() =>
#elif UNITY_IOS || UNITY_ANDROID
        JoinRoomButton.onClick.AddListener(() =>
#endif
        {
            if (PhotonNetwork.InLobby)
            {
                PhotonNetwork.LeaveLobby();
            }

            PhotonNetwork.JoinRoom(roomName);
        });
    }

    public void Initialize(string name, byte currentPlayers, byte maxPlayers)
    {
        roomName = name;

        RoomNameText.text = name;
        RoomPlayersText.text = currentPlayers.ToString();
    }
}

using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    public static LobbyManager Instance { get; private set; }

    /// <summary>
    /// Root manager class covers interactions to log in to Photon, as well as instantiate anchors and assetbundles. This class should always be active in the scene. Covers both MobileAR and HoloLens interactions.
    /// Access via <see cref="Instance"/> (registered with <see cref="ServiceLocator"/>).
    /// </summary>


    public GameObject LoaderUI;
    public GameObject LobbyUI;
    public GameObject RoomUI;
    public GameObject InAppUI;
    public GameObject MenuUI;
    public GameObject TutorialUI;
    public PlanetManager PlanetManager;


    public TMP_InputField PlayerNameInput;
    public TMP_InputField RoomNameInputField;

    public GameObject RoomListEntryPrefab;
    public GameObject RoomListContent;
    public GameObject PlayerListEntryPrefab;
    public GameObject InsideRoomPanel;
    public GameObject StartGameButton;

    public GameObject downloadIndicatorText;

    private string assetLoaderName = "AssetBundleLoader";
    private Vector3 ModuleLocations = new Vector3(0, 0, 0);
    private object[] customInitData;
    private object[] userInitData;
    string playerName;

    private Dictionary<string, RoomInfo> cachedRoomList;
    private Dictionary<string, GameObject> roomListEntries;
    private Dictionary<int, GameObject> playerListEntries;

    private Player[] photonPlayers;
    private int playersInRoom;
    private int myNumberInRoom;

    public delegate void OnCharacterInstantiated(GameObject character);
    public static event OnCharacterInstantiated CharacterInstantiated;


    public void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("LobbyManager: duplicate instance destroyed.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ServiceLocator.Register(this);

        Debug.developerConsoleVisible = false;

        PhotonNetwork.AutomaticallySyncScene = true;

        cachedRoomList = new Dictionary<string, RoomInfo>();
        roomListEntries = new Dictionary<string, GameObject>();

        PlayerNameInput.text = "User " + UnityEngine.Random.Range(1000, 10000);
    }

    public override void OnConnectedToMaster()
    {
        //this.SetActivePanel(SelectionPanel.name);
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        ClearRoomListView();

        UpdateCachedRoomList(roomList);
        UpdateRoomListView();
    }

    public override void OnLeftLobby()
    {
        cachedRoomList.Clear();

        ClearRoomListView();

    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        //SetActivePanel(SelectionPanel.name);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        //SetActivePanel(SelectionPanel.name);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        string roomName = "Room " + UnityEngine.Random.Range(1000, 10000);

        RoomOptions options = new RoomOptions { MaxPlayers = 8 };

        PhotonNetwork.CreateRoom(roomName, options, null);
    }


    public void OnLoginButtonClicked()
    {
        playerName = PlayerNameInput.text;

        if (!playerName.Equals(""))
        {
            PhotonNetwork.LocalPlayer.NickName = playerName;
            PhotonNetwork.ConnectUsingSettings();
            userInitData = new object[1];
            userInitData[0] = playerName;
        }
        else
        {
            Debug.LogError("Player Name is invalid.");
        }
    }

    public void OnBackButtonClicked()
    {
        if (PhotonNetwork.InLobby)
        {
            PhotonNetwork.LeaveLobby();
        }
    }

    public void OnCreateRoomButtonClicked()
    {
        string roomName = RoomNameInputField.text;
        roomName = (roomName.Equals(string.Empty)) ? "Room " + UnityEngine.Random.Range(1000, 10000) : roomName;

        byte maxPlayers = 8;

        RoomOptions options = new RoomOptions { MaxPlayers = maxPlayers };
        print(roomName + " " + maxPlayers);
        PhotonNetwork.CreateRoom(roomName, options, null);
    }

    public void OnJoinRandomRoomButtonClicked()
    {
       //SetActivePanel(JoinRandomRoomPanel.name);

        PhotonNetwork.JoinRandomRoom();
    }

    public void OnLeaveGameButtonClicked()
    {
        PhotonNetwork.LeaveRoom();
    }

    public void OnRoomListButtonClicked()
    {
        if (!PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }

        //SetActivePanel(RoomListPanel.name);
    }

    public void OnStartGameButtonClicked()
    {
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;
        print("Lets go to " + PhotonNetwork.CurrentRoom.Name);
    }

    private void ClearRoomListView()
    {
        foreach (GameObject entry in roomListEntries.Values)
        {
            Destroy(entry.gameObject);
        }

        roomListEntries.Clear();
    }

    public void TemporaryClearRoomListView()
    {
        foreach (GameObject entry in roomListEntries.Values)
        {
            Destroy(entry.gameObject);
        }
    }

    public void LocalPlayerPropertiesUpdated()
    {
        StartGameButton.gameObject.SetActive(CheckPlayersReady());
    }

    private void UpdateCachedRoomList(List<RoomInfo> roomList)
    {
        foreach (RoomInfo info in roomList)
        {
            // Remove room from cached room list if it got closed, became invisible or was marked as removed
            if (!info.IsOpen || !info.IsVisible || info.RemovedFromList)
            {
                if (cachedRoomList.ContainsKey(info.Name))
                {
                    cachedRoomList.Remove(info.Name);
                }

                continue;
            }

            // Update cached room info
            if (cachedRoomList.ContainsKey(info.Name))
            {
                cachedRoomList[info.Name] = info;
            }
            // Add new room info to cache
            else
            {
                cachedRoomList.Add(info.Name, info);
            }
        }
    }

    private void UpdateRoomListView()
    {
        foreach (RoomInfo info in cachedRoomList.Values)
        {
            //GameObject entry = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject entry = Instantiate(RoomListEntryPrefab);
            entry.transform.SetParent(RoomListContent.transform);
            entry.transform.localScale = Vector3.one * 0.5f;
            entry.transform.localPosition = new Vector3(0, -50f, 0);

            entry.GetComponent<RoomListEntry>().Initialize(info.Name, (byte)info.PlayerCount, info.MaxPlayers);

            roomListEntries.Add(info.Name, entry);
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (playerListEntries == null)
        {
            playerListEntries = new Dictionary<int, GameObject>();
        }

        GameObject entry;
        if (playerListEntries.TryGetValue(targetPlayer.ActorNumber, out entry))
        {
            object isPlayerReady;
            if (changedProps.TryGetValue(GeoXSession.PLAYER_READY, out isPlayerReady))
            {
                entry.GetComponent<PlayerListEntry>().SetPlayerReady((bool)isPlayerReady);
            }
        }

        StartGameButton.gameObject.SetActive(CheckPlayersReady());
    }

    private bool CheckPlayersReady()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return false;
        }

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            object isPlayerReady;
            if (p.CustomProperties.TryGetValue(GeoXSession.PLAYER_READY, out isPlayerReady))
            {
                if (!(bool)isPlayerReady)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();

        photonPlayers = PhotonNetwork.PlayerList;
        playersInRoom = photonPlayers.Length;
        myNumberInRoom = playersInRoom;
        PhotonNetwork.NickName = myNumberInRoom.ToString();

        StartGame();

        LobbyUI.SetActive(false);
        RoomUI.SetActive(true);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        photonPlayers = PhotonNetwork.PlayerList;
        playersInRoom++;
    }

    void CreatePlayer()
    {
        GameObject player = PhotonNetwork.Instantiate(Path.Combine("Prefabs", "PhotonUser"), Vector3.zero, Quaternion.identity,0,userInitData);
        player.transform.parent = Camera.main.transform;
        player.GetComponentInChildren<Renderer>().enabled = false; //Sets own head to be invisible

        if (CharacterInstantiated != null)
        {
            CharacterInstantiated(player);
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        ServiceLocator.Unregister(this);
    }

    void StartGame()
    {
        CreatePlayer();

        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }
    }

    /// <summary>
    /// This section now deals with setting up an anchor, and dealing with in-game UI and objects
    /// </summary>

    public void OnAnchorSuccessful(GameObject anchorObject)
    {
#if UNITY_IOS || UNITY_ANDROID
        Pose anchorPose = Pose.identity;
        anchorPose.position = anchorObject.transform.position;
        anchorPose.rotation = anchorObject.transform.rotation;
        // Cold path: AR managers are scene-wired once at session start (not per-frame).
        ARAnchorManager aRReferencePointManager = FindObjectOfType<ARAnchorManager>();
        if (aRReferencePointManager == null)
        {
            // No AR Foundation session (e.g. Quest OpenXR). Position the shared table
            // anchor directly in Unity world space so spawned content has a fallback origin.
            TableAnchor.instance.transform.SetPositionAndRotation(anchorPose.position, anchorPose.rotation);
            Debug.LogWarning("OnAnchorSuccessful: no ARAnchorManager present; positioned TableAnchor in fallback world space.");
        }
        else
        {
            ARAnchor arReferencePoint = aRReferencePointManager.AddAnchor(anchorPose);

            if (arReferencePoint == null)
            {
                Debug.Log("There was an error creating a reference point");
            }
            else
            {
                //set the table anchor instance to it's new position and rotation and zero it in the AR Reference Point transform.
                TableAnchor.instance.transform.SetPositionAndRotation(anchorPose.position, anchorPose.rotation);
                TableAnchor.instance.transform.parent = arReferencePoint.transform;
            }

            // Cold path: disable AR planes once when entering a Photon room.
            ARPlaneManager arPlaneManager = FindObjectOfType<ARPlaneManager>();

            if (arPlaneManager != null)
            {
                foreach (var plane in arPlaneManager.trackables)
                {
                    plane.gameObject.SetActive(false);
                }
                arPlaneManager.enabled = false;
            }
        }

#endif

        RoomUI.SetActive(false);
        InAppUI.SetActive(true);
    }

    public void OnMenuSelect()
    {
        InAppUI.SetActive(false);
        MenuUI.SetActive(true);
    }

    public void OnHideMenu()
    {
        MenuUI.SetActive(false);
        InAppUI.SetActive(true);
    }

    public void OnPlanetSelect()
    {
        MenuUI.SetActive(false);
        InAppUI.SetActive(true);
    }

    public void CreateInteractablePlanet(string modelToInstantiate)
    {
        ClearInteractablePlanet();

        //if (PlanetManager.activePlanet != null)
        //{
        //    if (PhotonNetwork.IsMasterClient)
        //    {
        //        PlanetManager.OnBack();
        //        PhotonNetwork.Destroy(PlanetManager.activePlanet);
        //    }
        //}

        GameObject gObject = PhotonNetwork.Instantiate(Path.Combine("Prefabs", modelToInstantiate), Vector3.zero, Quaternion.identity, 0);
        gObject.transform.parent = TableAnchor.instance.transform;
    }

    PlanetManager ResolvePlanetManager()
    {
        if (PlanetManager != null)
        {
            return PlanetManager;
        }

        if (TableAnchor.instance == null)
        {
            return null;
        }

        return TableAnchor.instance.GetComponent<PlanetManager>();
    }

    public void ClearInteractablePlanet()
    {
        PlanetManager manager = ResolvePlanetManager();
        if (manager != null && manager.activePlanet != null)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                manager.OnBack();
                PhotonNetwork.Destroy(manager.activePlanet);
            }
        }
    }


    public void CreateInteractableObjects(string storageAccountName, string containerName, string prefabName, string bundleName, string modelName)
    {
        customInitData = new object[5];
        customInitData[0] = storageAccountName;
        customInitData[1] = containerName;
        customInitData[2] = prefabName;
        customInitData[3] = bundleName;
        customInitData[4] = modelName;


        GameObject gObject =
            PhotonNetwork.Instantiate(Path.Combine("Prefabs", assetLoaderName), Vector3.zero, Quaternion.identity, 0, customInitData);
        object[] initData = gObject.GetPhotonView().InstantiationData;

        gObject.GetComponent<FetchAssetBundle>().storageAccountName = initData[0].ToString();
        gObject.GetComponent<FetchAssetBundle>().containerName = initData[1].ToString();
        gObject.GetComponent<FetchAssetBundle>().prefabName = initData[2].ToString();
        gObject.GetComponent<FetchAssetBundle>().bundleName = initData[3].ToString();
        gObject.GetComponent<FetchAssetBundle>().modelName = initData[4].ToString();
        gObject.transform.parent = TableAnchor.instance.transform;
        gObject.transform.localPosition = ModuleLocations;
        gObject.name = "AssetBundleLoader_" + initData[2];
    }

    public void ShowDownloadState()
    {
        downloadIndicatorText.SetActive(true);
    }

    public void HideDownloadState()
    {
        downloadIndicatorText.SetActive(false);
    }

    public void DeleteAssetBundle(GameObject assetBundleToDelete)
    {
        if (assetBundleToDelete.GetComponent<PhotonView>().IsMine)
        {
            GameObject gObject = assetBundleToDelete;
            PhotonNetwork.Destroy(gObject);
        }

        if (SceneQueries.WithTag("AssetBundle").Length == 0)
        {
            TableAnchor.instance.GetComponent<PlanetManager>().geoSlider.SetActive(false);
        }
    }

    public void ResetAllAssetBundles()
    {
        GameObject[] gos = SceneQueries.WithTag("AssetBundleLoader");
        foreach (var go in gos)
        {
            go.GetComponent<AssetBundleInteraction>().OnReset();
        }
    }

    public void ShowTutorial()
    {
        if (!TutorialUI.activeInHierarchy)
        {
            TutorialUI.SetActive(true);
        }
        else
        {
            TutorialUI.SetActive(false);
        }
    }

}

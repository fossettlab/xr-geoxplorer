using System.Collections;
using NUnit.Framework;
using Photon.Pun;
using UnityEngine;
using UnityEngine.TestTools;

namespace GeoX.Tests.Network
{
    /// <summary>
    /// Workflow 1 of 4 (issue #21): ROOM JOIN — lobby flow, picking/creating a room,
    /// entering it.
    ///
    /// These tests capture the current PUN 2 matchmaking behavior that LobbyManager
    /// drives (OnLoginButtonClicked → ConnectUsingSettings → CreateRoom/JoinRandomRoom,
    /// and the OnJoinRandomFailed → CreateRoom fallback). They run against
    /// PhotonNetwork.OfflineMode so no Photon AppId / dev app / network is required —
    /// see docs/networking-harness.md for why offline mode is the chosen "mock endpoint".
    ///
    /// This is the regression target for #23: the replacement stack (NGO + Relay +
    /// Lobby) must preserve the observable contract — create a named room, become the
    /// sole occupant, and surface a "no room found" signal when a random join has
    /// nothing to join.
    /// </summary>
    public class RoomJoinTests
    {
        private GameObject recorderGo;
        private CallbackRecorder recorder;

        [SetUp]
        public void SetUp()
        {
            recorderGo = new GameObject("CallbackRecorder");
            recorder = recorderGo.AddComponent<CallbackRecorder>();
        }

        [TearDown]
        public void TearDown()
        {
            if (PhotonNetwork.InRoom)
            {
                PhotonNetwork.LeaveRoom();
            }

            PhotonNetwork.OfflineMode = false;

            if (recorderGo != null)
            {
                Object.Destroy(recorderGo);
            }
        }

        /// <summary>
        /// What: enabling offline mode then creating a named room.
        /// Expected: PhotonNetwork reports InRoom, CurrentRoom.Name matches, and both
        ///           OnCreatedRoom and OnJoinedRoom callbacks fire (LobbyManager relies
        ///           on OnJoinedRoom to swap LobbyUI → RoomUI and spawn the player).
        /// Pass/fail: fails if the room is not entered within the frame budget or the
        ///            room name does not round-trip.
        /// </summary>
        [UnityTest]
        public IEnumerator CreateRoom_EntersNamedRoom_AndFiresJoinedCallback()
        {
            const string roomName = "HarnessRoom";

            PhotonNetwork.OfflineMode = true;
            yield return null;

            PhotonNetwork.CreateRoom(roomName);

            float deadline = Time.realtimeSinceStartup + 5f;
            while (!PhotonNetwork.InRoom && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.IsTrue(PhotonNetwork.InRoom, "Expected to be in a room after CreateRoom in offline mode.");
            Assert.IsNotNull(PhotonNetwork.CurrentRoom, "CurrentRoom should be set once in a room.");
            Assert.AreEqual(roomName, PhotonNetwork.CurrentRoom.Name, "Room name should round-trip.");
            Assert.AreEqual(1, PhotonNetwork.CurrentRoom.PlayerCount, "Creator should be the sole occupant.");
            Assert.IsTrue(recorder.JoinedRoom, "OnJoinedRoom must fire (LobbyManager swaps to RoomUI on it).");
            Assert.IsTrue(recorder.CreatedRoom, "OnCreatedRoom must fire for the room creator.");
        }

        /// <summary>
        /// What: requesting a random room when none exists (offline mode has no room list).
        /// Expected: OnJoinRandomFailed fires. LobbyManager's override responds by
        ///           creating a fresh room, so this failure callback is load-bearing.
        /// Pass/fail: fails if OnJoinRandomFailed is not observed within the frame budget.
        /// </summary>
        [UnityTest]
        public IEnumerator JoinRandomRoom_WithNoRooms_FailsAndSignalsFallback()
        {
            PhotonNetwork.OfflineMode = true;
            yield return null;

            recorder.Reset();
            PhotonNetwork.JoinRandomRoom();

            float deadline = Time.realtimeSinceStartup + 5f;
            while (!recorder.JoinRandomFailed && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.IsTrue(recorder.JoinRandomFailed,
                "OnJoinRandomFailed must fire so LobbyManager can fall back to CreateRoom.");
        }
    }
}

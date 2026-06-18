using System.Collections;
using NUnit.Framework;
using Photon.Pun;
using UnityEngine;
using UnityEngine.TestTools;

namespace GeoX.Tests.Network
{
    /// <summary>
    /// Workflow 4 of 4 (issue #21): TEARDOWN — leaving a room, reconnecting, exiting.
    ///
    /// Captures the lifecycle LobbyManager.OnLeaveGameButtonClicked / OnBackButtonClicked
    /// drive: a clean LeaveRoom returns to the not-in-room state and fires OnLeftRoom,
    /// a subsequent room create succeeds (reconnect), and dropping the connection leaves
    /// no lingering in-room state. Runs in offline mode (no Photon credentials).
    ///
    /// Regression target for #23: the NGO/Lobby replacement must expose the same
    /// leave → rejoin → disconnect transitions without leaking room membership.
    /// </summary>
    public class TeardownTests
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

        private IEnumerator EnterRoom(string roomName)
        {
            PhotonNetwork.CreateRoom(roomName);
            float deadline = Time.realtimeSinceStartup + 5f;
            while (!PhotonNetwork.InRoom && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
        }

        /// <summary>
        /// What: leaving a room, then creating another (reconnect), then dropping offline.
        /// Expected: LeaveRoom → not InRoom and OnLeftRoom fired; a second CreateRoom
        ///           re-enters a room; turning offline mode off ends in a not-in-room,
        ///           not-connected state.
        /// Pass/fail: fails if any transition does not settle within the frame budget.
        /// </summary>
        [UnityTest]
        public IEnumerator LeaveReconnectExit_TransitionsCleanly()
        {
            PhotonNetwork.OfflineMode = true;
            yield return null;

            // Enter the first room.
            yield return EnterRoom("TeardownRoomA");
            Assert.IsTrue(PhotonNetwork.InRoom, "Precondition: should be in the first room.");

            // Leave it.
            recorder.Reset();
            PhotonNetwork.LeaveRoom();
            float deadline = Time.realtimeSinceStartup + 5f;
            while (PhotonNetwork.InRoom && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.IsFalse(PhotonNetwork.InRoom, "LeaveRoom should clear in-room state.");
            Assert.IsTrue(recorder.LeftRoom, "OnLeftRoom must fire so the UI can return to the lobby.");

            // Reconnect into a new room.
            yield return EnterRoom("TeardownRoomB");
            Assert.IsTrue(PhotonNetwork.InRoom, "Should be able to re-enter a room after leaving.");
            Assert.AreEqual("TeardownRoomB", PhotonNetwork.CurrentRoom.Name, "Reconnected room name should match.");

            // Exit / drop the session.
            PhotonNetwork.LeaveRoom();
            deadline = Time.realtimeSinceStartup + 5f;
            while (PhotonNetwork.InRoom && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            PhotonNetwork.OfflineMode = false;
            yield return null;

            Assert.IsFalse(PhotonNetwork.InRoom, "After exit there should be no room membership.");
            Assert.IsFalse(PhotonNetwork.IsConnected, "Leaving offline mode should drop the simulated connection.");
        }
    }
}

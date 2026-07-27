using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Photon.Pun;
using UnityEngine;

namespace GeoX.Network.Tests
{
    /// <summary>
    /// Characterization tests that pin the current Photon PUN 2 wire contracts as a
    /// regression target for the networking rewrite (#23). They assert the shape of
    /// what crosses the wire (order, types, values) and the observable effect of the
    /// shared-anchor-ID handler - behaviour the rewrite must preserve regardless of
    /// the transport it eventually lands on. No network connection is opened.
    ///
    /// The gameplay scripts live in Assembly-CSharp, which has no assembly definition
    /// and therefore cannot be referenced from a test assembly, so the game types are
    /// resolved by reflection over the loaded Assembly-CSharp. The Photon types are
    /// referenced directly through the PhotonUnityNetworking assembly.
    ///
    /// This is the deterministic tier only. The full two-client workflows (live room
    /// join, remote sees movement, reconnect/teardown) run against a live Photon app
    /// and are documented as a manual procedure in docs/networking-harness.md; they
    /// are non-deterministic and multi-process, so they are not a CI gate.
    /// </summary>
    public class PunWireContractTests
    {
        private const string GameAssemblyName = "Assembly-CSharp";

        private readonly List<GameObject> spawned = new List<GameObject>();

        // --- reflection helpers over the game assembly -----------------------

        private static Assembly GameAssembly()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Single(a => a.GetName().Name == GameAssemblyName);
        }

        private static Type GameType(string typeName)
        {
            return GameAssembly().GetType(typeName, throwOnError: true);
        }

        private static FieldInfo Field(Type type, string name)
        {
            FieldInfo f = type.GetField(name,
                BindingFlags.Instance | BindingFlags.Static |
                BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(f, "Field '" + name + "' not found on " + type.Name);
            return f;
        }

        private static object GetField(object target, string name)
        {
            return Field(target.GetType(), name).GetValue(target);
        }

        private static void SetField(object target, string name, object value)
        {
            Field(target.GetType(), name).SetValue(target, value);
        }

        private GameObject NewGameObject(string name)
        {
            GameObject go = new GameObject(name);
            spawned.Add(go);
            return go;
        }

        [TearDown]
        public void TearDown()
        {
            // GenericNetworkManager.instance is a persistent static; clearing it keeps
            // tests order-independent.
            Field(GameType("GenericNetworkManager"), "instance").SetValue(null, null);

            foreach (GameObject go in spawned)
            {
                if (go != null)
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }
            spawned.Clear();
        }

        // --- transform sync: write path --------------------------------------

        /// <summary>
        /// Sending side: an owned/plain object serializes exactly localPosition,
        /// localRotation, localScale, in that order. Pins the transform-sync payload
        /// the #23 rewrite must reproduce.
        /// </summary>
        [Test]
        public void SerializeView_Write_EmitsLocalPositionRotationScaleInOrder()
        {
            Type syncType = GameType("GenericNetSync");
            GameObject go = NewGameObject("sync-write");
            PhotonView view = go.AddComponent<PhotonView>();
            Component sync = go.AddComponent(syncType);

            // Bypass Start(): wire the private PhotonView the component reads, and
            // leave User == false so serialization takes the plain-object branch
            // (the general transform contract, independent of camera/anchor state).
            SetField(sync, "PV", view);
            SetField(sync, "User", false);

            go.transform.localPosition = new Vector3(1f, 2f, 3f);
            go.transform.localRotation = Quaternion.Euler(10f, 20f, 30f);
            go.transform.localScale = new Vector3(4f, 5f, 6f);

            PhotonStream stream = new PhotonStream(true, null);
            // The public PUN constructor leaves the internal write list null (PUN
            // fills it via the internal SetWriteStream before use), so initialize it
            // here or the first SendNext throws a NullReferenceException.
            typeof(PhotonStream).GetField("writeData",
                BindingFlags.Instance | BindingFlags.NonPublic).SetValue(stream, new List<object>());
            ((IPunObservable)sync).OnPhotonSerializeView(stream, default(PhotonMessageInfo));
            object[] payload = stream.ToArray();

            Assert.AreEqual(3, payload.Length, "expected exactly position, rotation, scale");
            Assert.IsInstanceOf<Vector3>(payload[0]);
            Assert.IsInstanceOf<Quaternion>(payload[1]);
            Assert.IsInstanceOf<Vector3>(payload[2]);
            Assert.AreEqual(go.transform.localPosition, (Vector3)payload[0]);
            Assert.AreEqual(go.transform.localRotation, (Quaternion)payload[1]);
            Assert.AreEqual(go.transform.localScale, (Vector3)payload[2]);
        }

        // --- transform sync: read path ---------------------------------------

        /// <summary>
        /// Receiving side: three incoming values are read, in order, as
        /// position/rotation/scale into the network-target fields. Pins the
        /// deserialization contract; the read branch touches no PUN connection state.
        /// </summary>
        [Test]
        public void SerializeView_Read_StoresReceivedPositionRotationScale()
        {
            Type syncType = GameType("GenericNetSync");
            GameObject go = NewGameObject("sync-read");
            Component sync = go.AddComponent(syncType);

            Vector3 pos = new Vector3(7f, 8f, 9f);
            Quaternion rot = Quaternion.Euler(15f, 25f, 35f);
            Vector3 scale = new Vector3(2f, 3f, 4f);

            PhotonStream stream = new PhotonStream(false, new object[] { pos, rot, scale });
            ((IPunObservable)sync).OnPhotonSerializeView(stream, default(PhotonMessageInfo));

            Assert.AreEqual(pos, (Vector3)GetField(sync, "networkLocalPosition"));
            Assert.AreEqual(rot, (Quaternion)GetField(sync, "networkLocalRotation"));
            Assert.AreEqual(scale, (Vector3)GetField(sync, "networkLocalScale"));
        }

        // --- anchor-ID exchange ----------------------------------------------

        /// <summary>
        /// Anchor-ID exchange: the buffered RPC handler copies the received string
        /// into GenericNetworkManager.AzureAnchorID. Pins the observable effect (the
        /// shared anchor ID propagates), which #23 must preserve whatever transport
        /// replaces the PUN RPC.
        /// </summary>
        [Test]
        public void SharedAnchorIdHandler_WritesIdIntoNetworkManager()
        {
            Type gnmType = GameType("GenericNetworkManager");
            GameObject gnmGo = NewGameObject("gnm");
            Component gnm = gnmGo.AddComponent(gnmType);
            // Awake() assigns the singleton, but does not run in EditMode; set it here.
            Field(gnmType, "instance").SetValue(null, gnm);

            Type userType = GameType("PhotonUser");
            GameObject userGo = NewGameObject("photon-user");
            Component user = userGo.AddComponent(userType);

            const string anchorId = "anchor-1234-abcd";
            MethodInfo handler = userType.GetMethod("RPC_SetSharedAnchorID",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(handler, "RPC_SetSharedAnchorID handler not found");
            handler.Invoke(user, new object[] { anchorId });

            Assert.AreEqual(anchorId, (string)GetField(gnm, "AzureAnchorID"));
        }
    }
}

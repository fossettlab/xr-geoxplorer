using System.Collections;
using NUnit.Framework;
using Photon.Pun;
using UnityEngine;
using UnityEngine.TestTools;

namespace GeoX.Tests.Network
{
    /// <summary>
    /// Workflow 2 of 4 (issue #21): TRANSFORM SYNC — one user moves an object, another
    /// sees the movement.
    ///
    /// GenericNetSync streams object state through IPunObservable.OnPhotonSerializeView.
    /// A true two-client "remote sees it move" check needs two live peers, which is a
    /// hardware/2-device concern owned by the #32 smoke suite. What this fixture pins
    /// down — and what #23 can actually regress against in CI — is the WIRE CONTRACT:
    /// the exact payload shape and ordering written to / read from the PhotonStream.
    ///
    /// Captured contract for a non-User object (planets, asset bundles; User == false):
    ///   write order = [ localPosition (Vector3), localRotation (Quaternion), localScale (Vector3) ]
    ///   read applies those three, in the same order, to the private network* fields
    ///   that FixedUpdate then copies onto a non-owned transform.
    ///
    /// The component is reached by reflection and exercised through IPunObservable; see
    /// PunHarnessSupport for why the harness does not compile-reference app code.
    /// </summary>
    public class TransformSyncTests
    {
        private GameObject syncGo;

        [SetUp]
        public void SetUp()
        {
            // Offline mode gives PhotonView.IsMine a defined value without a server.
            PhotonNetwork.OfflineMode = true;
        }

        [TearDown]
        public void TearDown()
        {
            PhotonNetwork.OfflineMode = false;
            if (syncGo != null)
            {
                Object.Destroy(syncGo);
            }
        }

        private object CreateGenericNetSync(out PhotonView view)
        {
            var type = PunHarnessSupport.ResolveAppType("GenericNetSync");
            Assert.IsNotNull(type, "GenericNetSync not found in Assembly-CSharp — was it renamed/moved?");

            // Build it on an inactive GameObject so Start() (which dereferences scene
            // singletons like GenericNetworkManager.instance) never runs.
            syncGo = new GameObject("GenericNetSync");
            syncGo.SetActive(false);
            view = syncGo.AddComponent<PhotonView>();
            Component comp = syncGo.AddComponent(type);

            // Inject the PhotonView the component would normally cache in Start(), and
            // mark it a non-"User" object (the planet/asset-bundle sync path).
            PunHarnessSupport.SetPrivateField(comp, "PV", view);
            PunHarnessSupport.SetPrivateField(comp, "User", false);
            return comp;
        }

        /// <summary>
        /// What: the write half of OnPhotonSerializeView for a non-User object.
        /// Expected: exactly three values are queued — localPosition, localRotation,
        ///           localScale — in that order and with those types.
        /// Pass/fail: fails if the count, order, types, or values diverge.
        /// </summary>
        [UnityTest]
        public IEnumerator OnPhotonSerializeView_Writing_SendsPositionRotationScaleInOrder()
        {
            object comp = CreateGenericNetSync(out _);

            var expectedPos = new Vector3(1.5f, -2.25f, 3.75f);
            var expectedRot = Quaternion.Euler(10f, 20f, 30f);
            var expectedScale = new Vector3(2f, 2f, 2f);
            syncGo.transform.localPosition = expectedPos;
            syncGo.transform.localRotation = expectedRot;
            syncGo.transform.localScale = expectedScale;

            var stream = new PhotonStream(true, null);
            ((IPunObservable)comp).OnPhotonSerializeView(stream, default);

            object[] written = stream.ToArray();
            Assert.AreEqual(3, written.Length, "Expected exactly position, rotation, scale on the wire.");
            Assert.IsInstanceOf<Vector3>(written[0], "Slot 0 should be localPosition.");
            Assert.IsInstanceOf<Quaternion>(written[1], "Slot 1 should be localRotation.");
            Assert.IsInstanceOf<Vector3>(written[2], "Slot 2 should be localScale.");
            Assert.AreEqual(expectedPos, (Vector3)written[0]);
            Assert.AreEqual(expectedRot, (Quaternion)written[1]);
            Assert.AreEqual(expectedScale, (Vector3)written[2]);

            yield break;
        }

        /// <summary>
        /// What: the read half of OnPhotonSerializeView.
        /// Expected: the three incoming values are stored into the private
        ///           networkLocalPosition / networkLocalRotation / networkLocalScale
        ///           fields that FixedUpdate applies to a non-owned transform.
        /// Pass/fail: fails if any deserialized field does not match the payload.
        /// </summary>
        [UnityTest]
        public IEnumerator OnPhotonSerializeView_Reading_StoresIncomingTransform()
        {
            object comp = CreateGenericNetSync(out _);

            var incomingPos = new Vector3(-5f, 6f, -7f);
            var incomingRot = Quaternion.Euler(45f, 0f, 90f);
            var incomingScale = new Vector3(0.5f, 0.5f, 0.5f);

            var stream = new PhotonStream(false, new object[] { incomingPos, incomingRot, incomingScale });
            ((IPunObservable)comp).OnPhotonSerializeView(stream, default);

            Assert.AreEqual(incomingPos, PunHarnessSupport.GetPrivateField<Vector3>(comp, "networkLocalPosition"));
            Assert.AreEqual(incomingRot, PunHarnessSupport.GetPrivateField<Quaternion>(comp, "networkLocalRotation"));
            Assert.AreEqual(incomingScale, PunHarnessSupport.GetPrivateField<Vector3>(comp, "networkLocalScale"));

            yield break;
        }
    }
}

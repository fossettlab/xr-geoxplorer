using System;
using System.Collections;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GeoX.Tests.Network
{
    /// <summary>
    /// Workflow 3 of 4 (issue #21): ANCHOR-ID EXCHANGE — an anchor is placed, its ID
    /// goes over the wire, and a remote loads it.
    ///
    /// Two mechanisms cooperate today and are characterized separately:
    ///   1. PhotonUser.RPC_SetSharedAnchorID(string) — the [PunRPC] that carries the
    ///      anchor ID to peers and writes GenericNetworkManager.instance.AzureAnchorID.
    ///   2. AnchorExchanger — the REST contract that persists/retrieves anchor keys
    ///      (POST baseAddress, GET baseAddress + "/last", GET baseAddress + "/{n}").
    ///
    /// #23 must preserve the AzureAnchorID hand-off; #40 later replaces the REST side
    /// with the Azure Function. Both contracts are pinned here so either rewrite has a
    /// concrete before-state. App types are reached by reflection (see PunHarnessSupport).
    /// </summary>
    public class AnchorIdExchangeTests
    {
        private GameObject gnmGo;
        private GameObject userGo;

        [TearDown]
        public void TearDown()
        {
            if (userGo != null) Object.Destroy(userGo);
            if (gnmGo != null) Object.Destroy(gnmGo);
        }

        /// <summary>
        /// What: receiving the anchor-ID RPC on PhotonUser.
        /// Expected: RPC_SetSharedAnchorID writes its argument to
        ///           GenericNetworkManager.instance.AzureAnchorID — the single field the
        ///           rest of the app reads to align on the shared anchor.
        /// Pass/fail: fails if the static singleton field is not updated to the sent ID.
        /// </summary>
        [UnityTest]
        public IEnumerator RpcSetSharedAnchorID_WritesAzureAnchorIDOntoNetworkManager()
        {
            const string anchorId = "anchor-id-abc123";

            var gnmType = PunHarnessSupport.ResolveAppType("GenericNetworkManager");
            var userType = PunHarnessSupport.ResolveAppType("PhotonUser");
            Assert.IsNotNull(gnmType, "GenericNetworkManager not found in Assembly-CSharp.");
            Assert.IsNotNull(userType, "PhotonUser not found in Assembly-CSharp.");

            // GenericNetworkManager.Awake sets its static 'instance'. Let one frame pass
            // so Awake has run.
            gnmGo = new GameObject("GenericNetworkManager");
            gnmGo.AddComponent(gnmType);
            yield return null;

            object gnmInstance = PunHarnessSupport.GetStaticField<object>(gnmType, "instance");
            Assert.IsNotNull(gnmInstance, "GenericNetworkManager.instance should be set by Awake.");

            // Build PhotonUser on an inactive object (with a PhotonView present) so its
            // Start() — which calls PV.RPC — does not run; we invoke the handler directly.
            userGo = new GameObject("PhotonUser");
            userGo.SetActive(false);
            userGo.AddComponent<Photon.Pun.PhotonView>();
            Component photonUser = userGo.AddComponent(userType);

            PunHarnessSupport.InvokeMethod(photonUser, "RPC_SetSharedAnchorID", anchorId);

            string stored = PunHarnessSupport.GetPrivateField<string>(gnmInstance, "AzureAnchorID");
            Assert.AreEqual(anchorId, stored, "RPC handler must publish the anchor ID onto the network manager.");
        }

        /// <summary>
        /// What: AnchorExchanger.StoreAnchorKey against a stand-in HTTP endpoint.
        /// Expected: the key is POSTed to baseAddress and the numeric body of the
        ///           response is parsed and returned as a long.
        /// Pass/fail: fails if the server does not receive the posted key, or the parsed
        ///            return value does not match the server's response.
        /// Note: skipped (not failed) if a local HttpListener cannot bind in the
        ///       sandbox, so restricted environments don't report a false failure.
        /// </summary>
        [UnityTest]
        public IEnumerator AnchorExchanger_StoreAnchorKey_PostsKeyAndParsesNumericResponse()
        {
            var exchangerType = PunHarnessSupport.ResolveAppType("AnchorExchanger");
            Assert.IsNotNull(exchangerType, "AnchorExchanger not found in Assembly-CSharp.");

            HttpListener listener = null;
            string prefix = null;
            string bindError = null;
            try
            {
                int port = GetFreeLoopbackPort();
                prefix = $"http://127.0.0.1:{port}/";
                listener = new HttpListener();
                listener.Prefixes.Add(prefix);
                listener.Start();
            }
            catch (Exception ex)
            {
                if (listener != null) ((IDisposable)listener).Dispose();
                bindError = ex.Message;
            }

            if (bindError != null)
            {
                // Restricted sandboxes may forbid binding a loopback listener; skip
                // (don't fail) so this reports honestly rather than as a false negative.
                Assert.Ignore($"Could not start a local HttpListener in this environment: {bindError}");
                yield break;
            }

            string receivedBody = null;
            string receivedMethod = null;
            // Serve exactly one request on a background thread, echoing a known anchor number.
            Task serveTask = Task.Run(() =>
            {
                HttpListenerContext ctx = listener.GetContext();
                receivedMethod = ctx.Request.HttpMethod;
                using (var reader = new System.IO.StreamReader(ctx.Request.InputStream, Encoding.UTF8))
                {
                    receivedBody = reader.ReadToEnd();
                }

                byte[] payload = Encoding.UTF8.GetBytes("777");
                ctx.Response.ContentLength64 = payload.Length;
                ctx.Response.OutputStream.Write(payload, 0, payload.Length);
                ctx.Response.OutputStream.Close();
            });

            object exchanger = Activator.CreateInstance(exchangerType);
            PunHarnessSupport.SetPrivateField(exchanger, "baseAddress", prefix.TrimEnd('/'));

            var storeTask = (Task<long>)PunHarnessSupport.InvokeMethod(exchanger, "StoreAnchorKey", "outcrop-anchor-key");

            float deadline = Time.realtimeSinceStartup + 10f;
            while (!storeTask.IsCompleted && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            listener.Stop();
            ((IDisposable)listener).Dispose();

            Assert.IsTrue(storeTask.IsCompleted, "StoreAnchorKey did not complete in time.");
            Assert.AreEqual("POST", receivedMethod, "Anchor keys are stored via HTTP POST.");
            Assert.AreEqual("outcrop-anchor-key", receivedBody, "The posted body should be the raw anchor key.");
            Assert.AreEqual(777L, storeTask.Result, "The numeric response body should be parsed and returned.");
        }

        private static int GetFreeLoopbackPort()
        {
            var probe = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            int port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();
            return port;
        }
    }
}

using System;
using System.Reflection;
using Photon.Pun;
using UnityEngine;

namespace GeoX.Tests.Network
{
    /// <summary>
    /// Shared support for the PUN 2 characterization harness (issue #21).
    ///
    /// The harness deliberately does NOT compile-reference the app scripts. Those
    /// classes (GenericNetSync, GenericNetworkManager, PhotonUser, AnchorExchanger)
    /// live in the predefined <c>Assembly-CSharp</c>, which an .asmdef test assembly
    /// cannot reference. Adding an .asmdef to Assets/Scripts would be a production
    /// refactor, which issue #21 explicitly forbids ("LobbyManager, PlanetManager,
    /// GenericNetworkManager, GenericNetSync, AnchorExchanger stay as-is"). So the
    /// tests resolve those types by name at runtime via reflection and, where
    /// possible, drive them through Photon interfaces (e.g. IPunObservable) that
    /// the harness CAN reference.
    ///
    /// When the #23 rewrite introduces a clean networking assembly, these reflection
    /// seams should be replaced with direct references.
    /// </summary>
    internal static class PunHarnessSupport
    {
        public const string AppAssembly = "Assembly-CSharp";

        /// <summary>
        /// Resolves a type that lives in the app's predefined assembly. Returns null
        /// (rather than throwing) so a test can Assert with a clear message if the
        /// production class was renamed or moved.
        /// </summary>
        public static Type ResolveAppType(string typeName)
        {
            return Type.GetType($"{typeName}, {AppAssembly}");
        }

        /// <summary>
        /// Reads a private/internal instance field via reflection. Used to inspect the
        /// values a component deserialized from the wire without modifying the
        /// production class.
        /// </summary>
        public static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field == null)
            {
                throw new MissingFieldException(target.GetType().Name, fieldName);
            }

            return (T)field.GetValue(target);
        }

        /// <summary>
        /// Sets a private/internal instance field via reflection. Used to put a
        /// component into a known state (e.g. assign its cached PhotonView) without
        /// running its Start(), which has scene-wide side effects.
        /// </summary>
        public static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field == null)
            {
                throw new MissingFieldException(target.GetType().Name, fieldName);
            }

            field.SetValue(target, value);
        }

        /// <summary>
        /// Invokes a private/internal/public instance method via reflection.
        /// </summary>
        public static object InvokeMethod(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (method == null)
            {
                throw new MissingMethodException(target.GetType().Name, methodName);
            }

            return method.Invoke(target, args);
        }

        /// <summary>
        /// Reads a static field via reflection (e.g. GenericNetworkManager.instance).
        /// </summary>
        public static T GetStaticField<T>(Type type, string fieldName)
        {
            FieldInfo field = type.GetField(
                fieldName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (field == null)
            {
                throw new MissingFieldException(type.Name, fieldName);
            }

            return (T)field.GetValue(null);
        }
    }

    /// <summary>
    /// Records Photon matchmaking/connection callbacks so room-lifecycle tests can
    /// assert which callbacks fired. MonoBehaviourPunCallbacks auto-registers itself
    /// with PhotonNetwork on enable, so adding this to a GameObject is enough.
    /// </summary>
    internal class CallbackRecorder : MonoBehaviourPunCallbacks
    {
        public bool ConnectedToMaster;
        public bool CreatedRoom;
        public bool JoinedRoom;
        public bool LeftRoom;
        public bool JoinRandomFailed;
        public bool Disconnected;

        public void Reset()
        {
            ConnectedToMaster = false;
            CreatedRoom = false;
            JoinedRoom = false;
            LeftRoom = false;
            JoinRandomFailed = false;
            Disconnected = false;
        }

        public override void OnConnectedToMaster() => ConnectedToMaster = true;
        public override void OnCreatedRoom() => CreatedRoom = true;
        public override void OnJoinedRoom() => JoinedRoom = true;
        public override void OnLeftRoom() => LeftRoom = true;
        public override void OnJoinRandomFailed(short returnCode, string message) => JoinRandomFailed = true;
        public override void OnDisconnected(Photon.Realtime.DisconnectCause cause) => Disconnected = true;
    }
}

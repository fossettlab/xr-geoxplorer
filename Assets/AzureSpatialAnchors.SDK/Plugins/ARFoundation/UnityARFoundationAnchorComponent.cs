#if UNITY_ANDROID || UNITY_IOS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

#if !UNITY_2019_3_OR_NEWER
// Adapt AR Foundation 3 types to AR Foundation 2 types Unity 2019.2 and earlier.
using ARAnchor = UnityEngine.XR.ARFoundation.ARReferencePoint;
#endif

namespace Microsoft.Azure.SpatialAnchors.Unity.ARFoundation
{
    public class UnityARFoundationAnchorComponent : MonoBehaviour
    {
        /// <summary>
        /// Gets the world anchor.
        /// </summary>
        public ARAnchor WorldAnchor { get; private set; }

        /// <summary>
        /// Gets the world anchor handle.
        /// </summary>
        public IntPtr WorldAnchorHandle => this.WorldAnchor.nativePtr.GetPlatformPointer();

        /// <summary>
        /// Gets the world anchor identifier.
        /// </summary>
        public string WorldAnchorIdentifier => Marshal.PtrToStringAuto(this.WorldAnchorHandle);

        /// <summary>
        /// On Unity 6+, anchor creation is async; SpatialAnchorExtensions creates the
        /// ARAnchor first and calls <see cref="InitializeWithAnchor"/> before returning.
        /// </summary>
#if UNITY_6000_0_OR_NEWER
        internal void InitializeWithAnchor(ARAnchor anchor)
        {
            if (anchor == null)
            {
                throw new ArgumentNullException(nameof(anchor));
            }

            this.WorldAnchor = anchor;
            this.gameObject.transform.SetParent(this.WorldAnchor.transform, true);
        }
#else
        private void Awake()
        {
            this.WorldAnchor = AnchorHelpers.CreateWorldAnchor(this.gameObject.transform);
            this.gameObject.transform.SetParent(this.WorldAnchor.transform, true);
        }
#endif

        /// <summary>
        /// Destroying the attached Behaviour will result in the game or Scene
        /// receiving OnDestroy.
        /// </summary>
        private void OnDestroy()
        {
            if (this.WorldAnchor != null)
            {
#if UNITY_6000_0_OR_NEWER
                SpatialAnchorManager.arAnchorManager.TryRemoveAnchor(this.WorldAnchor);
#elif UNITY_2019_3_OR_NEWER
                SpatialAnchorManager.arAnchorManager.RemoveAnchor(this.WorldAnchor);
#else
                SpatialAnchorManager.arAnchorManager.RemoveReferencePoint(this.WorldAnchor);
#endif
                this.WorldAnchor = null;
            }
        }
    }
}
#endif

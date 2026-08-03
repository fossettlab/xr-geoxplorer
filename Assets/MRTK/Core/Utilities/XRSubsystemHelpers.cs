// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace Microsoft.MixedReality.Toolkit.Utilities
{
    public static class XRSubsystemHelpers
    {
        public static bool IsDisplaySubsystemRunning()
        {
#if UNITY_2020_2_OR_NEWER
            var displaySubsystems = new List<XRDisplaySubsystem>();
            SubsystemManager.GetSubsystems(displaySubsystems);

            for (int i = 0; i < displaySubsystems.Count; i++)
            {
                if (displaySubsystems[i].running)
                {
                    return true;
                }
            }

            return false;
#else
            return XRDevice.isPresent;
#endif
        }
    }
}

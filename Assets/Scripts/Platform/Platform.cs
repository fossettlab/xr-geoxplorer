using System;
using UnityEngine;
using UnityEngine.XR;

public enum PlatformId
{
    Editor,
    Quest,
    Mobile,
    [Obsolete("HoloLens 2 / UWP is not supported. Kept for API compatibility.")]
    LegacyHoloLens2,
    Other
}

public static class Platform
{
    public static PlatformId Current
    {
        get
        {
            if (Application.isEditor)
            {
                return PlatformId.Editor;
            }

            RuntimePlatform runtimePlatform = Application.platform;
            if (runtimePlatform == RuntimePlatform.Android)
            {
                return IsQuestRuntime() ? PlatformId.Quest : PlatformId.Mobile;
            }

            if (runtimePlatform == RuntimePlatform.IPhonePlayer)
            {
                return PlatformId.Mobile;
            }

            return PlatformId.Other;
        }
    }

    public static bool IsEditor
    {
        get { return Current == PlatformId.Editor; }
    }

    public static bool IsQuest
    {
        get { return Current == PlatformId.Quest; }
    }

    public static bool IsMobile
    {
        get { return Current == PlatformId.Mobile; }
    }

    [Obsolete("HoloLens 2 / UWP is not supported.")]
    public static bool IsLegacyHoloLens2
    {
        get { return false; }
    }

    public static bool IsAnyXR
    {
        get { return IsQuest; }
    }

    private static bool IsQuestRuntime()
    {
        string deviceModel = (SystemInfo.deviceModel ?? string.Empty).ToLowerInvariant();
        if (deviceModel.Contains("quest") ||
            deviceModel.Contains("oculus") ||
            deviceModel.Contains("meta"))
        {
            return true;
        }

        string loadedDeviceName = XRSettings.loadedDeviceName;
        return !string.IsNullOrEmpty(loadedDeviceName) &&
               loadedDeviceName != "None" &&
               loadedDeviceName.ToLowerInvariant().Contains("oculus");
    }
}

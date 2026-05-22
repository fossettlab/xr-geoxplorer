using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class Unity2022UpgradeVerification
{
    private const string ScenePath = "Assets/Scenes/GeoXShared.unity";
    private const double PlayModeDurationSeconds = 30.0d;
    private const string ActiveKey = "Unity2022UpgradeVerification.Active";
    private const string StartedAtKey = "Unity2022UpgradeVerification.StartedAt";
    private const string RequestedExitKey = "Unity2022UpgradeVerification.RequestedExit";
    private const string BlockingLogsKey = "Unity2022UpgradeVerification.BlockingLogs";

    private static readonly List<string> BlockingLogs = new List<string>();
    private static bool callbacksRegistered;

    static Unity2022UpgradeVerification()
    {
        if (SessionState.GetBool(ActiveKey, false))
        {
            RegisterCallbacks();
        }
    }

    public static void OpenGeoXSharedAndRunPlayMode()
    {
        SessionState.SetBool(ActiveKey, true);
        SessionState.SetBool(RequestedExitKey, false);
        SessionState.SetString(StartedAtKey, string.Empty);
        SessionState.SetString(BlockingLogsKey, string.Empty);
        BlockingLogs.Clear();
        RegisterCallbacks();

        var scene = EditorSceneManager.OpenScene(ScenePath);
        if (!scene.IsValid())
        {
            Fail("Could not open " + ScenePath);
            return;
        }

        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.update += OnEditorUpdate;
        EditorApplication.isPlaying = true;
    }

    private static void CaptureBlockingLog(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Assert || type == LogType.Exception)
        {
            var entry = type + ": " + condition + "\n" + stackTrace;
            BlockingLogs.Add(entry);
            var savedLogs = SessionState.GetString(BlockingLogsKey, string.Empty);
            SessionState.SetString(BlockingLogsKey, savedLogs + "\n\n" + entry);
        }
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            SessionState.SetString(
                StartedAtKey,
                EditorApplication.timeSinceStartup.ToString(CultureInfo.InvariantCulture));
            return;
        }

        if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(RequestedExitKey, false))
        {
            Complete();
        }
    }

    private static void OnEditorUpdate()
    {
        if (!SessionState.GetBool(ActiveKey, false) || SessionState.GetBool(RequestedExitKey, false))
        {
            return;
        }

        var startedAtString = SessionState.GetString(StartedAtKey, string.Empty);
        if (string.IsNullOrEmpty(startedAtString))
        {
            return;
        }

        if (!double.TryParse(startedAtString, NumberStyles.Float, CultureInfo.InvariantCulture, out var startedAt))
        {
            Fail("Could not parse Unity 2022 verification start time.");
            return;
        }

        if (EditorApplication.timeSinceStartup - startedAt >= PlayModeDurationSeconds)
        {
            SessionState.SetBool(RequestedExitKey, true);
            EditorApplication.isPlaying = false;
        }
    }

    private static void Complete()
    {
        var savedLogs = SessionState.GetString(BlockingLogsKey, string.Empty).Trim();
        Cleanup();

        if (!string.IsNullOrEmpty(savedLogs))
        {
            Fail("GeoXShared play mode emitted blocking logs:\n" + savedLogs);
            return;
        }

        Debug.Log("Unity 2022 upgrade verification passed for " + ScenePath);
        EditorApplication.Exit(0);
    }

    private static void Fail(string message)
    {
        Cleanup();
        Debug.LogError(message);
        EditorApplication.Exit(1);
    }

    private static void RegisterCallbacks()
    {
        if (callbacksRegistered)
        {
            return;
        }

        Application.logMessageReceived += CaptureBlockingLog;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.update += OnEditorUpdate;
        callbacksRegistered = true;
    }

    private static void Cleanup()
    {
        SessionState.SetBool(ActiveKey, false);
        SessionState.SetBool(RequestedExitKey, false);
        SessionState.SetString(StartedAtKey, string.Empty);
        SessionState.SetString(BlockingLogsKey, string.Empty);
        Application.logMessageReceived -= CaptureBlockingLog;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.update -= OnEditorUpdate;
        callbacksRegistered = false;
    }
}

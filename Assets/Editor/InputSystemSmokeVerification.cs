using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

[InitializeOnLoad]
public static class InputSystemSmokeVerification
{
    private const string ScenePath = "Assets/Scenes/GeoXShared.unity";
    private const string TestUsername = "Codex UI Smoke";
    private const string TestRoomName = "Codex Room 42";
    private const string TestAnchorName = "Codex Anchor 42";
    private const string PendingSessionKey = "InputSystemSmokeVerification.Pending";
    private const string StepSessionKey = "InputSystemSmokeVerification.Step";
    private const string BlockingLogsSessionKey = "InputSystemSmokeVerification.BlockingLogs";

    private static double playModeStartedAt;

    static InputSystemSmokeVerification()
    {
        if (!SessionState.GetBool(PendingSessionKey, false))
        {
            return;
        }

        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        Application.logMessageReceived -= OnLogMessageReceived;
        Application.logMessageReceived += OnLogMessageReceived;

        if (EditorApplication.isPlaying)
        {
            playModeStartedAt = EditorApplication.timeSinceStartup;
            EditorApplication.update -= VerifyAfterSceneSettles;
            EditorApplication.update += VerifyAfterSceneSettles;
        }
    }

    public static void OpenGeoXSharedAndVerifyLoginUI()
    {
        SessionState.SetBool(PendingSessionKey, true);
        SessionState.SetInt(StepSessionKey, 0);
        SessionState.SetString(BlockingLogsSessionKey, string.Empty);
        EditorSceneManager.OpenScene(ScenePath);
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        Application.logMessageReceived -= OnLogMessageReceived;
        Application.logMessageReceived += OnLogMessageReceived;
        EditorApplication.EnterPlaymode();
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            playModeStartedAt = EditorApplication.timeSinceStartup;
            EditorApplication.update -= VerifyAfterSceneSettles;
            EditorApplication.update += VerifyAfterSceneSettles;
        }
    }

    private static void VerifyAfterSceneSettles()
    {
        if (EditorApplication.timeSinceStartup - playModeStartedAt < 1.0d)
        {
            return;
        }

        try
        {
            string blockingLogs = SessionState.GetString(BlockingLogsSessionKey, string.Empty);
            if (!string.IsNullOrEmpty(blockingLogs))
            {
                throw new InvalidOperationException("Blocking logs were emitted during input smoke verification:\n" + blockingLogs);
            }

            VerifyEventSystemModules();
            int step = SessionState.GetInt(StepSessionKey, 0);
            if (step == 0)
            {
                VerifyLoginUI();
                PrepareFieldFocus("RoomNameInputField (TMP)");
                SessionState.SetInt(StepSessionKey, 1);
                playModeStartedAt = EditorApplication.timeSinceStartup;
                return;
            }

            if (step == 1)
            {
                VerifyActiveFieldFocus("RoomNameInputField (TMP)", TestRoomName);
                PrepareFieldFocus("CreateAnchorsInputField (TMP)");
                SessionState.SetInt(StepSessionKey, 2);
                playModeStartedAt = EditorApplication.timeSinceStartup;
                return;
            }

            VerifyActiveFieldFocus("CreateAnchorsInputField (TMP)", TestAnchorName);
            Debug.Log("InputSystemSmokeVerification passed.");
            SessionState.SetBool(PendingSessionKey, false);
            SessionState.SetInt(StepSessionKey, 0);
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            SessionState.SetBool(PendingSessionKey, false);
            SessionState.SetInt(StepSessionKey, 0);
            Debug.LogError("InputSystemSmokeVerification failed: " + exception);
            EditorApplication.Exit(1);
        }
        finally
        {
            if (!SessionState.GetBool(PendingSessionKey, false))
            {
                EditorApplication.update -= VerifyAfterSceneSettles;
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                Application.logMessageReceived -= OnLogMessageReceived;
            }
        }
    }

    private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
    {
        if (!SessionState.GetBool(PendingSessionKey, false))
        {
            return;
        }

        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
        {
            return;
        }

        if (!IsInputSystemBlockingLog(condition))
        {
            return;
        }

        string existingLogs = SessionState.GetString(BlockingLogsSessionKey, string.Empty);
        SessionState.SetString(BlockingLogsSessionKey, existingLogs + condition + "\n" + stackTrace + "\n");
    }

    private static bool IsInputSystemBlockingLog(string condition)
    {
        return condition.Contains("You are trying to read Input using the UnityEngine.Input class") ||
            condition.Contains("StandaloneInputModule") ||
            condition.Contains("All compiler errors have to be fixed before you can enter playmode");
    }

    private static void VerifyLoginUI()
    {
        TMP_InputField usernameField = FindInputField("UsernameInputField (TMP)");
        EventSystem eventSystem = UnityEngine.Object.FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            throw new InvalidOperationException("No EventSystem found while verifying input-field focus.");
        }

        if (eventSystem.currentSelectedGameObject != usernameField.gameObject || !usernameField.isFocused)
        {
            throw new InvalidOperationException("UsernameInputField (TMP) was not focused by the input bootstrapper.");
        }

        usernameField.text = TestUsername;
        if (usernameField.text != TestUsername)
        {
            throw new InvalidOperationException("UsernameInputField (TMP) did not accept text assignment.");
        }

        Transform loginButton = Resources.FindObjectsOfTypeAll<Transform>()
            .FirstOrDefault(transform => transform.name == "LoginButton");
        if (loginButton == null)
        {
            throw new InvalidOperationException("LoginButton was not found.");
        }
    }

    private static void VerifyEventSystemModules()
    {
        EventSystem[] eventSystems = UnityEngine.Object.FindObjectsOfType<EventSystem>();
        if (eventSystems.Length == 0)
        {
            throw new InvalidOperationException("No EventSystem found in the loaded scene.");
        }

#if ENABLE_INPUT_SYSTEM
        if (eventSystems.Any(eventSystem => eventSystem.GetComponent<InputSystemUIInputModule>() == null))
        {
            throw new InvalidOperationException("At least one EventSystem is missing InputSystemUIInputModule.");
        }

        if (eventSystems.Any(eventSystem =>
                eventSystem.GetComponents<StandaloneInputModule>()
                    .Any(module => module.GetType() == typeof(StandaloneInputModule) && module.enabled)))
        {
            throw new InvalidOperationException("At least one legacy StandaloneInputModule is still enabled.");
        }
#endif
    }

    private static void PrepareFieldFocus(string fieldName)
    {
        TMP_InputField inputField = FindInputField(fieldName);
        ActivateSelfAndParents(inputField.transform);

        EventSystem eventSystem = UnityEngine.Object.FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            throw new InvalidOperationException("No EventSystem found while preparing " + fieldName + " focus.");
        }

        eventSystem.SetSelectedGameObject(null);
    }

    private static void VerifyActiveFieldFocus(string fieldName, string testText)
    {
        TMP_InputField inputField = FindInputField(fieldName);
        EventSystem eventSystem = UnityEngine.Object.FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            throw new InvalidOperationException("No EventSystem found while verifying " + fieldName + " focus.");
        }

        if (eventSystem.currentSelectedGameObject != inputField.gameObject || !inputField.isFocused)
        {
            throw new InvalidOperationException(fieldName + " was not focused by the input bootstrapper.");
        }

        inputField.text = testText;
        if (inputField.text != testText)
        {
            throw new InvalidOperationException(fieldName + " did not accept text assignment.");
        }
    }

    private static TMP_InputField FindInputField(string fieldName)
    {
        TMP_InputField inputField = UnityEngine.Object.FindObjectsOfType<TMP_InputField>(true)
            .FirstOrDefault(field => field.name == fieldName);
        if (inputField == null)
        {
            throw new InvalidOperationException(fieldName + " was not found.");
        }

        return inputField;
    }

    private static void ActivateSelfAndParents(Transform transform)
    {
        if (transform.parent != null)
        {
            ActivateSelfAndParents(transform.parent);
        }

        transform.gameObject.SetActive(true);
    }
}

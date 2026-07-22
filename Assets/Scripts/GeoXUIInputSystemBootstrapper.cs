using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR;
using TMPro;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Input;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.XR;
#endif

public static class GeoXUIInputSystemBootstrapper
{
    private const string BridgeName = "GeoX UI Input System Bridge";
    private const float InputFieldFallbackRadiusPixels = 220f;
    private const float VrCanvasDistance = 1.8f;
    private const float VrCanvasHeightMeters = 1.35f;

    private static readonly string[] QuestPlatformCanvasNames =
    {
        "LoaderCanvas",
        "LobbyCanvas",
        "RoomCanvas",
        "MenuCanvas",
        "InAppCanvas",
        "TutorialCanvas"
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ConfigureLoadedEventSystems()
    {
        ConfigureEventSystems();
        EnsureBridge();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ConfigureEventSystems();
        EnsureBridge();
    }

    private static void ConfigureEventSystems()
    {
#if ENABLE_INPUT_SYSTEM
        EventSystem[] eventSystems = Object.FindObjectsOfType<EventSystem>();
        if (eventSystems.Length == 0)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystems = new[] { eventSystemObject.GetComponent<EventSystem>() };
        }

        bool questVrRuntime = IsQuestOrVrRuntimeStatic();
        foreach (EventSystem eventSystem in eventSystems)
        {
            InputSystemUIInputModule uiInputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (uiInputModule == null)
            {
                uiInputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                uiInputModule.AssignDefaultActions();
            }

            uiInputModule.enabled = true;

            foreach (StandaloneInputModule legacyModule in eventSystem.GetComponents<StandaloneInputModule>())
            {
                // Keep MixedRealityInputModule available for HoloLens; disable legacy modules on Quest.
                if (legacyModule.GetType() == typeof(StandaloneInputModule) || questVrRuntime)
                {
                    legacyModule.enabled = false;
                }
            }
        }
#endif
    }

    private static bool IsQuestOrVrRuntimeStatic()
    {
        if (HasQuestPlatformRoot())
        {
            return true;
        }

        if (XRSettings.enabled &&
            !string.IsNullOrEmpty(XRSettings.loadedDeviceName) &&
            XRSettings.loadedDeviceName != "None")
        {
            string loaded = XRSettings.loadedDeviceName.ToLowerInvariant();
            if (loaded.Contains("oculus") || loaded.Contains("meta") || loaded.Contains("openxr"))
            {
                return IsQuestDeviceModel() || Application.platform == RuntimePlatform.Android;
            }
        }

        return IsQuestDeviceModel();
    }

    private static bool HasQuestPlatformRoot()
    {
        return FindPlatformRootByName("PlatformRoot.Quest3") != null;
    }

    private static GameObject FindPlatformRootByName(string rootName)
    {
        foreach (Transform transform in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (transform == null ||
                !transform.gameObject.scene.IsValid() ||
                transform.name != rootName)
            {
                continue;
            }

            return transform.gameObject;
        }

        return null;
    }

    private static GameObject FindAnyPlatformRoot()
    {
        GameObject quest = FindPlatformRootByName("PlatformRoot.Quest3");
        if (quest != null)
        {
            return quest;
        }

        GameObject mobile = FindPlatformRootByName("PlatformRoot.Mobile");
        if (mobile != null)
        {
            return mobile;
        }

        return FindPlatformRootByName("PlatformRoot.HoloLens2");
    }

    private static bool IsQuestDeviceModel()
    {
        string deviceModel = SystemInfo.deviceModel;
        if (string.IsNullOrEmpty(deviceModel))
        {
            return false;
        }

        deviceModel = deviceModel.ToLowerInvariant();
        return deviceModel.Contains("quest") ||
               deviceModel.Contains("oculus") ||
               deviceModel.Contains("meta");
    }

    private static void EnsureBridge()
    {
#if ENABLE_INPUT_SYSTEM
        if (Object.FindObjectOfType<GeoXUIInputSystemBridge>() != null)
        {
            return;
        }

        GameObject bridgeObject = new GameObject(BridgeName);
        Object.DontDestroyOnLoad(bridgeObject);
        bridgeObject.AddComponent<GeoXUIInputSystemBridge>();
#endif
    }

#if ENABLE_INPUT_SYSTEM
    [DefaultExecutionOrder(-32000)]
    private sealed class GeoXUIInputSystemBridge : MonoBehaviour
    {
        private static readonly string[] DefaultInputFieldPriority =
        {
            "CreateAnchorsInputField (TMP)",
            "FindAnchorsInputField (TMP)",
            "RoomNameInputField (TMP)",
            "UsernameInputField (TMP)",
            "UsernameField (TMP)"
        };

        private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();
        private LineRenderer rightHandRay;
        private InputActionAsset questUiActions;
        private Transform questUiAnchor;
        private TouchScreenKeyboard questSoftKeyboard;
        private TMP_InputField questSoftKeyboardField;
        private bool loggedQuestSoftKeyboard;
        private bool preparedQuestInputFields;
        private bool loggedVrRuntimeConfiguration;
        private bool loggedQuestDetection;
        private bool loggedMissingPlatformRoot;
        private bool configuredXrUiActions;
        private bool disabledHoloLensUi;
        private bool disabledGazePointers;
        private bool fixedQuestUiContrast;
        private bool laidOutQuestLobbyLogin;
        private Canvas lastLobbyCanvas;

        /// <summary>
        /// Resolves the spawned platform root from the PlatformBootstrapper component.
        /// The platform prefabs are variants whose runtime root is NOT named
        /// "PlatformRoot.Quest3", so name matching is unreliable; the component
        /// reference is authoritative.
        /// </summary>
        private static Transform GetPlatformRootTransform()
        {
            PlatformBootstrapper bootstrapper = Object.FindObjectOfType<PlatformBootstrapper>();
            if (bootstrapper != null && bootstrapper.InstantiatedPlatformRoot != null)
            {
                return bootstrapper.InstantiatedPlatformRoot.transform;
            }

            // Fallbacks for older name-based layout.
            GameObject named = FindPlatformRootByName("PlatformRoot.Quest3") ??
                               FindPlatformRootByName("PlatformRoot.Mobile") ??
                               FindPlatformRootByName("PlatformRoot.HoloLens2");
            return named != null ? named.transform : null;
        }

        private void Awake()
        {
            ConfigureEventSystems();
        }

        private void OnEnable()
        {
            ConfigureEventSystems();
        }

        private void OnDisable()
        {
            if (rightHandRay != null)
            {
                Destroy(rightHandRay.gameObject);
                rightHandRay = null;
            }

            if (questUiActions != null)
            {
                questUiActions.Disable();
            }
        }

        private void OnDestroy()
        {
            if (questUiActions != null)
            {
                Destroy(questUiActions);
                questUiActions = null;
            }
        }

        private void Start()
        {
            ConfigureEventSystems();
            ConfigureQuestVrRuntimeIfNeeded();
            FocusDefaultInputFieldIfNeeded();
        }

        private void Update()
        {
            ConfigureEventSystems();
            ConfigureQuestVrRuntimeIfNeeded();
            FocusDefaultInputFieldIfNeeded();
            UpdateRightHandRay();
            UpdateQuestUiAnchor();
            UpdateQuestUiTriggerClick();
            UpdateQuestSoftKeyboard();

            EventSystem eventSystem = EventSystem.current ?? Object.FindObjectOfType<EventSystem>();
            if (eventSystem == null)
            {
                return;
            }

            if (TrySubmitFocusedField(eventSystem))
            {
                return;
            }

            Vector2 pointerPosition;
            if (!TryGetPointerPress(out pointerPosition))
            {
                return;
            }

            raycastResults.Clear();
            PointerEventData pointerData = new PointerEventData(eventSystem)
            {
                position = pointerPosition
            };

            eventSystem.RaycastAll(pointerData, raycastResults);
            bool raycastHitControl = false;
            foreach (RaycastResult result in raycastResults)
            {
                if (TryFocusInputField(result.gameObject, eventSystem, pointerData))
                {
                    return;
                }

                raycastHitControl |= HasSubmitControl(result.gameObject);
            }

            TMP_InputField pointerTargetField = GetPointerTargetInputField(pointerPosition, !raycastHitControl);
            if (pointerTargetField != null &&
                TryFocusInputField(pointerTargetField.gameObject, eventSystem, pointerData))
            {
                return;
            }

            if (!raycastHitControl &&
                (TrySubmitPhysicsControl(pointerPosition, eventSystem, pointerData) ||
                 TrySubmitRectTransformControl(pointerPosition, eventSystem, pointerData)))
            {
                return;
            }

            TMP_InputField fallbackField = GetDefaultActiveInputField();
            if (fallbackField != null)
            {
                TryFocusInputField(fallbackField.gameObject, eventSystem, eventData: pointerData);
            }
        }

        private bool TryFocusInputField(GameObject hitObject, EventSystem eventSystem, BaseEventData eventData)
        {
            TMP_InputField tmpInputField = hitObject.GetComponentInParent<TMP_InputField>();
            if (tmpInputField != null && tmpInputField.interactable && tmpInputField.isActiveAndEnabled)
            {
                PrepareQuestTmpInputField(tmpInputField);
                eventSystem.SetSelectedGameObject(tmpInputField.gameObject, eventData);
                tmpInputField.Select();
                tmpInputField.ActivateInputField();
                OpenQuestSoftKeyboard(tmpInputField);
                return true;
            }

            InputField inputField = hitObject.GetComponentInParent<InputField>();
            if (inputField != null && inputField.interactable && inputField.isActiveAndEnabled)
            {
                eventSystem.SetSelectedGameObject(inputField.gameObject, eventData);
                inputField.Select();
                inputField.ActivateInputField();
                return true;
            }

            return false;
        }

        private static void PrepareQuestTmpInputField(TMP_InputField field)
        {
            if (field == null)
            {
                return;
            }

            field.interactable = true;
            field.readOnly = false;
            // Quest/Android: show the system keyboard, but hide the separate mobile input bar
            // which does not work well in VR.
            field.shouldHideSoftKeyboard = false;
            field.shouldHideMobileInput = true;

            Graphic target = field.targetGraphic;
            if (target != null)
            {
                target.raycastTarget = true;
            }

            if (field.textComponent != null)
            {
                field.textComponent.raycastTarget = true;
            }

            if (field.placeholder is Graphic placeholderGraphic)
            {
                placeholderGraphic.raycastTarget = false;
            }
        }

        private void OpenQuestSoftKeyboard(TMP_InputField field)
        {
            if (!IsQuestOrVrRuntime() || field == null || field.readOnly)
            {
                return;
            }

            PrepareQuestTmpInputField(field);

            bool secure = field.inputType == TMP_InputField.InputType.Password;
            string placeholder = string.Empty;
            if (field.placeholder is TMP_Text placeholderText)
            {
                placeholder = placeholderText.text;
            }

            questSoftKeyboardField = field;
            questSoftKeyboard = TouchScreenKeyboard.Open(
                field.text ?? string.Empty,
                field.keyboardType,
                autocorrection: false,
                multiline: field.multiLine,
                secure: secure,
                alert: false,
                textPlaceholder: placeholder,
                characterLimit: field.characterLimit);

            if (!loggedQuestSoftKeyboard)
            {
                loggedQuestSoftKeyboard = true;
                Debug.LogFormat(
                    "GeoX UI Input System opened Quest soft keyboard for '{0}' (supported={1}, keyboard={2}).",
                    field.name,
                    TouchScreenKeyboard.isSupported,
                    questSoftKeyboard != null);
            }
        }

        private void UpdateQuestSoftKeyboard()
        {
            if (!IsQuestOrVrRuntime() || questSoftKeyboard == null)
            {
                return;
            }

            if (questSoftKeyboardField == null ||
                !questSoftKeyboardField.isActiveAndEnabled)
            {
                questSoftKeyboard = null;
                questSoftKeyboardField = null;
                return;
            }

            // Keep TMP text in sync while the Quest/Android system keyboard is open.
            if (questSoftKeyboard.status == TouchScreenKeyboard.Status.Visible)
            {
                string keyboardText = questSoftKeyboard.text ?? string.Empty;
                if (questSoftKeyboardField.text != keyboardText)
                {
                    questSoftKeyboardField.text = keyboardText;
                    questSoftKeyboardField.caretPosition = keyboardText.Length;
                }

                return;
            }

            if (questSoftKeyboard.status == TouchScreenKeyboard.Status.Done ||
                questSoftKeyboard.status == TouchScreenKeyboard.Status.LostFocus)
            {
                questSoftKeyboardField.text = questSoftKeyboard.text ?? string.Empty;
            }

            questSoftKeyboardField.DeactivateInputField();
            questSoftKeyboard = null;
            questSoftKeyboardField = null;
        }

        private bool PrepareQuestInputFields()
        {
            if (preparedQuestInputFields)
            {
                return false;
            }

            bool changed = false;
            foreach (TMP_InputField field in Object.FindObjectsOfType<TMP_InputField>(true))
            {
                if (field == null ||
                    !field.gameObject.scene.IsValid() ||
                    !IsUnderQuestPlatform(field.transform))
                {
                    continue;
                }

                PrepareQuestTmpInputField(field);
                changed = true;
            }

            preparedQuestInputFields = true;
            return changed;
        }

        private static bool HasSubmitControl(GameObject hitObject)
        {
            Button button = hitObject.GetComponentInParent<Button>();
            if (button != null && button.isActiveAndEnabled && button.interactable)
            {
                return true;
            }

            Interactable interactable = hitObject.GetComponentInParent<Interactable>();
            return interactable != null && interactable.isActiveAndEnabled && interactable.IsEnabled;
        }

        private static bool TrySubmitControl(GameObject hitObject, EventSystem eventSystem, BaseEventData eventData)
        {
            Button button = hitObject.GetComponentInParent<Button>();
            if (button != null && button.isActiveAndEnabled && button.interactable)
            {
                eventSystem.SetSelectedGameObject(button.gameObject, eventData);
                button.onClick.Invoke();
                return true;
            }

            Interactable interactable = hitObject.GetComponentInParent<Interactable>();
            if (interactable != null && interactable.isActiveAndEnabled && interactable.IsEnabled)
            {
                eventSystem.SetSelectedGameObject(interactable.gameObject, eventData);
                interactable.TriggerOnClick();
                return true;
            }

            return false;
        }

        private static bool TrySubmitFocusedField(EventSystem eventSystem)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null ||
                (!keyboard.enterKey.wasPressedThisFrame && !keyboard.numpadEnterKey.wasPressedThisFrame))
            {
                return false;
            }

            GameObject selectedObject = eventSystem.currentSelectedGameObject;
            if (selectedObject == null ||
                selectedObject.GetComponent<TMP_InputField>() == null &&
                selectedObject.GetComponent<InputField>() == null)
            {
                return false;
            }

            BaseEventData eventData = new BaseEventData(eventSystem);
            string submitButtonName = GetSubmitButtonNameForField(selectedObject);
            if (string.IsNullOrEmpty(submitButtonName))
            {
                return false;
            }

            foreach (Transform transform in FindObjectsOfType<Transform>())
            {
                if (transform.name == submitButtonName &&
                    transform.gameObject.activeInHierarchy &&
                    TrySubmitControl(transform.gameObject, eventSystem, eventData))
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetSubmitButtonNameForField(GameObject selectedObject)
        {
            TMP_InputField tmpInputField = selectedObject.GetComponent<TMP_InputField>();
            string fieldName = tmpInputField != null ? tmpInputField.name : selectedObject.name;

            if (fieldName == "UsernameInputField (TMP)" || fieldName == "UsernameField (TMP)")
            {
                return "LoginButton";
            }

            if (fieldName == "RoomNameInputField (TMP)")
            {
                return "CreateRoomButton";
            }

            if (fieldName == "CreateAnchorsInputField (TMP)")
            {
                return "StartCreatingAnchorButton";
            }

            if (fieldName == "FindAnchorsInputField (TMP)")
            {
                return "StartFindingAnchorButton";
            }

            return null;
        }

        private static bool TrySubmitPhysicsControl(Vector2 pointerPosition, EventSystem eventSystem, BaseEventData eventData)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return false;
            }

            Ray ray = camera.ScreenPointToRay(pointerPosition);
            RaycastHit hit;
            if (!Physics.Raycast(ray, out hit))
            {
                return false;
            }

            return TrySubmitControl(hit.collider.gameObject, eventSystem, eventData);
        }

        private static bool TrySubmitRectTransformControl(Vector2 pointerPosition, EventSystem eventSystem, BaseEventData eventData)
        {
            foreach (Button button in FindObjectsOfType<Button>())
            {
                RectTransform rectTransform = button.transform as RectTransform;
                if (button.isActiveAndEnabled &&
                    button.interactable &&
                    IsPointerInsideRectTransform(rectTransform, pointerPosition) &&
                    TrySubmitControl(button.gameObject, eventSystem, eventData))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsPointerInsideRectTransform(RectTransform rectTransform, Vector2 pointerPosition)
        {
            Rect screenRect;
            return TryGetScreenRect(rectTransform, out screenRect) && screenRect.Contains(pointerPosition);
        }

        private static TMP_InputField GetPointerTargetInputField(Vector2 pointerPosition, bool includeNearestFallback)
        {
            TMP_InputField closestField = null;
            float closestDistance = float.MaxValue;

            foreach (TMP_InputField inputField in FindObjectsOfType<TMP_InputField>())
            {
                if (!inputField.gameObject.activeInHierarchy || !inputField.interactable)
                {
                    continue;
                }

                RectTransform rectTransform = inputField.transform as RectTransform;
                Rect screenRect;
                if (!TryGetScreenRect(rectTransform, out screenRect))
                {
                    continue;
                }

                if (screenRect.Contains(pointerPosition))
                {
                    return inputField;
                }

                float distance = DistanceToRect(screenRect, pointerPosition);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestField = inputField;
                }
            }

            return includeNearestFallback && closestDistance <= InputFieldFallbackRadiusPixels ? closestField : null;
        }

        private static bool TryGetScreenRect(RectTransform rectTransform, out Rect screenRect)
        {
            screenRect = default;
            if (rectTransform == null || !rectTransform.gameObject.activeInHierarchy)
            {
                return false;
            }

            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
            Camera eventCamera = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                eventCamera = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
            }

            Vector2 min = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[0]);
            Vector2 max = min;
            for (int i = 1; i < corners.Length; i++)
            {
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[i]);
                min = Vector2.Min(min, screenPoint);
                max = Vector2.Max(max, screenPoint);
            }

            screenRect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            return screenRect.width > 0f && screenRect.height > 0f;
        }

        private static float DistanceToRect(Rect rect, Vector2 point)
        {
            float dx = Mathf.Max(rect.xMin - point.x, 0f, point.x - rect.xMax);
            float dy = Mathf.Max(rect.yMin - point.y, 0f, point.y - rect.yMax);
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        private void FocusDefaultInputFieldIfNeeded()
        {
            if (IsQuestOrVrRuntime())
            {
                // On Quest, do not auto-steal focus from buttons; selection comes from controller click.
                return;
            }

            EventSystem eventSystem = EventSystem.current ?? Object.FindObjectOfType<EventSystem>();
            if (eventSystem == null)
            {
                return;
            }

            GameObject selectedObject = eventSystem.currentSelectedGameObject;
            if (selectedObject != null &&
                selectedObject.activeInHierarchy &&
                IsSelectedInputField(selectedObject))
            {
                return;
            }

            TMP_InputField defaultInputField = GetDefaultActiveInputField();
            if (defaultInputField == null)
            {
                return;
            }

            BaseEventData eventData = new BaseEventData(eventSystem);
            TryFocusInputField(defaultInputField.gameObject, eventSystem, eventData);
        }

        private static TMP_InputField GetDefaultActiveInputField()
        {
            TMP_InputField[] priorityFields = new TMP_InputField[DefaultInputFieldPriority.Length];
            TMP_InputField selectedField = null;

            foreach (TMP_InputField inputField in FindObjectsOfType<TMP_InputField>())
            {
                if (!inputField.gameObject.activeInHierarchy || !inputField.interactable)
                {
                    continue;
                }

                bool isPriorityField = false;
                for (int i = 0; i < DefaultInputFieldPriority.Length; i++)
                {
                    if (inputField.name == DefaultInputFieldPriority[i])
                    {
                        priorityFields[i] = inputField;
                        isPriorityField = true;
                        break;
                    }
                }

                if (isPriorityField)
                {
                    continue;
                }

                if (selectedField != null)
                {
                    return null;
                }

                selectedField = inputField;
            }

            for (int i = 0; i < priorityFields.Length; i++)
            {
                if (priorityFields[i] != null)
                {
                    return priorityFields[i];
                }
            }

            return selectedField;
        }

        private static bool IsSelectedInputField(GameObject selectedObject)
        {
            TMP_InputField tmpInputField = selectedObject.GetComponent<TMP_InputField>();
            if (tmpInputField != null)
            {
                return tmpInputField.isActiveAndEnabled && tmpInputField.interactable;
            }

            InputField inputField = selectedObject.GetComponent<InputField>();
            return inputField != null && inputField.isActiveAndEnabled && inputField.interactable;
        }

        private static bool TryGetPointerPress(out Vector2 pointerPosition)
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                pointerPosition = Mouse.current.position.ReadValue();
                return true;
            }

            if (Touchscreen.current != null)
            {
                foreach (TouchControl touch in Touchscreen.current.touches)
                {
                    if (touch.press.wasPressedThisFrame)
                    {
                        pointerPosition = touch.position.ReadValue();
                        return true;
                    }
                }
            }

            pointerPosition = default;
            return false;
        }

        private void ConfigureQuestVrRuntimeIfNeeded()
        {
            if (!IsQuestOrVrRuntime())
            {
                return;
            }

            if (!loggedQuestDetection)
            {
                PlatformBootstrapper bootstrapper = Object.FindObjectOfType<PlatformBootstrapper>();
                Transform platformRoot = GetPlatformRootTransform();
                Debug.LogFormat(
                    "GeoX UI Input System detected Quest/VR runtime (device='{0}', xrDevice='{1}', bootstrapper={2}, activeVariant={3}, platformRoot='{4}').",
                    SystemInfo.deviceModel,
                    XRSettings.loadedDeviceName,
                    bootstrapper != null,
                    bootstrapper != null ? bootstrapper.ActiveVariant.ToString() : "n/a",
                    platformRoot != null ? platformRoot.name : "none");
                loggedQuestDetection = true;
            }

            bool resolvedCamera = RunQuestStep("EnsureQuestMainCamera", EnsureQuestMainCamera);
            Camera mainCamera = GetQuestPreferredCamera();
            if (mainCamera == null)
            {
                if (!loggedVrRuntimeConfiguration)
                {
                    Debug.LogWarning("GeoX UI Input System could not find a Quest camera; UI cannot be placed yet.");
                }

                return;
            }

            // Visibility first: Overlay canvases are invisible in VR, so converting the
            // Quest canvases to World Space is what actually makes the UI appear. Do this
            // before the input/pose wiring so a failure there can never hide the UI.
            int changedCanvases = RunQuestStep("ConfigureQuestCanvases", () => ConfigureQuestCanvases(mainCamera));
            bool ensuredQuestUi = RunQuestStep("EnsureQuestPlatformUiVisible", EnsureQuestPlatformUiVisible);

            // Only hide the scene MRTK GameUI after Quest/platform canvases actually exist.
            // Otherwise we black-screen (disable GameUI with nothing to replace it).
            bool disabledSceneUi = false;
            if (FindQuestPlatformCanvasObject("LoaderCanvas") != null ||
                FindQuestPlatformCanvasObject("LobbyCanvas") != null)
            {
                disabledSceneUi = RunQuestStep("DisableSceneMrtkUiForQuest", DisableSceneMrtkUiForQuest);
            }

            bool changedLoginLayout = RunQuestStep("EnsureQuestLobbyLoginLayout", EnsureQuestLobbyLoginLayout);
            bool preparedInputFields = RunQuestStep("PrepareQuestInputFields", PrepareQuestInputFields);
            bool changedContrast = RunQuestStep("EnsureQuestUiContrast", EnsureQuestUiContrast);

            // Input wiring last; these are the steps most likely to throw on odd bindings.
            bool disabledGaze = RunQuestStep("DisableGazePointersForQuest", DisableGazePointersForQuest);
            bool changedCamera = RunQuestStep("EnsureInputSystemTrackedPoseDriver", () => EnsureInputSystemTrackedPoseDriver(mainCamera)) || resolvedCamera;
            bool changedUiActions = RunQuestStep("EnsureQuestXrUiInputActions", EnsureQuestXrUiInputActions);

            bool hasVisibleQuestUi =
                FindQuestPlatformCanvasObject("LoaderCanvas") != null ||
                FindQuestPlatformCanvasObject("LobbyCanvas") != null;

            if (hasVisibleQuestUi)
            {
                if (!loggedVrRuntimeConfiguration)
                {
                    Debug.LogFormat(
                        "GeoX UI Input System configured Quest UI (actions={0}, canvases={1}, contrast={2}, loginLayout={3}, sceneMrtkUiDisabled={4}, gazeDisabled={5}, questUiEnsured={6}).",
                        changedUiActions,
                        changedCanvases,
                        changedContrast,
                        changedLoginLayout,
                        disabledSceneUi,
                        disabledGaze,
                        ensuredQuestUi);
                    loggedVrRuntimeConfiguration = true;
                }
            }
            else if (!loggedMissingPlatformRoot)
            {
                // One-time diagnostic: dump scene roots so we can see what actually
                // spawned when no LoaderCanvas/LobbyCanvas is present.
                loggedMissingPlatformRoot = true;
                DumpSceneRootsForDiagnostics();
            }
        }

        private static void DumpSceneRootsForDiagnostics()
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.Append("GeoX UI Input System found no Quest LoaderCanvas/LobbyCanvas. Scene roots: ");

            var seen = new HashSet<string>();
            foreach (Transform transform in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (transform == null ||
                    transform.parent != null ||
                    !transform.gameObject.scene.IsValid())
                {
                    continue;
                }

                string entry = transform.name + "(active=" + transform.gameObject.activeInHierarchy + ")";
                if (seen.Add(entry))
                {
                    builder.Append(entry).Append("; ");
                }
            }

            builder.Append(" | Canvases: ");
            foreach (Canvas canvas in Resources.FindObjectsOfTypeAll<Canvas>())
            {
                if (canvas == null || !canvas.gameObject.scene.IsValid())
                {
                    continue;
                }

                builder.Append(canvas.gameObject.name).Append("; ");
            }

            Debug.LogWarning(builder.ToString());
        }

        private bool RunQuestStep(string stepName, System.Func<bool> step)
        {
            try
            {
                return step();
            }
            catch (System.Exception exception)
            {
                if (!loggedVrRuntimeConfiguration)
                {
                    Debug.LogErrorFormat("GeoX UI Input System step '{0}' failed: {1}", stepName, exception);
                }

                return false;
            }
        }

        private int RunQuestStep(string stepName, System.Func<int> step)
        {
            try
            {
                return step();
            }
            catch (System.Exception exception)
            {
                if (!loggedVrRuntimeConfiguration)
                {
                    Debug.LogErrorFormat("GeoX UI Input System step '{0}' failed: {1}", stepName, exception);
                }

                return 0;
            }
        }

        /// <summary>
        /// Quest uses PlatformRoot.Quest3 uGUI (LoaderCanvas / LobbyCanvas). The scene
        /// GameUI tree is the HoloLens/MRTK login path and must stay off on Quest so the
        /// two lobbies do not overlap.
        /// </summary>
        private bool DisableSceneMrtkUiForQuest()
        {
            if (disabledHoloLensUi)
            {
                return false;
            }

            bool changed = false;

            foreach (Transform transform in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (transform == null ||
                    !transform.gameObject.scene.IsValid() ||
                    transform.parent != null)
                {
                    continue;
                }

                if (transform.name == "GameUI" && transform.gameObject.activeSelf)
                {
                    transform.gameObject.SetActive(false);
                    changed = true;
                }
            }

            // Belt-and-suspenders: any leftover MRTK canvases not under the Quest prefab.
            foreach (Transform transform in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (transform == null || !transform.gameObject.scene.IsValid())
                {
                    continue;
                }

                string name = transform.name;
                if (name != "TagalongToggleSwitch" &&
                    name != "LobbyMRTKCanvas" &&
                    name != "MenuMRTKCanvas" &&
                    name != "RoomMRTKCanvas")
                {
                    continue;
                }

                if (IsUnderQuestPlatformRoot(transform) || !transform.gameObject.activeSelf)
                {
                    continue;
                }

                transform.gameObject.SetActive(false);
                changed = true;
            }

            disabledHoloLensUi = true;
            if (changed)
            {
                Debug.Log("GeoX UI Input System disabled scene MRTK GameUI for Quest (using PlatformRoot.Quest3 canvases).");
            }

            return changed;
        }

        /// <summary>
        /// GeoXShared and PlatformRoot.Quest3 both ship a MainCamera. On Quest the scene
        /// camera is not XR-tracked, so UI anchored to Camera.main can end up invisible
        /// while the headset still shows controller rays.
        /// </summary>
        private bool EnsureQuestMainCamera()
        {
            Camera questCamera = null;
            foreach (Camera camera in Resources.FindObjectsOfTypeAll<Camera>())
            {
                if (camera == null || !camera.gameObject.scene.IsValid())
                {
                    continue;
                }

                if (IsUnderQuestPlatformRoot(camera.transform))
                {
                    questCamera = camera;
                    break;
                }
            }

            if (questCamera == null)
            {
                return false;
            }

            bool changed = false;
            if (!questCamera.gameObject.activeSelf)
            {
                questCamera.gameObject.SetActive(true);
                changed = true;
            }

            if (!questCamera.enabled)
            {
                questCamera.enabled = true;
                changed = true;
            }

            if (!questCamera.CompareTag("MainCamera"))
            {
                questCamera.tag = "MainCamera";
                changed = true;
            }

            foreach (Camera camera in Resources.FindObjectsOfTypeAll<Camera>())
            {
                if (camera == null ||
                    !camera.gameObject.scene.IsValid() ||
                    camera == questCamera ||
                    IsUnderQuestPlatformRoot(camera.transform))
                {
                    continue;
                }

                // Leave non-quest cameras enabled only if they are not competing for MainCamera.
                if (camera.CompareTag("MainCamera"))
                {
                    camera.tag = "Untagged";
                    changed = true;
                }

                if (camera.enabled)
                {
                    camera.enabled = false;
                    changed = true;
                }
            }

            return changed;
        }

        private static Camera GetQuestPreferredCamera()
        {
            foreach (Camera camera in Resources.FindObjectsOfTypeAll<Camera>())
            {
                if (camera == null ||
                    !camera.gameObject.scene.IsValid() ||
                    !camera.isActiveAndEnabled)
                {
                    continue;
                }

                if (IsUnderQuestPlatformRoot(camera.transform))
                {
                    return camera;
                }
            }

            return Camera.main;
        }

        /// <summary>
        /// After hiding GameUI, make sure the Quest prefab start flow is actually on:
        /// LoaderCanvas (Get Started) until LobbyCanvas takes over.
        /// </summary>
        private bool EnsureQuestPlatformUiVisible()
        {
            GameObject loaderCanvas = FindQuestPlatformCanvasObject("LoaderCanvas");
            GameObject lobbyCanvas = FindQuestPlatformCanvasObject("LobbyCanvas");

            bool lobbyActive = lobbyCanvas != null && lobbyCanvas.activeInHierarchy;
            bool loaderActive = loaderCanvas != null && loaderCanvas.activeInHierarchy;

            if (lobbyActive || loaderActive)
            {
                return false;
            }

            if (loaderCanvas != null)
            {
                loaderCanvas.SetActive(true);
                Debug.Log("GeoX UI Input System activated Quest LoaderCanvas (Get Started).");
                return true;
            }

            if (lobbyCanvas != null)
            {
                lobbyCanvas.SetActive(true);
                Debug.Log("GeoX UI Input System activated Quest LobbyCanvas.");
                return true;
            }

            return false;
        }

        private static GameObject FindQuestPlatformCanvasObject(string canvasName)
        {
            foreach (Canvas canvas in Resources.FindObjectsOfTypeAll<Canvas>())
            {
                if (canvas == null ||
                    !canvas.gameObject.scene.IsValid() ||
                    canvas.gameObject.name != canvasName)
                {
                    continue;
                }

                if (IsQuestPlatformCanvas(canvas))
                {
                    return canvas.gameObject;
                }
            }

            return null;
        }

        private static bool IsUnderQuestPlatformRoot(Transform transform)
        {
            Transform platformRoot = GetPlatformRootTransform();
            if (platformRoot != null && IsDescendantOf(transform, platformRoot))
            {
                return true;
            }

            Transform current = transform;
            while (current != null)
            {
                if (current.name == "PlatformRoot.Quest3" ||
                    current.name == "PlatformRoot.Mobile")
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static bool IsDescendantOf(Transform candidate, Transform ancestor)
        {
            Transform current = candidate;
            while (current != null)
            {
                if (current == ancestor)
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private bool DisableGazePointersForQuest()
        {
            if (disabledGazePointers)
            {
                return false;
            }

            bool changed = false;
            foreach (GazeProvider gazeProvider in Resources.FindObjectsOfTypeAll<GazeProvider>())
            {
                if (gazeProvider == null || !gazeProvider.gameObject.scene.IsValid())
                {
                    continue;
                }

                if (gazeProvider.enabled)
                {
                    gazeProvider.enabled = false;
                    changed = true;
                }
            }

            foreach (EventSystem eventSystem in Object.FindObjectsOfType<EventSystem>())
            {
                foreach (BaseInputModule module in eventSystem.GetComponents<BaseInputModule>())
                {
                    if (module == null)
                    {
                        continue;
                    }

                    // Keep InputSystemUIInputModule; disable MRTK/legacy modules that drive gaze UI.
                    if (module is InputSystemUIInputModule)
                    {
                        continue;
                    }

                    if (module.enabled)
                    {
                        module.enabled = false;
                        changed = true;
                    }
                }
            }

            disabledGazePointers = true;
            return changed;
        }

        private bool EnsureQuestXrUiInputActions()
        {
            if (configuredXrUiActions && questUiActions != null)
            {
                // Do not re-assign actions every frame — that resets press state and breaks clicks.
                return false;
            }

            if (questUiActions == null)
            {
                questUiActions = ScriptableObject.CreateInstance<InputActionAsset>();
                questUiActions.name = "GeoXQuestUIActions";

                InputActionMap map = questUiActions.AddActionMap("UI");

                InputAction point = map.AddAction("Point", type: InputActionType.PassThrough, expectedControlLayout: "Vector2");
                point.AddBinding("<Mouse>/position");
                point.AddBinding("<Pen>/position");
                point.AddBinding("<Touchscreen>/touch*/position");

                // Mouse/touch clicks stay on the UI module. Quest XR clicks are handled in
                // UpdateQuestUiTriggerClick so we never lose trigger edges or double-fire.
                InputAction click = map.AddAction("Click", type: InputActionType.PassThrough, expectedControlLayout: "Button");
                click.AddBinding("<Mouse>/leftButton");
                click.AddBinding("<Pen>/tip");
                click.AddBinding("<Touchscreen>/touch*/press");

                InputAction submit = map.AddAction("Submit", type: InputActionType.Button, expectedControlLayout: "Button");
                submit.AddBinding("*/{Submit}");
                submit.AddBinding("<Keyboard>/enter");
                submit.AddBinding("<Keyboard>/numpadEnter");

                InputAction cancel = map.AddAction("Cancel", type: InputActionType.Button, expectedControlLayout: "Button");
                cancel.AddBinding("*/{Cancel}");
                cancel.AddBinding("<Keyboard>/escape");

                InputAction navigate = map.AddAction("Navigate", type: InputActionType.PassThrough, expectedControlLayout: "Vector2");
                navigate.AddCompositeBinding("2DVector")
                    .With("Up", "<Keyboard>/w")
                    .With("Down", "<Keyboard>/s")
                    .With("Left", "<Keyboard>/a")
                    .With("Right", "<Keyboard>/d");

                // Prefer OpenXR aim/pointer pose so the ray matches the physical controller pointer.
                InputAction trackedPosition = map.AddAction(
                    "TrackedDevicePosition",
                    type: InputActionType.PassThrough,
                    expectedControlLayout: "Vector3");
                trackedPosition.AddBinding("<XRController>{RightHand}/pointerPosition");
                trackedPosition.AddBinding("<XRController>{LeftHand}/pointerPosition");
                trackedPosition.AddBinding("<XRController>{RightHand}/devicePosition");
                trackedPosition.AddBinding("<XRController>{LeftHand}/devicePosition");

                InputAction trackedOrientation = map.AddAction(
                    "TrackedDeviceOrientation",
                    type: InputActionType.PassThrough,
                    expectedControlLayout: "Quaternion");
                trackedOrientation.AddBinding("<XRController>{RightHand}/pointerRotation");
                trackedOrientation.AddBinding("<XRController>{LeftHand}/pointerRotation");
                trackedOrientation.AddBinding("<XRController>{RightHand}/deviceRotation");
                trackedOrientation.AddBinding("<XRController>{LeftHand}/deviceRotation");

                questUiActions.Enable();
            }

            ApplyQuestUiActionsToEventSystems();
            configuredXrUiActions = true;
            return true;
        }

        private void ApplyQuestUiActionsToEventSystems()
        {
            if (questUiActions == null)
            {
                return;
            }

            InputActionMap map = questUiActions.FindActionMap("UI", throwIfNotFound: false);
            if (map == null)
            {
                return;
            }

            foreach (EventSystem eventSystem in Object.FindObjectsOfType<EventSystem>())
            {
                InputSystemUIInputModule uiInputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
                if (uiInputModule == null)
                {
                    uiInputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                }

                uiInputModule.actionsAsset = questUiActions;
                uiInputModule.point = InputActionReference.Create(map.FindAction("Point"));
                uiInputModule.leftClick = InputActionReference.Create(map.FindAction("Click"));
                uiInputModule.submit = InputActionReference.Create(map.FindAction("Submit"));
                uiInputModule.cancel = InputActionReference.Create(map.FindAction("Cancel"));
                uiInputModule.move = InputActionReference.Create(map.FindAction("Navigate"));
                uiInputModule.trackedDevicePosition = InputActionReference.Create(map.FindAction("TrackedDevicePosition"));
                uiInputModule.trackedDeviceOrientation = InputActionReference.Create(map.FindAction("TrackedDeviceOrientation"));
                uiInputModule.pointerBehavior = UIPointerBehavior.SingleMouseOrPenButMultiTouchAndTrack;
                Camera preferredCamera = GetQuestPreferredCamera();
                if (preferredCamera != null)
                {
                    uiInputModule.xrTrackingOrigin = preferredCamera.transform;
                }

                uiInputModule.enabled = true;
            }
        }

        private static bool EnsureInputSystemTrackedPoseDriver(Camera camera)
        {
            TrackedPoseDriver trackedPoseDriver = camera.GetComponent<TrackedPoseDriver>();
            bool addedDriver = false;
            if (trackedPoseDriver == null)
            {
                trackedPoseDriver = camera.gameObject.AddComponent<TrackedPoseDriver>();
                addedDriver = true;
            }

            trackedPoseDriver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            trackedPoseDriver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;

            if (trackedPoseDriver.positionInput.action == null)
            {
                InputAction positionAction = new InputAction(
                    "Position",
                    binding: "<XRHMD>/centerEyePosition",
                    expectedControlType: "Vector3");
                positionAction.AddBinding("<HandheldARInputDevice>/devicePosition");
                trackedPoseDriver.positionInput = new InputActionProperty(positionAction);
                addedDriver = true;
            }

            if (trackedPoseDriver.rotationInput.action == null)
            {
                InputAction rotationAction = new InputAction(
                    "Rotation",
                    binding: "<XRHMD>/centerEyeRotation",
                    expectedControlType: "Quaternion");
                rotationAction.AddBinding("<HandheldARInputDevice>/deviceRotation");
                trackedPoseDriver.rotationInput = new InputActionProperty(rotationAction);
                addedDriver = true;
            }

            return addedDriver;
        }

        private int ConfigureQuestCanvases(Camera mainCamera)
        {
            EnsureQuestUiAnchor(mainCamera);

            int changedCanvases = 0;
            foreach (Canvas canvas in Resources.FindObjectsOfTypeAll<Canvas>())
            {
                if (canvas == null ||
                    !canvas.gameObject.scene.IsValid() ||
                    !canvas.isRootCanvas ||
                    !IsQuestPlatformCanvas(canvas))
                {
                    continue;
                }

                bool changed = false;
                if (canvas.renderMode != RenderMode.WorldSpace)
                {
                    canvas.renderMode = RenderMode.WorldSpace;
                    changed = true;
                }

                if (canvas.worldCamera != mainCamera)
                {
                    canvas.worldCamera = mainCamera;
                    changed = true;
                }

                if (EnsureTrackedDeviceRaycaster(canvas))
                {
                    changed = true;
                }

                if (EnsureGraphicRaycaster(canvas))
                {
                    changed = true;
                }

                if (PrepareQuestCanvasTransform(canvas))
                {
                    changed = true;
                }

                if (questUiAnchor != null && canvas.transform.parent != questUiAnchor)
                {
                    // Resources.FindObjectsOfTypeAll can return prefab assets; never parent those.
                    if (!canvas.gameObject.scene.IsValid())
                    {
                        continue;
                    }

                    canvas.transform.SetParent(questUiAnchor, false);
                    canvas.transform.localPosition = Vector3.zero;
                    canvas.transform.localRotation = Quaternion.identity;
                    changed = true;
                }

                if (changed)
                {
                    changedCanvases++;
                }
            }

            return changedCanvases;
        }

        private static bool IsQuestPlatformCanvas(Canvas canvas)
        {
            if (canvas == null)
            {
                return false;
            }

            // Never treat the HoloLens/MRTK scene canvases as Quest UI.
            string name = canvas.gameObject.name;
            if (name == "LobbyMRTKCanvas" ||
                name == "MenuMRTKCanvas" ||
                name == "RoomMRTKCanvas")
            {
                return false;
            }

            // A canvas already reparented under our anchor is Quest UI.
            Transform anchorScan = canvas.transform;
            while (anchorScan != null)
            {
                if (anchorScan.name == "GeoX Quest UI Anchor")
                {
                    return true;
                }

                if (anchorScan.name == "GameUI")
                {
                    return false;
                }

                anchorScan = anchorScan.parent;
            }

            // Authoritative: descendant of the spawned platform root.
            Transform platformRoot = GetPlatformRootTransform();
            if (platformRoot != null && IsDescendantOf(canvas.transform, platformRoot))
            {
                return true;
            }

            for (int i = 0; i < QuestPlatformCanvasNames.Length; i++)
            {
                if (name == QuestPlatformCanvasNames[i])
                {
                    return true;
                }
            }

            return false;
        }

        private void EnsureQuestUiAnchor(Camera mainCamera)
        {
            if (questUiAnchor != null)
            {
                return;
            }

            GameObject anchorObject = new GameObject("GeoX Quest UI Anchor");
            questUiAnchor = anchorObject.transform;
            questUiAnchor.SetParent(transform, false);
            PlaceAnchorInFrontOfCamera(mainCamera, immediate: true);
        }

        private void UpdateQuestUiAnchor()
        {
            if (!IsQuestOrVrRuntime() || questUiAnchor == null)
            {
                return;
            }

            Camera mainCamera = GetQuestPreferredCamera();
            if (mainCamera == null)
            {
                return;
            }

            PlaceAnchorInFrontOfCamera(mainCamera, immediate: false);
        }

        private void PlaceAnchorInFrontOfCamera(Camera mainCamera, bool immediate)
        {
            Vector3 targetPosition = mainCamera.transform.position + mainCamera.transform.forward * VrCanvasDistance;
            Vector3 toCamera = mainCamera.transform.position - targetPosition;
            if (toCamera.sqrMagnitude < 0.0001f)
            {
                toCamera = -mainCamera.transform.forward;
            }

            Quaternion targetRotation = Quaternion.LookRotation(-toCamera.normalized, Vector3.up);
            if (immediate)
            {
                questUiAnchor.position = targetPosition;
                questUiAnchor.rotation = targetRotation;
                return;
            }

            questUiAnchor.position = Vector3.Lerp(questUiAnchor.position, targetPosition, Time.deltaTime * 3f);
            questUiAnchor.rotation = Quaternion.Slerp(questUiAnchor.rotation, targetRotation, Time.deltaTime * 3f);
        }

        private static bool PrepareQuestCanvasTransform(Canvas canvas)
        {
            RectTransform rectTransform = canvas.transform as RectTransform;
            if (rectTransform == null)
            {
                return false;
            }

            bool changed = false;
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            // Landscape-friendly Quest panel; phone portrait reference pushed controls off the usable view.
            Vector2 reference = new Vector2(900f, 700f);

            if (scaler != null && scaler.enabled)
            {
                scaler.enabled = false;
                changed = true;
            }

            if (rectTransform.anchorMin != new Vector2(0.5f, 0.5f) ||
                rectTransform.anchorMax != new Vector2(0.5f, 0.5f))
            {
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                changed = true;
            }

            if (rectTransform.pivot != new Vector2(0.5f, 0.5f))
            {
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                changed = true;
            }

            if (rectTransform.sizeDelta != reference)
            {
                rectTransform.sizeDelta = reference;
                changed = true;
            }

            float scale = VrCanvasHeightMeters / reference.y;
            Vector3 targetScale = new Vector3(scale, scale, scale);
            if ((rectTransform.localScale - targetScale).sqrMagnitude > 0.0000001f)
            {
                rectTransform.localScale = targetScale;
                changed = true;
            }

            if (rectTransform.localPosition.sqrMagnitude > 0.0001f)
            {
                rectTransform.localPosition = Vector3.zero;
                changed = true;
            }

            if (rectTransform.localRotation != Quaternion.identity)
            {
                rectTransform.localRotation = Quaternion.identity;
                changed = true;
            }

            return changed;
        }

        private bool EnsureQuestLobbyLoginLayout()
        {
            Canvas lobbyCanvas = null;
            foreach (Canvas canvas in Resources.FindObjectsOfTypeAll<Canvas>())
            {
                if (canvas == null ||
                    !canvas.gameObject.scene.IsValid() ||
                    canvas.gameObject.name != "LobbyCanvas" ||
                    !IsQuestPlatformCanvas(canvas))
                {
                    continue;
                }

                lobbyCanvas = canvas;
                break;
            }

            if (lobbyCanvas == null || !lobbyCanvas.gameObject.activeInHierarchy)
            {
                laidOutQuestLobbyLogin = false;
                lastLobbyCanvas = null;
                return false;
            }

            if (laidOutQuestLobbyLogin && lastLobbyCanvas == lobbyCanvas)
            {
                return false;
            }

            // Soft panel background should not steal raycasts from controls.
            foreach (Image image in lobbyCanvas.GetComponentsInChildren<Image>(true))
            {
                if (image.gameObject.name == "Panel")
                {
                    image.raycastTarget = false;
                    image.color = new Color(0.08f, 0.1f, 0.14f, 0.82f);
                }
            }

            // Keep post-login room controls hidden until LobbyManager shows them.
            HideQuestLobbyRoomControls(lobbyCanvas);

            TMP_Text connectionStatus = null;
            TMP_InputField usernameField = null;
            Button loginButton = null;

            foreach (TMP_Text text in lobbyCanvas.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.name.StartsWith("ConnectionStatus"))
                {
                    connectionStatus = text;
                    break;
                }
            }

            foreach (TMP_InputField field in lobbyCanvas.GetComponentsInChildren<TMP_InputField>(true))
            {
                if (field.name.Contains("Username"))
                {
                    usernameField = field;
                    break;
                }
            }

            foreach (Button button in lobbyCanvas.GetComponentsInChildren<Button>(true))
            {
                if (button.name == "LoginButton")
                {
                    loginButton = button;
                    break;
                }
            }

            float y = 180f;
            if (connectionStatus != null)
            {
                connectionStatus.gameObject.SetActive(true);
                connectionStatus.color = Color.white;
                connectionStatus.fontSize = 36f;
                PlaceQuestLoginControl(connectionStatus.rectTransform, 0f, y, 760f, 48f);
                y -= 70f;
            }

            TMP_Text usernameLabel = EnsureQuestUsernameLabel(lobbyCanvas.transform, usernameField);
            if (usernameLabel != null)
            {
                PlaceQuestLoginControl(usernameLabel.rectTransform, 0f, y, 520f, 36f);
                y -= 44f;
            }

            if (usernameField != null)
            {
                usernameField.gameObject.SetActive(true);
                PrepareQuestTmpInputField(usernameField);
                PlaceQuestLoginControl(usernameField.transform as RectTransform, 0f, y, 520f, 64f);

                Image fieldImage = usernameField.targetGraphic as Image;
                if (fieldImage != null)
                {
                    fieldImage.color = Color.white;
                    fieldImage.raycastTarget = true;
                }

                if (usernameField.textComponent != null)
                {
                    usernameField.textComponent.color = new Color(0.1f, 0.1f, 0.1f, 1f);
                    usernameField.textComponent.fontSize = 30f;
                }

                if (usernameField.placeholder is TMP_Text placeholder)
                {
                    placeholder.color = new Color(0.25f, 0.25f, 0.25f, 0.85f);
                    placeholder.fontSize = 28f;
                    placeholder.raycastTarget = false;
                    if (string.IsNullOrWhiteSpace(placeholder.text) || placeholder.text == "\u200B")
                    {
                        placeholder.text = "Enter username...";
                    }
                }

                y -= 90f;
            }

            if (loginButton != null)
            {
                loginButton.gameObject.SetActive(true);
                loginButton.interactable = true;
                PlaceQuestLoginControl(loginButton.transform as RectTransform, 0f, y, 280f, 72f);

                Image loginImage = loginButton.targetGraphic as Image;
                if (loginImage != null)
                {
                    loginImage.color = new Color(0.12f, 0.14f, 0.18f, 0.96f);
                    loginImage.raycastTarget = true;
                }

                foreach (TMP_Text label in loginButton.GetComponentsInChildren<TMP_Text>(true))
                {
                    label.color = Color.white;
                    label.fontSize = 34f;
                }
            }

            laidOutQuestLobbyLogin = true;
            lastLobbyCanvas = lobbyCanvas;
            Debug.Log("GeoX UI Input System laid out Quest LobbyCanvas login controls.");
            return true;
        }

        private static void HideQuestLobbyRoomControls(Canvas lobbyCanvas)
        {
            if (lobbyCanvas == null)
            {
                return;
            }

            string[] hideNames =
            {
                "CreateNewRoomButton",
                "JoinRoomButton",
                "RoomNameInputField (TMP)",
                "CreateRoomButton",
                "StartGameButton"
            };

            foreach (Transform child in lobbyCanvas.GetComponentsInChildren<Transform>(true))
            {
                if (child == null)
                {
                    continue;
                }

                for (int i = 0; i < hideNames.Length; i++)
                {
                    if (child.name == hideNames[i])
                    {
                        child.gameObject.SetActive(false);
                        break;
                    }
                }
            }
        }

        private static TMP_Text EnsureQuestUsernameLabel(Transform lobbyCanvasTransform, TMP_InputField usernameField)
        {
            Transform existing = lobbyCanvasTransform.Find("UsernameLabel (TMP)");
            if (existing == null && usernameField != null && usernameField.transform.parent != null)
            {
                existing = usernameField.transform.parent.Find("UsernameLabel (TMP)");
            }

            if (existing != null)
            {
                TMP_Text existingText = existing.GetComponent<TMP_Text>();
                if (existingText != null)
                {
                    existingText.gameObject.SetActive(true);
                    existingText.text = "Username";
                    existingText.color = Color.white;
                    return existingText;
                }
            }

            Transform parent = usernameField != null ? usernameField.transform.parent : lobbyCanvasTransform;
            GameObject labelObject = new GameObject("UsernameLabel (TMP)", typeof(RectTransform));
            labelObject.transform.SetParent(parent, false);
            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            label.text = "Username";
            label.color = Color.white;
            label.fontSize = 32f;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.raycastTarget = false;
            return label;
        }

        private static void PlaceQuestLoginControl(RectTransform rectTransform, float x, float y, float width, float height)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.localScale = Vector3.one;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(x, y);
            rectTransform.sizeDelta = new Vector2(width, height);
            rectTransform.localRotation = Quaternion.identity;
        }

        private void UpdateQuestUiTriggerClick()
        {
            if (!IsQuestOrVrRuntime())
            {
                return;
            }

            if (!WasRightTriggerPressedThisFrame())
            {
                return;
            }

            Camera mainCamera = GetQuestPreferredCamera();
            EventSystem eventSystem = EventSystem.current ?? Object.FindObjectOfType<EventSystem>();
            if (mainCamera == null || eventSystem == null)
            {
                return;
            }

            Vector3 origin;
            Vector3 direction;
            if (!TryGetRightHandAimPose(out origin, out direction))
            {
                return;
            }

            Vector3 hitPoint = origin + direction * 2f;
            if (questUiAnchor != null)
            {
                Plane uiPlane = new Plane(questUiAnchor.forward, questUiAnchor.position);
                float enter;
                if (uiPlane.Raycast(new Ray(origin, direction), out enter) && enter > 0f)
                {
                    hitPoint = origin + direction * enter;
                }
            }

            Vector2 screenPosition = mainCamera.WorldToScreenPoint(hitPoint);
            PointerEventData pointerData = new PointerEventData(eventSystem)
            {
                position = screenPosition,
                button = PointerEventData.InputButton.Left
            };

            raycastResults.Clear();
            eventSystem.RaycastAll(pointerData, raycastResults);
            foreach (RaycastResult result in raycastResults)
            {
                if (result.gameObject == null)
                {
                    continue;
                }

                if (TryFocusInputField(result.gameObject, eventSystem, pointerData))
                {
                    return;
                }

                if (TrySubmitControl(result.gameObject, eventSystem, pointerData))
                {
                    return;
                }

                GameObject clickHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(result.gameObject);
                if (clickHandler != null &&
                    ExecuteEvents.Execute(clickHandler, pointerData, ExecuteEvents.pointerClickHandler))
                {
                    return;
                }
            }

            // Fallback: GraphicRaycaster can miss world-space TMP fields; hit-test by controller ray.
            if (TryFocusQuestInputFieldByWorldRay(origin, direction, eventSystem, pointerData))
            {
                return;
            }
        }

        private bool TryFocusQuestInputFieldByWorldRay(
            Vector3 origin,
            Vector3 direction,
            EventSystem eventSystem,
            PointerEventData pointerData)
        {
            Camera mainCamera = GetQuestPreferredCamera();
            if (mainCamera == null)
            {
                return false;
            }

            TMP_InputField bestField = null;
            float bestDistance = float.MaxValue;
            Ray ray = new Ray(origin, direction);

            foreach (TMP_InputField field in Object.FindObjectsOfType<TMP_InputField>())
            {
                if (field == null ||
                    !field.isActiveAndEnabled ||
                    !field.interactable ||
                    !IsUnderQuestPlatform(field.transform))
                {
                    continue;
                }

                RectTransform rectTransform = field.transform as RectTransform;
                if (rectTransform == null)
                {
                    continue;
                }

                float distance;
                if (!TryRaycastRectTransform(ray, rectTransform, mainCamera, out distance))
                {
                    continue;
                }

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestField = field;
                }
            }

            return bestField != null &&
                   TryFocusInputField(bestField.gameObject, eventSystem, pointerData);
        }

        private static bool TryRaycastRectTransform(
            Ray ray,
            RectTransform rectTransform,
            Camera camera,
            out float distance)
        {
            distance = 0f;
            if (rectTransform == null)
            {
                return false;
            }

            Plane plane = new Plane(rectTransform.forward, rectTransform.position);
            float enter;
            if (!plane.Raycast(ray, out enter) || enter <= 0f)
            {
                return false;
            }

            Vector3 worldPoint = ray.GetPoint(enter);
            if (!RectTransformUtility.RectangleContainsScreenPoint(
                    rectTransform,
                    camera.WorldToScreenPoint(worldPoint),
                    camera))
            {
                return false;
            }

            distance = enter;
            return true;
        }

        private static bool WasRightTriggerPressedThisFrame()
        {
            XRController right = XRController.rightHand;
            if (right == null)
            {
                return false;
            }

            ButtonControl triggerPressed = right.TryGetChildControl<ButtonControl>("triggerPressed");
            if (triggerPressed != null && triggerPressed.wasPressedThisFrame)
            {
                return true;
            }

            ButtonControl triggerButton = right.TryGetChildControl<ButtonControl>("triggerButton");
            if (triggerButton != null && triggerButton.wasPressedThisFrame)
            {
                return true;
            }

            AxisControl trigger = right.TryGetChildControl<AxisControl>("trigger");
            if (trigger != null)
            {
                const float pressPoint = 0.5f;
                if (trigger.ReadValue() >= pressPoint &&
                    trigger.ReadValueFromPreviousFrame() < pressPoint)
                {
                    return true;
                }
            }

            ButtonControl primaryButton = right.TryGetChildControl<ButtonControl>("primaryButton");
            return primaryButton != null && primaryButton.wasPressedThisFrame;
        }

        private static bool EnsureTrackedDeviceRaycaster(Canvas canvas)
        {
            TrackedDeviceRaycaster trackedRaycaster = canvas.GetComponent<TrackedDeviceRaycaster>();
            if (trackedRaycaster != null)
            {
                trackedRaycaster.enabled = true;
                trackedRaycaster.ignoreReversedGraphics = true;
                trackedRaycaster.checkFor3DOcclusion = false;
                trackedRaycaster.maxDistance = 20f;
                return false;
            }

            trackedRaycaster = canvas.gameObject.AddComponent<TrackedDeviceRaycaster>();
            trackedRaycaster.ignoreReversedGraphics = true;
            trackedRaycaster.checkFor3DOcclusion = false;
            trackedRaycaster.maxDistance = 20f;
            return true;
        }

        private static bool EnsureGraphicRaycaster(Canvas canvas)
        {
            GraphicRaycaster graphicRaycaster = canvas.GetComponent<GraphicRaycaster>();
            if (graphicRaycaster != null)
            {
                graphicRaycaster.enabled = true;
                return false;
            }

            canvas.gameObject.AddComponent<GraphicRaycaster>();
            return true;
        }

        private bool EnsureQuestUiContrast()
        {
            if (fixedQuestUiContrast)
            {
                return false;
            }

            bool changed = false;
            foreach (Button button in Resources.FindObjectsOfTypeAll<Button>())
            {
                if (button == null || !button.gameObject.scene.IsValid() || !IsUnderQuestPlatform(button.transform))
                {
                    continue;
                }

                Image image = button.targetGraphic as Image;
                if (image != null)
                {
                    // Solid dark button background so white or dark labels remain readable.
                    Color background = image.color;
                    if (background.a < 0.8f || background.maxColorComponent > 0.85f)
                    {
                        image.color = new Color(0.12f, 0.14f, 0.18f, 0.94f);
                        changed = true;
                    }
                }

                foreach (TMP_Text label in button.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (label.color.r > 0.85f && label.color.g > 0.85f && label.color.b > 0.85f)
                    {
                        // Keep light text on the dark button we just enforced.
                        label.color = Color.white;
                    }
                    else if (label.color.maxColorComponent < 0.35f && image != null && image.color.maxColorComponent < 0.35f)
                    {
                        label.color = Color.white;
                        changed = true;
                    }
                }

                if (button.name == "LoginButton")
                {
                    foreach (TMP_Text label in button.GetComponentsInChildren<TMP_Text>(true))
                    {
                        label.color = Color.white;
                        changed = true;
                    }
                }
            }

            foreach (TMP_InputField inputField in Resources.FindObjectsOfTypeAll<TMP_InputField>())
            {
                if (inputField == null || !inputField.gameObject.scene.IsValid() || !IsUnderQuestPlatform(inputField.transform))
                {
                    continue;
                }

                // Do NOT force SetActive(true) — that re-shows RoomNameInputField / other
                // post-login fields on the login screen.
                if (!inputField.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Image image = inputField.targetGraphic as Image;
                if (image != null)
                {
                    image.color = Color.white;
                }

                if (inputField.textComponent != null)
                {
                    inputField.textComponent.color = new Color(0.12f, 0.12f, 0.12f, 1f);
                }

                if (inputField.placeholder is TMP_Text placeholder)
                {
                    placeholder.color = new Color(0.25f, 0.25f, 0.25f, 0.75f);
                }

                changed = true;
            }

            fixedQuestUiContrast = true;
            return changed;
        }

        private static bool IsUnderQuestPlatform(Transform transform)
        {
            Transform platformRoot = GetPlatformRootTransform();
            if (platformRoot != null && IsDescendantOf(transform, platformRoot))
            {
                return true;
            }

            while (transform != null)
            {
                if (transform.name == "PlatformRoot.Quest3" ||
                    transform.name == "PlatformRoot.Mobile" ||
                    transform.name == "GeoX Quest UI Anchor")
                {
                    return true;
                }

                for (int i = 0; i < QuestPlatformCanvasNames.Length; i++)
                {
                    if (transform.name == QuestPlatformCanvasNames[i])
                    {
                        return true;
                    }
                }

                transform = transform.parent;
            }

            return false;
        }

        private void UpdateRightHandRay()
        {
            if (!IsQuestOrVrRuntime())
            {
                if (rightHandRay != null)
                {
                    rightHandRay.enabled = false;
                }

                return;
            }

            Vector3 origin;
            Vector3 direction;
            if (!TryGetRightHandAimPose(out origin, out direction))
            {
                if (rightHandRay != null)
                {
                    rightHandRay.enabled = false;
                }

                return;
            }

            LineRenderer lineRenderer = GetOrCreateRightHandRay();
            float length = 4f;
            RaycastHit hit;
            if (Physics.Raycast(origin, direction, out hit, 20f))
            {
                length = hit.distance;
            }

            if (questUiAnchor != null)
            {
                Plane uiPlane = new Plane(questUiAnchor.forward, questUiAnchor.position);
                float enter;
                if (uiPlane.Raycast(new Ray(origin, direction), out enter) && enter > 0f)
                {
                    length = Mathf.Min(length, enter);
                }
            }

            lineRenderer.enabled = true;
            lineRenderer.SetPosition(0, origin);
            lineRenderer.SetPosition(1, origin + direction * length);
        }

        private static bool TryGetRightHandAimPose(out Vector3 origin, out Vector3 direction)
        {
            origin = default;
            direction = Vector3.forward;

            XRController right = XRController.rightHand;
            if (right == null)
            {
                return false;
            }

            // Prefer aim/pointer pose; fall back to grip/device pose.
            Vector3Control pointerPosition = right.TryGetChildControl<Vector3Control>("pointerPosition");
            QuaternionControl pointerRotation = right.TryGetChildControl<QuaternionControl>("pointerRotation");
            if (pointerPosition != null && pointerRotation != null)
            {
                origin = pointerPosition.ReadValue();
                direction = pointerRotation.ReadValue() * Vector3.forward;
                return direction.sqrMagnitude > 0.0001f;
            }

            origin = right.devicePosition.ReadValue();
            direction = right.deviceRotation.ReadValue() * Vector3.forward;
            return direction.sqrMagnitude > 0.0001f;
        }

        private LineRenderer GetOrCreateRightHandRay()
        {
            if (rightHandRay != null)
            {
                return rightHandRay;
            }

            GameObject rayObject = new GameObject("GeoX Right Controller UI Ray");
            rayObject.transform.SetParent(transform, false);
            rightHandRay = rayObject.AddComponent<LineRenderer>();
            rightHandRay.positionCount = 2;
            rightHandRay.startWidth = 0.006f;
            rightHandRay.endWidth = 0.0015f;
            rightHandRay.useWorldSpace = true;
            rightHandRay.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rightHandRay.receiveShadows = false;
            rightHandRay.material = CreateControllerRayMaterial();
            Color rayColor = new Color(0.35f, 0.85f, 1f, 0.95f);
            rightHandRay.startColor = rayColor;
            rightHandRay.endColor = new Color(rayColor.r, rayColor.g, rayColor.b, 0.2f);
            return rightHandRay;
        }

        private static Material CreateControllerRayMaterial()
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("UI/Default");
            }

            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            Material material = new Material(shader != null ? shader : Shader.Find("Hidden/InternalErrorShader"));
            material.color = new Color(0.35f, 0.85f, 1f, 0.95f);
            return material;
        }

        private static bool IsQuestOrVrRuntime()
        {
            return IsQuestOrVrRuntimeStatic();
        }
    }
#endif
}

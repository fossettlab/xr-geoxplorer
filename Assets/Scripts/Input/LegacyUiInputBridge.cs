using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

namespace GeoX.Input
{
    /// <summary>
    /// Supplies new Input System values to retained MRTK/uGUI modules through
    /// BaseInputModule.inputOverride. Remove with the scheduled MRTK3/XRI UI migration.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LegacyUiInputBridge : BaseInput
    {
        // Matches the wheel normalization already used by GeoXInput.
        private const float WheelUnitsPerTick = 120;
        private const float LegacyJoystickDeadZone = 0.19f; // InputManager.asset UI axes.
        private readonly Dictionary<BaseInputModule, BaseInput> previous = new Dictionary<BaseInputModule, BaseInput>();
        private Keyboard keyboard;
        private string composition = string.Empty;
        private Vector2 compositionPosition;
        private IMECompositionMode compositionMode;

        protected override void OnEnable()
        {
            base.OnEnable();
            foreach (BaseInputModule module in GetComponents<BaseInputModule>())
            {
                previous[module] = module.inputOverride;
                module.inputOverride = this;
            }
            RefreshKeyboard();
            InputSystem.onDeviceChange += DeviceChanged;
        }

        protected override void OnDisable()
        {
            InputSystem.onDeviceChange -= DeviceChanged;
            if (keyboard != null) keyboard.onIMECompositionChange -= CompositionChanged;
            keyboard = null;
            composition = string.Empty;
            foreach (var entry in previous)
                if (entry.Key != null && entry.Key.inputOverride == this) entry.Key.inputOverride = entry.Value;
            previous.Clear();
            base.OnDisable();
        }

        private void DeviceChanged(InputDevice device, InputDeviceChange change) => RefreshKeyboard();

        private void RefreshKeyboard()
        {
            if (keyboard == Keyboard.current) return;
            if (keyboard != null) keyboard.onIMECompositionChange -= CompositionChanged;
            keyboard = Keyboard.current;
            composition = string.Empty;
            if (keyboard != null) keyboard.onIMECompositionChange += CompositionChanged;
        }

        private void CompositionChanged(IMECompositionString value) => composition = value.ToString();
        public override string compositionString => composition;
        public override IMECompositionMode imeCompositionMode
        {
            get => compositionMode;
            set { compositionMode = value; Keyboard.current?.SetIMEEnabled(value != IMECompositionMode.Off); }
        }
        public override Vector2 compositionCursorPos
        {
            get => compositionPosition;
            set { compositionPosition = value; Keyboard.current?.SetIMECursorPosition(value); }
        }

        public override bool mousePresent => Mouse.current != null;
        public override Vector2 mousePosition => Mouse.current?.position.ReadValue() ?? Vector2.zero;
        public override Vector2 mouseScrollDelta => (Mouse.current?.scroll.ReadValue() ?? Vector2.zero) / WheelUnitsPerTick;
        public override float mouseScrollDeltaPerTick => 1;
        public override bool GetMouseButton(int button) => MouseButton(button)?.isPressed ?? false;
        public override bool GetMouseButtonDown(int button) => MouseButton(button)?.wasPressedThisFrame ?? false;
        public override bool GetMouseButtonUp(int button) => MouseButton(button)?.wasReleasedThisFrame ?? false;

        private static ButtonControl MouseButton(int button)
        {
            switch (button)
            {
                case 0: return Mouse.current?.leftButton;
                case 1: return Mouse.current?.rightButton;
                case 2: return Mouse.current?.middleButton;
                default: throw new ArgumentOutOfRangeException(nameof(button));
            }
        }

        public override bool touchSupported => Touchscreen.current != null;
        public override int touchCount
        {
            get { int count = 0; foreach (var touch in ActiveTouches()) count++; return count; }
        }

        private static IEnumerable<TouchControl> ActiveTouches()
        {
            if (Touchscreen.current == null) yield break;
            foreach (TouchControl touch in Touchscreen.current.touches)
                if (touch.press.isPressed || touch.press.wasReleasedThisFrame) yield return touch;
        }

        public override Touch GetTouch(int index)
        {
            int current = 0;
            foreach (TouchControl touch in ActiveTouches())
            {
                if (current++ != index) continue;
                return new Touch
                {
                    fingerId = touch.touchId.ReadValue(), position = touch.position.ReadValue(),
                    rawPosition = touch.position.ReadValue(), deltaPosition = touch.delta.ReadValue(),
                    pressure = touch.pressure.ReadValue(), tapCount = touch.tapCount.ReadValue(),
                    phase = LegacyPhase(touch.phase.ReadValue())
                };
            }
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        private static UnityEngine.TouchPhase LegacyPhase(UnityEngine.InputSystem.TouchPhase phase)
        {
            switch (phase)
            {
                case UnityEngine.InputSystem.TouchPhase.Began: return UnityEngine.TouchPhase.Began;
                case UnityEngine.InputSystem.TouchPhase.Moved: return UnityEngine.TouchPhase.Moved;
                case UnityEngine.InputSystem.TouchPhase.Stationary: return UnityEngine.TouchPhase.Stationary;
                case UnityEngine.InputSystem.TouchPhase.Ended: return UnityEngine.TouchPhase.Ended;
                case UnityEngine.InputSystem.TouchPhase.Canceled: return UnityEngine.TouchPhase.Canceled;
                default: throw new InvalidOperationException("unexpected_active_touch_phase");
            }
        }

        // Bindings mirror Horizontal/Vertical/Submit/Cancel in InputManager.asset.
        public override float GetAxisRaw(string axisName)
        {
            Keyboard k = Keyboard.current;
            float key;
            if (axisName == "Horizontal") key =
                ((k?.rightArrowKey.isPressed == true || k?.dKey.isPressed == true) ? 1 : 0) -
                ((k?.leftArrowKey.isPressed == true || k?.aKey.isPressed == true) ? 1 : 0);
            else if (axisName == "Vertical") key =
                ((k?.upArrowKey.isPressed == true || k?.wKey.isPressed == true) ? 1 : 0) -
                ((k?.downArrowKey.isPressed == true || k?.sKey.isPressed == true) ? 1 : 0);
            else throw new ArgumentException("unsupported_ui_axis: " + axisName);
            Vector2 stick = Gamepad.current?.leftStick.ReadUnprocessedValue() ?? Vector2.zero;
            float axis = axisName == "Horizontal" ? stick.x : stick.y;
            if (Mathf.Abs(axis) < LegacyJoystickDeadZone) axis = 0;
            return Mathf.Abs(key) >= Mathf.Abs(axis) ? key : axis;
        }

        public override bool GetButtonDown(string buttonName)
        {
            Keyboard k = Keyboard.current;
            // StandaloneInputModule also queries axis names as digital buttons.
            if (buttonName == "Horizontal") return k?.leftArrowKey.wasPressedThisFrame == true ||
                k?.rightArrowKey.wasPressedThisFrame == true || k?.aKey.wasPressedThisFrame == true ||
                k?.dKey.wasPressedThisFrame == true;
            if (buttonName == "Vertical") return k?.upArrowKey.wasPressedThisFrame == true ||
                k?.downArrowKey.wasPressedThisFrame == true || k?.wKey.wasPressedThisFrame == true ||
                k?.sKey.wasPressedThisFrame == true;
            if (buttonName == "Submit") return k?.enterKey.wasPressedThisFrame == true ||
                k?.numpadEnterKey.wasPressedThisFrame == true || k?.spaceKey.wasPressedThisFrame == true ||
                Gamepad.current?.buttonSouth.wasPressedThisFrame == true;
            if (buttonName == "Cancel") return k?.escapeKey.wasPressedThisFrame == true ||
                Gamepad.current?.buttonEast.wasPressedThisFrame == true;
            throw new ArgumentException("unsupported_ui_button: " + buttonName);
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace Microsoft.MixedReality.Toolkit.Input
{
    internal static class InputSimulationInput
    {
        public static Vector3 MousePosition
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                if (Mouse.current != null)
                {
                    Vector2 position = Mouse.current.position.ReadValue();
                    return new Vector3(position.x, position.y, 0.0f);
                }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
                return UnityEngine.Input.mousePosition;
#else
                return Vector3.zero;
#endif
            }
        }

        public static Vector2 MouseScrollDelta
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                if (Mouse.current != null)
                {
                    return Mouse.current.scroll.ReadValue() / 120.0f;
                }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
                return UnityEngine.Input.mouseScrollDelta;
#else
                return Vector2.zero;
#endif
            }
        }

        public static float GetAxis(string axisName)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                return UnityEngine.Input.GetAxis(axisName);
            }
            catch (System.Exception)
            {
                // Fall through to the Input System approximation below.
            }
#endif

#if ENABLE_INPUT_SYSTEM
            if (axisName == "Mouse X")
            {
                return Mouse.current != null ? Mouse.current.delta.ReadValue().x : 0.0f;
            }

            if (axisName == "Mouse Y")
            {
                return Mouse.current != null ? Mouse.current.delta.ReadValue().y : 0.0f;
            }

            if (axisName == "Mouse ScrollWheel")
            {
                return Mouse.current != null ? Mouse.current.scroll.ReadValue().y / 120.0f : 0.0f;
            }

            if (axisName == "Horizontal")
            {
                return GetKeyAxis(KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.A, KeyCode.D);
            }

            if (axisName == "Vertical")
            {
                return GetKeyAxis(KeyCode.DownArrow, KeyCode.UpArrow, KeyCode.S, KeyCode.W);
            }

            if (axisName == "UpDown" || axisName == "Fly")
            {
                return GetKeyAxis(KeyCode.PageDown, KeyCode.PageUp, KeyCode.Q, KeyCode.E);
            }
#endif

            return 0.0f;
        }

        public static bool GetKey(string keyName)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return UnityEngine.Input.GetKey(keyName);
#else
            if (string.IsNullOrEmpty(keyName))
            {
                return false;
            }

            switch (keyName.ToLowerInvariant())
            {
                case "page down":
                    return GetKey(KeyCode.PageDown);
                case "page up":
                    return GetKey(KeyCode.PageUp);
                default:
                    return false;
            }
#endif
        }

        public static bool GetMouseButton(int mouseButton)
        {
#if ENABLE_INPUT_SYSTEM
            ButtonControl control = GetMouseButtonControl(mouseButton);
            if (control != null)
            {
                return control.isPressed;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return UnityEngine.Input.GetMouseButton(mouseButton);
#else
            return false;
#endif
        }

        public static bool GetMouseButtonDown(int mouseButton)
        {
#if ENABLE_INPUT_SYSTEM
            ButtonControl control = GetMouseButtonControl(mouseButton);
            if (control != null)
            {
                return control.wasPressedThisFrame;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return UnityEngine.Input.GetMouseButtonDown(mouseButton);
#else
            return false;
#endif
        }

        public static bool GetMouseButtonUp(int mouseButton)
        {
#if ENABLE_INPUT_SYSTEM
            ButtonControl control = GetMouseButtonControl(mouseButton);
            if (control != null)
            {
                return control.wasReleasedThisFrame;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return UnityEngine.Input.GetMouseButtonUp(mouseButton);
#else
            return false;
#endif
        }

        public static bool GetKey(KeyCode keyCode)
        {
#if ENABLE_INPUT_SYSTEM
            KeyControl control = GetKeyControl(keyCode);
            if (control != null)
            {
                return control.isPressed;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return UnityEngine.Input.GetKey(keyCode);
#else
            return false;
#endif
        }

        public static bool GetKeyDown(KeyCode keyCode)
        {
#if ENABLE_INPUT_SYSTEM
            KeyControl control = GetKeyControl(keyCode);
            if (control != null)
            {
                return control.wasPressedThisFrame;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return UnityEngine.Input.GetKeyDown(keyCode);
#else
            return false;
#endif
        }

        public static bool GetKeyUp(KeyCode keyCode)
        {
#if ENABLE_INPUT_SYSTEM
            KeyControl control = GetKeyControl(keyCode);
            if (control != null)
            {
                return control.wasReleasedThisFrame;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return UnityEngine.Input.GetKeyUp(keyCode);
#else
            return false;
#endif
        }

        private static float GetKeyAxis(KeyCode negative, KeyCode positive, KeyCode negativeAlternate, KeyCode positiveAlternate)
        {
            float value = 0.0f;
            if (GetKey(negative) || GetKey(negativeAlternate))
            {
                value -= 1.0f;
            }

            if (GetKey(positive) || GetKey(positiveAlternate))
            {
                value += 1.0f;
            }

            return value;
        }

#if ENABLE_INPUT_SYSTEM
        private static ButtonControl GetMouseButtonControl(int mouseButton)
        {
            if (Mouse.current == null)
            {
                return null;
            }

            switch (mouseButton)
            {
                case 0:
                    return Mouse.current.leftButton;
                case 1:
                    return Mouse.current.rightButton;
                case 2:
                    return Mouse.current.middleButton;
                case 3:
                    return Mouse.current.forwardButton;
                case 4:
                    return Mouse.current.backButton;
                default:
                    return null;
            }
        }

        private static KeyControl GetKeyControl(KeyCode keyCode)
        {
            if (Keyboard.current == null)
            {
                return null;
            }

            switch (keyCode)
            {
                case KeyCode.A:
                    return Keyboard.current.aKey;
                case KeyCode.D:
                    return Keyboard.current.dKey;
                case KeyCode.E:
                    return Keyboard.current.eKey;
                case KeyCode.Q:
                    return Keyboard.current.qKey;
                case KeyCode.S:
                    return Keyboard.current.sKey;
                case KeyCode.T:
                    return Keyboard.current.tKey;
                case KeyCode.W:
                    return Keyboard.current.wKey;
                case KeyCode.Y:
                    return Keyboard.current.yKey;
                case KeyCode.DownArrow:
                    return Keyboard.current.downArrowKey;
                case KeyCode.Escape:
                    return Keyboard.current.escapeKey;
                case KeyCode.LeftAlt:
                    return Keyboard.current.leftAltKey;
                case KeyCode.LeftControl:
                    return Keyboard.current.leftCtrlKey;
                case KeyCode.LeftShift:
                    return Keyboard.current.leftShiftKey;
                case KeyCode.LeftArrow:
                    return Keyboard.current.leftArrowKey;
                case KeyCode.PageDown:
                    return Keyboard.current.pageDownKey;
                case KeyCode.PageUp:
                    return Keyboard.current.pageUpKey;
                case KeyCode.RightAlt:
                    return Keyboard.current.rightAltKey;
                case KeyCode.RightControl:
                    return Keyboard.current.rightCtrlKey;
                case KeyCode.RightShift:
                    return Keyboard.current.rightShiftKey;
                case KeyCode.RightArrow:
                    return Keyboard.current.rightArrowKey;
                case KeyCode.Space:
                    return Keyboard.current.spaceKey;
                case KeyCode.UpArrow:
                    return Keyboard.current.upArrowKey;
                default:
                    return null;
            }
        }
#endif
    }
}

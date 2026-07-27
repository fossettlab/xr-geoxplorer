using UnityEngine;
using UnityEngine.InputSystem;

public static class GeoXInput
{
    public static bool PrimaryPointerPressed => Mouse.current != null && Mouse.current.leftButton.isPressed;

    public static bool SecondaryPointerPressed => Mouse.current != null && Mouse.current.rightButton.isPressed;

    public static bool PrimaryPointerPressedThisFrame
    {
        get
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                return true;
            }

            return Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
        }
    }

    public static bool PrimaryTouchPressedThisFrame =>
        Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;

    public static bool PrimaryTouchReleasedThisFrame =>
        Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame;

    public static Vector2 PointerPosition
    {
        get
        {
            if (Touchscreen.current != null &&
                (Touchscreen.current.primaryTouch.press.isPressed ||
                 Touchscreen.current.primaryTouch.press.wasPressedThisFrame ||
                 Touchscreen.current.primaryTouch.press.wasReleasedThisFrame))
            {
                return Touchscreen.current.primaryTouch.position.ReadValue();
            }

            if (Pointer.current != null)
            {
                return Pointer.current.position.ReadValue();
            }

            return Vector2.zero;
        }
    }

    public static Vector2 PointerDelta
    {
        get
        {
            if (Touchscreen.current != null &&
                (Touchscreen.current.primaryTouch.press.isPressed ||
                 Touchscreen.current.primaryTouch.press.wasPressedThisFrame ||
                 Touchscreen.current.primaryTouch.press.wasReleasedThisFrame))
            {
                return Touchscreen.current.primaryTouch.delta.ReadValue();
            }

            return Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
        }
    }

    public static float ScrollWheelY
    {
        get
        {
            if (Mouse.current == null)
            {
                return 0.0f;
            }

            return Mouse.current.scroll.ReadValue().y / 120.0f;
        }
    }
}

using System;
using System.Reflection;
using GeoX.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace GeoX.Input.Tests
{
    public class LegacyUiInputBridgeTests : InputTestFixture
    {
        private GameObject root;
        private LegacyUiInputBridge bridge;
        private StandaloneInputModule module;
        private Mouse mouse;
        private Keyboard keyboard;
        private Touchscreen screen;
        private Gamepad gamepad;

        public override void Setup()
        {
            base.Setup();
            mouse = InputSystem.AddDevice<Mouse>();
            keyboard = InputSystem.AddDevice<Keyboard>();
            screen = InputSystem.AddDevice<Touchscreen>();
            root = new GameObject("Input bridge test", typeof(EventSystem));
            module = root.AddComponent<StandaloneInputModule>();
            typeof(BaseInputModule).GetMethod("OnEnable",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(module, null);
            bridge = root.AddComponent<LegacyUiInputBridge>();
            // Edit Mode does not run this MonoBehaviour lifecycle automatically.
            InvokeLifecycle("OnEnable");
        }

        public override void TearDown()
        {
            if (bridge != null) InvokeLifecycle("OnDisable");
            UnityEngine.Object.DestroyImmediate(root);
            foreach (InputDevice device in new InputDevice[] { mouse, keyboard, screen, gamepad })
                if (device != null && device.added) InputSystem.RemoveDevice(device);
            base.TearDown();
        }

        private void InvokeLifecycle(string method)
        {
            typeof(LegacyUiInputBridge).GetMethod(method,
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(bridge, null);
        }

        [Test]
        public void ModuleReadsNewMouseStateAndRestoresItsPreviousInput()
        {
            Assert.That(module.inputOverride, Is.SameAs(bridge));
            InputSystem.QueueStateEvent(mouse, new MouseState
            {
                position = new Vector2(25, 50), scroll = new Vector2(0, 120), buttons = 1
            });
            InputSystem.Update();
            Assert.That(bridge.mousePosition, Is.EqualTo(new Vector2(25, 50)));
            Assert.That(bridge.mouseScrollDelta.y, Is.EqualTo(1));
            Assert.That(bridge.GetMouseButtonDown(0), Is.True);
            Assert.DoesNotThrow(() => module.UpdateModule());
            Assert.DoesNotThrow(() => module.ActivateModule());
            Assert.DoesNotThrow(() => module.Process());
            InvokeLifecycle("OnDisable");
            Assert.That(module.inputOverride, Is.Null);
            InvokeLifecycle("OnEnable");
            Assert.That(module.inputOverride, Is.SameAs(bridge));
        }

        [Test]
        public void KeyboardNavigationSubmitAndCancelUseNewDevices()
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.RightArrow, Key.Enter, Key.Escape));
            InputSystem.Update();
            Assert.That(bridge.GetAxisRaw("Horizontal"), Is.EqualTo(1));
            Assert.That(bridge.GetAxisRaw("Vertical"), Is.Zero);
            Assert.That(bridge.GetButtonDown("Horizontal"), Is.True);
            Assert.That(bridge.GetButtonDown("Vertical"), Is.False);
            Assert.That(bridge.GetButtonDown("Submit"), Is.True);
            Assert.That(bridge.GetButtonDown("Cancel"), Is.True);
            Assert.Throws<ArgumentException>(() => bridge.GetAxisRaw("unknown"));
        }

        [Test]
        public void GamepadAndNumpadBindingsRemainAvailable()
        {
            gamepad = InputSystem.AddDevice<Gamepad>();
            InputSystem.QueueStateEvent(gamepad, new GamepadState
            {
                leftStick = new Vector2(0.5f, -0.5f)
            }.WithButton(GamepadButton.South));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.NumpadEnter));
            InputSystem.Update();
            Assert.That(bridge.GetAxisRaw("Horizontal"), Is.EqualTo(0.5f));
            Assert.That(bridge.GetAxisRaw("Vertical"), Is.EqualTo(-0.5f));
            Assert.That(bridge.GetButtonDown("Submit"), Is.True);
        }

        [Test]
        public void TouchPressAndReleaseKeepTheirIdentityAndPhase()
        {
            InputSystem.QueueStateEvent(screen, new TouchState
            {
                touchId = 7, phase = UnityEngine.InputSystem.TouchPhase.Began,
                position = new Vector2(25, 50)
            });
            InputSystem.Update();
            Assert.That(bridge.touchCount, Is.EqualTo(1));
            Assert.That(bridge.GetTouch(0).fingerId, Is.EqualTo(7));
            Assert.That(bridge.GetTouch(0).phase, Is.EqualTo(UnityEngine.TouchPhase.Began));
            InputSystem.QueueStateEvent(screen, new TouchState
            {
                touchId = 7, phase = UnityEngine.InputSystem.TouchPhase.Ended,
                position = new Vector2(25, 50)
            });
            InputSystem.Update();
            Assert.That(bridge.touchCount, Is.EqualTo(1));
            Assert.That(bridge.GetTouch(0).phase, Is.EqualTo(UnityEngine.TouchPhase.Ended));
            InputSystem.Update();
            Assert.That(bridge.touchCount, Is.Zero);
        }
    }

    // Deliberately outside InputTestFixture: it must detect production layout
    // collisions that an isolated unit-test input system would otherwise hide.
    public class OpenXrGamepadCompatibilityTests
    {
        [Test]
        public void ProjectLayoutsPermitGamepadCreation()
        {
            Gamepad device = InputSystem.AddDevice<Gamepad>();
            try { Assert.That(device.dpad, Is.Not.Null); }
            finally { InputSystem.RemoveDevice(device); }
        }
    }
}

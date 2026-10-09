using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DeadSector
{
    public static class SectorInput
    {
        // Shared between the old and the new Input System paths.
        static float lookSensitivity = 1f;
        public static float LookSensitivity
        {
            get => lookSensitivity;
            set => lookSensitivity = Mathf.Clamp(value, .35f, 2.5f);
        }

        public static Vector2 Move
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var k = Keyboard.current;
                if (k == null) return Vector2.zero;
                return Vector2.ClampMagnitude(new Vector2((k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0),
                    (k.wKey.isPressed ? 1 : 0) - (k.sKey.isPressed ? 1 : 0)), 1f);
#else
                return Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);
#endif
            }
        }
        public static Vector2 Look
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Mouse.current == null ? Vector2.zero :
                    Mouse.current.delta.ReadValue() * (.12f * lookSensitivity);
#else
                return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * (2f * lookSensitivity);
#endif
            }
        }
        public static bool Sprint
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
#else
                return Input.GetKey(KeyCode.LeftShift);
#endif
            }
        }
#if ENABLE_INPUT_SYSTEM
        // One authoritative key map for the New Input System. All gameplay
        // hotkeys (including construction) must go through this table.
        // Previously unmapped B/Q/4/5 silently returned false, so a
        // crafted build kit could never be placed in PlayMode.
        static Key MapKey(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.Space: return Key.Space;
                case KeyCode.V: return Key.V;
                case KeyCode.Escape: return Key.Escape;
                case KeyCode.R: return Key.R;
                case KeyCode.M: return Key.M;
                case KeyCode.I: return Key.I;
                case KeyCode.C: return Key.C;
                case KeyCode.J: return Key.J;
                case KeyCode.B: return Key.B;
                case KeyCode.H: return Key.H;
                case KeyCode.Q: return Key.Q;
                case KeyCode.E: return Key.E;
                case KeyCode.Alpha1: return Key.Digit1;
                case KeyCode.Alpha2: return Key.Digit2;
                case KeyCode.Alpha3: return Key.Digit3;
                case KeyCode.Alpha4: return Key.Digit4;
                case KeyCode.Alpha5: return Key.Digit5;
                case KeyCode.Alpha6: return Key.Digit6;
                case KeyCode.Alpha7: return Key.Digit7;
                case KeyCode.F3: return Key.F3;
                case KeyCode.F5: return Key.F5;
                case KeyCode.F9: return Key.F9;
                default: return Key.None;
            }
        }
#endif

        // Testable mapping probe: no keyboard device required in EditMode.
        public static bool HasNewInputBinding(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            return MapKey(key) != Key.None;
#else
            // Legacy Input.GetKeyDown accepts these KeyCodes natively.
            switch (key)
            {
                case KeyCode.B:
                case KeyCode.H:
                case KeyCode.Q:
                case KeyCode.E:
                case KeyCode.Alpha1:
                case KeyCode.Alpha2:
                case KeyCode.Alpha3:
                case KeyCode.Alpha4:
                case KeyCode.Alpha5:
                case KeyCode.Alpha6:
                case KeyCode.Alpha7:
                    return true;
                default:
                    return false;
            }
#endif
        }

        public static bool Pressed(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return false;
            Key mapped = MapKey(key);
            return mapped != Key.None && keyboard[mapped].wasPressedThisFrame;
#else
            return Input.GetKeyDown(key);
#endif
        }
        public static bool RightClick
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
#else
                return Input.GetMouseButtonDown(1);
#endif
            }
        }

        public static bool Click
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
                return Input.GetMouseButtonDown(0);
#endif
            }
        }
    }
}

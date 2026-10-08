using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DeadSector
{
    public static class SectorInput
    {
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
                return Mouse.current == null ? Vector2.zero : Mouse.current.delta.ReadValue() * .12f;
#else
                return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 2f;
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
        public static bool Pressed(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k == null) return false;
            switch (key)
            {
                case KeyCode.Space: return k.spaceKey.wasPressedThisFrame;
                case KeyCode.V: return k.vKey.wasPressedThisFrame;
                case KeyCode.Escape: return k.escapeKey.wasPressedThisFrame;
                case KeyCode.R: return k.rKey.wasPressedThisFrame;
                case KeyCode.M: return k.mKey.wasPressedThisFrame;
                case KeyCode.I: return k.iKey.wasPressedThisFrame;
                case KeyCode.E: return k.eKey.wasPressedThisFrame;
                case KeyCode.Alpha1: return k.digit1Key.wasPressedThisFrame;
                case KeyCode.Alpha2: return k.digit2Key.wasPressedThisFrame;
                case KeyCode.Alpha3: return k.digit3Key.wasPressedThisFrame;
                case KeyCode.F5: return k.f5Key.wasPressedThisFrame;
                case KeyCode.F9: return k.f9Key.wasPressedThisFrame;
                default: return false;
            }
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

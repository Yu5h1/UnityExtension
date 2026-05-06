#if ENABLE_INPUT_SYSTEM
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using TouchPhase = UnityEngine.TouchPhase;

namespace Yu5h1Lib.Input
{
    internal class InputSystemBackend : InputHandler.IBackend
    {
        private InputActionAsset _actions;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register() => InputHandler.CurrentBackend = new InputSystemBackend();

        private InputSystemBackend()
        {
            _actions = UnityEngine.InputSystem.InputSystem.actions;
            _actions?.Enable();
        }

        public bool GetKeyDown(KeyCode k)
        {
            var key = InputSystemCompat.KeyCodeToKey(k);
            return key != Key.None && Keyboard.current?[key].wasPressedThisFrame == true;
        }

        public bool GetKey(KeyCode k)
        {
            var key = InputSystemCompat.KeyCodeToKey(k);
            return key != Key.None && Keyboard.current?[key].isPressed == true;
        }

        public bool GetKeyUp(KeyCode k)
        {
            var key = InputSystemCompat.KeyCodeToKey(k);
            return key != Key.None && Keyboard.current?[key].wasReleasedThisFrame == true;
        }

        public bool GetMouseButtonDown(int b) => MouseButton(b)?.wasPressedThisFrame == true;
        public bool GetMouseButton(int b) => MouseButton(b)?.isPressed == true;
        public bool GetMouseButtonUp(int b) => MouseButton(b)?.wasReleasedThisFrame == true;

        public Vector2 MousePosition => Mouse.current?.position.ReadValue() ?? Vector2.zero;

        public float GetAxis(string name)
        {
            if (name == "Mouse ScrollWheel") return Mouse.current?.scroll.ReadValue().y ?? 0f;
            return _actions?.FindAction(name)?.ReadValue<float>() ?? 0f;
        }

        public float GetAxisRaw(string name) => GetAxis(name);

        public bool GetButtonDown(string name) => _actions?.FindAction(name)?.WasPressedThisFrame() == true;
        public bool GetButton(string name) => _actions?.FindAction(name)?.IsPressed() == true;
        public bool GetButtonUp(string name) => _actions?.FindAction(name)?.WasReleasedThisFrame() == true;

        public Touch GetTouch(int index)
        {
            if (index != 0 || Touchscreen.current == null) return default;
            return new Touch
            {
                position         = Touchscreen.current.position.ReadValue(),
                rawPosition      = Touchscreen.current.position.ReadValue(),
                fingerId         = 0,
                tapCount         = 1,
                phase            = GetTouchPhase(),
                pressure         = 1f,
                maximumPossiblePressure = 1f,
                type             = TouchType.Direct,
            };
        }

        private ButtonControl MouseButton(int b) => b switch
        {
            0 => Mouse.current?.leftButton,
            1 => Mouse.current?.rightButton,
            2 => Mouse.current?.middleButton,
            _ => null
        };

        private TouchPhase GetTouchPhase()
        {
            var ts = Touchscreen.current;
            if (ts == null) return TouchPhase.Canceled;
            if (ts.press.wasPressedThisFrame)  return TouchPhase.Began;
            if (ts.press.wasReleasedThisFrame) return TouchPhase.Ended;
            if (ts.press.isPressed)            return TouchPhase.Moved;
            return TouchPhase.Canceled;
        }
    }
}
#endif

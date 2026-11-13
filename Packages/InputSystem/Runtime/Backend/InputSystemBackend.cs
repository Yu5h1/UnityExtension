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
        public Vector2 MouseDelta => Mouse.current?.delta.ReadValue() ?? Vector2.zero;

        public float GetAxis(string name)
        {
            if (name == "Mouse ScrollWheel") return Mouse.current?.scroll.ReadValue().y ?? 0f;
            return _actions?.FindAction(name)?.ReadValue<float>() ?? 0f;
        }

        public float GetAxisRaw(string name) => GetAxis(name);

        public bool GetButtonDown(string name) => _actions?.FindAction(name)?.WasPressedThisFrame() == true;
        public bool GetButton(string name) => _actions?.FindAction(name)?.IsPressed() == true;
        public bool GetButtonUp(string name) => _actions?.FindAction(name)?.WasReleasedThisFrame() == true;

        public int TouchCount => CountTouches(Touchscreen.current);

        public Touch GetTouch(int index) => ReadTouch(Touchscreen.current, index);

        /// <summary>
        /// Fingers that are down plus fingers that lifted this frame, in slot order. A slot keeps reporting Ended after
        /// its finger lifts until a new touch reuses it, so an Ended or Canceled slot counts only in the frame its press
        /// was released; counting every non-None slot would keep a lifted pinch finger in the count.
        /// </summary>
        internal static int CountTouches(Touchscreen screen)
        {
            if (screen == null) return 0;
            var count = 0;
            foreach (var slot in screen.touches)
                if (IsCurrent(slot)) count++;
            return count;
        }

        internal static Touch ReadTouch(Touchscreen screen, int index)
        {
            if (screen == null || index < 0) return default;
            foreach (var slot in screen.touches)
            {
                if (!IsCurrent(slot)) continue;
                if (index-- == 0) return ToTouch(slot);
            }
            return default;
        }

        private static bool IsCurrent(TouchControl slot)
        {
            switch (slot.phase.ReadValue())
            {
                case UnityEngine.InputSystem.TouchPhase.Began:
                case UnityEngine.InputSystem.TouchPhase.Moved:
                case UnityEngine.InputSystem.TouchPhase.Stationary:
                    return true;
                case UnityEngine.InputSystem.TouchPhase.Ended:
                case UnityEngine.InputSystem.TouchPhase.Canceled:
                    return slot.press.wasReleasedThisFrame;
                default:
                    return false;
            }
        }

        private static Touch ToTouch(TouchControl slot)
        {
            var position = slot.position.ReadValue();
            return new Touch
            {
                fingerId         = slot.touchId.ReadValue(),
                position         = position,
                rawPosition      = position,
                deltaPosition    = slot.delta.ReadValue(),
                tapCount         = slot.tapCount.ReadValue(),
                phase            = ToLegacyPhase(slot.phase.ReadValue()),
                pressure         = 1f,
                maximumPossiblePressure = 1f,
                type             = TouchType.Direct,
            };
        }

        private static TouchPhase ToLegacyPhase(UnityEngine.InputSystem.TouchPhase phase) => phase switch
        {
            UnityEngine.InputSystem.TouchPhase.Began      => TouchPhase.Began,
            UnityEngine.InputSystem.TouchPhase.Moved      => TouchPhase.Moved,
            UnityEngine.InputSystem.TouchPhase.Stationary => TouchPhase.Stationary,
            UnityEngine.InputSystem.TouchPhase.Ended      => TouchPhase.Ended,
            _                                             => TouchPhase.Canceled,
        };

        private ButtonControl MouseButton(int b) => b switch
        {
            0 => Mouse.current?.leftButton,
            1 => Mouse.current?.rightButton,
            2 => Mouse.current?.middleButton,
            _ => null
        };
    }
}
#endif

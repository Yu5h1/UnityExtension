using System;
using UnityEngine;

namespace Yu5h1Lib.CameraControl
{
    /// <summary>
    /// Mouse and keyboard source for <see cref="CameraController"/>. It reads the mouse through
    /// <see cref="InputHandler"/> and turns it into <see cref="CameraController.Orbit"/> while the cursor is locked.
    /// Enabling it locks the cursor; <see cref="toggleKeys"/> lock and release it; a <see cref="blocker"/> that returns
    /// true releases the cursor and stops the orbit. The cursor belongs to this component, not to the controller, because
    /// only a desktop has one.
    /// </summary>
    /// <remarks>
    /// The blocker is evaluated here, not through <see cref="CameraController.inputEnabled"/>: the controller stops
    /// calling <see cref="Feed"/> while that is false, so this component would never get to release the cursor.
    /// </remarks>
    [DisallowMultipleComponent]
    public class MouseKeyboardCameraInput : MonoBehaviour, ICameraInputSource
    {
        [SerializeField, Tooltip("Degrees of rotation per pixel of mouse movement.")]
        private float _degreesPerPixel = 0.2f;
        [SerializeField] private bool _lockCursorOnEnable = true;
        [SerializeField] private KeyCode[] _toggleKeys = { KeyCode.LeftAlt, KeyCode.Escape };

        private CursorLockMode _previousLockState;
        private bool _previousVisible;
        private bool _locked;

        public float degreesPerPixel { get => _degreesPerPixel; set => _degreesPerPixel = value; }
        public KeyCode[] toggleKeys { get => _toggleKeys; set => _toggleKeys = value; }

        /// <summary>
        /// While it returns true the cursor is released and the mouse does not turn the view; set by the consumer, for
        /// example to "a UI element has focus". It does not lock the cursor again when it turns false: press a toggle key.
        /// </summary>
        public Func<bool> blocker { get; set; }

        /// <summary>
        /// Whether this component wants the cursor locked. It follows the engine's cursor while the application has focus,
        /// so a release by the operating system or the editor is noticed.
        /// </summary>
        public bool isCursorLocked
        {
            get
            {
                if (_locked && Application.isPlaying && Application.isFocused && Cursor.lockState != CursorLockMode.Locked)
                    _locked = false;
                return _locked;
            }
        }

        public void SetCursorLocked(bool locked)
        {
            _locked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void OnEnable()
        {
            _previousLockState = Cursor.lockState;
            _previousVisible = Cursor.visible;
            if (_lockCursorOnEnable)
                SetCursorLocked(true);
            var controller = GetComponent<CameraController>();
            if (controller != null)
                controller.AddInputSource(this);
        }

        private void OnDisable()
        {
            var controller = GetComponent<CameraController>();
            if (controller != null)
                controller.RemoveInputSource(this);
            Cursor.lockState = _previousLockState;
            Cursor.visible = _previousVisible;
        }

        public void Feed(CameraController controller, float deltaTime)
        {
            if (blocker != null && blocker())
            {
                if (isCursorLocked)
                    SetCursorLocked(false);
                return;
            }
            if (WasTogglePressed())
                SetCursorLocked(!isCursorLocked);
            if (!isCursorLocked)
                return;
            controller.Orbit(InputHandler.MouseDelta * _degreesPerPixel);
        }

        private bool WasTogglePressed()
        {
            if (_toggleKeys == null)
                return false;
            for (var i = 0; i < _toggleKeys.Length; i++)
                if (InputHandler.GetKeyDown(_toggleKeys[i]))
                    return true;
            return false;
        }
    }
}

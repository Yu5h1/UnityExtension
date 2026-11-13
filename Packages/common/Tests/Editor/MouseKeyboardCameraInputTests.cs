using NUnit.Framework;
using UnityEngine;
using Yu5h1Lib.CameraControl;

namespace Yu5h1Lib.Tests
{
    public class MouseKeyboardCameraInputTests
    {
        private sealed class FakeBackend : InputHandler.IBackend
        {
            public Vector2 delta;
            public KeyCode keyDown;

            public bool GetKeyDown(KeyCode k) => k == keyDown;
            public bool GetKey(KeyCode k) => false;
            public bool GetKeyUp(KeyCode k) => false;
            public bool GetMouseButtonDown(int b) => false;
            public bool GetMouseButton(int b) => false;
            public bool GetMouseButtonUp(int b) => false;
            public Vector2 MousePosition => Vector2.zero;
            public Vector2 MouseDelta => delta;
            public float GetAxis(string name) => 0f;
            public float GetAxisRaw(string name) => 0f;
            public bool GetButtonDown(string name) => false;
            public bool GetButton(string name) => false;
            public bool GetButtonUp(string name) => false;
            public int TouchCount => 0;
            public Touch GetTouch(int index) => default;
        }

        private InputHandler.IBackend _previousBackend;
        private FakeBackend _backend;
        private GameObject _rig;
        private CameraController _controller;
        private MouseKeyboardCameraInput _input;

        [SetUp]
        public void SetUp()
        {
            _previousBackend = InputHandler.CurrentBackend;
            _backend = new FakeBackend { keyDown = KeyCode.None };
            InputHandler.CurrentBackend = _backend;
            _rig = new GameObject("rig");
            _rig.AddComponent<Camera>();
            _controller = _rig.AddComponent<CameraController>();
            _input = _rig.AddComponent<MouseKeyboardCameraInput>();
            _input.degreesPerPixel = 0.5f;
            _controller.look.Set(0f, 0f);
            _input.SetCursorLocked(true);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_rig);
            InputHandler.CurrentBackend = _previousBackend;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        [Test]
        public void MouseRightAndUpTurnsRightAndLooksUp()
        {
            _backend.delta = new Vector2(20f, 10f);
            _input.Feed(_controller, 0.016f);

            Assert.That(_controller.look.yaw, Is.EqualTo(10f).Within(1e-4f));
            Assert.That(_controller.look.pitch, Is.EqualTo(-5f).Within(1e-4f));
        }

        [Test]
        public void ReleasedCursorDoesNotTurnTheView()
        {
            _input.SetCursorLocked(false);
            _backend.delta = new Vector2(20f, 10f);
            _input.Feed(_controller, 0.016f);

            Assert.That(_controller.look.yaw, Is.EqualTo(0f));
            Assert.That(_controller.look.pitch, Is.EqualTo(0f));
        }

        [Test]
        public void ToggleKeyReleasesAndLocksAgain()
        {
            _backend.keyDown = KeyCode.Escape;
            _input.Feed(_controller, 0.016f);
            Assert.That(_input.isCursorLocked, Is.False);

            _input.Feed(_controller, 0.016f);
            Assert.That(_input.isCursorLocked, Is.True);
        }

        [Test]
        public void BlockerReleasesTheCursorAndStopsTheOrbit()
        {
            var blocked = true;
            _input.blocker = () => blocked;
            _backend.delta = new Vector2(20f, 10f);
            _input.Feed(_controller, 0.016f);

            Assert.That(_input.isCursorLocked, Is.False);
            Assert.That(_controller.look.yaw, Is.EqualTo(0f));

            blocked = false;
            _input.Feed(_controller, 0.016f);
            Assert.That(_input.isCursorLocked, Is.False, "release by the blocker is not undone by itself");
            Assert.That(_controller.look.yaw, Is.EqualTo(0f));
        }
    }
}

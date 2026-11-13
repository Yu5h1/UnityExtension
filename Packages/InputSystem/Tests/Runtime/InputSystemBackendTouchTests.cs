using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using Yu5h1Lib.Input;
using InputTouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace Yu5h1Lib.Tests
{
    /// <summary>
    /// Drives a virtual touchscreen so <see cref="InputSystemBackend"/>'s touch count and per-index reads are checked on
    /// a desktop with no touch hardware, including the slot that keeps reporting Ended after its finger lifts. These run
    /// in Play Mode because an Edit Mode update does not track which frame a press changed in, so "released this frame"
    /// is never true there. A batchmode Play Mode has no focused Game view, and the Input System disables non-background
    /// devices such as a touchscreen while unfocused, so each test ignores focus and restores both settings afterwards.
    /// Replacing <see cref="InputSystem.settings"/> instead would destroy a settings object that is not an asset.
    /// </summary>
    public class InputSystemBackendTouchTests
    {
        private Touchscreen screen;
        private InputSettings.BackgroundBehavior previousBackground;
        private InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;

        [SetUp]
        public void SetUp()
        {
            var settings = InputSystem.settings;
            previousBackground = settings.backgroundBehavior;
            previousEditorInput = settings.editorInputBehaviorInPlayMode;
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            screen = InputSystem.AddDevice<Touchscreen>();
        }

        [TearDown]
        public void TearDown()
        {
            if (screen != null && screen.added) InputSystem.RemoveDevice(screen);
            var settings = InputSystem.settings;
            settings.backgroundBehavior = previousBackground;
            settings.editorInputBehaviorInPlayMode = previousEditorInput;
        }

        private IEnumerator Send(int touchId, InputTouchPhase phase, Vector2 position)
        {
            InputSystem.QueueStateEvent(screen, new TouchState { touchId = touchId, phase = phase, position = position });
            yield return null;
        }

        [Test]
        public void NoTouchscreenReportsNoTouches()
        {
            Assert.AreEqual(0, InputSystemBackend.CountTouches(null));
            Assert.AreEqual(default(Touch), InputSystemBackend.ReadTouch(null, 0));
        }

        [UnityTest]
        public IEnumerator TwoFingersDownAreBothReadable()
        {
            yield return Send(1, InputTouchPhase.Began, new Vector2(10, 20));
            yield return Send(2, InputTouchPhase.Began, new Vector2(300, 400));

            Assert.AreEqual(2, InputSystemBackend.CountTouches(screen));
            var first = InputSystemBackend.ReadTouch(screen, 0);
            var second = InputSystemBackend.ReadTouch(screen, 1);
            Assert.AreEqual(1, first.fingerId);
            Assert.AreEqual(new Vector2(10, 20), first.position);
            Assert.AreEqual(2, second.fingerId);
            Assert.AreEqual(new Vector2(300, 400), second.position);
        }

        [UnityTest]
        public IEnumerator MovedFingerReportsItsDelta()
        {
            yield return Send(1, InputTouchPhase.Began, new Vector2(10, 10));
            yield return Send(1, InputTouchPhase.Moved, new Vector2(25, 40));

            var touch = InputSystemBackend.ReadTouch(screen, 0);
            Assert.AreEqual(UnityEngine.TouchPhase.Moved, touch.phase);
            Assert.AreEqual(new Vector2(15, 30), touch.deltaPosition);
        }

        [UnityTest]
        public IEnumerator ALiftedFingerCountsOnlyInTheFrameItLifts()
        {
            yield return Send(1, InputTouchPhase.Began, new Vector2(10, 10));
            yield return Send(2, InputTouchPhase.Began, new Vector2(50, 50));
            yield return Send(2, InputTouchPhase.Ended, new Vector2(50, 50));

            Assert.AreEqual(2, InputSystemBackend.CountTouches(screen), "the lift frame still reports the finger so a gesture sees its end");
            Assert.AreEqual(UnityEngine.TouchPhase.Ended, InputSystemBackend.ReadTouch(screen, 1).phase);

            yield return null;

            Assert.AreEqual(1, InputSystemBackend.CountTouches(screen), "a slot still reading Ended after the lift frame must not count as a second finger");
            Assert.AreEqual(1, InputSystemBackend.ReadTouch(screen, 0).fingerId);
        }

        [UnityTest]
        public IEnumerator AnIndexOutsideTheTouchesReadsDefault()
        {
            yield return Send(1, InputTouchPhase.Began, new Vector2(10, 10));

            Assert.AreEqual(default(Touch), InputSystemBackend.ReadTouch(screen, 1));
            Assert.AreEqual(default(Touch), InputSystemBackend.ReadTouch(screen, -1));
        }
    }
}

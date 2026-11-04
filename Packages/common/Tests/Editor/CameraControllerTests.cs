using NUnit.Framework;
using UnityEngine;
using MinMax = Yu5h1Lib.Runtime.MinMax;
using Yu5h1Lib.CameraControl;

namespace Yu5h1Lib.Tests
{
    public class CameraControllerTests
    {
        private GameObject _rig;
        private GameObject _targetObject;
        private CameraController _controller;
        private Camera _camera;

        [SetUp]
        public void SetUp()
        {
            _rig = new GameObject("rig");
            _camera = _rig.AddComponent<Camera>();
            _controller = _rig.AddComponent<CameraController>();
            _targetObject = new GameObject("target");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_rig);
            Object.DestroyImmediate(_targetObject);
        }

        private static void AssertVector(Vector3 expected, Vector3 actual, float tolerance = 1e-4f)
            => Assert.That((expected - actual).magnitude, Is.LessThan(tolerance), $"expected {expected} but was {actual}");

        [Test]
        public void LockedDirectionKeepsItsPlaneWhileFollowing()
        {
            _controller.look.Set(0f, 0f);
            _controller.look.locked = true;
            _controller.distance = 10f;
            _controller.SetTarget(_targetObject.transform);

            _targetObject.transform.position = new Vector3(3f, 2f, 0f);
            _controller.Orbit(new Vector2(40f, 20f));
            _controller.Evaluate(0.016f);

            AssertVector(new Vector3(3f, 2f, -10f), _rig.transform.position);
            Assert.That(Quaternion.Angle(Quaternion.identity, _rig.transform.rotation), Is.LessThan(1e-3f));
        }

        [Test]
        public void FollowContinuesWithoutAnyInput()
        {
            _controller.look.Set(0f, 0f);
            _controller.distance = 5f;
            _controller.SetTarget(_targetObject.transform);
            _controller.Evaluate(0.016f);

            _targetObject.transform.position = new Vector3(0f, 0f, 20f);
            _controller.Evaluate(0.016f);

            AssertVector(new Vector3(0f, 0f, 15f), _rig.transform.position);
        }

        [Test]
        public void SmoothingIsFrameRateIndependent()
        {
            _controller.look.Set(0f, 0f);
            _controller.distance = 5f;
            _controller.follow.smoothing = 5f;
            _controller.SetTarget(_targetObject.transform);
            _controller.Evaluate(0.016f);
            _targetObject.transform.position = new Vector3(10f, 0f, 0f);

            _controller.Evaluate(0.1f);
            var oneStep = _controller.pivot.x;

            _controller.Snap();
            _targetObject.transform.position = Vector3.zero;
            _controller.Evaluate(0.016f);
            _targetObject.transform.position = new Vector3(10f, 0f, 0f);
            for (var i = 0; i < 10; i++)
                _controller.Evaluate(0.01f);

            Assert.That(oneStep, Is.EqualTo(10f * (1f - Mathf.Exp(-0.5f))).Within(1e-3f));
            Assert.That(_controller.pivot.x, Is.EqualTo(oneStep).Within(1e-3f));
        }

        [Test]
        public void OrbitTurnsTheViewAndClampsPitch()
        {
            _controller.look.Set(0f, 0f);
            _controller.Orbit(new Vector2(30f, 500f));

            Assert.That(_controller.look.yaw, Is.EqualTo(30f).Within(1e-4f));
            Assert.That(_controller.look.pitch, Is.EqualTo(-89f).Within(1e-4f));
        }

        [Test]
        public void PanMovesTheFocusPointAlongTheViewAxes()
        {
            _controller.look.Set(90f, 0f);
            _controller.Pan(new Vector2(2f, 3f));

            AssertVector(new Vector3(0f, 3f, -2f), _controller.focusPoint);
        }

        [Test]
        public void PanWhileFollowingEditsTheOffsetInTargetSpace()
        {
            _targetObject.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            _controller.look.Set(0f, 0f);
            _controller.follow.space = CameraFollow.OffsetSpace.Target;
            _controller.SetTarget(_targetObject.transform);
            _controller.Pan(new Vector2(1f, 0f));

            AssertVector(new Vector3(0f, 0f, -1f), _controller.follow.offset);
            AssertVector(new Vector3(1f, 0f, 0f), _controller.follow.goal);
        }

        [Test]
        public void ZoomControlsAreSeparateAndClamped()
        {
            _controller.distance = 10f;
            _camera.orthographicSize = 5f;
            _camera.fieldOfView = 60f;

            _controller.Dolly(1000f);
            _controller.ZoomOrthographic(-1000f);
            _controller.AddFieldOfView(1000f);

            Assert.That(_controller.distance, Is.EqualTo(100f));
            Assert.That(_controller.orthographicSize, Is.EqualTo(1f));
            Assert.That(_controller.fieldOfView, Is.EqualTo(120f));
        }

        [Test]
        public void FrameFitsBoundsIntoAnOrthographicView()
        {
            _camera.orthographic = true;
            _camera.aspect = 2f;
            _controller.look.Set(0f, 0f);
            _controller.orthographicSizeRange = new MinMax(0.1f, 100f);

            _controller.Frame(new Bounds(new Vector3(4f, 1f, 0f), new Vector3(20f, 4f, 2f)));

            AssertVector(new Vector3(4f, 1f, 0f), _controller.focusPoint);
            Assert.That(_controller.orthographicSize, Is.EqualTo(5f).Within(1e-4f));
        }

        [Test]
        public void TargetSwitchEasesAndRemovalKeepsThePivot()
        {
            _controller.look.Set(0f, 0f);
            _controller.SetTarget(_targetObject.transform);
            _targetObject.transform.position = new Vector3(6f, 0f, 0f);
            _controller.Evaluate(0.016f);

            _controller.SetTarget(null);
            _controller.Evaluate(0.016f);

            AssertVector(new Vector3(6f, 0f, 0f), _controller.focusPoint);
            AssertVector(new Vector3(6f, 0f, 0f), _controller.pivot);
        }

        [Test]
        public void ResetViewRestoresTheStateFromFirstUse()
        {
            _controller.look.Set(10f, 5f);
            _controller.distance = 8f;
            _controller.focusPoint = new Vector3(1f, 2f, 3f);
            _controller.SetHome();

            _controller.Orbit(new Vector2(50f, 10f));
            _controller.Pan(new Vector2(4f, 4f));
            _controller.Dolly(20f);
            _controller.ResetView();

            Assert.That(_controller.look.yaw, Is.EqualTo(10f).Within(1e-4f));
            Assert.That(_controller.look.pitch, Is.EqualTo(5f).Within(1e-4f));
            Assert.That(_controller.distance, Is.EqualTo(8f));
            AssertVector(new Vector3(1f, 2f, 3f), _controller.focusPoint);
        }
    }
}

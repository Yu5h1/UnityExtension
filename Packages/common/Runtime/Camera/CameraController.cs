using System.Collections.Generic;
using UnityEngine;
using MinMax = Yu5h1Lib.Runtime.MinMax;

namespace Yu5h1Lib.CameraControl
{
    /// <summary>
    /// Shared camera control. Game code, UI and <see cref="ICameraInputSource"/>s all drive it through the same methods
    /// (<see cref="Orbit"/>, <see cref="Pan"/>, <see cref="Dolly"/>, <see cref="ZoomOrthographic"/>,
    /// <see cref="AddFieldOfView"/>, <see cref="FocusOn"/>, <see cref="Frame"/>, <see cref="ResetView"/>); it owns the
    /// state and writes the result to the camera every <c>LateUpdate</c>, and it never reads a device.
    /// </summary>
    /// <remarks>
    /// The camera sits <see cref="distance"/> behind the pivot along the view direction. The pivot is
    /// <see cref="follow"/>'s goal while it has a target, otherwise the focus point. <see cref="look"/> supplies the
    /// view direction. A fixed 2D view is a locked look direction plus a follow target and orthographic zoom; a 3D orbit
    /// is an unlocked look direction. Following and evaluating continue when no source feeds anything.
    /// <para>
    /// Distance, orthographic size and field of view are three controls with their own methods and ranges; none of
    /// them is chosen for the caller from the camera's projection.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public class CameraController : MonoBehaviour
    {
        private struct Home
        {
            public float yaw, pitch, distance, orthographicSize, fieldOfView;
            public Vector3 focusPoint, followOffset;
        }

        [SerializeField] private Camera _camera;
        [SerializeField] private CameraFollow _follow = new CameraFollow();
        [SerializeField] private CameraLookDirection _look = new CameraLookDirection();
        [SerializeField] private Vector3 _focusPoint;
        [SerializeField] private float _distance = 10f;
        [SerializeField] private MinMax _distanceRange = new MinMax(1f, 100f);
        [SerializeField] private MinMax _orthographicSizeRange = new MinMax(1f, 50f);
        [SerializeField] private MinMax _fieldOfViewRange = new MinMax(1f, 120f);
        [SerializeField] private bool _inputEnabled = true;

        private readonly List<ICameraInputSource> _sources = new List<ICameraInputSource>();
        private Home _home;
        private bool _hasHome;
        private Vector3 _pivot;
        private bool _hasPivot;
        private Transform _lastTarget;

        public Camera targetCamera
        {
            get
            {
                if (_camera == null)
                    _camera = GetComponent<Camera>();
                return _camera;
            }
            set => _camera = value;
        }

        public CameraFollow follow => _follow;
        public CameraLookDirection look => _look;

        /// <summary>The pivot while nothing is followed. Panning moves it in that case.</summary>
        public Vector3 focusPoint { get => _focusPoint; set { EnsureHome(); _focusPoint = value; } }

        /// <summary>The pivot the last <see cref="Evaluate"/> used.</summary>
        public Vector3 pivot => _hasPivot ? _pivot : CurrentGoal();

        public float distance { get => _distance; set { EnsureHome(); _distance = _distanceRange.Clamp(value); } }
        public MinMax distanceRange { get => _distanceRange; set { _distanceRange = value; _distance = value.Clamp(_distance); } }

        public float orthographicSize
        {
            get => targetCamera != null ? targetCamera.orthographicSize : 0f;
            set
            {
                EnsureHome();
                if (targetCamera != null)
                    targetCamera.orthographicSize = _orthographicSizeRange.Clamp(value);
            }
        }
        public MinMax orthographicSizeRange { get => _orthographicSizeRange; set => _orthographicSizeRange = value; }

        public float fieldOfView
        {
            get => targetCamera != null ? targetCamera.fieldOfView : 0f;
            set
            {
                EnsureHome();
                if (targetCamera != null)
                    targetCamera.fieldOfView = _fieldOfViewRange.Clamp(value);
            }
        }
        public MinMax fieldOfViewRange { get => _fieldOfViewRange; set => _fieldOfViewRange = value; }

        /// <summary>
        /// Whether the registered sources are fed. A consumer clears it while a UI element has focus or the cursor is
        /// free; direct calls to the control methods are not affected.
        /// </summary>
        public bool inputEnabled { get => _inputEnabled; set => _inputEnabled = value; }

        public void AddInputSource(ICameraInputSource source)
        {
            if (source != null && !_sources.Contains(source))
                _sources.Add(source);
        }

        public void RemoveInputSource(ICameraInputSource source) => _sources.Remove(source);

        /// <summary>
        /// Positive <c>delta.x</c> turns the view right and positive <c>delta.y</c> looks up, in degrees. Ignored while
        /// <see cref="CameraLookDirection.locked"/>.
        /// </summary>
        public void Orbit(Vector2 delta)
        {
            EnsureHome();
            _look.Orbit(delta);
        }

        /// <summary>
        /// Moves the camera along its own right and up axes by world units: positive <c>delta.x</c> moves it right, so
        /// the scene appears to move left. A followed target moves <see cref="CameraFollow.offset"/> instead of the focus
        /// point.
        /// </summary>
        public void Pan(Vector2 delta)
        {
            EnsureHome();
            var rotation = _look.rotation;
            var world = rotation * Vector3.right * delta.x + rotation * Vector3.up * delta.y;
            if (_follow.isFollowing)
                _follow.AddWorldOffset(world);
            else
                _focusPoint += world;
        }

        /// <summary>Changes <see cref="distance"/> by world units; positive moves the camera away.</summary>
        public void Dolly(float delta) => distance = _distance + delta;

        /// <summary>Changes <see cref="orthographicSize"/>; positive shows more of the scene.</summary>
        public void ZoomOrthographic(float delta) => orthographicSize = orthographicSize + delta;

        /// <summary>Changes <see cref="fieldOfView"/> in degrees; positive widens the view.</summary>
        public void AddFieldOfView(float degrees) => fieldOfView = fieldOfView + degrees;

        /// <summary>
        /// Puts the pivot on a world point. While following it edits the offset so the goal lands there, so the view
        /// still follows afterwards.
        /// </summary>
        public void FocusOn(Vector3 worldPoint)
        {
            EnsureHome();
            if (_follow.isFollowing)
                _follow.SetGoal(worldPoint);
            else
                _focusPoint = worldPoint;
        }

        /// <summary>
        /// Focuses the bounds' center and sets the active projection's size so the bounds fit the view's width and
        /// height, with <paramref name="padding"/> world units on every side. An orthographic camera changes
        /// <see cref="orthographicSize"/>; a perspective camera changes <see cref="distance"/>.
        /// </summary>
        public void Frame(Bounds bounds, float padding = 0f)
        {
            var cam = targetCamera;
            if (cam == null)
                return;
            FocusOn(bounds.center);
            var rotation = _look.rotation;
            var halfWidth = ProjectedHalfExtent(bounds.extents, rotation * Vector3.right) + padding;
            var halfHeight = ProjectedHalfExtent(bounds.extents, rotation * Vector3.up) + padding;
            var aspect = Mathf.Max(cam.aspect, 0.0001f);
            if (cam.orthographic)
            {
                orthographicSize = Mathf.Max(halfHeight, halfWidth / aspect);
                return;
            }
            var halfDepth = ProjectedHalfExtent(bounds.extents, rotation * Vector3.forward);
            var tan = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            distance = Mathf.Max(halfHeight / tan, halfWidth / (tan * aspect)) + halfDepth;
        }

        /// <summary>
        /// Starts following a target. The pivot eases from where it is unless <paramref name="snap"/> is true. Passing
        /// null stops following and leaves the pivot where it was.
        /// </summary>
        public void SetTarget(Transform target, bool snap = false)
        {
            EnsureHome();
            _follow.target = target;
            if (snap)
                Snap();
        }

        /// <summary>Makes the next <see cref="Evaluate"/> put the pivot on its goal without easing.</summary>
        public void Snap() => _hasPivot = false;

        /// <summary>
        /// Restores yaw, pitch, follow offset, focus point, distance, orthographic size and field of view to what they
        /// were when the controller was first used or when <see cref="SetHome"/> last ran.
        /// </summary>
        [ContextMenu(nameof(ResetView))]
        public void ResetView()
        {
            EnsureHome();
            _look.Set(_home.yaw, _home.pitch);
            _follow.offset = _home.followOffset;
            _focusPoint = _home.focusPoint;
            _distance = _home.distance;
            if (targetCamera != null)
            {
                targetCamera.orthographicSize = _home.orthographicSize;
                targetCamera.fieldOfView = _home.fieldOfView;
            }
        }

        /// <summary>Takes the current state as what <see cref="ResetView"/> restores.</summary>
        public void SetHome()
        {
            var cam = targetCamera;
            _home = new Home
            {
                yaw = _look.yaw,
                pitch = _look.pitch,
                distance = _distance,
                focusPoint = _focusPoint,
                followOffset = _follow.offset,
                orthographicSize = cam != null ? cam.orthographicSize : 0f,
                fieldOfView = cam != null ? cam.fieldOfView : 0f,
            };
            _hasHome = true;
        }

        /// <summary>
        /// Takes yaw, pitch, focus point and distance from where the camera is now, keeping its current distance to
        /// <paramref name="pivotPoint"/>.
        /// </summary>
        public void AlignToCamera(Vector3 pivotPoint)
        {
            var cam = targetCamera;
            if (cam == null)
                return;
            EnsureHome();
            _look.Set(cam.transform.rotation);
            _distance = _distanceRange.Clamp(Vector3.Distance(cam.transform.position, pivotPoint));
            if (_follow.isFollowing)
                _follow.SetGoal(pivotPoint);
            else
                _focusPoint = pivotPoint;
            Snap();
        }

        /// <summary>Updates the pivot by <paramref name="deltaTime"/> and writes the camera's position and rotation.</summary>
        public void Evaluate(float deltaTime)
        {
            EnsureHome();
            var cam = targetCamera;
            if (cam == null)
                return;
            UpdatePivot(deltaTime);
            var rotation = _look.rotation;
            cam.transform.SetPositionAndRotation(_pivot - rotation * Vector3.forward * _distance, rotation);
        }

        private void UpdatePivot(float deltaTime)
        {
            var target = _follow.target;
            if (_lastTarget != target)
            {
                if (target == null && _hasPivot)
                    _focusPoint = _pivot;
                _lastTarget = target;
            }
            if (target == null)
            {
                _pivot = _focusPoint;
                _hasPivot = true;
                return;
            }
            _pivot = _hasPivot ? _follow.Step(_pivot, deltaTime) : _follow.goal;
            _hasPivot = true;
        }

        private Vector3 CurrentGoal() => _follow.isFollowing ? _follow.goal : _focusPoint;

        private void EnsureHome()
        {
            if (!_hasHome)
                SetHome();
        }

        private static float ProjectedHalfExtent(Vector3 extents, Vector3 axis)
            => Mathf.Abs(axis.x) * extents.x + Mathf.Abs(axis.y) * extents.y + Mathf.Abs(axis.z) * extents.z;

        private void OnEnable()
        {
            EnsureHome();
            GetComponents(_found);
            foreach (var source in _found)
                AddInputSource(source);
            _found.Clear();
        }

        private static readonly List<ICameraInputSource> _found = new List<ICameraInputSource>();

        private void Update()
        {
            if (!_inputEnabled)
                return;
            for (var i = 0; i < _sources.Count; i++)
                _sources[i].Feed(this, Time.deltaTime);
        }

        private void LateUpdate() => Evaluate(Time.deltaTime);
    }
}

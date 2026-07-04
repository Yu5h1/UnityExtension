using UnityEngine;
using UnityEngine.Animations;

namespace Yu5h1Lib.UI
{
    [DisallowMultipleComponent]
    public class DirectionIndicator : BaseMonoBehaviour
    {
        [SerializeField] private Renderer _renderer;
        [SerializeField] private Transform _origin;
        [SerializeField] private Transform _target;
        [SerializeField] private bool _updateEveryFrame = true;
        [SerializeField] private bool _hideWhenDirectionIsZero;
        [SerializeField] private Axis _forwardAxis = Axis.Z;

        /// <summary>
        /// World: apply as world rotation so <see cref="forwardAxis"/> points along the world direction to the target.
        /// Screen: project positions to the camera viewport and apply as local rotation, so a camera-facing 2D arrow
        /// aligns <see cref="forwardAxis"/> (pick an in-plane axis, X or Y) toward the target's on-screen direction.
        /// Requires a camera (falls back to Camera.main).
        /// </summary>
        public enum Space { World, Screen }

        [SerializeField] private Space _space = Space.World;
        [SerializeField] private Camera _camera;

        private Vector3 _normal;

        public Transform origin { get => _origin; set => _origin = value; }
        public Transform target 
        { 
            get => _target;
            set
            {
                if (_target == value)
                    return;
                _target = value;
                UpdateDirection();
            }
        }
        
        public Renderer renderer => _renderer;
        public Axis forwardAxis { get => _forwardAxis; set => _forwardAxis = value; }
        public Vector3 normal => _normal;

        private Camera cam => _camera != null ? _camera : Camera.main;

        protected override void OnInitializing()
        {
            if (_renderer == null)
                _renderer = GetComponent<Renderer>();
        }

        private void LateUpdate()
        {
            if (_updateEveryFrame)
                UpdateDirection();
        }

        public bool UpdateDirection()
        {
            if (_target == null)
                return false;

            Transform from = _origin != null ? _origin : transform;
            Vector3 delta = ToSpace(_target.position) - ToSpace(from.position);
            return SetDirection(delta);
        }

        /// <summary>
        /// World: returns the position unchanged. Screen: projects it to the camera viewport (z flattened to 0,
        /// corrected when the point is behind the camera) so the delta becomes an on-screen direction.
        /// </summary>
        private Vector3 ToSpace(Vector3 worldPosition)
        {
            if (_space == Space.World || cam == null)
                return worldPosition;

            Vector3 viewport = cam.WorldToViewportPoint(worldPosition);
            if (viewport.z < 0f)
            {
                viewport.x = 1f - viewport.x;
                viewport.y = 1f - viewport.y;
            }
            viewport.z = 0f;
            return viewport;
        }

        public bool SetDirection(Vector3 direction)
        {
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                SetVisible(!_hideWhenDirectionIsZero);
                _normal = Vector3.zero;
                return false;
            }

            _normal = direction.normalized;
            ApplyRotation(_normal);
            SetVisible(true);
            return true;
        }

        public void SetTarget(Transform target)
        {
            _target = target;
            UpdateDirection();
        }
        private void ApplyRotation(Vector3 normal)
        {
            Quaternion rotation = Quaternion.FromToRotation(GetAxis(_forwardAxis), normal);
            if (_space == Space.Screen)
                transform.localRotation = rotation;
            else
                transform.rotation = rotation;
        }

        private void SetVisible(bool visible)
        {
            if (renderer != null)
                renderer.enabled = visible;
        }

        private static Vector3 GetAxis(Axis axis)
        {
            switch (axis)
            {
                case Axis.X: return Vector3.right;
                case Axis.Y: return Vector3.up;
                case Axis.Z: return Vector3.forward;
            }

            return Vector3.forward;
        }
    }
}

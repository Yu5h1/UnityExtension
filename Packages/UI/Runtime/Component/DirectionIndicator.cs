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

        private Vector3 _normal;

        public Transform origin { get => _origin; set => _origin = value; }
        public Transform target { get => _target; set => _target = value; }
        
        public Renderer renderer => _renderer;
        public Axis forwardAxis { get => _forwardAxis; set => _forwardAxis = value; }
        public Vector3 normal => _normal;

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
            Vector3 delta = _target.position - from.position;
            return SetDirection(delta);
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
            transform.rotation = Quaternion.FromToRotation(GetAxis(_forwardAxis), normal);
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

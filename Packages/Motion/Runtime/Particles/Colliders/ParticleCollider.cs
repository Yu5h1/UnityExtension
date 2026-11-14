using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   Base of the scene colliders particles collide with. A collider registers with the world it
    ///   resolves whenever it is enabled, whatever the enable order of world and collider, and
    ///   always unregisters when disabled (ParticleWorld WORLD-02). Each step the world reads its
    ///   shape from the Transform and its surface velocity from the pose change since the previous
    ///   step; the first step after registering or <see cref="MarkTeleported"/> reports zero velocity
    ///   (WORLD-15, ParticleSolver SOLVER-14). <see cref="CollidesWith"/> selects the particle layers
    ///   it affects.
    /// </summary>
    public abstract class ParticleCollider : MonoBehaviour
    {
        [Tooltip("World to register with. Empty uses the only active ParticleWorld.")]
        [SerializeField]
        ParticleWorld _world;
        [Tooltip("Particle layers this collider pushes. A particle's layer is its source GameObject's layer.")]
        [SerializeField]
        LayerMask _collidesWith = ~0;

        ParticleWorld _bound;
        bool _enabled;
        Vector3 _previousPosition;
        Quaternion _previousRotation;
        bool _hasPrevious;
        bool _teleported;

        public abstract ParticleColliderShape Shape { get; }

        public ParticleWorld World
        {
            get => _world;
            set
            {
                _world = value;
                if (_enabled)
                    Rebind();
            }
        }

        public LayerMask CollidesWith
        {
            get => _collidesWith;
            set => _collidesWith = value;
        }

        public ParticleWorld BoundWorld => _bound;

        public void MarkTeleported() => _teleported = true;

        void OnEnable() => EnableCollider();
        void OnDisable() => DisableCollider();

        internal void EnableCollider()
        {
            _enabled = true;
            ParticleWorld.ActiveWorldsChanged -= Rebind;
            ParticleWorld.ActiveWorldsChanged += Rebind;
            Rebind();
        }

        internal void DisableCollider()
        {
            _enabled = false;
            ParticleWorld.ActiveWorldsChanged -= Rebind;
            Unbind();
        }

        void Rebind()
        {
            if (_bound != null && _bound.IsDestroyed)
                _bound = null;
            if (!ParticleWorld.TryResolve(_world, out ParticleWorld world, out _))
                return;
            if (world == _bound)
                return;
            Unbind();
            if (world.RegisterCollider(this, out string error))
            {
                _bound = world;
                _hasPrevious = false;
            }
            else
                Debug.LogError(error, this);
        }

        void Unbind()
        {
            if (_bound != null && !_bound.IsDestroyed)
                _bound.UnregisterCollider(this);
            _bound = null;
        }

        internal ParticleColliderState Capture(float deltaTime)
        {
            var state = new ParticleColliderState { shape = Shape, mask = (uint)_collidesWith.value };
            BuildShape(ref state);
            Vector3 position = transform.position;
            Quaternion rotation = transform.rotation;
            if (_hasPrevious && !_teleported && deltaTime > 0f)
            {
                state.linearVelocity = (position - _previousPosition) / deltaTime;
                (rotation * Quaternion.Inverse(_previousRotation)).ToAngleAxis(out float angle, out Vector3 axis);
                if (angle > 180f)
                    angle -= 360f;
                if (!float.IsInfinity(axis.x) && !float.IsNaN(axis.x))
                    state.angularVelocity = axis * (angle * Mathf.Deg2Rad / deltaTime);
            }
            _previousPosition = position;
            _previousRotation = rotation;
            _hasPrevious = true;
            _teleported = false;
            return state;
        }

        /// <summary>
        ///   Fills the shape fields of <paramref name="state"/> from the Transform.
        /// </summary>
        protected abstract void BuildShape(ref ParticleColliderState state);

        protected static Vector3 AbsoluteScale(Transform target)
        {
            Vector3 scale = target.lossyScale;
            return new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        }
    }
}

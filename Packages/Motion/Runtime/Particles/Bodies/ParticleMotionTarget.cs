using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   What a <see cref="ParticleMotionTarget"/> asks of the bodies it reaches.
    /// </summary>
    public enum ParticleMotionTargetMode
    {
        /// <summary>
        ///   Head for the target's position; the direction is recomputed per body, so a group converges.
        /// </summary>
        Point = 0,

        /// <summary>
        ///   Head the way the target faces, wherever the body is, so a group travels in parallel.
        /// </summary>
        Direction = 1
    }

    /// <summary>
    ///   Where self-propelled bodies are trying to get to (ParticleExtensions EXT-12, Unified-Solver's
    ///   SolverMotionTarget): a point or a direction, and a radius. A body follows the nearest target
    ///   that reaches it; radius 0 reaches every body. It registers with the world it resolves,
    ///   whatever the enable order of world and target, and unregisters when disabled (ParticleWorld
    ///   WORLD-02). Moving the Transform moves the target, so animation and Timeline drive it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ParticleMotionTarget : MonoBehaviour
    {
        [Tooltip("World to register with. Empty uses the only active ParticleWorld.")]
        [SerializeField]
        ParticleWorld _world;
        [SerializeField]
        ParticleMotionTargetMode _mode = ParticleMotionTargetMode.Point;
        [Tooltip("Meters. 0 reaches every body in the world.")]
        [Min(0f)]
        [SerializeField]
        float _radius;

        ParticleWorld _bound;
        bool _enabled;

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

        public ParticleMotionTargetMode Mode
        {
            get => _mode;
            set => _mode = value;
        }

        public float Radius
        {
            get => _radius;
            set => _radius = Mathf.Max(0f, value);
        }

        public ParticleWorld BoundWorld => _bound;
        public Vector3 Position => transform.position;
        public Vector3 Direction => transform.forward;

        void OnEnable() => EnableTarget();
        void OnDisable() => DisableTarget();

        internal void EnableTarget()
        {
            _enabled = true;
            ParticleWorld.ActiveWorldsChanged -= Rebind;
            ParticleWorld.ActiveWorldsChanged += Rebind;
            Rebind();
        }

        internal void DisableTarget()
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
            if (world.RegisterMotionTarget(this, out string error))
                _bound = world;
            else
                Debug.LogError(error, this);
        }

        void Unbind()
        {
            if (_bound != null && !_bound.IsDestroyed)
                _bound.UnregisterMotionTarget(this);
            _bound = null;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.5f, 1f, 0.6f, 1f);
            if (_radius > 0f)
                Gizmos.DrawWireSphere(Position, _radius);
            if (_mode == ParticleMotionTargetMode.Point)
                Gizmos.DrawWireSphere(Position, 0.1f);
            else
                Gizmos.DrawLine(Position, Position + Direction);
        }
    }
}

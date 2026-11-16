using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   A region of space with its own rules, shaped as an oriented box derived from its Transform:
    ///   centre = position, half extents = lossyScale / 2, axes = right, up, forward (ParticleExtensions
    ///   EXT-14). It carries a list of <see cref="ParticleVolumeEffect"/>: a medium makes it water or
    ///   air, a bounds effect makes it a place bodies are kept in or out of. It registers with the world
    ///   it resolves whenever it is enabled, whatever the enable order of world and volume, and always
    ///   unregisters when disabled (ParticleWorld WORLD-02). Every step the world turns the registered
    ///   medium effects into its medium list; emitters read the bounds effects.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ParticleVolume : MonoBehaviour
    {
        [Tooltip("World to register with. Empty uses the only active ParticleWorld.")]
        [SerializeField]
        ParticleWorld _world;
        [Tooltip("What this region does. Several may act on the same space.")]
        [Inline]
        [SerializeField]
        ParticleVolumeEffect[] _effects = System.Array.Empty<ParticleVolumeEffect>();

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

        public ParticleVolumeEffect[] Effects
        {
            get => _effects;
            set => _effects = value ?? System.Array.Empty<ParticleVolumeEffect>();
        }

        public ParticleWorld BoundWorld => _bound;

        public Vector3 Center => transform.position;

        public Vector3 HalfExtents
        {
            get
            {
                Vector3 scale = transform.lossyScale;
                return new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)) * 0.5f;
            }
        }

        public Quaternion Rotation => transform.rotation;

        void OnEnable() => EnableVolume();
        void OnDisable() => DisableVolume();

        internal void EnableVolume()
        {
            _enabled = true;
            ParticleWorld.ActiveWorldsChanged -= Rebind;
            ParticleWorld.ActiveWorldsChanged += Rebind;
            Rebind();
        }

        internal void DisableVolume()
        {
            _enabled = false;
            ParticleWorld.ActiveWorldsChanged -= Rebind;
            Unbind();
        }

        /// <summary>
        ///   Whether a world point is inside the box, whatever side any effect acts on. The CPU twin of
        ///   the inside test in the behaviour kernels.
        /// </summary>
        public bool ContainsPoint(Vector3 worldPoint)
        {
            Vector3 offset = worldPoint - Center;
            Quaternion rotation = Rotation;
            Vector3 local = new Vector3(
                Vector3.Dot(offset, rotation * Vector3.right),
                Vector3.Dot(offset, rotation * Vector3.up),
                Vector3.Dot(offset, rotation * Vector3.forward));
            Vector3 half = HalfExtents;
            return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z;
        }

        /// <summary>
        ///   Appends one medium for every enabled <see cref="ParticleMediumEffect"/>, in list order.
        /// </summary>
        internal void AppendMedia(List<ParticleMedium> media)
        {
            foreach (ParticleVolumeEffect effect in _effects)
                if (effect is ParticleMediumEffect medium && medium.Enabled)
                    media.Add(medium.ToMedium(this));
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
            if (world.RegisterVolume(this, out string error))
                _bound = world;
            else
                Debug.LogError(error, this);
        }

        void Unbind()
        {
            if (_bound != null && !_bound.IsDestroyed)
                _bound.UnregisterVolume(this);
            _bound = null;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.matrix = Matrix4x4.TRS(Center, Rotation, Vector3.one);
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 1f);
            Gizmos.DrawWireCube(Vector3.zero, HalfExtents * 2f);
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   Holds chosen particles of a <see cref="PhysicsParticle"/> at scene Transforms
    ///   (ParticleExtensions EXT-06). One Transform holds many particles, each at its own offset in
    ///   the Transform's local space, so a held edge keeps its shape. Points address particles by
    ///   flat number (<see cref="PhysicsParticle.TryLocate"/>). The anchor registers as a substep
    ///   extension with the world its source is bound to and moves held particles smoothly between
    ///   the Transform's poses at the start and end of each step (EXT-05); disabling it gives the
    ///   particles their allocated inverse mass back.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ParticleAnchor : ParticleHoldBehaviour
    {
        /// <summary>
        ///   One held particle: its flat number in the source and its position in the holding
        ///   Transform's local space.
        /// </summary>
        [Serializable]
        public sealed class Point
        {
            public int index;
            public Vector3 localOffset;
        }

        /// <summary>
        ///   A Transform and the particles it holds.
        /// </summary>
        [Serializable]
        public sealed class Binding
        {
            public Transform target;
            public List<Point> points = new List<Point>();
        }

        [Tooltip("The driver whose particles this anchor holds.")]
        [SerializeField]
        PhysicsParticle _source;
        [SerializeField]
        List<Binding> _bindings = new List<Binding>();

        Dictionary<Transform, Matrix4x4> _last = new Dictionary<Transform, Matrix4x4>();
        Dictionary<Transform, Matrix4x4> _next = new Dictionary<Transform, Matrix4x4>();
        Matrix4x4[] _previous = new Matrix4x4[4];
        Matrix4x4[] _current = new Matrix4x4[4];
        ParticleHoldSet.Entry[] _entries = new ParticleHoldSet.Entry[16];

        public PhysicsParticle Source
        {
            get => _source;
            set
            {
                _source = value;
                SyncWorld();
            }
        }

        public List<Binding> Bindings => _bindings;

        protected override PhysicsParticle HeldDriver => _source;

        protected override void OnRegistered() => _last.Clear();

        /// <summary>
        ///   Adds an error for every point whose flat number is outside the source's particles.
        /// </summary>
        public void Validate(List<PhysicsParticleMessage> messages)
        {
            if (_source == null)
            {
                messages.Add(PhysicsParticleMessage.Error("No source PhysicsParticle."));
                return;
            }
            int count = _source.FlatParticleCount;
            foreach (Binding binding in _bindings)
            {
                if (binding == null || binding.points == null)
                    continue;
                foreach (Point point in binding.points)
                    if (point != null && (point.index < 0 || point.index >= count))
                        messages.Add(PhysicsParticleMessage.Error(
                            $"Point {point.index} under '{(binding.target != null ? binding.target.name : "no target")}' "
                            + $"is outside the source's {count} particles and is skipped."));
            }
        }

        public override void Prepare(ParticleExtensionContext context)
        {
            int holders = 0;
            int entries = 0;
            IReadOnlyList<ParticleEntity> entities = _source != null ? _source.Entities : null;
            if (entities != null)
                foreach (Binding binding in _bindings)
                {
                    if (binding == null || binding.target == null || binding.points == null || binding.points.Count == 0)
                        continue;
                    Matrix4x4 current = binding.target.localToWorldMatrix;
                    if (!_last.TryGetValue(binding.target, out Matrix4x4 previous))
                        previous = current;
                    _next[binding.target] = current;
                    int added = AddEntries(context, entities, binding.points, holders, ref entries);
                    if (added == 0)
                        continue;
                    Grow(ref _previous, holders + 1);
                    Grow(ref _current, holders + 1);
                    _previous[holders] = previous;
                    _current[holders] = current;
                    holders++;
                }
            (_last, _next) = (_next, _last);
            _next.Clear();
            Holds.SetHolders(_previous, _current, holders);
            Holds.SetEntries(_entries, entries);
        }

        int AddEntries(
            ParticleExtensionContext context,
            IReadOnlyList<ParticleEntity> entities,
            List<Point> points,
            int holder,
            ref int entries)
        {
            int added = 0;
            foreach (Point point in points)
            {
                if (point == null || !_source.TryLocate(point.index, out int entityIndex, out int particleIndex))
                    continue;
                if (entityIndex >= entities.Count)
                    continue;
                ParticleEntity entity = entities[entityIndex];
                if (!context.IsSimulated(entity) || particleIndex >= entity.Particles.length)
                    continue;
                Grow(ref _entries, entries + 1);
                _entries[entries++] = new ParticleHoldSet.Entry
                {
                    particle = (uint)(entity.Particles.start + particleIndex),
                    holder = (uint)holder,
                    held = 1u,
                    originalInvMass = entity.RestParticles[particleIndex].invMass,
                    localOffset = point.localOffset
                };
                added++;
            }
            return added;
        }

        static void Grow<T>(ref T[] array, int count)
        {
            if (array.Length < count)
                Array.Resize(ref array, Mathf.NextPowerOfTwo(count));
        }
    }
}

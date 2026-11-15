using System;
using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   Kinds of change a <see cref="ParticleWorld"/> hands to its backend at the start of a step.
    /// </summary>
    internal enum ParticleWorldChangeKind
    {
        Upload,
        Release,
        Teleport,
        Reset,
        SetParked,
        SetCategory,
        SetRadius,
        SetGroup
    }

    /// <summary>
    ///   One change to one entity, queued when its owner calls the entity handle and applied by the
    ///   backend in call order at the next step boundary (ParticleWorld WORLD-03). Ranges are the
    ///   entity's slots in the world's particle, constraint and rigid-group storage.
    /// </summary>
    internal sealed class ParticleWorldChange
    {
        public ParticleWorldChangeKind kind;
        public int entityId;
        public RangeInt particles;
        public RangeInt constraints;
        public RangeInt rigidGroups;
        public int group;
        public ParticleEntityRequest request;
        public Pose pose;
        public Matrix4x4 transformDelta;
        public Vector3 velocity;
        public bool parked;
        public uint category;
        public float radius;
    }

    /// <summary>
    ///   The solver side of a <see cref="ParticleWorld"/>: it owns the simulated state and receives
    ///   the world's queued changes and per-step inputs. The world keeps allocation, registration
    ///   and lifecycle; the backend keeps only numbers (ParticleSolver 1.0.0).
    /// </summary>
    internal interface IParticleBackend : IDisposable
    {
        void Apply(IReadOnlyList<ParticleWorldChange> changes);

        void Step(
            float deltaTime,
            ParticleSolverSettings settings,
            IReadOnlyList<ParticleColliderState> colliders,
            IReadOnlyList<ParticleMedium> media,
            int activeParticleCount);

        /// <summary>
        ///   Reads the given slots asynchronously and fills <paramref name="readback"/> before
        ///   invoking <paramref name="onComplete"/>.
        /// </summary>
        void RequestReadback(
            RangeInt particles,
            RangeInt constraints,
            RangeInt rigidGroups,
            ParticleReadback readback,
            Action<ParticleReadback> onComplete);
    }
}

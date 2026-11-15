using System;
using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   Handle to one entity a <see cref="ParticleWorld"/> allocated: a fish, a net, a rope or a
    ///   fragment. It exposes the entity's slots and roles without reflection (ParticleWorld
    ///   WORLD-12) and forwards every change to the world, which applies it at the next step
    ///   (WORLD-03). After <see cref="Release"/> the handle is invalid; releasing again does nothing,
    ///   and any other read or write throws <see cref="InvalidOperationException"/>.
    /// </summary>
    public readonly struct ParticleEntity : IEquatable<ParticleEntity>
    {
        readonly ParticleWorld _world;
        readonly int _id;
        readonly int _generation;

        internal ParticleEntity(ParticleWorld world, int id, int generation)
        {
            _world = world;
            _id = id;
            _generation = generation;
        }

        public ParticleWorld World => _world;
        internal int Id => _id;
        internal int Generation => _generation;

        public bool IsValid => _world != null && _world.IsAlive(this);
        public RangeInt Particles => _world.RecordOf(this).particles;
        public RangeInt Constraints => _world.RecordOf(this).constraints;
        public RangeInt RigidGroups => _world.RecordOf(this).rigidGroups;
        public int Group => _world.RecordOf(this).group;
        public Pose Pose => _world.RecordOf(this).pose;
        public bool IsParked => _world.RecordOf(this).parked;
        public uint Category => _world.RecordOf(this).rest.category;

        /// <summary>
        ///   Each particle's role in its layout, such as grid column and row or chain position,
        ///   indexed like <see cref="Particles"/>.
        /// </summary>
        public IReadOnlyList<Vector3Int> Roles => _world.RecordOf(this).rest.roles;

        /// <summary>
        ///   Each particle as allocated: rest position in the entity's local frame, velocity,
        ///   inverse mass and radius, indexed like <see cref="Particles"/>.
        /// </summary>
        public IReadOnlyList<ParticleSpawn> RestParticles => _world.RecordOf(this).rest.particles;

        public void Release()
        {
            if (_world != null)
                _world.Release(this);
        }

        /// <summary>
        ///   Moves the entity rigidly to <paramref name="pose"/>, writing position and previous
        ///   position together and setting the velocity (ParticleWorld WORLD-07).
        /// </summary>
        public void Teleport(Pose pose, Vector3 velocity = default) => _world.Teleport(this, pose, velocity);

        /// <summary>
        ///   Returns the entity to its rest layout at <paramref name="pose"/> with zero velocity,
        ///   restoring broken constraints and rigid rotations (ParticleWorld WORLD-07).
        /// </summary>
        public void Reset(Pose pose) => _world.Reset(this, pose);

        public void SetParked(bool parked) => _world.SetParked(this, parked);
        public void SetCategory(uint category) => _world.SetCategory(this, category);
        public void SetRadius(float radius) => _world.SetRadius(this, radius);
        public void SetGroupMode(ParticleGroupMode mode) => _world.SetGroupMode(this, mode);

        /// <summary>
        ///   Replaces the rest length, compliance, damping and break force of every constraint from
        ///   the next step on, keeping positions, velocities and broken constraints (ParticleWorld
        ///   WORLD-16). <paramref name="constraints"/> must list the entity's constraints in order
        ///   with the same particle pairs; a NaN rest length keeps the current one. Returns false
        ///   with the reason otherwise.
        /// </summary>
        public bool SetConstraintParameters(IReadOnlyList<DistanceConstraintSpec> constraints, out string error) =>
            _world.SetConstraintParameters(this, constraints, out error);

        public bool Equals(ParticleEntity other) =>
            ReferenceEquals(_world, other._world) && _id == other._id && _generation == other._generation;

        public override bool Equals(object obj) => obj is ParticleEntity other && Equals(other);

        public override int GetHashCode() => (_id * 397) ^ _generation;
    }
}

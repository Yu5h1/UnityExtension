using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   Whether an entity's particles collide with each other. <see cref="NewGroup"/> gives the
    ///   entity a group of its own so its particles skip one another (ParticleSolver SOLVER-08);
    ///   <see cref="Shared"/> keeps group 0, colliding with every particle.
    /// </summary>
    public enum ParticleGroupMode
    {
        Shared,
        NewGroup
    }

    /// <summary>
    ///   One particle of an entity at rest, in the entity's local frame.
    /// </summary>
    public struct ParticleSpawn
    {
        public Vector3 position;
        public Vector3 velocity;
        public float invMass;
        public float radius;
    }

    /// <summary>
    ///   A distance constraint between two particles of the same entity, addressed by their
    ///   index inside the entity. A NaN <see cref="restLength"/> takes the distance between the two
    ///   particles when the entity is placed.
    /// </summary>
    public struct DistanceConstraintSpec
    {
        public int a;
        public int b;
        public float restLength;
        public float compliance;
        public float damping;
        public float breakForce;
    }

    /// <summary>
    ///   Everything one entity needs from a world: its particles at rest, the constraints and rigid
    ///   groups that join them, and per-particle classification. Positions are in the entity's
    ///   local frame; the world places them with the pose given at allocation.
    /// </summary>
    public sealed class ParticleEntityRequest
    {
        public readonly List<ParticleSpawn> particles = new List<ParticleSpawn>();
        public readonly List<DistanceConstraintSpec> constraints = new List<DistanceConstraintSpec>();
        public readonly List<int[]> rigidGroups = new List<int[]>();
        public readonly List<Vector3Int> roles = new List<Vector3Int>();
        public ParticleGroupMode groupMode;
        public uint category = 1u;
        public Pose pose = Pose.identity;

        /// <summary>
        ///   Returns false and the reason when the request breaks a rule ParticleWorld WORLD-04
        ///   rejects: non-positive radius, negative inverse mass, non-finite values, or constraint
        ///   and rigid-group indices outside the entity.
        /// </summary>
        public bool TryValidate(out string error)
        {
            error = null;
            if (particles.Count == 0)
                return Fail("an entity needs at least one particle.", out error);
            for (int i = 0; i < particles.Count; i++)
            {
                ParticleSpawn p = particles[i];
                if (!(p.radius > 0f) || float.IsInfinity(p.radius))
                    return Fail($"particle {i} has a radius of {p.radius}; it must be a finite value above 0.", out error);
                if (!(p.invMass >= 0f) || float.IsInfinity(p.invMass))
                    return Fail($"particle {i} has an inverse mass of {p.invMass}; it must be a finite value of at least 0.", out error);
                if (!ParticleSolverSettings.IsFinite(p.position) || !ParticleSolverSettings.IsFinite(p.velocity))
                    return Fail($"particle {i} has a non-finite position or velocity.", out error);
            }
            for (int i = 0; i < constraints.Count; i++)
            {
                DistanceConstraintSpec c = constraints[i];
                if (c.a < 0 || c.b < 0 || c.a >= particles.Count || c.b >= particles.Count)
                    return Fail($"constraint {i} joins {c.a} and {c.b}, outside the entity's {particles.Count} particles.", out error);
                if (c.a == c.b)
                    return Fail($"constraint {i} joins particle {c.a} to itself.", out error);
                if (!(c.compliance >= 0f) || !(c.damping >= 0f) || !(c.breakForce >= 0f))
                    return Fail($"constraint {i} needs compliance, damping and break force of at least 0.", out error);
                if (!float.IsNaN(c.restLength) && !(c.restLength >= 0f))
                    return Fail($"constraint {i} has a negative rest length.", out error);
            }
            for (int g = 0; g < rigidGroups.Count; g++)
            {
                int[] members = rigidGroups[g];
                if (members == null || members.Length == 0)
                    return Fail($"rigid group {g} has no members.", out error);
                foreach (int index in members)
                    if (index < 0 || index >= particles.Count)
                        return Fail($"rigid group {g} names particle {index}, outside the entity's {particles.Count} particles.", out error);
            }
            if (roles.Count != 0 && roles.Count != particles.Count)
                return Fail($"roles has {roles.Count} entries for {particles.Count} particles.", out error);
            if (!ParticleSolverSettings.IsFinite(pose.position))
                return Fail("the pose position is not finite.", out error);
            return true;
        }

        static bool Fail(string message, out string error)
        {
            error = message;
            return false;
        }
    }
}

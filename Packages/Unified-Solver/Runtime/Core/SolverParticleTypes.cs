using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Yu5h1.UnifiedSolver
{
    public enum SolverParticleTopology
    {
        Single = 0,
        Chain3 = 3,
        GuideChain4 = 4,
        DualRail6 = 6,
        RigidCluster4 = 104,
        RigidCluster6 = 106,
        RigidCluster8 = 108,
        ArticulatedCluster12 = 12
    }

    public static class SolverTopologyInfo
    {
        // Particles in a single rigid cluster variant, or 0 for anything else.
        //
        // The variant is per instance rather than per profile, because a body of
        // ice is a mix of sizes and a profile that could only spawn one of them
        // would need three profiles and three emitters to make one pile.
        public static int RigidClusterParticles(
            SolverParticleTopology topology)
        {
            switch (topology)
            {
                case SolverParticleTopology.RigidCluster4:
                    return 4;
                case SolverParticleTopology.RigidCluster6:
                    return 6;
                case SolverParticleTopology.RigidCluster8:
                    return 8;
                default:
                    return 0;
            }
        }

        public static bool IsRigidCluster(
            SolverParticleTopology topology)
        {
            return RigidClusterParticles(topology) > 0;
        }

        public static SolverParticleTopology RigidClusterFor(
            int particles)
        {
            switch (particles)
            {
                case 4:
                    return SolverParticleTopology
                        .RigidCluster4;
                case 6:
                    return SolverParticleTopology
                        .RigidCluster6;
                default:
                    return SolverParticleTopology
                        .RigidCluster8;
            }
        }
    }

    public enum SolverMeshMode
    {
        Rigid = 0,
        Articulated = 1
    }

    public enum SolverMeshForwardAxis
    {
        X = 0,
        Y = 1,
        Z = 2
    }

    [Serializable]
    public struct SolverParticleSpawnRequest
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public Vector3 angularVelocity;
        public Vector3 scale;
        public Color color;

        public static SolverParticleSpawnRequest Create(
            Vector3 position,
            Vector3 velocity,
            Color color)
        {
            return new SolverParticleSpawnRequest
            {
                position = position,
                rotation = Quaternion.identity,
                velocity = velocity,
                angularVelocity = Vector3.zero,
                scale = Vector3.one,
                color = color
            };
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SolverParticleInstance
    {
        public const int Stride = 64;

        public int particleOffset;
        public int particleCount;
        public int constraintOffset;
        public int constraintCount;
        public int rigidBodyOffset;
        public int rigidBodyCount;
        public int topology;
        public int profileId;
        public Vector3 scale;
        public float _padding;
        public Quaternion spawnRotation;
    }

    public enum SolverMediumShape
    {
        // A flat top, which is what makes a waterline. An ellipsoid has none:
        // its surface height varies with horizontal position, so a body floating
        // up settles at a different level depending on where it happens to be.
        Box = 0,
        // For a medium with no surface to speak of, such as a current or an
        // eddy sitting inside a larger body of water.
        Ellipsoid = 1
    }

    public enum SolverMotionTargetMode
    {
        // Head for a place. The direction is recomputed per body, so a group
        // converges.
        Point = 0,
        // Head the same way, wherever the body is. A group travels in parallel
        // instead of converging.
        Direction = 1
    }

    // Mirrored by SolverMotionTarget in SolverParticleModifiers.compute.
    [StructLayout(LayoutKind.Sequential)]
    public struct SolverMotionTargetGPU
    {
        public const int Stride = 32;

        public Vector3 position;
        public float mode;
        public Vector3 direction;
        public float radius;
    }

    // Mirrored by SolverMedium in SolverParticleModifiers.compute. Both
    // declarations have to agree and nothing checks that they do.
    [StructLayout(LayoutKind.Sequential)]
    public struct SolverMediumGPU
    {
        public const int Stride = 96;

        public Vector3 center;
        public float shape;
        public Vector3 halfExtents;
        public float density;
        public Vector3 axisX;
        public float viscosity;
        public Vector3 axisY;
        public float _pad0;
        public Vector3 axisZ;
        public float _pad1;
        public Vector3 flow;
        public float _pad2;
    }

    public readonly struct SolverParticleRequirements
    {
        public readonly int particles;
        public readonly int constraints;
        public readonly int rigidBodies;
        public readonly int rigidParticleRefs;

        public SolverParticleRequirements(
            int particles,
            int constraints,
            int rigidBodies,
            int rigidParticleRefs)
        {
            this.particles = particles;
            this.constraints = constraints;
            this.rigidBodies = rigidBodies;
            this.rigidParticleRefs = rigidParticleRefs;
        }
    }
}

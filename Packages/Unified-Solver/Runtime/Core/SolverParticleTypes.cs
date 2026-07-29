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
        ArticulatedCluster12 = 12
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

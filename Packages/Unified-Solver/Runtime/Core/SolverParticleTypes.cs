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

    public enum SolverVolumeShape
    {
        // A flat top, which is what makes a waterline. An ellipsoid has none:
        // its surface height varies with horizontal position, so a body floating
        // up settles at a different level depending on where it happens to be.
        Box = 0,
        // For a medium with no surface to speak of, such as a current or an
        // eddy sitting inside a larger body of water.
        Ellipsoid = 1
    }

    // Which branch of the volume kernels an entry belongs to.
    //
    // Uploaded as a float in the shared entry and switched on by the kernel,
    // rather than each effect getting its own buffer and its own inside test.
    // The geometry is the expensive part to duplicate and it is identical for
    // every effect, which is the whole reason volumes and effects are separate.
    public enum SolverVolumeEffectType
    {
        Medium = 0,
        Bounds = 1
    }

    // Where an instance is in the fade-out / respawn / fade-in cycle.
    //
    // Mirrored by the LIFE_ constants in SolverParticleModifiers.compute and
    // stored in the emitter's lifecycle buffer. Alive is deliberately 0, and the
    // buffer stores *hidden* rather than visible, so an untouched all-zero
    // buffer already means "alive and fully visible" and a scene with no bounds
    // effect needs nothing written to it, ever.
    public enum SolverInstanceLife
    {
        Alive = 0,
        FadingOut = 1,
        FadingIn = 2
    }

    // Which loop consumes an effect, and therefore what question it is able to
    // answer at all.
    //
    // Per particle is what gives a floating body its waterline for nothing: part
    // of it is inside and part is not, and it settles at partial submersion
    // without anything modelling a surface. Per instance is for decisions that
    // are only meaningful about a whole body -- half a fish cannot be recycled.
    // Lives on the C# side only: a kernel already knows its own granularity
    // because it is the loop, so uploading it would be dead weight.
    public enum SolverVolumeGranularity
    {
        Particle = 0,
        Instance = 1
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

    // One volume paired with one of its effects. Mirrored by SolverVolume in
    // SolverParticleModifiers.compute. Both declarations have to agree and
    // nothing checks that they do.
    //
    // Flattened per pair rather than per volume, so a box carrying two effects
    // uploads two entries with the same geometry. That repeats the inside test
    // once per effect, which at scene volume counts is cheaper than the
    // alternative: a variable-length effect list inside a GPU struct.
    //
    // The payload is deliberately untyped. Every effect gets the same four
    // slots and reads them as whatever it means, which is what lets one buffer,
    // one upload and one geometry test serve all of them. Each effect's Write
    // is the only place that knows the mapping; see SolverMediumProfile.
    //
    // The layout was chosen to keep the original 96-byte stride: the medium's
    // three padding floats were exactly the room the effect tag, the side flag
    // and the extra payload scalar needed.
    [StructLayout(LayoutKind.Sequential)]
    public struct SolverVolumeGPU
    {
        public const int Stride = 96;

        public Vector3 center;
        public float shape;
        public Vector3 halfExtents;
        public float effectType;
        public Vector3 axisX;
        // 1 acts everywhere except inside. On the effect rather than the
        // volume, because one box is water inside and recycles outside.
        public float invert;
        public Vector3 axisY;
        public float payloadX;
        public Vector3 axisZ;
        public float payloadY;
        public Vector3 payloadVector;
        public float payloadZ;
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

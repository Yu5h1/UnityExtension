using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   How a body's particles are arranged (ParticleExtensions EXT-10). Values match the
    ///   BODY_* constants in Shaders/ParticleBodyFrame.hlsl.
    /// </summary>
    public enum ParticleBodyTopology
    {
        Single = 0,
        Chain3 = 3,
        GuideChain4 = 4,
        DualRail6 = 6,
        ArticulatedCluster12 = 12,
        RigidCluster4 = 104,
        RigidCluster6 = 106,
        RigidCluster8 = 108
    }

    /// <summary>
    ///   One body as behaviour kernels and drawing read it on the GPU; mirrors ParticleBody in
    ///   Shaders/ParticleBodyFrame.hlsl (ParticleExtensions EXT-09). <see cref="scale"/>.w carries the
    ///   body's drag coefficient; <see cref="dimensions"/> holds the layout's base dimensions in
    ///   meters before scaling and <see cref="color"/> the body colour as RGBA8 (r in the low byte).
    ///   The colour is an integer field because RGBA8 bits read as a float can be a signalling NaN
    ///   that a float copy rewrites.
    ///   <see cref="flags"/> bit 0 is set while the body's entity is simulated.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ParticleBodyData
    {
        public const int Stride = 64;

        public uint particleStart;
        public uint particleCount;
        public uint topology;
        public uint flags;
        public Vector4 scale;
        public Vector4 spawnRotation;
        public Vector3 dimensions;
        public uint color;
    }

    /// <summary>
    ///   A component whose entities are bodies that behaviours and drawing share
    ///   (ParticleExtensions EXT-09): the entity list, a GPU buffer of <see cref="ParticleBodyData"/>
    ///   with one element per entity in the same order, and an optional lifecycle buffer
    ///   (float4 per body: state, progress, hidden fraction, fade rate). <see cref="Version"/>
    ///   changes whenever the entity list or the body buffer's contents change.
    /// </summary>
    public interface IParticleBodySource
    {
        ParticleWorld BoundWorld { get; }

        IReadOnlyList<ParticleEntity> Entities { get; }

        PhysicsParticleAppearance Appearance { get; }

        /// <summary>
        ///   One <see cref="ParticleBodyData"/> per entity, or null before anything is allocated.
        /// </summary>
        ComputeBuffer Bodies { get; }

        /// <summary>
        ///   One float4 per entity, or null when the source has no lifecycle (every body visible).
        /// </summary>
        ComputeBuffer Lifecycle { get; }

        int Version { get; }
    }
}

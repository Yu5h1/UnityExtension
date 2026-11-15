using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   One entity's simulated state, delivered asynchronously some frames after the request
    ///   (ParticleWorld WORLD-14). <see cref="Step"/> is the world step count when the request was
    ///   made; the data reflects the last completed step before it, without changes still queued
    ///   for the next step. Arrays are indexed like the entity's own particles, constraints and
    ///   rigid groups. When <see cref="Succeeded"/> is false, <see cref="Error"/> says why and the
    ///   arrays are empty.
    /// </summary>
    public sealed class ParticleReadback
    {
        public ParticleEntity Entity { get; internal set; }
        public int Step { get; internal set; }
        public bool Succeeded { get; internal set; }
        public string Error { get; internal set; }
        public Vector3[] Positions { get; internal set; } = System.Array.Empty<Vector3>();
        public Vector3[] Velocities { get; internal set; } = System.Array.Empty<Vector3>();
        public bool[] BrokenConstraints { get; internal set; } = System.Array.Empty<bool>();
        public Quaternion[] RigidRotations { get; internal set; } = System.Array.Empty<Quaternion>();
    }
}

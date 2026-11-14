using UnityEngine;

namespace Yu5h1Lib
{
    public enum ParticleColliderShape
    {
        Sphere,
        Capsule,
        Box
    }

    /// <summary>
    ///   A collider as the solver sees it for one step: its shape in world space at the start of the
    ///   step, the particle categories it affects, and the surface velocity measured from its previous
    ///   step (ParticleSolver SOLVER-10 to SOLVER-14). Unused fields of a shape stay zero.
    /// </summary>
    public struct ParticleColliderState
    {
        public ParticleColliderShape shape;
        public uint mask;
        public Vector3 center;
        public float radius;
        public Vector3 pointA;
        public Vector3 pointB;
        public Vector3 halfExtents;
        public Vector3 axisX;
        public Vector3 axisY;
        public Vector3 axisZ;
        public Vector3 linearVelocity;
        public Vector3 angularVelocity;

        /// <summary>
        ///   Surface velocity of the collider at a world-space point: linear velocity plus the
        ///   rotational part about the collider's center.
        /// </summary>
        public Vector3 SurfaceVelocity(Vector3 point) =>
            linearVelocity + Vector3.Cross(angularVelocity, point - center);
    }
}

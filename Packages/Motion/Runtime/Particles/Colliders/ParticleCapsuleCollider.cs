using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   Capsule along the Transform's local Y axis, the size of Unity's capsule primitive: radius
    ///   0.5 × max(scale.x, scale.z), segment half-length max(scale.y, radius) − radius
    ///   (ParticleWorld WORLD-15).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ParticleCapsuleCollider : ParticleCollider
    {
        public override ParticleColliderShape Shape => ParticleColliderShape.Capsule;

        protected override void BuildShape(ref ParticleColliderState state)
        {
            Vector3 scale = AbsoluteScale(transform);
            float radius = 0.5f * Mathf.Max(scale.x, scale.z);
            float halfSegment = Mathf.Max(scale.y, radius) - radius;
            Vector3 center = transform.position;
            Vector3 up = transform.up;
            state.center = center;
            state.radius = radius;
            state.pointA = center - up * halfSegment;
            state.pointB = center + up * halfSegment;
        }
    }
}

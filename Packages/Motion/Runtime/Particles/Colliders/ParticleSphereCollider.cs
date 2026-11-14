using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   Sphere centered on the Transform with a radius of half the largest absolute scale axis, the
    ///   size of Unity's sphere primitive (ParticleWorld WORLD-15).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ParticleSphereCollider : ParticleCollider
    {
        public override ParticleColliderShape Shape => ParticleColliderShape.Sphere;

        protected override void BuildShape(ref ParticleColliderState state)
        {
            Vector3 scale = AbsoluteScale(transform);
            state.center = transform.position;
            state.radius = 0.5f * Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
        }
    }
}

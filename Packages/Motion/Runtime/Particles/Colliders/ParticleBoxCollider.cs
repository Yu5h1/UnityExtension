using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   Oriented box centered on the Transform with half extents of half the absolute scale and the
    ///   Transform's right, up and forward as axes, the size of Unity's cube primitive
    ///   (ParticleWorld WORLD-15).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ParticleBoxCollider : ParticleCollider
    {
        public override ParticleColliderShape Shape => ParticleColliderShape.Box;

        protected override void BuildShape(ref ParticleColliderState state)
        {
            state.center = transform.position;
            state.halfExtents = AbsoluteScale(transform) * 0.5f;
            state.axisX = transform.right;
            state.axisY = transform.up;
            state.axisZ = transform.forward;
        }
    }
}

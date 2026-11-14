using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   A mesh bent along a chain of control particles, such as a fish body. It needs a chain
    ///   layout of at least three particles and a material whose vertex shader reads the control
    ///   points.
    /// </summary>
    public sealed class ArticulatedMeshAppearance : PhysicsParticleAppearance
    {
        [SerializeField]
        Mesh _mesh;
        [SerializeField]
        Vector3 _forwardAxis = Vector3.forward;
        [SerializeField]
        bool _flipForward;

        public Mesh Mesh
        {
            get => _mesh;
            set => _mesh = value;
        }

        public Vector3 ForwardAxis
        {
            get => _forwardAxis;
            set => _forwardAxis = value;
        }

        public bool FlipForward
        {
            get => _flipForward;
            set => _flipForward = value;
        }

        public override void Validate(
            PhysicsParticleLayout layout,
            PhysicsParticleInteraction interaction,
            List<PhysicsParticleMessage> messages)
        {
            base.Validate(layout, interaction, messages);
            if (_mesh == null)
                messages.Add(PhysicsParticleMessage.Error($"{name}: no mesh."));
            if (layout == null)
                return;
            if (layout.Kind != ParticleLayoutKind.Chain)
                messages.Add(PhysicsParticleMessage.Error($"{name}: an articulated mesh needs a chain layout, not {layout.Kind}."));
            else
                for (int v = 0; v < layout.VariantCount; v++)
                    if (layout.GetParticleCount(v) < 3)
                        messages.Add(PhysicsParticleMessage.Error($"{name}: an articulated mesh needs at least 3 control particles; variant {v} has {layout.GetParticleCount(v)}."));
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   A mesh drawn at the entity's rigid pose, oriented by a forward axis and optionally fitted to
    ///   the layout's dimensions. Suited to rigid groups; other interactions only move the pose.
    /// </summary>
    public sealed class AuthoredMeshAppearance : PhysicsParticleAppearance
    {
        [SerializeField]
        Mesh _mesh;
        [SerializeField]
        Vector3 _forwardAxis = Vector3.forward;
        [SerializeField]
        bool _flipForward;
        [SerializeField]
        bool _fitToDimensions;

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

        public bool FitToDimensions
        {
            get => _fitToDimensions;
            set => _fitToDimensions = value;
        }

        public override void Validate(
            PhysicsParticleLayout layout,
            PhysicsParticleInteraction interaction,
            List<PhysicsParticleMessage> messages)
        {
            base.Validate(layout, interaction, messages);
            if (_mesh == null)
                messages.Add(PhysicsParticleMessage.Error($"{name}: no mesh."));
            if (_forwardAxis.sqrMagnitude == 0f)
                messages.Add(PhysicsParticleMessage.Warning($"{name}: forward axis is zero."));
            if (interaction != null && !(interaction is RigidGroupInteraction))
                messages.Add(PhysicsParticleMessage.Warning(
                    $"{name}: an authored mesh follows only the entity pose; with {interaction.GetType().Name} it will not deform."));
        }
    }
}

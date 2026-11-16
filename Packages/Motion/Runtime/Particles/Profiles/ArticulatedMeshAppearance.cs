using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   A mesh bent along a chain of control particles, such as a fish body (ParticleRendering
    ///   REND-05). It needs a chain or body layout of at least three particles per body and a
    ///   material whose vertex shader reads the control points. The head is the end of the mesh in the positive
    ///   <see cref="ForwardAxis"/> direction (the dominant axis of the vector; a negative component
    ///   or <see cref="FlipForward"/> turns the mesh end for end, both together cancel).
    ///   <see cref="FitMeshToDimensions"/> scales the mesh to each body's dimensions and
    ///   <see cref="VisualScale"/> multiplies on top.
    /// </summary>
    public sealed class ArticulatedMeshAppearance : PhysicsParticleAppearance
    {
        [SerializeField]
        Mesh _mesh;
        [SerializeField]
        Vector3 _forwardAxis = Vector3.forward;
        [SerializeField]
        bool _flipForward;
        [Tooltip("Scale the mesh to the body's dimensions times its scale.")]
        [SerializeField]
        bool _fitMeshToDimensions = true;
        [SerializeField]
        Vector3 _visualScale = Vector3.one;

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

        public bool FitMeshToDimensions
        {
            get => _fitMeshToDimensions;
            set => _fitMeshToDimensions = value;
        }

        public Vector3 VisualScale
        {
            get => _visualScale;
            set => _visualScale = ParticleMeshAxis.ClampScale(value);
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
            if (layout.Kind == ParticleLayoutKind.Grid)
                messages.Add(PhysicsParticleMessage.Error($"{name}: an articulated mesh needs a chain or body layout, not {layout.Kind}."));
            else
                for (int v = 0; v < layout.VariantCount; v++)
                    if (layout.GetParticleCount(v) < 3)
                        messages.Add(PhysicsParticleMessage.Error($"{name}: an articulated mesh needs at least 3 control particles; variant {v} has {layout.GetParticleCount(v)}."));
        }
    }
}

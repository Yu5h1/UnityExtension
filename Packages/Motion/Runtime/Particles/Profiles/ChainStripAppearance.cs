using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   A camera-facing strip along a chain entity, for hoses and ropes (ParticleRendering
    ///   REND-04): two vertices per particle, opened along cross(tangent, view). The shading is a
    ///   single colour darkened towards the edges, unlit. Shadows are off by default, like the rope
    ///   of Unified-Solver, which has no shadow pass. The material must use the
    ///   Yu5h1Lib/Particles/ChainStrip shader or one that reads the same particle data.
    /// </summary>
    public sealed class ChainStripAppearance : PhysicsParticleAppearance
    {
        [Tooltip("Distance from the centre line to each edge of the strip, in meters (the original rope width).")]
        [Min(0.0001f)]
        [SerializeField]
        float _width = 0.03f;

        public ChainStripAppearance()
        {
            Color = new Color(0.8f, 0.6f, 0.2f, 1f);
            CastShadows = false;
        }

        public float Width
        {
            get => _width;
            set => _width = Mathf.Max(0.0001f, value);
        }

        public override void Validate(
            PhysicsParticleLayout layout,
            PhysicsParticleInteraction interaction,
            List<PhysicsParticleMessage> messages)
        {
            base.Validate(layout, interaction, messages);
            if (layout == null)
                return;
            if (layout.Kind != ParticleLayoutKind.Chain)
            {
                messages.Add(PhysicsParticleMessage.Error($"{name}: a chain strip needs a chain layout, not {layout.Kind}."));
                return;
            }
            for (int v = 0; v < layout.VariantCount; v++)
                if (layout.GetParticleCount(v) < 2)
                    messages.Add(PhysicsParticleMessage.Error($"{name}: a chain strip needs at least 2 particles; variant {v} has {layout.GetParticleCount(v)}."));
        }
    }
}

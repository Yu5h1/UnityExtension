using System.Collections.Generic;

namespace Yu5h1Lib
{
    /// <summary>
    ///   Draws the convex hull of the layout's rest particles at the entity's rigid pose. The hull is
    ///   read from the layout; the layout does not know it is drawn this way.
    /// </summary>
    public sealed class ParticleHullAppearance : PhysicsParticleAppearance
    {
        public override void Validate(
            PhysicsParticleLayout layout,
            PhysicsParticleInteraction interaction,
            List<PhysicsParticleMessage> messages)
        {
            base.Validate(layout, interaction, messages);
            if (layout != null)
                for (int v = 0; v < layout.VariantCount; v++)
                    if (layout.GetParticleCount(v) < 4)
                        messages.Add(PhysicsParticleMessage.Error($"{name}: a hull needs at least 4 particles; variant {v} has {layout.GetParticleCount(v)}."));
            if (interaction != null && !(interaction is RigidGroupInteraction))
                messages.Add(PhysicsParticleMessage.Warning(
                    $"{name}: a hull follows only the entity pose; with {interaction.GetType().Name} it will not deform."));
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   The arrangement a layout produces, which interactions and appearances declare they accept
    ///   so incompatible profiles are reported in the editor instead of silently doing nothing.
    /// </summary>
    public enum ParticleLayoutKind
    {
        Points,
        Chain,
        Grid
    }

    /// <summary>
    ///   Rest arrangement of one entity's particles: how many, where in the entity's local frame,
    ///   and each particle's role. A layout knows nothing about the constraints that join its
    ///   particles or how they are drawn; interaction and appearance read it, never the reverse.
    ///   A layout may offer several variants (for example 4, 6 and 8 particle fragments); the
    ///   driver picks one per entity.
    /// </summary>
    public abstract class PhysicsParticleLayout : ScriptableObject
    {
        public abstract ParticleLayoutKind Kind { get; }

        public virtual int VariantCount => 1;

        public abstract int GetParticleCount(int variant);

        /// <summary>
        ///   Clears <paramref name="positions"/> and fills it with the variant's rest positions in
        ///   the entity's local frame, in meters.
        /// </summary>
        public abstract void GetRestPositions(int variant, List<Vector3> positions);

        /// <summary>
        ///   Role of a particle in the layout, such as grid column and row or chain index.
        /// </summary>
        public virtual Vector3Int GetRole(int variant, int index) => new Vector3Int(index, 0, 0);

        /// <summary>
        ///   Adds layout errors and warnings to <paramref name="messages"/>.
        /// </summary>
        public virtual void Validate(List<PhysicsParticleMessage> messages)
        {
            if (VariantCount < 1)
                messages.Add(PhysicsParticleMessage.Error($"{name}: the layout has no variants."));
            for (int v = 0; v < VariantCount; v++)
                if (GetParticleCount(v) < 1)
                    messages.Add(PhysicsParticleMessage.Error($"{name}: variant {v} has no particles."));
        }
    }
}

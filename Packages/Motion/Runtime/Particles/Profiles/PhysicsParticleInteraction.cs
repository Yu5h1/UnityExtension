using System;
using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   Stiffness, damping and break force shared by the distance constraints a recipe creates
    ///   (ParticleSolver SOLVER-03, SOLVER-04). Compliance is in m/N; 0 is rigid. Break force is in
    ///   newtons; 0 never breaks.
    /// </summary>
    [Serializable]
    public sealed class ParticleConstraintMaterial
    {
        [Min(0f)]
        public float compliance = 0f;
        [Min(0f)]
        public float damping = 0f;
        [Min(0f)]
        public float breakForce = 0f;

        internal DistanceConstraintSpec Join(int a, int b) => new DistanceConstraintSpec
        {
            a = a,
            b = b,
            restLength = float.NaN,
            compliance = compliance,
            damping = damping,
            breakForce = breakForce
        };
    }

    /// <summary>
    ///   Physical relations of one entity built on a layout: per-particle mass and radius, whether
    ///   its particles collide with each other, and which constraints and rigid groups join them.
    ///   Concrete recipes declare the layout kinds they accept; the driver reports any other kind
    ///   as an editor error.
    /// </summary>
    public abstract class PhysicsParticleInteraction : ScriptableObject
    {
        [Tooltip("Mass of each particle, in kilograms. 0 pins every particle.")]
        [Min(0f)]
        [SerializeField]
        float _particleMass = 1f;
        [Tooltip("Radius of each particle, in meters.")]
        [Min(0.0001f)]
        [SerializeField]
        float _particleRadius = 0.1f;
        [Tooltip("When off, the entity's particles get a group of their own and skip each other.")]
        [SerializeField]
        bool _selfCollision;

        public float ParticleMass
        {
            get => _particleMass;
            set => _particleMass = Mathf.Max(0f, value);
        }

        public float ParticleRadius
        {
            get => _particleRadius;
            set => _particleRadius = Mathf.Max(0.0001f, value);
        }

        public bool SelfCollision
        {
            get => _selfCollision;
            set => _selfCollision = value;
        }

        public abstract bool Accepts(ParticleLayoutKind kind);

        /// <summary>
        ///   Adds the constraints and rigid groups joining one entity of
        ///   <paramref name="layout"/>'s <paramref name="variant"/>, indexed inside the entity.
        /// </summary>
        public abstract void Build(PhysicsParticleLayout layout, int variant, ParticleEntityRequest request);

        public virtual void Validate(PhysicsParticleLayout layout, List<PhysicsParticleMessage> messages)
        {
            if (layout != null && !Accepts(layout.Kind))
                messages.Add(PhysicsParticleMessage.Error($"{name} does not accept a {layout.Kind} layout ({layout.name})."));
            if (_particleMass == 0f)
                messages.Add(PhysicsParticleMessage.Info($"{name}: particle mass is 0, so every particle is pinned."));
        }

        protected static void ValidateMaterial(ParticleConstraintMaterial material, string owner, List<PhysicsParticleMessage> messages)
        {
            if (material == null)
                messages.Add(PhysicsParticleMessage.Error($"{owner}: the constraint material is missing."));
            else if (material.damping > 0f && material.compliance == 0f)
                messages.Add(PhysicsParticleMessage.Warning($"{owner}: damping has no effect while compliance is 0."));
        }
    }
}

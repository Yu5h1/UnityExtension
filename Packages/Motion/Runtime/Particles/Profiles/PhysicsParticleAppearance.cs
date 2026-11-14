using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   Everything visible about an entity: where its triangles come from and how they follow the
    ///   particles (the concrete type), and how they are shaded (<see cref="Material"/>). It reads
    ///   the simulation and the layout and never writes physical state. Drawing is implemented in a
    ///   later stage; this profile already declares what it needs so incompatible setups are caught
    ///   in the editor.
    /// </summary>
    public abstract class PhysicsParticleAppearance : ScriptableObject
    {
        [SerializeField]
        Material _material;
        [SerializeField]
        Color _color = Color.white;

        public Material Material
        {
            get => _material;
            set => _material = value;
        }

        public Color Color
        {
            get => _color;
            set => _color = value;
        }

        public virtual void Validate(
            PhysicsParticleLayout layout,
            PhysicsParticleInteraction interaction,
            List<PhysicsParticleMessage> messages)
        {
            if (_material == null)
                messages.Add(PhysicsParticleMessage.Error($"{name}: no material."));
        }
    }
}

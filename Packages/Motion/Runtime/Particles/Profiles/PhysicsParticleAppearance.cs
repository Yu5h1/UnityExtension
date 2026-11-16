using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   Everything visible about an entity: where its triangles come from and how they follow the
    ///   particles (the concrete type), and how they are shaded (<see cref="Material"/>). It reads
    ///   the simulation and the layout and never writes physical state. PhysicsParticleRenderer
    ///   draws it (ParticleRendering REND-01); the profile also declares what it needs so
    ///   incompatible setups are caught in the editor. The draw layer is the source GameObject's.
    /// </summary>
    public abstract class PhysicsParticleAppearance : ScriptableObject
    {
        [SerializeField]
        Material _material;
        [SerializeField]
        Color _color = Color.white;
        [Tooltip("Whether the entities cast shadows (ParticleRendering REND-01).")]
        [SerializeField]
        bool _castShadows = true;
        [Tooltip("Whether the entities receive shadows (ParticleRendering REND-01).")]
        [SerializeField]
        bool _receiveShadows = true;

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

        public bool CastShadows
        {
            get => _castShadows;
            set => _castShadows = value;
        }

        public bool ReceiveShadows
        {
            get => _receiveShadows;
            set => _receiveShadows = value;
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

using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   Rope and hose recipe for a chain: a link between neighbours and, optionally, a bend link
    ///   between every particle and the one two places on.
    /// </summary>
    public sealed class ChainInteraction : PhysicsParticleInteraction
    {
        [SerializeField]
        bool _bend = true;
        [SerializeField]
        ParticleConstraintMaterial _material = new ParticleConstraintMaterial();
        [SerializeField]
        ParticleConstraintMaterial _bendMaterial = new ParticleConstraintMaterial { compliance = 1e-4f };

        public bool Bend
        {
            get => _bend;
            set => _bend = value;
        }

        public ParticleConstraintMaterial Material => _material;
        public ParticleConstraintMaterial BendMaterial => _bendMaterial;

        public override bool Accepts(ParticleLayoutKind kind) => kind == ParticleLayoutKind.Chain;

        public override void Build(PhysicsParticleLayout layout, int variant, ParticleEntityRequest request)
        {
            int count = layout.GetParticleCount(variant);
            for (int i = 0; i + 1 < count; i++)
                request.constraints.Add(_material.Join(i, i + 1));
            if (!_bend)
                return;
            for (int i = 0; i + 2 < count; i++)
                request.constraints.Add(_bendMaterial.Join(i, i + 2));
        }

        public override void Validate(PhysicsParticleLayout layout, List<PhysicsParticleMessage> messages)
        {
            base.Validate(layout, messages);
            ValidateMaterial(_material, name, messages);
            if (_bend)
                ValidateMaterial(_bendMaterial, name + " bend", messages);
        }
    }
}

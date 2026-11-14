using System;
using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   Authored point sets, one per variant, for fragments and other unstructured bodies. One
    ///   batch can mix variants of different particle counts.
    /// </summary>
    public sealed class PointParticleLayout : PhysicsParticleLayout
    {
        [Serializable]
        public sealed class Variant
        {
            public Vector3[] positions = { Vector3.zero };
        }

        [SerializeField]
        Variant[] _variants = { new Variant() };

        public Variant[] Variants
        {
            get => _variants;
            set => _variants = value ?? Array.Empty<Variant>();
        }

        public override ParticleLayoutKind Kind => ParticleLayoutKind.Points;

        public override int VariantCount => _variants.Length;

        public override int GetParticleCount(int variant) => _variants[variant].positions?.Length ?? 0;

        public override void GetRestPositions(int variant, List<Vector3> positions)
        {
            positions.Clear();
            if (_variants[variant].positions != null)
                positions.AddRange(_variants[variant].positions);
        }
    }
}

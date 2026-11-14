using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   Particles in a line from the origin along a local direction, for ropes, hoses and fish
    ///   bodies. The role is (index, 0, 0).
    /// </summary>
    public sealed class ChainParticleLayout : PhysicsParticleLayout
    {
        [Min(1)]
        [SerializeField]
        int _count = 10;
        [Min(0.0001f)]
        [SerializeField]
        float _spacing = 0.1f;
        [SerializeField]
        Vector3 _direction = Vector3.down;

        public int Count
        {
            get => _count;
            set => _count = Mathf.Max(1, value);
        }

        public float Spacing
        {
            get => _spacing;
            set => _spacing = Mathf.Max(0.0001f, value);
        }

        public Vector3 Direction
        {
            get => _direction;
            set => _direction = value;
        }

        public override ParticleLayoutKind Kind => ParticleLayoutKind.Chain;

        public override int GetParticleCount(int variant) => _count;

        public override void GetRestPositions(int variant, List<Vector3> positions)
        {
            positions.Clear();
            Vector3 step = _direction.sqrMagnitude > 0f ? _direction.normalized * _spacing : Vector3.down * _spacing;
            for (int i = 0; i < _count; i++)
                positions.Add(step * i);
        }

        public override void Validate(List<PhysicsParticleMessage> messages)
        {
            base.Validate(messages);
            if (_direction.sqrMagnitude == 0f)
                messages.Add(PhysicsParticleMessage.Warning($"{name}: direction is zero; the chain hangs along -Y."));
        }
    }
}

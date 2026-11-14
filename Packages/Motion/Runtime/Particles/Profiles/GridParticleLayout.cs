using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   Columns × rows lattice in the local XY plane, centered on the origin, for cloth and nets.
    ///   Particle index is row × columns + column; the role is (column, row, 0).
    /// </summary>
    public sealed class GridParticleLayout : PhysicsParticleLayout
    {
        [Min(1)]
        [SerializeField]
        int _columns = 10;
        [Min(1)]
        [SerializeField]
        int _rows = 10;
        [Min(0.0001f)]
        [SerializeField]
        float _spacing = 0.1f;

        public int Columns
        {
            get => _columns;
            set => _columns = Mathf.Max(1, value);
        }

        public int Rows
        {
            get => _rows;
            set => _rows = Mathf.Max(1, value);
        }

        public float Spacing
        {
            get => _spacing;
            set => _spacing = Mathf.Max(0.0001f, value);
        }

        public override ParticleLayoutKind Kind => ParticleLayoutKind.Grid;

        public override int GetParticleCount(int variant) => _columns * _rows;

        public override void GetRestPositions(int variant, List<Vector3> positions)
        {
            positions.Clear();
            Vector3 offset = new Vector3((_columns - 1) * _spacing * 0.5f, (_rows - 1) * _spacing * 0.5f, 0f);
            for (int y = 0; y < _rows; y++)
                for (int x = 0; x < _columns; x++)
                    positions.Add(new Vector3(x * _spacing, y * _spacing, 0f) - offset);
        }

        public override Vector3Int GetRole(int variant, int index) =>
            new Vector3Int(index % _columns, index / _columns, 0);
    }
}

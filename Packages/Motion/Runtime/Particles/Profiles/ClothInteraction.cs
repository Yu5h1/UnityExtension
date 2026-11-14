using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   Cloth and net recipe for a grid: structural links to the right and upper neighbour, optional
    ///   shear diagonals, and optional bend links two cells apart, all with one material.
    /// </summary>
    public sealed class ClothInteraction : PhysicsParticleInteraction
    {
        [SerializeField]
        bool _shear = true;
        [SerializeField]
        bool _bend;
        [SerializeField]
        ParticleConstraintMaterial _material = new ParticleConstraintMaterial();

        public bool Shear
        {
            get => _shear;
            set => _shear = value;
        }

        public bool Bend
        {
            get => _bend;
            set => _bend = value;
        }

        public ParticleConstraintMaterial Material => _material;

        public override bool Accepts(ParticleLayoutKind kind) => kind == ParticleLayoutKind.Grid;

        public override void Build(PhysicsParticleLayout layout, int variant, ParticleEntityRequest request)
        {
            var grid = (GridParticleLayout)layout;
            int columns = grid.Columns;
            int rows = grid.Rows;
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    int a = y * columns + x;
                    if (x + 1 < columns)
                        request.constraints.Add(_material.Join(a, a + 1));
                    if (y + 1 < rows)
                        request.constraints.Add(_material.Join(a, a + columns));
                    if (_shear && x + 1 < columns && y + 1 < rows)
                        request.constraints.Add(_material.Join(a, a + columns + 1));
                    if (_shear && x > 0 && y + 1 < rows)
                        request.constraints.Add(_material.Join(a, a + columns - 1));
                    if (_bend && x + 2 < columns)
                        request.constraints.Add(_material.Join(a, a + 2));
                    if (_bend && y + 2 < rows)
                        request.constraints.Add(_material.Join(a, a + 2 * columns));
                }
            }
        }

        public override void Validate(PhysicsParticleLayout layout, List<PhysicsParticleMessage> messages)
        {
            base.Validate(layout, messages);
            ValidateMaterial(_material, name, messages);
            if (SelfCollision && layout is GridParticleLayout grid && grid.Spacing < 2f * ParticleRadius)
                messages.Add(PhysicsParticleMessage.Warning(
                    $"{name}: self collision with spacing {grid.Spacing} below twice the radius {ParticleRadius} makes neighbours push apart and wrinkle."));
        }
    }
}

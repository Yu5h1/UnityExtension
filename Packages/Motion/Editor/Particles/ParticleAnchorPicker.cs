using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>
    ///   Scene-view picking state for one <see cref="PhysicsParticle"/> source
    ///   (ParticleExtensions EXT-06, anchor authoring tool). Rest positions and grid lines are
    ///   rebuilt only when the source's layout, count or Transform changes (<see cref="Ensure"/>);
    ///   <see cref="Process"/> runs one nearest-particle search per mouse-move or mouse-down event
    ///   and does nothing on any other event, so repaints never pick. Particles are addressed by
    ///   flat number; only a <see cref="GridParticleLayout"/> yields grid lines, any other layout
    ///   is points.
    /// </summary>
    public sealed class ParticleAnchorPicker
    {
        public const float PickRadiusPixels = 18f;

        struct Key : IEquatable<Key>
        {
            public PhysicsParticleLayout layout;
            public int columns;
            public int rows;
            public float spacing;
            public int variants;
            public int flatCount;
            public int count;
            public Vector3 batchSpacing;
            public Matrix4x4 matrix;

            public bool Equals(Key other) =>
                layout == other.layout
                && columns == other.columns
                && rows == other.rows
                && spacing == other.spacing
                && variants == other.variants
                && flatCount == other.flatCount
                && count == other.count
                && batchSpacing == other.batchSpacing
                && matrix == other.matrix;

            public override bool Equals(object obj) => obj is Key other && Equals(other);

            public override int GetHashCode() => flatCount;
        }

        readonly List<Vector3> _local = new List<Vector3>();
        readonly List<Vector3[]> _lines = new List<Vector3[]>();
        Vector3[] _positions = Array.Empty<Vector3>();
        PhysicsParticle _source;
        Key _key;
        bool _valid;

        /// <summary>
        ///   How many times the rest positions and grid lines were rebuilt.
        /// </summary>
        public int RebuildCount { get; private set; }

        /// <summary>
        ///   How many nearest-particle searches ran.
        /// </summary>
        public int SearchCount { get; private set; }

        /// <summary>
        ///   Flat number of the particle under the mouse, or -1.
        /// </summary>
        public int Hovered { get; private set; } = -1;

        public int Count { get; private set; }

        public IReadOnlyList<Vector3> RestPositions => _positions;

        /// <summary>
        ///   Polylines of the grid layout, one per row and column of every entity; empty for a
        ///   layout that is not a grid.
        /// </summary>
        public IReadOnlyList<Vector3[]> Lines => _lines;

        public void Invalidate()
        {
            _valid = false;
            Hovered = -1;
        }

        /// <summary>
        ///   Makes the cache match <paramref name="source"/>, rebuilding only when its layout,
        ///   count or Transform changed since the last call. Returns false when the source has no
        ///   particles.
        /// </summary>
        public bool Ensure(PhysicsParticle source)
        {
            if (source == null)
                return false;
            Key key = MakeKey(source);
            if (key.flatCount <= 0)
            {
                Count = 0;
                _valid = false;
                return false;
            }
            if (_valid && source == _source && key.Equals(_key))
                return true;
            Build(source, key);
            return true;
        }

        /// <summary>
        ///   Runs the nearest-particle search for a mouse-move or mouse-down event and returns
        ///   whether the hovered particle changed. Every other event type, repaint included, does
        ///   no work.
        /// </summary>
        public bool Process(EventType type, Vector2 mouse) =>
            Process(type, mouse, HandleUtility.WorldToGUIPoint);

        public bool Process(EventType type, Vector2 mouse, Func<Vector3, Vector2> project)
        {
            if (type != EventType.MouseMove && type != EventType.MouseDown)
                return false;
            int found = FindNearest(mouse, project);
            if (found == Hovered)
                return false;
            Hovered = found;
            return true;
        }

        /// <summary>
        ///   Flat number of the particle whose projected position is nearest to
        ///   <paramref name="mouse"/> within <see cref="PickRadiusPixels"/>, or -1.
        /// </summary>
        public int FindNearest(Vector2 mouse, Func<Vector3, Vector2> project)
        {
            SearchCount++;
            int best = -1;
            float bestDistance = PickRadiusPixels * PickRadiusPixels;
            for (int i = 0; i < Count; i++)
            {
                float distance = (project(_positions[i]) - mouse).sqrMagnitude;
                if (distance >= bestDistance)
                    continue;
                bestDistance = distance;
                best = i;
            }
            return best;
        }

        /// <summary>
        ///   Label for a particle: its (column, row) in a grid layout, otherwise its flat number;
        ///   the entity number is added when the driver spawns more than one.
        /// </summary>
        public string Describe(int flatIndex)
        {
            if (_source == null || !_source.TryLocate(flatIndex, out int entity, out int particle))
                return flatIndex.ToString();
            string text = flatIndex.ToString();
            PhysicsParticleLayout layout = _source.Layout;
            if (layout.Kind == ParticleLayoutKind.Grid)
            {
                Vector3Int role = layout.GetRole(entity % layout.VariantCount, particle);
                text = $"({role.x}, {role.y})";
            }
            return _source.Count > 1 ? $"#{entity} {text}" : text;
        }

        static Key MakeKey(PhysicsParticle source)
        {
            PhysicsParticleLayout layout = source.Layout;
            var key = new Key
            {
                layout = layout,
                flatCount = source.FlatParticleCount,
                count = source.Count,
                batchSpacing = source.BatchSpacing,
                matrix = source.transform.localToWorldMatrix
            };
            if (layout == null)
                return key;
            key.variants = layout.VariantCount;
            if (layout is GridParticleLayout grid)
            {
                key.columns = grid.Columns;
                key.rows = grid.Rows;
                key.spacing = grid.Spacing;
            }
            return key;
        }

        void Build(PhysicsParticle source, Key key)
        {
            RebuildCount++;
            _source = source;
            _key = key;
            _valid = true;
            Hovered = -1;
            Count = key.flatCount;
            _positions = new Vector3[Count];
            _lines.Clear();
            PhysicsParticleLayout layout = source.Layout;
            var grid = layout as GridParticleLayout;
            int start = 0;
            for (int entity = 0; entity < source.Count; entity++)
            {
                int variant = entity % layout.VariantCount;
                layout.GetRestPositions(variant, _local);
                Pose pose = source.SpawnPose(entity);
                for (int i = 0; i < _local.Count; i++)
                    _positions[start + i] = pose.position + pose.rotation * _local[i];
                if (grid != null && grid.Columns * grid.Rows == _local.Count)
                    AddGridLines(start, grid.Columns, grid.Rows);
                start += _local.Count;
            }
        }

        void AddGridLines(int start, int columns, int rows)
        {
            if (columns <= 1 && rows <= 1)
                return;
            if (columns > 1)
                for (int y = 0; y < rows; y++)
                {
                    var row = new Vector3[columns];
                    Array.Copy(_positions, start + y * columns, row, 0, columns);
                    _lines.Add(row);
                }
            if (rows > 1)
                for (int x = 0; x < columns; x++)
                {
                    var column = new Vector3[rows];
                    for (int y = 0; y < rows; y++)
                        column[y] = _positions[start + y * columns + x];
                    _lines.Add(column);
                }
        }
    }
}

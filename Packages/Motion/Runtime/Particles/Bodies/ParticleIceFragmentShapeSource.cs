using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   A library of ice fragments: tetrahedra, octahedra and boxes, each scaled, stretched along one
    ///   axis and jittered per template from a seed (ParticleExtensions EXT-10; Unified-Solver's
    ///   SolverIceFragmentShapeSource, same fields and defaults). The template is a pure function of
    ///   its index and the seed, so spawning and drawing rebuild identical vertices.
    /// </summary>
    public sealed class ParticleIceFragmentShapeSource : ParticleBodyShapeSource
    {
        [Tooltip("Distinct shapes in the library, and the draw call count.")]
        [Range(1, 64)]
        [SerializeField]
        int _templateCount = 24;
        [Tooltip("The same seed always rebuilds the same library.")]
        [SerializeField]
        int _seed = 7;
        [Tooltip("Tetrahedron: the sharpest, cheapest shard.")]
        [Min(0f)]
        [SerializeField]
        float _weight4 = 1f;
        [Tooltip("Octahedron: the middle size.")]
        [Min(0f)]
        [SerializeField]
        float _weight6 = 1f;
        [Tooltip("Box: bulkiest, and best at covering its own volume with particles.")]
        [Min(0f)]
        [SerializeField]
        float _weight8 = 1f;
        [Tooltip("Fraction of the half extent. 0 gives clean regular solids.")]
        [Range(0f, ParticleHullShapes.MaximumJitter)]
        [SerializeField]
        float _jitter = 0.25f;
        [Tooltip("Multiplier range on the base dimensions across the library.")]
        [SerializeField]
        Vector2 _sizeRange = new Vector2(0.6f, 1.4f);
        [Tooltip("Stretch along one random axis, so the pile is not all equally chunky.")]
        [Range(0f, 1f)]
        [SerializeField]
        float _stretch = 0.3f;

        public int Seed
        {
            get => _seed;
            set => _seed = value;
        }

        public float Weight4
        {
            get => _weight4;
            set => _weight4 = Mathf.Max(0f, value);
        }

        public float Weight6
        {
            get => _weight6;
            set => _weight6 = Mathf.Max(0f, value);
        }

        public float Weight8
        {
            get => _weight8;
            set => _weight8 = Mathf.Max(0f, value);
        }

        public float Jitter
        {
            get => _jitter;
            set => _jitter = Mathf.Clamp(value, 0f, ParticleHullShapes.MaximumJitter);
        }

        public Vector2 SizeRange
        {
            get => _sizeRange;
            set => _sizeRange = new Vector2(Mathf.Max(0.01f, value.x), Mathf.Max(0.01f, value.y));
        }

        public float Stretch
        {
            get => _stretch;
            set => _stretch = Mathf.Clamp01(value);
        }

        public int TemplateCountSetting
        {
            get => _templateCount;
            set => _templateCount = Mathf.Clamp(value, 1, 64);
        }

        public override int TemplateCount => Mathf.Max(1, _templateCount);

        public override int MaximumParticles => 8;

        public override ParticleBodyTopology BuildTemplate(int templateIndex, Vector3 dimensions, Vector3[] result, out int count)
        {
            int key = _seed * 73856093 ^ (templateIndex + 1) * 19349663;

            ParticleBodyTopology topology = PickVariant(Random01(key, 0u));
            Vector3[] baseVertices = ParticleHullShapes.BaseVertices(topology);
            count = baseVertices.Length;

            float size = Mathf.Lerp(Mathf.Min(_sizeRange.x, _sizeRange.y), Mathf.Max(_sizeRange.x, _sizeRange.y), Random01(key, 1u));
            Vector3 halfExtents = dimensions * (0.5f * size);

            int stretchAxis = (int)(Random01(key, 2u) * 2.999f);
            float stretchAmount = 1f + _stretch * Random01(key, 3u);
            halfExtents[stretchAxis] *= stretchAmount;

            float bounded = Mathf.Clamp(_jitter, 0f, ParticleHullShapes.MaximumJitter);
            for (int i = 0; i < count; i++)
            {
                Vector3 vertex = Vector3.Scale(baseVertices[i], halfExtents);
                uint corner = (uint)(i * 3 + 8);
                result[i] = vertex + new Vector3(
                    halfExtents.x * bounded * Signed(key, corner),
                    halfExtents.y * bounded * Signed(key, corner + 1u),
                    halfExtents.z * bounded * Signed(key, corner + 2u));
            }
            return topology;
        }

        ParticleBodyTopology PickVariant(float random)
        {
            float w4 = Mathf.Max(0f, _weight4);
            float w6 = Mathf.Max(0f, _weight6);
            float w8 = Mathf.Max(0f, _weight8);
            float total = w4 + w6 + w8;
            if (total <= 0f)
                return ParticleBodyTopology.RigidCluster8;

            float pick = random * total;
            if (pick < w4)
                return ParticleBodyTopology.RigidCluster4;
            if (pick < w4 + w6)
                return ParticleBodyTopology.RigidCluster6;
            return ParticleBodyTopology.RigidCluster8;
        }

        static float Random01(int key, uint salt)
        {
            uint value = (uint)key * 747796405u + salt * 2891336453u + 1u;
            value ^= value >> 16;
            value *= 0x7feb352du;
            value ^= value >> 15;
            value *= 0x846ca68bu;
            value ^= value >> 16;
            return (value & 0x00ffffffu) / 16777215f;
        }

        static float Signed(int key, uint salt) => Random01(key, salt) * 2f - 1f;

        void OnValidate()
        {
            _templateCount = Mathf.Clamp(_templateCount, 1, 64);
            _jitter = Mathf.Clamp(_jitter, 0f, ParticleHullShapes.MaximumJitter);
            _sizeRange = new Vector2(Mathf.Max(0.01f, _sizeRange.x), Mathf.Max(0.01f, _sizeRange.y));
        }
    }
}

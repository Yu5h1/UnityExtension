using UnityEngine;

namespace Yu5h1.UnifiedSolver
{
    [CreateAssetMenu(
        fileName = "IceFragmentShape",
        menuName = "Yu5h1/Unified Solver/Shapes/Ice Fragment")]
    public sealed class SolverIceFragmentShapeSource :
        SolverShapeSource
    {
        [Header("Variant Mix")]
        [Tooltip("Relative chance of a 4 particle fragment: a tetrahedron, the sharpest and cheapest shard.")]
        [Min(0f)]
        public float weight4 = 1f;
        [Tooltip("Relative chance of a 6 particle fragment: an octahedron, the middle size.")]
        [Min(0f)]
        public float weight6 = 1f;
        [Tooltip("Relative chance of an 8 particle fragment: a box, the bulkiest and the best at covering its own volume with particles.")]
        [Min(0f)]
        public float weight8 = 1f;

        [Header("Shape")]
        [Tooltip("How far each corner may be displaced, as a fraction of the half extent it sits at. 0 gives clean regular solids that read as manufactured; higher values read as broken ice. Bounded well inside where a corner could fall inside the hull of the others and turn the fragment inside out.")]
        [Range(0f, SolverHullShapes.MaximumJitter)]
        public float jitter = 0.25f;
        [Tooltip("Per fragment size multiplier range, applied on top of the profile's Base Dimensions. Both ends at 1 gives fragments that differ only in shape.")]
        public Vector2 sizeRange =
            new Vector2(0.6f, 1.4f);
        [Tooltip("How far a fragment may be stretched along one randomly chosen axis, so the pile is not all equally chunky. 0 keeps every fragment the shape of Base Dimensions.")]
        [Range(0f, 1f)]
        public float stretch = 0.3f;

        public override int MaximumParticles => 8;

        public override SolverParticleTopology BuildShape(
            Vector3 dimensions,
            int seed,
            Vector3[] result,
            out int count)
        {
            SolverParticleTopology topology =
                PickVariant(Random01(seed, 0u));
            Vector3[] baseVertices =
                SolverHullShapes.BaseVertices(topology);
            count = baseVertices.Length;

            // Anisotropic scale first, displacement second.
            //
            // Scaling is a linear map, so it cannot break convexity however
            // extreme it is; the displacement is what has to stay bounded. Doing
            // it in this order also makes the bound mean the same thing on every
            // axis, because it is taken against that axis' own half extent.
            float size = Mathf.Lerp(
                Mathf.Min(sizeRange.x, sizeRange.y),
                Mathf.Max(sizeRange.x, sizeRange.y),
                Random01(seed, 1u));
            Vector3 halfExtents =
                dimensions * (0.5f * size);

            int stretchAxis =
                (int)(Random01(seed, 2u) * 2.999f);
            float stretchAmount = 1f +
                stretch * Random01(seed, 3u);
            halfExtents[stretchAxis] *= stretchAmount;

            float bounded = Mathf.Clamp(
                jitter,
                0f,
                SolverHullShapes.MaximumJitter);
            for (int i = 0; i < count; i++)
            {
                Vector3 vertex = Vector3.Scale(
                    baseVertices[i],
                    halfExtents);
                uint corner = (uint)(i * 3 + 8);
                result[i] = vertex + new Vector3(
                    halfExtents.x * bounded *
                        Signed(seed, corner),
                    halfExtents.y * bounded *
                        Signed(seed, corner + 1u),
                    halfExtents.z * bounded *
                        Signed(seed, corner + 2u));
            }

            return topology;
        }

        SolverParticleTopology PickVariant(float random)
        {
            float w4 = Mathf.Max(0f, weight4);
            float w6 = Mathf.Max(0f, weight6);
            float w8 = Mathf.Max(0f, weight8);
            float total = w4 + w6 + w8;
            if (total <= 0f)
            {
                return SolverParticleTopology
                    .RigidCluster8;
            }

            float pick = random * total;
            if (pick < w4)
                return SolverParticleTopology.RigidCluster4;
            if (pick < w4 + w6)
                return SolverParticleTopology.RigidCluster6;
            return SolverParticleTopology.RigidCluster8;
        }

        // Reproducible from the seed alone, so the same emitter with the same
        // seed lays out the same pile every run. Nothing about a fragment's
        // shape is stored: the solver keeps the realized rest offsets once it
        // is spawned, and until then the seed is the whole description.
        static float Random01(int seed, uint salt)
        {
            uint value =
                (uint)seed * 747796405u +
                salt * 2891336453u + 1u;
            value ^= value >> 16;
            value *= 0x7feb352du;
            value ^= value >> 15;
            value *= 0x846ca68bu;
            value ^= value >> 16;
            return (value & 0x00ffffffu) / 16777215f;
        }

        static float Signed(int seed, uint salt)
        {
            return Random01(seed, salt) * 2f - 1f;
        }

        void OnValidate()
        {
            jitter = Mathf.Clamp(
                jitter,
                0f,
                SolverHullShapes.MaximumJitter);
            sizeRange = new Vector2(
                Mathf.Max(0.01f, sizeRange.x),
                Mathf.Max(0.01f, sizeRange.y));
        }
    }
}

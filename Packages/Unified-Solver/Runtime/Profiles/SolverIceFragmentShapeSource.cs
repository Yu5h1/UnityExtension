using UnityEngine;

namespace Yu5h1.UnifiedSolver
{
    public sealed class SolverIceFragmentShapeSource :
        SolverShapeSource
    {
        [Header("Library")]
        // Everything sharing a template draws in one batch, so this is also the
        // draw call count.
        [Tooltip("Distinct shapes in the library, and the draw call count.")]
        [Range(1, 64)]
        public int templateCount = 24;
        [Tooltip("The same seed always rebuilds the same library.")]
        public int seed = 7;

        [Header("Variant Mix")]
        [Tooltip("Tetrahedron: the sharpest, cheapest shard.")]
        [Min(0f)]
        public float weight4 = 1f;
        [Tooltip("Octahedron: the middle size.")]
        [Min(0f)]
        public float weight6 = 1f;
        [Tooltip("Box: bulkiest, and best at covering its own volume with particles.")]
        [Min(0f)]
        public float weight8 = 1f;

        [Header("Shape")]
        // Bounded well inside where a corner could fall inside the hull of the
        // others and turn the fragment inside out.
        [Tooltip("Fraction of the half extent. 0 gives clean regular solids.")]
        [Range(0f, SolverHullShapes.MaximumJitter)]
        public float jitter = 0.25f;
        [Tooltip("Multiplier range on Base Dimensions across the library.")]
        public Vector2 sizeRange =
            new Vector2(0.6f, 1.4f);
        [Tooltip("Stretch along one random axis, so the pile is not all equally chunky.")]
        [Range(0f, 1f)]
        public float stretch = 0.3f;

        public override int TemplateCount =>
            Mathf.Max(1, templateCount);

        public override int MaximumParticles => 8;

        public override SolverParticleTopology BuildTemplate(
            int templateIndex,
            Vector3 dimensions,
            Vector3[] result,
            out int count)
        {
            // Everything below is a pure function of the template index and the
            // seed. The emitter calls it to place particles and the renderer
            // calls it again to build the mesh; if the two ever disagreed, the
            // fragment would collide as one shape and be drawn as another.
            int key = seed * 73856093 ^
                (templateIndex + 1) * 19349663;

            SolverParticleTopology topology =
                PickVariant(Random01(key, 0u));
            Vector3[] baseVertices =
                SolverHullShapes.BaseVertices(topology);
            count = baseVertices.Length;

            // Anisotropic scale first, displacement second.
            //
            // Scaling is a linear map, so it cannot break convexity however
            // extreme it is; the displacement is what has to stay bounded. This
            // order also makes the bound mean the same thing on every axis,
            // because it is taken against that axis' own half extent.
            float size = Mathf.Lerp(
                Mathf.Min(sizeRange.x, sizeRange.y),
                Mathf.Max(sizeRange.x, sizeRange.y),
                Random01(key, 1u));
            Vector3 halfExtents =
                dimensions * (0.5f * size);

            int stretchAxis =
                (int)(Random01(key, 2u) * 2.999f);
            float stretchAmount = 1f +
                stretch * Random01(key, 3u);
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
                        Signed(key, corner),
                    halfExtents.y * bounded *
                        Signed(key, corner + 1u),
                    halfExtents.z * bounded *
                        Signed(key, corner + 2u));
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

        static float Random01(int key, uint salt)
        {
            uint value =
                (uint)key * 747796405u +
                salt * 2891336453u + 1u;
            value ^= value >> 16;
            value *= 0x7feb352du;
            value ^= value >> 15;
            value *= 0x846ca68bu;
            value ^= value >> 16;
            return (value & 0x00ffffffu) / 16777215f;
        }

        static float Signed(int key, uint salt)
        {
            return Random01(key, salt) * 2f - 1f;
        }

        void OnValidate()
        {
            templateCount =
                Mathf.Clamp(templateCount, 1, 64);
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

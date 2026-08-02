using UnityEngine;

namespace Yu5h1.UnifiedSolver
{
    public sealed class SolverIceFragmentShapeSource :
        SolverShapeSource
    {
        [Header("Library")]
        [Tooltip("How many distinct fragment shapes to generate. Every instance is assigned one, and everything sharing a template is drawn in a single batch, so this is also the draw call count. A couple of dozen is enough for a pile of ice to read as all different.")]
        [Range(1, 64)]
        public int templateCount = 24;
        [Tooltip("Changes every template at once. The same seed always rebuilds the same library, so a set of shapes that works can be kept.")]
        public int seed = 7;

        [Header("Variant Mix")]
        [Tooltip("Relative chance of a 4 particle template: a tetrahedron, the sharpest and cheapest shard.")]
        [Min(0f)]
        public float weight4 = 1f;
        [Tooltip("Relative chance of a 6 particle template: an octahedron, the middle size.")]
        [Min(0f)]
        public float weight6 = 1f;
        [Tooltip("Relative chance of an 8 particle template: a box, the bulkiest and the best at covering its own volume with particles.")]
        [Min(0f)]
        public float weight8 = 1f;

        [Header("Shape")]
        [Tooltip("How far each corner may be displaced, as a fraction of the half extent it sits at. 0 gives clean regular solids that read as manufactured; higher values read as broken ice. Bounded well inside where a corner could fall inside the hull of the others and turn the fragment inside out.")]
        [Range(0f, SolverHullShapes.MaximumJitter)]
        public float jitter = 0.25f;
        [Tooltip("Size multiplier range across the library, applied on top of the profile's Base Dimensions. Both ends at 1 makes every template the same size and differ only in shape.")]
        public Vector2 sizeRange =
            new Vector2(0.6f, 1.4f);
        [Tooltip("How far a template may be stretched along one randomly chosen axis, so the pile is not all equally chunky. 0 keeps every template the shape of Base Dimensions.")]
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

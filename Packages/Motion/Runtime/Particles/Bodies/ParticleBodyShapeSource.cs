using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   A small, fixed library of body templates, each a set of rest particle positions in local
    ///   space (ParticleExtensions EXT-10). The same vertices serve as physics rest positions and as
    ///   the drawn mesh, so collision shape and visible surface never disagree; everything sharing a
    ///   template can be drawn in one batch. <see cref="BuildTemplate"/> must be deterministic: the
    ///   same index and dimensions always give the same vertices at spawn and at draw time.
    /// </summary>
    public abstract class ParticleBodyShapeSource : ScriptableObject
    {
        /// <summary>
        ///   How many distinct templates the source produces.
        /// </summary>
        public abstract int TemplateCount { get; }

        /// <summary>
        ///   Largest particle count any template uses.
        /// </summary>
        public abstract int MaximumParticles { get; }

        /// <summary>
        ///   Fills <paramref name="result"/> with one template's local rest positions, unrotated and
        ///   relative to the spawn origin, and returns the topology it realized.
        /// </summary>
        public abstract ParticleBodyTopology BuildTemplate(
            int templateIndex,
            Vector3 dimensions,
            Vector3[] result,
            out int count);
    }
}

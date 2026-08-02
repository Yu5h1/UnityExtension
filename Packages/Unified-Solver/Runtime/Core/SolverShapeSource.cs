using UnityEngine;

namespace Yu5h1.UnifiedSolver
{
    // Supplies a small, fixed library of fragment templates: each one a set of
    // rest particle positions in local space.
    //
    // A template rather than a fresh shape per instance, because the same
    // vertices serve as both the physics rest positions and the drawn mesh.
    // Sharing them is what keeps the collision shape and the visible surface
    // from ever disagreeing, and drawing needs a mesh that many instances can
    // share: Graphics.RenderMeshInstanced takes one mesh and a list of matrices,
    // so a body whose shape is unique to itself cannot be batched with anything.
    //
    // Variety comes from the number of templates plus each instance's own
    // rotation. Size variation belongs inside a template rather than in the
    // instance matrix: the matrix carries no scale, so that a template's mesh
    // can be inflated by the particle radius exactly once, at build time, and
    // stay correct. A per-instance scale would stretch that inflation with it.
    //
    // This is also the seam baked mesh fracture attaches to. A fracture asset is
    // already a fixed library of distinct fragments, so it becomes a subclass
    // where TemplateCount is the fragment count and the spawn path is unchanged.
    public abstract class SolverShapeSource : ScriptableObject
    {
        // How many distinct templates this source can produce. Instances are
        // assigned one each, and everything sharing a template is drawn in one
        // batch, so this is also the draw call count.
        public abstract int TemplateCount { get; }

        // Largest particle count any template may use.
        //
        // Capacity has to be reserved before an instance's template is known, so
        // the reservation is made against the worst case. Reserving per realized
        // template instead would let a batch pass the check and then run out
        // partway through, leaving half-built instances behind.
        public abstract int MaximumParticles { get; }

        // Fills result with one template's local rest positions and returns the
        // topology it realized.
        //
        // Must be deterministic: the same index and dimensions always produce the
        // same vertices, because the renderer builds its mesh from a second call
        // with the same arguments and the two have to agree exactly.
        //
        // Positions are relative to the spawn origin and unrotated. The caller
        // applies the spawn rotation and translation, and the renderer's matrix
        // reproduces both.
        public abstract SolverParticleTopology BuildTemplate(
            int templateIndex,
            Vector3 dimensions,
            Vector3[] result,
            out int count);
    }
}

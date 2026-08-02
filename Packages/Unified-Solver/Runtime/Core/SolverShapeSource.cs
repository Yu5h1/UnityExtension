using UnityEngine;

namespace Yu5h1.UnifiedSolver
{
    // Supplies one instance's rest particle positions in local space.
    //
    // This is the seam that keeps the procedural ice work from being thrown
    // away when Editor mesh fracture arrives. A fractured fragment's rest
    // positions cannot be derived from a seed, so they have to be read from a
    // baked asset; a procedural fragment's can, so storing them would only be
    // storing a derived value. Both are the same question asked of a different
    // supplier, and everything downstream of this call is identical: capacity
    // reservation, particle creation, the rigid group, and the renderer.
    //
    // Without this, `SolverParticleEmitter.BuildLocalShape` stays a switch on a
    // profile-level enum, and fracture would have to rewrite the spawn path
    // rather than add a subclass.
    public abstract class SolverShapeSource : ScriptableObject
    {
        // Largest particle count any variant may return.
        //
        // Capacity has to be reserved before the variant for a given request is
        // known, so the reservation is made against the worst case. Reserving
        // per realized variant instead would let a batch pass the check and then
        // run out partway through, leaving half-built instances behind.
        public abstract int MaximumParticles { get; }

        // Fills result with local rest positions and returns the topology the
        // instance actually realized.
        //
        // Positions are relative to the spawn origin, unrotated and already
        // scaled: the caller applies only the spawn rotation and translation.
        public abstract SolverParticleTopology BuildShape(
            Vector3 dimensions,
            int seed,
            Vector3[] result,
            out int count);
    }
}

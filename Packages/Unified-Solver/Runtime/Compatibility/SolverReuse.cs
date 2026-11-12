using UnityEngine;

namespace Yu5h1Lib.UnifiedSolver
{
    // What emitters and cloth share to reuse one body round after round on a
    // solver that can neither remove a particle nor add one without disturbing
    // the rest.
    //
    // The vendored SolverManager keeps a CPU list of every particle as it was
    // *created*, and re-uploads the whole list over the GPU buffer whenever
    // anything is added. So once the first step has run, an add puts every live
    // particle back where it was born. The reuse model is therefore: add
    // everything once, before the first step, and from then on rewrite only the
    // slice a body owns in the public ParticleBuffer -- the same way
    // SolverRopeParticles patches ConstraintBuffer -- without touching the
    // vendored solver.
    internal static class SolverReuse
    {
        // Far enough that no volume, collider or spatial-hash neighbour is ever
        // there, and small enough that float precision still resolves a metre.
        const float ParkDistance = 100000f;

        // Particles are given distinct parking slots rather than one shared
        // point, so the hash does not grow one chain holding every parked body.
        const int ParkRow = 64;

        /// <summary>
        ///   Whether the solver has run a step, which is the moment its CPU
        ///   snapshot stops being the truth about the GPU.
        /// </summary>
        /// <remarks>
        ///   Before the first step the CPU list *is* the state, so writing the
        ///   GPU buffer is pointless -- the first upload would overwrite it --
        ///   and so is warning about an add. After it, the GPU buffer is the
        ///   truth and the snapshot is a trap. Read from LastFrameTotalMs, which
        ///   the solver sets only at the end of a step, so no private field is
        ///   needed.
        /// </remarks>
        internal static bool HasStepped(SolverManager solver)
        {
            return solver != null &&
                solver.LastFrameTotalMs > 0.0;
        }

        /// <summary>
        ///   Where a parked particle sits. Unique per global particle index.
        /// </summary>
        internal static Vector3 ParkPosition(int globalParticleIndex)
        {
            int x = globalParticleIndex % ParkRow;
            int y = (globalParticleIndex / ParkRow) % ParkRow;
            int z = globalParticleIndex / (ParkRow * ParkRow);
            return new Vector3(
                ParkDistance + x,
                ParkDistance + y,
                ParkDistance + z);
        }

        /// <summary>
        ///   Warns once, naming the component, that it is adding particles to a
        ///   solver that has already stepped.
        /// </summary>
        internal static void WarnLateAdd(
            SolverManager solver,
            Object component,
            string what,
            ref bool reported)
        {
            if (reported || !HasStepped(solver))
                return;

            reported = true;
            Debug.LogWarning(
                $"{component.GetType().Name} '{component.name}' is " +
                $"adding {what} after the solver's first step. " +
                "SolverManager re-uploads its whole CPU particle list on " +
                "any add, which puts every live particle back to the state " +
                "it was created in. Create everything before the first step " +
                "and reuse it instead (Respawn / Park on an emitter, " +
                "ResetToRest / Park on a cloth).",
                component);
        }
    }
}

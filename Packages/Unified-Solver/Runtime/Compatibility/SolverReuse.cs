using System;
using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib.UnifiedSolver
{
    // A body that can be taken out of play and put back by rewriting its own
    // slice of the solver's particle buffer: an emitter, or the glue beside a
    // vendored generator.
    internal interface ISolverReuseTarget
    {
        UnityEngine.Object Owner { get; }

        // Applies whatever Respawn / ResetToRest / Park is pending. True when
        // nothing is left to apply, which includes "nothing was ever asked".
        bool TryApplyReuse();

        // Whether the body has added all of its particles to the solver.
        bool IsAllocated { get; }

        // Whether it has finished adding anything else it will ever add (a
        // rope's bend constraints). A body that is allocated but not settled
        // has to stay alive until it is.
        bool IsSettled { get; }

        // While held, enabling and disabling the GameObject do nothing, and the
        // body reports itself as parked. Used to bring an object authored
        // inactive to life for exactly as long as its Start needs.
        bool AllocationHold { get; set; }

        void RequestPark();
    }

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

        static bool _quitting;

        /// <summary>
        ///   True once the application has begun to quit. Nothing should be
        ///   created or written then: objects made while shutting down are not
        ///   cleaned up, and the solver's buffers may already be gone.
        /// </summary>
        internal static bool IsQuitting => _quitting;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            _quitting = false;
            Application.quitting -= OnQuitting;
            Application.quitting += OnQuitting;
        }

        static void OnQuitting()
        {
            _quitting = true;
        }

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
        ///   Whether it is safe to write the solver's particle buffer on behalf
        ///   of this object right now.
        /// </summary>
        /// <remarks>
        ///   Disabling is exactly when a body is parked, and disabling also
        ///   happens while a scene unloads and the application quits, when the
        ///   solver's buffers may already be released. A write then throws.
        /// </remarks>
        internal static bool CanWrite(
            SolverManager solver,
            Component owner)
        {
            if (_quitting || solver == null || owner == null)
                return false;

            ComputeBuffer buffer = solver.ParticleBuffer;
            return buffer != null &&
                buffer.IsValid() &&
                owner.gameObject.scene.isLoaded;
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
        ///   Where an object authored inactive is put while it allocates, so
        ///   the particles it creates are born out of reach of everything.
        /// </summary>
        /// <remarks>
        ///   Apart from the parking slots, and from each other, so two such
        ///   objects do not start overlapped.
        /// </remarks>
        internal static Vector3 AllocationSite(int ordinal)
        {
            return new Vector3(
                ParkDistance + 2000f * (ordinal + 1),
                ParkDistance,
                ParkDistance);
        }

        /// <summary>
        ///   Warns once, naming the component, that it is adding particles to a
        ///   solver that has already stepped.
        /// </summary>
        internal static void WarnLateAdd(
            SolverManager solver,
            UnityEngine.Object component,
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
                "and reuse it instead (an object that is authored inactive " +
                "is created in advance when its scene loads; enable and " +
                "disable it to respawn and park, or call Respawn / Park / " +
                "ResetToRest).",
                component);
        }
    }
}

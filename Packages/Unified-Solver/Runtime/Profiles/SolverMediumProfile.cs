using UnityEngine;

namespace Yu5h1.UnifiedSolver
{
    // What a medium is made of: how dense it is, which way it is moving, and how
    // strongly it drags what is inside along with it.
    //
    // Water, air, sand and a current are all the same three numbers. The volume
    // that references this says only where the medium is; everything about how
    // it behaves lives here, so one profile can serve several volumes.
    public sealed class SolverMediumProfile : ScriptableObject
    {
        // Buoyancy is a ratio, not a force: a = g * (1 - medium / body). So a
        // heavy fragment sinks and a light one floats in the same medium, which
        // is what separates this from simply overriding gravity inside the
        // volume. Body density comes from the particle's own mass and the
        // solver's global particle radius.
        //
        // The units are whatever the profile masses and particle radius imply,
        // not kg/m3, so the useful value has nothing to do with water being
        // 1000. Raise it until things stop sinking; that point is neutral.
        // The value that means neutral is typically in the hundreds, because
        // the solver's masses are not kilograms. The runner logs the exact
        // number for each profile the first time a medium touches it; do not
        // guess, and do not expect water to be 1000.
        [Tooltip("Equals the body's own density for neutral buoyancy. See the console for the number.")]
        [Min(0f)]
        public float density;

        [Space]
        [Tooltip("Metres per second the medium itself moves at. Zero is still water.")]
        public Vector3 flow;
        // A rate, not a force: things converge on `flow` rather than
        // accelerating without limit. That is the difference between authoring a
        // current of 1 m/s and authoring an acceleration whose final speed turns
        // out to be set by the solver's global damping.
        [Tooltip("How fast things are dragged to match Flow. 0 lets them pass through freely.")]
        [Min(0f)]
        public float viscosity = 1f;
    }
}

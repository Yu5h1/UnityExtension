using UnityEngine;

namespace Yu5h1.UnifiedSolver
{
    // Sends a body back to where it was born when it reaches somewhere it should
    // not be, and hides the journey.
    //
    // Two separable things, kept separable. The *trigger* is this effect landing
    // on an instance; the *envelope* is the fade that covers the teleport. Not
    // fading is a legitimate setting (both durations 0), and so is a volume that
    // recycles on entry rather than on exit -- which is the same effect with
    // `actOutside` off, and is a kill zone rather than a boundary. That pair
    // costing nothing extra is the whole reason effects are separate from
    // volumes.
    //
    // Per instance, not per particle. Half a fish is not something that can be
    // recycled, and the test is against the body's centre, so a body is not sent
    // home for dipping one particle over the line.
    //
    // Deliberately not a modifier. A modifier belongs to an emitter and travels
    // with the bodies; a boundary belongs to the scene and applies to whatever
    // wanders into it, which is the same reason a medium is a volume.
    [CreateAssetMenu(
        fileName = "BoundsEffect",
        menuName = "Yu5h1/Unified Solver/Volume Effects/Bounds")]
    public sealed class SolverBoundsProfile :
        SolverVolumeEffectProfile
    {
        // The fade is a visual envelope and nothing more: a shrinking body still
        // collides at full particle radius, because the solver has no idea the
        // renderer is scaling anything. Keep these short enough that a body is
        // not bumping into things it appears too small to reach.
        [Tooltip("Seconds to shrink away before the body is moved. 0 teleports outright.")]
        [Min(0f)]
        public float fadeOut = 0.5f;
        [Tooltip("Seconds to grow back once it has been moved.")]
        [Min(0f)]
        public float fadeIn = 0.5f;

        public override SolverVolumeEffectType EffectType =>
            SolverVolumeEffectType.Bounds;

        public override SolverVolumeGranularity Granularity =>
            SolverVolumeGranularity.Instance;

        // Acting outside is the case worth defaulting to: a boundary is a place
        // to be kept within far more often than a place to be kept out of. The
        // base class cannot default it, because a field initialiser there would
        // have to be right for every effect, and a medium acts inside.
        void Reset()
        {
            actOutside = true;
        }

        public override void Write(
            SolverVolume volume,
            ref SolverVolumeGPU entry)
        {
            entry.payloadX = Mathf.Max(0f, fadeOut);
            entry.payloadY = Mathf.Max(0f, fadeIn);
        }
    }
}

using UnityEngine;

namespace Yu5h1.UnifiedSolver
{
    // How a body moves itself toward a SolverMotionTarget.
    //
    // Locomotion rather than swimming: a fish, a snake and a herd differ in what
    // they push against and how they look doing it, not in this. Speed and
    // rhythm describe all of them.
    //
    // Only acts on a body inside a medium, because propulsion needs something to
    // push against. That is also what makes a fish out of water go limp without
    // any rule saying so: the water it was pushing on is no longer there.
    public sealed class SolverLocomotionProfile :
        SolverParticleModifierProfile
    {
        // Authored as a speed rather than a force, so the medium's viscosity
        // decides how quickly a glide decays instead of deciding how fast the
        // animal can go. A force would make every animal in the scene change
        // pace the moment the water was retuned.
        [Tooltip("m/s the body works toward while pushing.")]
        [Min(0f)]
        public float speed = 1.5f;

        [Space]
        // Between pushes nothing is applied and the medium's viscosity bleeds
        // the speed off, which is the glide. Steady locomotion needs no separate
        // mode: a duration at or past the period leaves no gap to glide in.
        [Tooltip("Pushes per second. 0 never pushes.")]
        [Min(0f)]
        public float frequency = 1f;
        [Tooltip("Seconds one push lasts. At or past 1/Frequency it is continuous.")]
        [Min(0.01f)]
        public float duration = 0.3f;
        [Tooltip("Spread of the rhythm across bodies, so a group does not pulse in unison.")]
        [Range(0f, 1f)]
        public float randomness = 0.3f;
    }
}

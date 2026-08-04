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

        // Steering runs through a glide as well as a push: thrust that arrives
        // before the body has come round only drives it further the wrong way.
        // Applied as angular velocity about the body's own centre, so it can
        // neither teleport through anything nor shift the body sideways.
        [Space]
        [Tooltip("Degrees per second the body swings its head onto the heading. 0 leaves it drifting sideways.")]
        [Min(0f)]
        public float turnRate = 180f;
        // A separate axis: a body can point exactly where it is going and still
        // be lying on its side, because nothing else resists rotation about the
        // long axis. 0 suits an animal with no up, like a snake.
        [Tooltip("Degrees per second the body rights itself. 0 lets it roll freely.")]
        [Min(0f)]
        public float uprightRate = 120f;
    }
}

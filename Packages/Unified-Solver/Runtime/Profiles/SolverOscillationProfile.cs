using UnityEngine;
using UnityEngine.Serialization;

namespace Yu5h1.UnifiedSolver
{
    [CreateAssetMenu(
        fileName = "OscillationModifier",
        menuName = "Yu5h1/Unified Solver/Modifiers/Oscillation")]
    public sealed class SolverOscillationProfile :
        SolverParticleModifierProfile
    {
        [Header("Drive")]
        [Tooltip("Caps only the velocity the modifier may add per step, which is the body's follow-through momentum. It does not affect the pose, which is always applied in full, and so has no effect on how high the body bounces. At the default it never engages: compare 0 against 120 to see what it actually does, and if there is no visible difference at 0 it is not earning its place.")]
        [Min(0f)]
        public float acceleration = 120f;
        [Tooltip("How hard the muscle tries, before Stiffness resists it. 0 stops all self-driven motion: the body still gets pushed around by contact, it just never moves itself. 1 is full effort and looks freshest. Also budgets the bounce the body gets off a surface, since that bounce is feedback from this same effort. Effective drive is Vitality * (1 - Stiffness), so either one at its limit stops both the motion and the bounce.")]
        [Range(0f, 1f)]
        public float vitality = 1f;
        [Tooltip("How often a run of the animation begins, in runs per second. Only the timing: it changes neither the shape nor how long a run takes. Between runs the body is left limp exactly as Vitality 0 leaves it, so it keeps whatever shape it had rather than being straightened. 0 means a run never begins.")]
        //[Range(0f, 8f)]
        public float frequency = 0.8f;
        [Tooltip("Seconds one run of the animation takes, start to finish. Set 1 and it takes 1, whatever shape is playing. If a run is longer than the gap Frequency leaves, runs play back to back with no idle time.")]
        [Min(0f)]
        public float duration = 1f;
        [Tooltip("Spread of frequency across instances. At 1 the range is 0x to 2x, so some bodies barely move while others run at double rate.")]
        [Range(0f, 1f)]
        public float frequencyRandomness = 0.35f;
        [Range(-180f, 180f)]
        public float directionAngle;
        [Range(0f, 180f)]
        public float directionRandomness;

        [Header("Body Bend")]
        [Tooltip("Hardness, as a rate on everything the body does. Reads like a timescale in reverse: 0 runs at full speed, 0.5 halves it, 1 stops it. At 1 the body freezes in whatever shape it currently holds, curled or straight, and keeps only its overall velocity, so it still travels and collides but no longer changes form. It holds no target shape of its own; reaching a straight body is Muscle Tension's job.")]
        [Range(0f, 1f)]
        public float stiffness;
        [Tooltip("How tight the muscle is drawn, which chooses the shape the animation aims for. 0 is released and swings to the fullest the topology allows. 1 is drawn tight and flattens the animation out into the topology's own resting form, so the body straightens as it approaches. Separate from Stiffness, which sets the rate rather than the shape.")]
        [Range(0f, 1f)]
        public float muscleTension = 0.3f;
        [FormerlySerializedAs("bendRandomness")]
        [Tooltip("Per-instance variation applied to Muscle Tension, so bodies do not all hold the same shape.")]
        [Range(0f, 1f)]
        public float tensionRandomness = 0.15f;
    }
}

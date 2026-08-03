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
        // Caps the velocity channel only. The pose is always applied in full, so
        // this does not affect bounce height. At the default it never engages.
        [Min(0f)]
        public float acceleration = 120f;
        // Effective drive is vitality * (1 - stiffness), so either at its limit
        // stops both the motion and the bounce it feeds back.
        [Tooltip("0 stops all self-driven motion; contact still moves the body.")]
        [Range(0f, 1f)]
        public float vitality = 1f;
        [Range(-180f, 180f)]
        public float directionAngle;
        [Range(0f, 180f)]
        public float directionRandomness;

        [Header("Animation")]
        // Holds no target shape of its own: at 1 it freezes whatever shape the
        // body currently has, curled or straight, keeping only its travel.
        // Reaching a straight body is Muscle Tension's job.
        [Tooltip("A rate, like a timescale in reverse. 1 freezes the current shape.")]
        [Range(0f, 1f)]
        public float stiffness;
        // Chooses the target shape rather than the rate. Useful range is about
        // 0.2 to 0.4; 0 is the geometric limit, not a natural amplitude.
        [Tooltip("0 swings fullest, 1 flattens to the topology's rest form.")]
        [Range(0f, 1f)]
        public float muscleTension = 0.3f;
        [FormerlySerializedAs("bendRandomness")]
        [Range(0f, 1f)]
        public float tensionRandomness = 0.15f;
        // Timing only: it changes neither the shape nor how long a run takes.
        // Between runs the body is left limp rather than straightened.
        [Tooltip("Runs per second. 0 means a run never begins.")]
        public float frequency = 0.8f;
        [Range(0f, 1f)]
        public float frequencyRandomness = 0.35f;
        // Also paces how fast the body may move, so a longer run is gentler on
        // whatever it rests against and launches it less.
        [Tooltip("Seconds per run. Longer runs play back to back if Frequency leaves no gap.")]
        [Min(0f)]
        public float duration = 1f;
        [Range(0f, 1f)]
        public float durationRandomness = 0.25f;
    }
}

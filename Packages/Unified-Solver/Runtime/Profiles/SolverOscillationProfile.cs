using UnityEngine;

namespace Yu5h1.UnifiedSolver
{
    [CreateAssetMenu(
        fileName = "OscillationModifier",
        menuName = "Yu5h1/Unified Solver/Modifiers/Oscillation")]
    public sealed class SolverOscillationProfile :
        SolverParticleModifierProfile
    {
        [Header("Drive")]
        [Tooltip("Maximum acceleration used to match the deformation velocity. Bend Ratio controls the visible bend independently.")]
        [Min(0f)]
        public float acceleration = 12f;
        [Min(0f)]
        public float frequency = 1.8f;
        [Range(0f, 1f)]
        public float frequencyRandomness = 0.35f;
        [Range(-180f, 180f)]
        public float directionAngle;
        [Range(0f, 180f)]
        public float directionRandomness;

        [Header("Body Bend")]
        [Tooltip("Target C-bend offset as a fraction of body length. 0.5 is the geometric limit of a three-control-point body.")]
        [Range(0f, 0.5f)]
        public float bendRatio = 0.35f;
        [Tooltip("Per-instance variation applied to Bend Ratio.")]
        [Range(0f, 1f)]
        public float bendRandomness = 0.15f;
    }
}

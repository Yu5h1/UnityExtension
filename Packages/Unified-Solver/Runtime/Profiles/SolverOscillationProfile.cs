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
        [Tooltip("Caps only the velocity the modifier may add per step, which is the body's follow-through momentum. It does not affect the pose, which is always applied in full, and so has no effect on how high the body bounces. At the default it never engages: compare 0 against 120 to see what it actually does, and if there is no visible difference at 0 it is not earning its place.")]
        [Min(0f)]
        public float acceleration = 120f;
        [Tooltip("How alive the body looks. 0 reads as dead: it still holds its pose but cannot push off anything. Higher looks fresher and more energetic. Physically it is a ceiling in metres per second on the launch the body gets from pressing into a surface, since the solver converts any downward part of the pose correction into velocity by dividing by the substep rather than the frame. Independent of substeps, so raising substeps for cloth stiffness does not change how lively bodies look.")]
        [Min(0f)]
        public float vitality = 3f;
        [Min(0f)]
        public float frequency = 1.8f;
        [Range(0f, 1f)]
        public float frequencyRandomness = 0.35f;
        [Range(-180f, 180f)]
        public float directionAngle;
        [Range(0f, 180f)]
        public float directionRandomness;

        [Header("Body Bend")]
        [Tooltip("Peak lateral offset of head and tail as a fraction of body length, reached by rotating both segments about the middle. 0.5 folds the body in half and is the geometric limit of a three-control-point body.")]
        [Range(0f, 0.5f)]
        public float bendRatio = 0.35f;
        [Tooltip("Per-instance variation applied to Bend Ratio.")]
        [Range(0f, 1f)]
        public float bendRandomness = 0.15f;
    }
}

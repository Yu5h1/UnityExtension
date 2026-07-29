using UnityEngine;

namespace Yu5h1.UnifiedSolver
{
    [CreateAssetMenu(
        fileName = "SurfaceImpulseModifier",
        menuName = "Yu5h1/Unified Solver/Modifiers/Surface Impulse")]
    public sealed class SolverSurfaceImpulseProfile :
        SolverParticleModifierProfile
    {
        [Min(0f)]
        public float acceleration = 4f;
        public float surfaceY;
        [Min(0f)]
        public float contactDistance = 0.25f;
        [Min(0f)]
        public float frequency = 1.8f;
        [Range(0f, 1f)]
        public float frequencyRandomness = 0.35f;
        [Range(-1f, 1f)]
        public float pulseThreshold = 0.65f;
    }
}

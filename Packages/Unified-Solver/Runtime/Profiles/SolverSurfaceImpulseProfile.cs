using UnityEngine;

namespace Yu5h1.UnifiedSolver
{
    [CreateAssetMenu(
        fileName = "SurfaceImpulseModifier",
        menuName = "Yu5h1/Unified Solver/Modifiers/Surface Impulse")]
    public sealed class SolverSurfaceImpulseProfile :
        SolverParticleModifierProfile
    {
        // Ballistic after launch, so height is about speed squared over twice
        // gravity: 2.5 gives roughly 0.32 m whatever it launched from.
        [Tooltip("m/s.")]
        [Min(0f)]
        public float impulseSpeed = 2.5f;
        // Sampled by the physics step, so past half the step rate it aliases.
        [Tooltip("Hops per second.")]
        [Range(0f, 8f)]
        public float frequency = 1.2f;
        [Range(0f, 1f)]
        public float frequencyRandomness = 0.134f;
        // Only tops the speed back up against gravity while the body gets
        // clear, so it affects how reliably a hop escapes, not how high it
        // goes. Clamped so a hop cannot outlast its own cycle.
        [Tooltip("Fixed steps the launch speed is held for.")]
        [Min(1f)]
        public float impactSpread = 6f;

        [Header("Contact")]
        // Measured on the centre of mass, so body bending cannot mask it, and
        // only descent is tested, so a hop already under way can finish. Raise
        // it if a body on a sagging net never triggers; lower it if hops fire
        // during a fall.
        [Tooltip("m/s of descent above which the body counts as airborne.")]
        [Min(0f)]
        public float fallSpeedLimit = 0.5f;

        [Space]
        // Answers whether the modifier fires at all, separately from whether
        // the launch is strong enough to see.
        [Tooltip("Tint instances red on the steps a hop is applied.")]
        public bool debugTintOnHop;
    }
}

using UnityEngine;

namespace Yu5h1.UnifiedSolver
{
    [CreateAssetMenu(
        fileName = "SurfaceImpulseModifier",
        menuName = "Yu5h1/Unified Solver/Modifiers/Surface Impulse")]
    public sealed class SolverSurfaceImpulseProfile :
        SolverParticleModifierProfile
    {
        [Header("Impulse")]
        [Tooltip("Launch speed of one hop, in metres per second. The body leaves at this speed and flies ballistically, so height is roughly speed squared over twice gravity: 2.5 gives about 0.32 m regardless of what it launched from.")]
        [Min(0f)]
        public float impulseSpeed = 2.5f;
        [Tooltip("Hops per second. Sampled by the physics step, so past half the step rate it aliases and the number stops meaning what it says.")]
        [Range(0f, 8f)]
        public float frequency = 1.2f;
        [Range(0f, 1f)]
        public float frequencyRandomness = 0.134f;
        [Tooltip("Fixed steps the launch speed is held for. Only tops the speed back up against gravity while the body gets clear, so it affects how reliably a hop escapes, not how high it goes. Clamped so a hop cannot outlast its own cycle.")]
        [Min(1f)]
        public float impactSpread = 6f;

        [Header("Contact")]
        [Tooltip("Downward speed above which the body counts as airborne and hops are suppressed. Measured on the centre of mass, so body bending cannot mask it. Only descent is tested, so a hop already under way is free to finish. Raise it if a body resting on a slowly sagging net never triggers; lower it if hops fire during a fall.")]
        [Min(0f)]
        public float fallSpeedLimit = 0.5f;

        [Header("Debug")]
        [Tooltip("Tint instances red on the steps a hop is actually being applied. Answers whether the modifier is firing at all, separately from whether the launch is strong enough to see.")]
        public bool debugTintOnHop;
    }
}

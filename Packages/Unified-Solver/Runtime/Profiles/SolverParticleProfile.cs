using UnityEngine;

namespace Yu5h1.UnifiedSolver
{
    [CreateAssetMenu(
        fileName = "SolverParticleProfile",
        menuName = "Yu5h1/Unified Solver/Particle Profile")]
    public sealed class SolverParticleProfile : ScriptableObject
    {
        [Header("Topology")]
        public SolverParticleTopology topology =
            SolverParticleTopology.Chain3;

        [Tooltip("X = width, Y = length/height, Z = thickness/depth.")]
        public Vector3 baseDimensions =
            new Vector3(0.12f, 0.4f, 0.08f);

        [Header("Physics")]
        [Min(0.0001f)]
        public float mass = 1f;
        [Min(0f)]
        public float jointCompliance = 0.00002f;
        [Min(0f)]
        public float bendCompliance = 0.0005f;
        [Min(0f)]
        public float jointDamping = 0.15f;
        [Tooltip("Per-step fraction of drift around the body's long axis to remove. Structural, not a performance: it runs for every instance whether or not a modifier is attached. GuideChain4 needs this most, since its constraints all reach the spine and so leave the guide free to orbit with no resistance at all, taking the body's cross direction with it. No effect on Chain3, which has nothing off the spine to hold onto.")]
        [Range(0f, 1f)]
        public float rollDamping = 0.25f;
        [Tooltip("Relative speed below which the body stops changing shape, in metres per second. Rotation about the long axis meets no resistance, so what the solver leaves behind each step is never given back and eventually adds up to the body reading as mirrored. Clearing the remainder while it is still small stops it ever getting there. Only motion relative to the body's own mean is removed, so it still travels and slides. Raise it if bodies still drift over time; lower it if they look stiff while settling.")]
        [Min(0f)]
        public float settleSpeed = 0.05f;
        public bool collideWithSameProfile = true;
        public bool showCollisionParticles;

        [Header("Appearance")]
        public Color baseColor =
            new Color(0.35f, 0.65f, 0.8f, 1f);
        [Range(0f, 1f)]
        public float colorVariation = 0.15f;
        public SolverRenderProfile renderProfile;

        [Header("Optional Modifiers")]
        public SolverParticleModifierProfile[] modifiers =
            new SolverParticleModifierProfile[0];

        public SolverParticleRequirements Requirements
        {
            get
            {
                switch (topology)
                {
                    case SolverParticleTopology.Single:
                        return new SolverParticleRequirements(
                            1, 0, 0, 0);
                    case SolverParticleTopology.Chain3:
                        return new SolverParticleRequirements(
                            3, 3, 0, 0);
                    case SolverParticleTopology.GuideChain4:
                        return new SolverParticleRequirements(
                            4, 6, 0, 0);
                    case SolverParticleTopology.DualRail6:
                        return new SolverParticleRequirements(
                            6, 13, 0, 0);
                    case SolverParticleTopology.RigidCluster4:
                        return new SolverParticleRequirements(
                            4, 0, 1, 4);
                    case SolverParticleTopology.ArticulatedCluster12:
                        return new SolverParticleRequirements(
                            12, 12, 3, 12);
                    default:
                        return new SolverParticleRequirements(
                            0, 0, 0, 0);
                }
            }
        }

        public SolverMeshMode ExpectedMeshMode =>
            topology == SolverParticleTopology.RigidCluster4
                ? SolverMeshMode.Rigid
                : SolverMeshMode.Articulated;

        void OnValidate()
        {
            baseDimensions = new Vector3(
                Mathf.Max(
                    0.0001f,
                    Mathf.Abs(baseDimensions.x)),
                Mathf.Max(
                    0.0001f,
                    Mathf.Abs(baseDimensions.y)),
                Mathf.Max(
                    0.0001f,
                    Mathf.Abs(baseDimensions.z)));
            mass = Mathf.Max(0.0001f, mass);
            jointCompliance =
                Mathf.Max(0f, jointCompliance);
            bendCompliance =
                Mathf.Max(0f, bendCompliance);
            jointDamping =
                Mathf.Max(0f, jointDamping);
            rollDamping =
                Mathf.Clamp01(rollDamping);
        }
    }
}

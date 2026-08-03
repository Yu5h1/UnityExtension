using UnityEngine;
using Yu5h1Lib;

namespace Yu5h1.UnifiedSolver
{
    public sealed class SolverParticleProfile : ScriptableObject
    {
        public SolverParticleTopology topology =
            SolverParticleTopology.Chain3;

        [Tooltip("X = width, Y = length/height, Z = thickness/depth.")]
        public Vector3 baseDimensions =
            new Vector3(0.12f, 0.4f, 0.08f);

        // Overrides Topology per instance. Also where baked mesh fracture data
        // will attach, so the spawn path does not change again for it.
        [Inline]
        public SolverShapeSource shapeSource;

        [Header("Physics")]
        [Min(0.0001f)]
        public float mass = 1f;
        [Min(0f)]
        public float jointCompliance = 0.00002f;
        [Min(0f)]
        public float bendCompliance = 0.0005f;
        [Min(0f)]
        public float jointDamping = 0.15f;
        // Runs for every instance, modifier or not. GuideChain4 needs it most:
        // its constraints all reach the spine, so the guide can orbit freely and
        // drag the body's cross direction with it.
        [Tooltip("Chain topologies only. No effect on Chain3.")]
        [Range(0f, 1f)]
        public float rollDamping = 0.25f;

        // A threshold, not a ceiling: the excess decays, so a body hit hard still
        // ends up faster than one hit lightly. Below it nothing happens at all,
        // which global damping cannot offer. Applies to the instance mean, so
        // internal motion is untouched, and only reaches free bodies: a contact
        // rebuilds velocity from position and overwrites it.
        [Tooltip("m/s. 0 disables.")]
        [Min(0f)]
        public float speedLimit;
        [Tooltip("Higher approaches a hard clamp; lower leaves follow-through.")]
        [Min(0.01f)]
        public float speedDecayRate = 10f;

        // The only control that can stop a settled body. Damping cannot: the
        // solver rebuilds velocity from positions every substep, so a velocity
        // written from outside survives one substep in thirty. Sleep writes
        // positions. An instance under a continuous modifier never sleeps.
        [Tooltip("m/s. 0 disables sleeping.")]
        [Min(0f)]
        public float sleepSpeed = 0.04f;
        [Tooltip("Seconds. Too short freezes bodies mid-tumble.")]
        [Min(0f)]
        public float sleepDelay = 0.5f;
        // Displacement, not speed: a sleeping body is held at zero velocity and
        // cannot report its own motion, but how far the solver pushed it covers
        // being hit, being landed on, and the pile under it shifting.
        [Tooltip("Metres. Too small and contact noise wakes everything.")]
        [Min(0.0001f)]
        public float wakeDistance = 0.005f;

        public bool collideWithSameProfile = true;
        public bool showCollisionParticles;

        [Header("Appearance")]
        public Color baseColor =
            new Color(0.35f, 0.65f, 0.8f, 1f);
        [Range(0f, 1f)]
        public float colorVariation = 0.15f;
        [Inline]
        public SolverRenderProfile renderProfile;

        public SolverParticleModifierProfile[] modifiers =
            new SolverParticleModifierProfile[0];

        public SolverParticleRequirements Requirements =>
            RequirementsFor(topology);

        // What a single spawn may cost before its variant is known.
        //
        // A shape source picks the variant per instance, so the capacity check
        // has to reserve against the largest one it could return. Checking
        // against the realized variant instead would let a batch pass and then
        // run out partway through, which leaves half-built instances behind.
        public SolverParticleRequirements
            WorstCaseRequirements
        {
            get
            {
                if (shapeSource == null)
                    return Requirements;

                return RequirementsFor(
                    SolverTopologyInfo.RigidClusterFor(
                        shapeSource.MaximumParticles));
            }
        }

        public static SolverParticleRequirements
            RequirementsFor(
                SolverParticleTopology topology)
        {
            int rigidParticles =
                SolverTopologyInfo.RigidClusterParticles(
                    topology);
            if (rigidParticles > 0)
            {
                return new SolverParticleRequirements(
                    rigidParticles,
                    0,
                    1,
                    rigidParticles);
            }

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
                case SolverParticleTopology.ArticulatedCluster12:
                    return new SolverParticleRequirements(
                        12, 12, 3, 12);
                default:
                    return new SolverParticleRequirements(
                        0, 0, 0, 0);
            }
        }

        // Derived, never authored. The physics setup already decides this, and
        // asking for it a second time on the render profile only created a way
        // for the two to disagree and draw nothing.
        public SolverMeshMode MeshMode =>
            shapeSource != null ||
            SolverTopologyInfo.IsRigidCluster(topology)
                ? SolverMeshMode.Rigid
                : SolverMeshMode.Articulated;

        // A rigid profile with no authored mesh has exactly one thing it can
        // draw: the hull of its own particles. Making that a toggle meant a
        // fully configured fragment profile rendered nothing until the toggle
        // was found, with no error to say so.
        //
        // Requires the shape source, because the hull meshes are built from its
        // templates. Without one there is nothing to build them from, and the
        // renderer says so rather than drawing nothing quietly.
        public bool UsesHullRendering =>
            MeshMode == SolverMeshMode.Rigid &&
            shapeSource != null &&
            (renderProfile == null ||
             renderProfile.mesh == null);

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

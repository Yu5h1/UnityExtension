using UnityEngine;
using Yu5h1Lib;

namespace Yu5h1.UnifiedSolver
{
    public sealed class SolverParticleProfile : ScriptableObject
    {
        [Header("Topology")]
        public SolverParticleTopology topology =
            SolverParticleTopology.Chain3;

        [Tooltip("X = width, Y = length/height, Z = thickness/depth.")]
        public Vector3 baseDimensions =
            new Vector3(0.12f, 0.4f, 0.08f);

        [Inline,Tooltip("Optional. Supplies each instance's rest particle positions instead of the fixed shape the Topology above would build, and chooses that instance's own topology. Leave empty for everything except procedurally varied rigid fragments. This is also where baked mesh fracture data will attach, so the spawn path does not have to change again for it.")]
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
        [Tooltip("Per-step fraction of drift around the body's long axis to remove. Structural, not a performance: it runs for every instance whether or not a modifier is attached. GuideChain4 needs this most, since its constraints all reach the spine and so leave the guide free to orbit with no resistance at all, taking the body's cross direction with it. No effect on Chain3, which has nothing off the spine to hold onto.")]
        [Range(0f, 1f)]
        public float rollDamping = 0.25f;

        [Header("Speed Limit")]
        [Tooltip("Travel speed above which an instance starts shedding the excess, in metres per second. 0 disables it.

Not a ceiling: everything above a hard ceiling comes out at the same speed in the same frame, which reads as obviously artificial and throws away the difference between a hard hit and a light one. The excess decays instead, so harder still means faster.

Below the threshold it does nothing at all, which is what makes it usable where global damping is not: damping pulls on everything all the time. Acts on the body's travel, never on its internal motion, so a tail still outruns its own centre.

Only reaches bodies that are actually free. A body in contact has its velocity rebuilt from positions, so this cannot serve as friction or as a substitute for Sleep.")]
        [Min(0f)]
        public float speedLimit;
        [Tooltip("How fast the excess above Speed Limit bleeds off. This is a rate, so 10 sheds about two thirds of the excess in a tenth of a second, and higher values approach a hard clamp. Low values leave a long, visible follow-through.")]
        [Min(0.01f)]
        public float speedDecayRate = 10f;

        [Header("Sleep")]
        [Tooltip("Speed below which an instance starts counting down to sleep, in metres per second. 0 disables sleeping entirely.\n\nThis is the one control that can actually stop a settled body. Damping cannot: the solver rebuilds velocity from positions at the end of every substep, so a velocity written from outside the loop survives one substep out of thirty. Sleep writes positions instead, which hold.\n\nAn instance under a continuous modifier never sleeps.")]
        [Min(0f)]
        public float sleepSpeed = 0.04f;
        [Tooltip("How long an instance must stay below Sleep Speed before it is held still, in seconds. Too short and things freeze mid-tumble; too long and a pile keeps twitching after it has visibly settled.")]
        [Min(0f)]
        public float sleepDelay = 0.5f;
        [Tooltip("How far the solver may push a sleeping instance before it wakes, in metres. A sleeping body's velocity is held at zero so it cannot report motion itself; displacement is the honest signal, and it covers being hit, being landed on, and the pile under it shifting. Too small and bodies wake from their own contact noise; too large and a fish can swim through a sleeping fragment.")]
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

        [Header("Optional Modifiers")]
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

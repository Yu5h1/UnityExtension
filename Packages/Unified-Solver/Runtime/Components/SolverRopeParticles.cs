using UnityEngine;

namespace Yu5h1Lib.UnifiedSolver
{
    // Presents a vendored RopeGenerator as something an anchor can bind to, and
    // optionally gives it the bend resistance RopeGenerator has no way to add.
    //
    // A rope is one dimensional, so its index is simply the segment number and
    // LayoutSize reports one row. That is the whole difference from cloth, which
    // is why both share a base class rather than each carrying a dispatch path.
    //
    // Watch RopeGenerator.fixStart / fixEnd. They set an end particle's inverse
    // mass to zero, pinning it where it spawned. Anchoring the same particle
    // still works -- the anchor only writes a position -- but two mechanisms
    // then govern one particle and the behaviour becomes hard to attribute.
    // Turn them off for any end an anchor holds.
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RopeGenerator))]
    public sealed class SolverRopeParticles :
        SolverParticleSource
    {
        // Authored as a plain 0..1 stiffness, not as XPBD compliance.
        //
        // Compliance is the inverse of stiffness, is divided by the SUBSTEP dt
        // squared, and depends on particle mass -- so its useful values live
        // between 1e-8 and 1e-4, run backwards, and move whenever substeps or
        // mass change. That is not a number anyone can author.
        //
        // This is the fraction of a full rigid correction the constraint gets
        // to apply each step, which is the quantity that actually decides how it
        // feels: 1 is a steel pipe, 0.5 a firm hose, 0.1 barely there. The
        // compliance that produces it is derived per rope from the live substep
        // dt and mass, so the feel survives changing either.
        //
        // A plain float rather than Optional<float>, because zero already means
        // no bend -- and because an attribute drawer beats a type drawer, so
        // [Range] on an Optional silently loses its slider.
        [Range(0f, 1f)]
        [Tooltip("1 is a rigid pipe, 0.5 a firm hose, 0.1 barely there. 0 leaves the rope a bare chain and adds no constraints.")]
        public float bendStiffness;

        // Normalised for the same reason bendStiffness is: the raw XPBD damping
        // beta enters as gamma = compliance * beta / subDt, so with a stiff
        // constraint the beta that matters is in the thousands.
        //
        // Damps how fast the bend angle changes, and nothing else. A rope
        // swinging as a pendulum has no bend rate and is untouched by this --
        // that needs SolverManager.damping, which is global. At stiffness 1 the
        // compliance is zero, so gamma is zero and no value here does anything.
        [Range(0f, 1f)]
        [Tooltip("Damps bending motion only. Useless at stiffness 1, and never damps a whole-rope swing -- that is SolverManager.damping.")]
        public float bendDamping;

        RopeGenerator _rope;

        // Enough to rewrite this rope's own slice of the solver's constraint
        // buffer in place, which is what makes the values tunable while playing.
        // The constraints are contiguous, so one SetData covers them.
        int _bendFirstIndex = -1;
        int _bendCount;
        int _bendParticleOffset;
        DistanceConstraintGPU[] _bendPatch;

        RopeGenerator Rope
        {
            get
            {
                if (_rope == null)
                    _rope = GetComponent<RopeGenerator>();
                return _rope;
            }
        }

        public override int ParticleCount
        {
            get
            {
                RopeGenerator rope = Rope;
                return rope == null
                    ? 0
                    : Mathf.Max(0, rope.segments);
            }
        }

        // Display only. A rope is a single line, so the editor draws one
        // polyline and labels a particle with its bare index.
        public override Vector3Int LayoutSize
        {
            get
            {
                return new Vector3Int(
                    ParticleCount, 1, 1);
            }
        }

        // Matches RopeGenerator's spawn loop, which walks local -Y.
        public override bool TryGetRestPosition(
            int index,
            out Vector3 worldPosition)
        {
            worldPosition = Vector3.zero;
            RopeGenerator rope = Rope;
            if (rope == null ||
                !IsIndexValid(index, ParticleCount))
            {
                return false;
            }

            Vector3 local = new Vector3(
                0f,
                -index * rope.spacing,
                0f);

            worldPosition =
                rope.transform.position +
                rope.transform.rotation * local;
            return true;
        }

        // RopeGenerator connects i to i+1 and nothing else. Those constraints
        // resist stretching only: folding the chain leaves every adjacent
        // distance untouched, so bend resistance is exactly zero and all the
        // curvature collapses into a single joint -- a straight bar, a sharp
        // kink, another straight bar. A second distance constraint spanning
        // i to i+2 is what finally makes curvature cost something.
        //
        // One uniform stiffness already gives "firm near the anchor, drooping
        // at the far end": the moment needed to hold a cantilever grows with the
        // square of its length, so a single constant beats gravity near the
        // fixed end and loses to it at the tip. No per-particle falloff needed.
        //
        // Owned here rather than by a component of its own because the offset
        // into the solver's global buffer is what these constraints need, and
        // this class is the only thing that can resolve it.
        //
        // Adding is one way -- SolverManager has no remove -- so the first pass
        // creates them and every later one rewrites this rope's slice of the
        // constraint buffer in place. Switching the toggle off writes a negative
        // rest length, the kernel's broken marker, which it skips.
        protected override void ApplySolverSettings(
            SolverManager manager,
            int particleOffset,
            int particleCount)
        {
            if (_bendCount <= 0 ||
                manager.ConstraintCount <
                    _bendFirstIndex + _bendCount)
            {
                AddBendConstraints(
                    manager, particleOffset, particleCount);
                return;
            }

            WriteBendPatch(manager);
        }

        void AddBendConstraints(
            SolverManager manager,
            int particleOffset,
            int particleCount)
        {
            if (bendStiffness <= 0f)
                return;

            float compliance = ComplianceFor(
                manager, bendStiffness);

            int added = 0;
            int first = -1;
            for (int i = 0; i + 2 < particleCount; i++)
            {
                int index = manager.AddDistanceConstraint(
                    particleOffset + i,
                    particleOffset + i + 2,
                    compliance,
                    0f,
                    DampingFor(
                        manager, compliance, bendDamping));
                if (index < 0)
                    break;

                if (first < 0)
                    first = index;
                added++;
            }

            _bendFirstIndex = first;
            _bendCount = added;
            _bendParticleOffset = particleOffset;

            Debug.Log(
                $"SolverRopeParticles: {added} bend constraints over " +
                $"{particleCount} particles at offset {particleOffset}; " +
                $"stiffness {bendStiffness:0.###} -> compliance " +
                $"{compliance:G4}.",
                this);
        }

        // Solves the XPBD correction ratio for the compliance that produces it.
        //
        // Per step a distance constraint applies wSum / (wSum + alphaTilde) of a
        // rigid correction, with alphaTilde = compliance / subDt^2. Asking for a
        // ratio and deriving compliance backwards is what keeps the authored
        // number meaningful when substeps or particle mass change.
        float ComplianceFor(
            SolverManager manager,
            float stiffness)
        {
            stiffness = Mathf.Clamp01(stiffness);
            if (stiffness >= 1f)
                return 0f;

            float subDt =
                Time.fixedDeltaTime /
                Mathf.Max(1, manager.substeps);
            float invMass =
                Rope != null && Rope.particleMass > 0f
                    ? 1f / Rope.particleMass
                    : 1f;
            float wSum = 2f * invMass;

            return subDt * subDt * wSum *
                (1f - stiffness) / Mathf.Max(1e-6f, stiffness);
        }

        // Solves gamma = compliance * beta / subDt backwards for beta, so the
        // authored 0..1 lands on gamma 0..1 -- the point where the damping term
        // is comparable to the constraint term. Zero compliance means a rigid
        // constraint, and a rigid constraint cannot carry damping at all.
        float DampingFor(
            SolverManager manager,
            float compliance,
            float normalised)
        {
            normalised = Mathf.Clamp01(normalised);
            if (normalised <= 0f || compliance <= 0f)
                return 0f;

            float subDt =
                Time.fixedDeltaTime /
                Mathf.Max(1, manager.substeps);

            return normalised * subDt / compliance;
        }

        // The CPU-side list SolverManager owns still holds the values these were
        // created with, so anything that dirties constraints re-uploads over
        // this. That is a tuning-time concern only: the values a scene starts
        // with go in through AddDistanceConstraint and are not affected.
        void WriteBendPatch(SolverManager manager)
        {
            if (_bendPatch == null ||
                _bendPatch.Length != _bendCount)
            {
                _bendPatch =
                    new DistanceConstraintGPU[_bendCount];
            }

            bool enabled = bendStiffness > 0f;
            float compliance = ComplianceFor(
                manager, bendStiffness);
            float restLength = 2f * Rope.spacing;

            for (int i = 0; i < _bendCount; i++)
            {
                _bendPatch[i] = new DistanceConstraintGPU
                {
                    particleA = _bendParticleOffset + i,
                    particleB = _bendParticleOffset + i + 2,
                    restLength = enabled ? restLength : -1f,
                    compliance = compliance,
                    breakForce = 0f,
                    damping = DampingFor(
                        manager, compliance, bendDamping)
                };
            }

            manager.ConstraintBuffer.SetData(
                _bendPatch, 0, _bendFirstIndex, _bendCount);
        }

        protected override bool TryGetParticleRange(
            SolverManager manager,
            out int particleOffset,
            out int particleCount)
        {
            particleOffset = -1;
            particleCount = 0;
            RopeGenerator rope = Rope;
            return rope != null &&
                SolverManagerAccess
                    .TryGetRopeParticleRange(
                        manager,
                        rope,
                        out particleOffset,
                        out particleCount);
        }

        protected override string DescribeMissingRange()
        {
            return "SolverRopeParticles could not identify this rope in " +
                   "SolverManager. Do not move the RopeGenerator transform " +
                   "after it spawns; move only the anchor Transforms.";
        }
    }
}

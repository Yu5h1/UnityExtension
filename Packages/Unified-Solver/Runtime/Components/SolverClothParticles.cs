using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using Yu5h1Lib.ParticlePhysics;

namespace Yu5h1Lib.UnifiedSolver
{
    // Presents a vendored ClothGenerator as something an anchor can bind to.
    //
    // Nothing here is a design choice: ClothGenerator is read-only, so it cannot
    // implement IPhysicsParticleSource itself, and its particle offset is a
    // private field. This wrapper is the smallest thing that closes both gaps.
    // The execution order is repeated rather than inherited: it puts the base
    // class's settings pass after the anchor at -100 and before the solver at 0,
    // and Unity resolves the attribute against the concrete type.
    //
    // It is also what puts this component's Start ahead of ClothGenerator's,
    // which is the only place a vendored generator can be stopped before it
    // spawns. Hence the capacity guard lives here: every ClothGenerator is
    // expected to carry one of these, anchored or not, and
    // SolverClothGuardBootstrap and the editor hook see to it.
    //
    // It also owns reusing the cloth. The vendored generator spawns once and
    // cannot be told to start again, so a net that is used round after round is
    // put back by rewriting its own slice of the particle buffer.
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ClothGenerator))]
    public sealed class SolverClothParticles :
        SolverParticleSource
    {
        enum ReuseOp
        {
            None,
            Rest,
            Park
        }

        // What every cloth starting this frame has already claimed. All the
        // guards run their Start before any ClothGenerator runs its own, so
        // without this two nets that fit separately would both be passed and
        // the second would still run out. Released in LateUpdate, by which time
        // every Start of the frame has run and the solver's own counters carry
        // the cost.
        static long _reservedParticles;
        static long _reservedConstraints;

        // With domain reload off, statics survive entering play mode, and a
        // reservation left over from a frame that never reached LateUpdate would
        // make every later cloth look as though the solver were smaller.
        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetReservations()
        {
            _reservedParticles = 0;
            _reservedConstraints = 0;
        }

        ClothGenerator _cloth;
        long _myReservedParticles;
        long _myReservedConstraints;
        bool _refusedForCapacity;
        bool _reportedLateAdd;

        ReuseOp _pendingOp;
        bool _parked;
        // phase, colour and visibility as the generator created them. Read back
        // once, on first reuse, because the generator never reports the phase it
        // allocated and a rewrite has to put it back.
        ParticleGPU[] _template;
        ParticleGPU[] _scratch;

        ClothGenerator Cloth
        {
            get
            {
                if (_cloth == null)
                    _cloth =
                        GetComponent<ClothGenerator>();
                return _cloth;
            }
        }

        public override bool IsParked => _parked;

        /// <summary>
        ///   Whether a ResetToRest or Park has been asked for and is waiting for
        ///   the solver's first step before it can be written.
        /// </summary>
        public bool IsReusePending => _pendingOp != ReuseOp.None;

        /// <summary>
        ///   Particles and constraints the generator will add when it starts.
        /// </summary>
        public void GetRequirements(
            out long particles,
            out long constraints)
        {
            particles = 0;
            constraints = 0;
            ClothGenerator cloth = Cloth;
            if (cloth == null ||
                cloth.resolutionX <= 0 ||
                cloth.resolutionY <= 0)
            {
                return;
            }

            long width = cloth.resolutionX;
            long height = cloth.resolutionY;
            particles = width * height;
            // Structural both ways, then the two shear diagonals per cell.
            constraints =
                (width - 1) * height +
                width * (height - 1);
            if (cloth.includeShear)
                constraints += 2 * (width - 1) * (height - 1);
        }

        // Runs ahead of ClothGenerator.Start (this component is at -50, the
        // generator at 0), which is the only moment a vendored generator can be
        // stopped before it adds anything.
        //
        // The failure being prevented: AddParticle returns -1 when the solver
        // is full, the generator does not look, and hands -1 to
        // AddDistanceConstraint, which throws. By then half a net exists. So
        // the generator is refused outright and left disabled -- no exception
        // and no partial net -- and the shortfall is named once.
        void Start()
        {
            CheckCapacity();
        }

        void LateUpdate()
        {
            ReleaseReservation();
        }

        void CheckCapacity()
        {
            ClothGenerator cloth = Cloth;
            SolverManager manager = Manager;
            if (cloth == null || manager == null || !cloth.enabled)
                return;

            // Already spawned, e.g. this component was added late. Its cost is
            // in the solver's counters already, and counting it again would
            // refuse a cloth that exists.
            MeshFilter meshFilter = cloth.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
                return;

            GetRequirements(
                out long needParticles,
                out long needConstraints);
            if (needParticles <= 0)
                return;

            long freeParticles =
                (long)manager.maxParticles -
                manager.ActiveCount -
                _reservedParticles;
            long freeConstraints =
                (long)manager.maxConstraints -
                manager.ConstraintCount -
                _reservedConstraints;

            if (needParticles <= freeParticles &&
                needConstraints <= freeConstraints)
            {
                _myReservedParticles = needParticles;
                _myReservedConstraints = needConstraints;
                _reservedParticles += needParticles;
                _reservedConstraints += needConstraints;

                // The generator is about to add particles. If the solver has
                // already stepped, that add will re-upload the CPU snapshot over
                // every live particle in the scene.
                SolverReuse.WarnLateAdd(
                    manager,
                    cloth,
                    $"{needParticles} particles and " +
                    $"{needConstraints} constraints",
                    ref _reportedLateAdd);
                return;
            }

            _refusedForCapacity = true;
            cloth.enabled = false;
            // The settings pass would otherwise wait on a cloth that will never
            // spawn and report that as a second, vaguer problem.
            enabled = false;

            Debug.LogError(
                $"ClothGenerator '{cloth.name}' was not started: it needs " +
                $"{needParticles} particles" +
                (needParticles > freeParticles ? " (SHORT)" : "") +
                $" and {needConstraints} constraints" +
                (needConstraints > freeConstraints ? " (SHORT)" : "") +
                $", but the solver has {Mathf.Max(0L, freeParticles)} " +
                $"particles and {Mathf.Max(0L, freeConstraints)} " +
                "constraints free " +
                $"(maxParticles {manager.maxParticles} with " +
                $"{manager.ActiveCount} in use, maxConstraints " +
                $"{manager.maxConstraints} with " +
                $"{manager.ConstraintCount} in use" +
                (_reservedParticles > 0 || _reservedConstraints > 0
                    ? $", {_reservedParticles} / {_reservedConstraints} " +
                      "held by other cloth starting now"
                    : "") +
                "). The ClothGenerator was disabled so no partial net is " +
                "built. Raise SolverManager.maxParticles / maxConstraints " +
                "or lower the cloth resolution, then enable it again.",
                cloth);
        }

        void ReleaseReservation()
        {
            if (_myReservedParticles == 0 &&
                _myReservedConstraints == 0)
            {
                return;
            }

            _reservedParticles -= _myReservedParticles;
            _reservedConstraints -= _myReservedConstraints;
            _myReservedParticles = 0;
            _myReservedConstraints = 0;
        }

        protected override void OnDestroy()
        {
            ReleaseReservation();
            base.OnDestroy();
        }

        /// <summary>
        ///   Puts the cloth back to its rest pose at the generator's current
        ///   Transform, with no velocity, in the simulation and drawn again.
        ///   Particles an anchor holds go straight to their Transforms.
        /// </summary>
        /// <remarks>
        ///   Rewrites only this cloth's own slice of the solver's particle
        ///   buffer, so no particle or constraint is added and the counts do not
        ///   move. If the solver has not stepped yet the request is held and
        ///   applied on the first step after it has, because until then the
        ///   solver's own first upload would overwrite it. Returns false only
        ///   when there is no solver to write to.
        /// </remarks>
        public bool ResetToRest()
        {
            return RequestReuse(ReuseOp.Rest);
        }

        /// <summary>
        ///   Takes the cloth out of play: not drawn, not colliding, not pushed
        ///   by any volume and not held by an anchor. ResetToRest brings it
        ///   back.
        /// </summary>
        public bool Park()
        {
            return RequestReuse(ReuseOp.Park);
        }

        bool RequestReuse(ReuseOp op)
        {
            SolverManager manager = Manager;
            if (manager == null || manager.ParticleBuffer == null)
                return false;

            _pendingOp = op;
            TryApplyPending(manager);
            return true;
        }

        protected override void OnPhysicsStep()
        {
            if (_pendingOp == ReuseOp.None)
                return;

            SolverManager manager = Manager;
            if (manager != null)
                TryApplyPending(manager);
        }

        void TryApplyPending(SolverManager manager)
        {
            if (!SolverReuse.HasStepped(manager))
                return;
            if (!TryGetParticleRange(
                    manager,
                    out int offset,
                    out int count))
            {
                return;
            }

            EnsureTemplate(manager, offset, count);
            if (_scratch == null || _scratch.Length != count)
                _scratch = new ParticleGPU[count];

            ReuseOp op = _pendingOp;
            _pendingOp = ReuseOp.None;
            for (int i = 0; i < count; i++)
            {
                ParticleGPU particle = _template[i];
                if (op == ReuseOp.Park)
                {
                    Vector3 slot =
                        SolverReuse.ParkPosition(offset + i);
                    particle.position = slot;
                    particle.prevPosition = slot;
                    particle.invMass = 0f;
                    particle.visible = 0u;
                }
                else
                {
                    TryGetRestPosition(i, out Vector3 rest);
                    particle.position = rest;
                    particle.prevPosition = rest;
                    particle.invMass = RestInverseMass(i);
                }
                particle.velocity = Vector3.zero;
                _scratch[i] = particle;
            }

            manager.ParticleBuffer.SetData(
                _scratch, 0, offset, count);

            _parked = op == ReuseOp.Park;
            MeshRenderer drawn = GetComponent<MeshRenderer>();
            if (drawn != null)
                drawn.enabled = !_parked;

            if (!_parked)
            {
                // After the write, so the pinned particles are at their
                // Transforms for the first step instead of at the rest pose.
                var anchor = GetComponent<PhysicsParticleAnchor>();
                if (anchor != null)
                    anchor.ApplyNow();
            }
        }

        void EnsureTemplate(
            SolverManager manager,
            int offset,
            int count)
        {
            if (_template != null && _template.Length == count)
                return;

            _template = new ParticleGPU[count];
            manager.ParticleBuffer.GetData(
                _template, 0, offset, count);
        }

        // What the generator gave each particle: nothing for a pinned edge,
        // 1 / particleMass for the rest. Authored rather than read back, so a
        // particle a grabber had released or an anchor had pinned at the moment
        // of the readback does not change what rest means.
        float RestInverseMass(int index)
        {
            ClothGenerator cloth = Cloth;
            int x = index % cloth.resolutionX;
            int y = index / cloth.resolutionX;
            bool pinned;
            if (cloth.fixTopCornersOnly)
            {
                pinned =
                    y == cloth.resolutionY - 1 &&
                    (x == 0 || x == cloth.resolutionX - 1);
            }
            else
            {
                pinned =
                    (cloth.fixTopEdge &&
                        y == cloth.resolutionY - 1) ||
                    (cloth.fixBottomEdge && y == 0) ||
                    (cloth.fixLeftEdge && x == 0) ||
                    (cloth.fixRightEdge &&
                        x == cloth.resolutionX - 1);
            }

            return pinned || cloth.particleMass <= 0f
                ? 0f
                : 1f / cloth.particleMass;
        }

        public override int ParticleCount
        {
            get
            {
                ClothGenerator cloth = Cloth;
                return cloth == null
                    ? 0
                    : cloth.resolutionX *
                      cloth.resolutionY;
            }
        }

        // Display only. Cloth is a flat sheet, so depth is 1 and the editor
        // draws rows and columns from it.
        public override Vector3Int LayoutSize
        {
            get
            {
                ClothGenerator cloth = Cloth;
                return cloth == null
                    ? Vector3Int.zero
                    : new Vector3Int(
                        cloth.resolutionX,
                        cloth.resolutionY,
                        1);
            }
        }

        // Reproduces the generator's own spawn layout rather than reading any
        // particle, because the editor calls this before anything has spawned.
        //
        // Row-major, matching how ClothGenerator spawns: index = y * width + x.
        public override bool TryGetRestPosition(
            int index,
            out Vector3 worldPosition)
        {
            worldPosition = Vector3.zero;
            ClothGenerator cloth = Cloth;
            if (cloth == null ||
                !IsIndexValid(index, ParticleCount))
            {
                return false;
            }

            int x = index % cloth.resolutionX;
            int y = index / cloth.resolutionX;

            Vector3 centering = new Vector3(
                (cloth.resolutionX - 1) *
                cloth.spacing * 0.5f,
                (cloth.resolutionY - 1) *
                cloth.spacing * 0.5f,
                0f);

            Vector3 local = new Vector3(
                x * cloth.spacing,
                y * cloth.spacing,
                0f) - centering;

            worldPosition =
                cloth.transform.position +
                cloth.transform.rotation * local;
            return true;
        }

        protected override bool TryGetParticleRange(
            SolverManager manager,
            out int particleOffset,
            out int particleCount)
        {
            particleOffset = -1;
            particleCount = 0;
            ClothGenerator cloth = Cloth;
            return cloth != null &&
                SolverManagerAccess
                    .TryGetClothParticleRange(
                        manager,
                        cloth,
                        out particleOffset,
                        out particleCount);
        }

        protected override string DescribeMissingRange()
        {
            if (_refusedForCapacity)
            {
                return "SolverClothParticles: the ClothGenerator was not " +
                       "started because the solver has no room for it (see " +
                       "the earlier error), so there is no cloth to find.";
            }

            return "SolverClothParticles could not identify this cloth in " +
                   "SolverManager. Do not move the ClothGenerator transform " +
                   "after it spawns; move only the anchor Transforms.";
        }

#if UNITY_EDITOR
        // The coordinate the old authoring wrote into each anchor object's name,
        // as in "Cloth Anchor (6, 95)".
        static readonly Regex NodeInName =
            new Regex(@"\(\s*(\d+)\s*,\s*(\d+)\s*\)");

        /// <summary>
        ///   Rebuilds the anchor's bindings from child objects whose names carry
        ///   a grid coordinate, as the pre-binding authoring produced.
        /// </summary>
        /// <remarks>
        ///   For scenes that predate `PhysicsParticleAnchor` and were never
        ///   migrated, where the anchors survive only as one child object per
        ///   pinned vertex with its coordinate in the name. Reads that
        ///   coordinate rather than the nearest particle, because a moved anchor
        ///   would otherwise bind to whatever vertex it now sits over.
        ///
        ///   Offsets are left at zero. The old component wrote the Transform's
        ///   position straight onto the particle, so zero reproduces exactly
        ///   what the scene does today; Recapture Offsets converts that to
        ///   shape-preserving offsets afterwards if that is wanted.
        ///
        ///   Replaces the whole binding list, so anything authored by hand is
        ///   lost. That is the point -- it is a migration, not a merge.
        /// </remarks>
        [ContextMenu("Find Existing Transform Anchors")]
        void FindExistingTransformAnchors()
        {
            var anchor =
                GetComponent<PhysicsParticleAnchor>();
            if (anchor == null)
            {
                Debug.LogWarning(
                    "No PhysicsParticleAnchor on this GameObject.",
                    this);
                return;
            }

            ClothGenerator cloth = Cloth;
            if (cloth == null || cloth.resolutionX <= 0)
            {
                Debug.LogWarning(
                    "ClothGenerator has no usable resolution yet.",
                    this);
                return;
            }

            Transform[] children =
                GetComponentsInChildren<Transform>(true);

            UnityEditor.Undo.RegisterCompleteObjectUndo(
                anchor, "Find Existing Transform Anchors");
            anchor.Bindings.Clear();

            int bound = 0;
            int skipped = 0;
            int count = ParticleCount;

            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child == transform)
                    continue;

                Match match = NodeInName.Match(child.name);
                if (!match.Success)
                    continue;

                int x = int.Parse(match.Groups[1].Value);
                int y = int.Parse(match.Groups[2].Value);
                int index = y * cloth.resolutionX + x;

                if (x >= cloth.resolutionX ||
                    y >= cloth.resolutionY ||
                    index >= count)
                {
                    Debug.LogWarning(
                        $"'{child.name}' names ({x}, {y}), outside the " +
                        $"{cloth.resolutionX} x {cloth.resolutionY} cloth. " +
                        "Skipped.",
                        child);
                    skipped++;
                    continue;
                }

                anchor.Bindings[child] =
                    new List<PhysicsParticleAnchor.Point>
                    {
                        new PhysicsParticleAnchor.Point
                        {
                            index = index,
                            localOffset = Vector3.zero
                        }
                    };
                bound++;
            }

            anchor.InvalidateBindings();
            UnityEditor.EditorUtility.SetDirty(anchor);

            Debug.Log(
                $"Found {bound} named anchors on a " +
                $"{cloth.resolutionX} x {cloth.resolutionY} cloth" +
                (skipped > 0 ? $", skipped {skipped}" : "") +
                ". Offsets are zero; use Recapture Offsets to keep the " +
                "cloth's shape instead.",
                anchor);
        }
#endif
    }
}

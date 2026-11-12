using UnityEngine;
using Yu5h1Lib.ParticlePhysics;

namespace Yu5h1Lib.UnifiedSolver
{
    // The Unified Solver half of an anchor.
    //
    // Everything backend-specific lives here and nowhere else: the particle
    // buffer, the compute kernel, and the reflection bridge that finds where a
    // vendored generator's particles start. `PhysicsParticleAnchor` holds the
    // bindings and the offsets and never learns any of it.
    //
    // This class exists at all because ClothGenerator and RopeGenerator are
    // read-only vendored dependencies and cannot implement an interface we
    // define. Subclasses supply the three things that differ -- how many
    // particles there are, where each one rests, and how they are laid out for
    // display -- and inherit the write path, so adding a third vendored body is
    // a small subclass rather than a second copy of the dispatch.
    public abstract class SolverParticleSource :
        MonoBehaviour,
        IPhysicsParticleSource,
        ISolverReuseTarget
    {
        enum ReuseOp
        {
            None,
            Rest,
            Park
        }

        const string AnchorComputeResource =
            "PhysicsParticleAnchor";
        const int AnchorStride = 16;
        const int ThreadsPerGroup = 64;

        struct AnchorGPU
        {
            public int particleIndex;
            public Vector3 position;
        }

        // Solver data -- constraints, groups -- is written once when a body
        // spawns and then lives on the GPU, so editing an authoring field after
        // that changes nothing. Every source has the same three reasons it
        // cannot write yet, and the same need to rewrite when a field changes,
        // so both live here rather than in each subclass.
        const int SettingsReportSteps = 250;

        bool _settingsDirty = true;
        int _settingsSteps;

        SolverManager _manager;
        ComputeShader _compute;
        ComputeBuffer _anchorBuffer;
        AnchorGPU[] _anchorData;
        int _kernel = -1;
        bool _reportedMissingCompute;
        bool _reportedMissingRange;

        ReuseOp _pendingOp;
        bool _parked;
        bool _wasDisabled;
        bool _allocationHold;
        // phase, colour and visibility as the generator created them. Read back
        // once, on first reuse, because a vendored generator never reports the
        // phase it allocated and a rewrite has to put it back.
        ParticleGPU[] _template;
        ParticleGPU[] _scratch;

        /// <summary>
        ///   True while the body is parked: out of the simulation, not drawn,
        ///   and not held by an anchor.
        /// </summary>
        public bool IsParked => _parked;

        /// <summary>
        ///   Whether a ResetToRest or Park has been asked for and is waiting for
        ///   the solver's first step before it can be written.
        /// </summary>
        public bool IsReusePending => _pendingOp != ReuseOp.None;

        // What the generator gave each particle: nothing for a pinned end, 1 /
        // particleMass for the rest. Authored rather than read back, so a
        // particle a grabber had released or an anchor had pinned at the moment
        // of the readback does not change what rest means.
        protected abstract float RestInverseMass(int index);

        // Whether this body still has solver constraints to add after its
        // particles exist, so it cannot be switched off yet while it allocates.
        protected virtual bool NeedsSettings => false;

        // True when the body will never exist, so a request to reuse it has
        // nothing to act on and must not wait for it.
        protected virtual bool ReuseBlocked => false;

        // Runs at the moment the particles are about to be rewritten.
        protected virtual void BeforeReuseWrite(bool park)
        {
        }

        /// <summary>
        ///   Puts the body back to its rest pose at the generator's current
        ///   Transform, with no velocity, in the simulation and drawn again.
        ///   Particles an anchor holds go straight to their Transforms.
        /// </summary>
        /// <remarks>
        ///   Rewrites only this body's own slice of the solver's particle
        ///   buffer, so no particle or constraint is added and the counts do not
        ///   move. If the solver has not stepped yet the request is held and
        ///   applied on the first step after it has, because until then the
        ///   solver's own first upload would overwrite it. Enabling the
        ///   GameObject again after disabling it does this by itself. Returns
        ///   false only when there is no solver to write to.
        /// </remarks>
        public bool ResetToRest()
        {
            return RequestReuse(ReuseOp.Rest);
        }

        /// <summary>
        ///   Takes the body out of play: not drawn, not colliding, not pushed by
        ///   any volume and not held by an anchor. ResetToRest brings it back.
        ///   Disabling the GameObject does this by itself.
        /// </summary>
        public bool Park()
        {
            return RequestReuse(ReuseOp.Park);
        }

        bool RequestReuse(ReuseOp op, bool immediate = true)
        {
            if (!Application.isPlaying ||
                SolverReuse.IsQuitting ||
                ReuseBlocked)
            {
                return false;
            }

            SolverManager manager = Manager;
            if (manager == null || manager.ParticleBuffer == null)
                return false;

            _pendingOp = op;
            if (!immediate ||
                !((ISolverReuseTarget)this).TryApplyReuse())
            {
                SolverReuseDriver.Defer(this);
            }
            return true;
        }

        UnityEngine.Object ISolverReuseTarget.Owner => this;

        // A body that will never exist counts as allocated: there is nothing
        // more to wait for, and the reason has already been reported.
        bool ISolverReuseTarget.IsAllocated =>
            ReuseBlocked ||
            (Manager != null &&
                TryGetParticleRange(Manager, out _, out _));

        bool ISolverReuseTarget.IsSettled =>
            !NeedsSettings || !_settingsDirty;

        bool ISolverReuseTarget.AllocationHold
        {
            get => _allocationHold;
            set
            {
                _allocationHold = value;
                if (!value)
                    return;

                // Parked from the first moment, so an anchor does not pull its
                // particles to Transforms elsewhere while the body is being made.
                _parked = true;
                SetDrawn(false);
            }
        }

        void ISolverReuseTarget.RequestPark()
        {
            Park();
        }

        void SetDrawn(bool drawn)
        {
            MeshRenderer drawer = GetComponent<MeshRenderer>();
            if (drawer != null)
                drawer.enabled = drawn;
        }

        bool ISolverReuseTarget.TryApplyReuse()
        {
            if (_pendingOp == ReuseOp.None)
                return true;
            if (ReuseBlocked)
            {
                _pendingOp = ReuseOp.None;
                return true;
            }

            SolverManager manager = Manager;
            if (!SolverReuse.CanWrite(manager, this) ||
                !SolverReuse.HasStepped(manager) ||
                !TryGetParticleRange(
                    manager,
                    out int offset,
                    out int count))
            {
                return false;
            }

            if (_template == null || _template.Length != count)
            {
                _template = new ParticleGPU[count];
                manager.ParticleBuffer.GetData(
                    _template, 0, offset, count);
            }
            if (_scratch == null || _scratch.Length != count)
                _scratch = new ParticleGPU[count];

            ReuseOp op = _pendingOp;
            _pendingOp = ReuseOp.None;
            BeforeReuseWrite(op == ReuseOp.Park);

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
            SetDrawn(!_parked);

            if (!_parked)
            {
                // After the write, so the pinned particles are at their
                // Transforms for the first step instead of at the rest pose.
                var anchor = GetComponent<PhysicsParticleAnchor>();
                if (anchor != null)
                    anchor.ApplyNow();
            }
            return true;
        }

        public abstract int ParticleCount { get; }

        public abstract Vector3Int LayoutSize { get; }

        public abstract bool TryGetRestPosition(
            int index,
            out Vector3 worldPosition);

        // Where this body's particles begin in the solver's global buffer, and
        // how many it owns.
        protected abstract bool TryGetParticleRange(
            SolverManager manager,
            out int particleOffset,
            out int particleCount);

        // Called once when the range cannot be resolved, so a subclass can say
        // something specific about its own generator instead of a generic
        // failure.
        protected abstract string DescribeMissingRange();

        protected SolverManager Manager
        {
            get
            {
                if (_manager == null)
                    _manager = SolverManager.Instance;
                return _manager;
            }
        }

        protected static bool IsIndexValid(
            int index,
            int particleCount)
        {
            return index >= 0 && index < particleCount;
        }

        public bool TryApplyTargets(
            PhysicsParticleTarget[] targets,
            int count)
        {
            if (targets == null || count <= 0)
                return false;

            // A parked body is somewhere nothing can touch it, and an anchor
            // writing its pinned particles back to their Transforms would
            // drag them out of there and into the scene.
            if (IsParked)
                return false;

            SolverManager manager = Manager;
            if (manager == null ||
                manager.ParticleBuffer == null)
            {
                return false;
            }

            if (!TryEnsureCompute())
                return false;

            if (!TryGetParticleRange(
                    manager,
                    out int particleOffset,
                    out int particleCount))
            {
                ReportMissingRangeOnce();
                return false;
            }
            _reportedMissingRange = false;

            EnsureCapacity(count);

            int written = 0;
            for (int i = 0; i < count; i++)
            {
                PhysicsParticleTarget target = targets[i];
                if (!IsIndexValid(
                        target.index, particleCount))
                {
                    continue;
                }

                _anchorData[written++] = new AnchorGPU
                {
                    particleIndex =
                        particleOffset + target.index,
                    position = target.position
                };
            }

            if (written == 0)
                return false;

            _anchorBuffer.SetData(
                _anchorData, 0, 0, written);
            _compute.SetInt("_AnchorCount", written);
            _compute.SetBuffer(
                _kernel, "_Anchors", _anchorBuffer);
            _compute.SetBuffer(
                _kernel,
                "_Particles",
                manager.ParticleBuffer);
            _compute.Dispatch(
                _kernel,
                Mathf.CeilToInt(
                    written / (float)ThreadsPerGroup),
                1,
                1);
            return true;
        }

        bool TryEnsureCompute()
        {
            if (_compute != null)
                return true;

            _compute = Resources.Load<ComputeShader>(
                AnchorComputeResource);
            if (_compute == null)
            {
                if (!_reportedMissingCompute)
                {
                    Debug.LogError(
                        "Could not load Resources/" +
                        AnchorComputeResource +
                        ".compute.",
                        this);
                    _reportedMissingCompute = true;
                }
                return false;
            }

            _kernel =
                _compute.FindKernel("ApplyAnchors");
            return true;
        }

        void EnsureCapacity(int capacity)
        {
            if (_anchorBuffer != null &&
                _anchorBuffer.count >= capacity)
            {
                return;
            }

            ReleaseBuffer();

            int bufferCapacity =
                Mathf.NextPowerOfTwo(
                    Mathf.Max(1, capacity));
            _anchorData =
                new AnchorGPU[bufferCapacity];
            _anchorBuffer = new ComputeBuffer(
                bufferCapacity, AnchorStride);
        }

        void ReleaseBuffer()
        {
            if (_anchorBuffer == null)
                return;

            _anchorBuffer.Release();
            _anchorBuffer = null;
        }

        void ReportMissingRangeOnce()
        {
            if (_reportedMissingRange)
                return;

            Debug.LogWarning(
                DescribeMissingRange(),
                this);
            _reportedMissingRange = true;
        }

        /// <summary>
        ///   Requests that this body rewrite whatever solver data it owns on the
        ///   next physics step. Call after changing anything the solver was told
        ///   at spawn time.
        /// </summary>
        public void MarkSolverSettingsDirty()
        {
            _settingsDirty = true;
            _settingsSteps = 0;
        }

        // Where a subclass writes its own constraints and groups.
        //
        // Called on the physics clock with an already-resolved range, never
        // straight from OnValidate: in edit mode there are no GPU buffers and no
        // particles, and OnValidate also fires during deserialization.
        protected virtual void ApplySolverSettings(
            SolverManager manager,
            int particleOffset,
            int particleCount)
        {
        }

        // Says nothing for the first few seconds because "the body has not
        // spawned yet" is the normal state then, and names whatever is still
        // blocking exactly once after that. Every one of these was a silent
        // return before, which is what made a missing effect impossible to tell
        // apart from a wrong value.
        void FixedUpdate()
        {
            if (_pendingOp != ReuseOp.None)
                ((ISolverReuseTarget)this).TryApplyReuse();

            if (!_settingsDirty)
                return;

            _settingsSteps++;

            SolverManager manager = Manager;
            if (manager == null)
            {
                ReportSettingsBlocked(
                    "SolverManager.Instance is null.");
                return;
            }

            if (!TryGetParticleRange(
                    manager,
                    out int particleOffset,
                    out int particleCount))
            {
                ReportSettingsBlocked(DescribeMissingRange());
                return;
            }

            _settingsDirty = false;
            ApplySolverSettings(
                manager, particleOffset, particleCount);
        }

        void ReportSettingsBlocked(string reason)
        {
            if (_settingsSteps != SettingsReportSteps)
                return;

            Debug.LogWarning(
                $"{GetType().Name}: solver settings still not applied after " +
                $"{SettingsReportSteps} physics steps -- {reason}",
                this);
        }

#if UNITY_EDITOR
        // Covers every authoring field on this component and, through the
        // subclass, the vendored generator beside it. Editing during play mode
        // is the whole point; in edit mode it simply queues for the next play.
        protected virtual void OnValidate()
        {
            MarkSolverSettingsDirty();
        }
#endif

        protected virtual void OnEnable()
        {
            MarkSolverSettingsDirty();

            // Not the first time: a body that has never been off is already at
            // its rest pose, and one that has just allocated is held.
            //
            // Not at once, either. Whatever aligns this object -- a follower, a
            // pool placing it -- usually runs after the activation that got us
            // here, and a rest pose taken from the Transform of this instant is
            // the pose it had before being moved. The next physics step is
            // after all of that.
            if (_wasDisabled && !_allocationHold)
                RequestReuse(ReuseOp.Rest, false);
        }

        protected virtual void OnDisable()
        {
            ReleaseBuffer();

            if (_allocationHold)
                return;

            _wasDisabled = true;
            Park();
        }

        protected virtual void OnDestroy()
        {
            ReleaseBuffer();
        }
    }
}

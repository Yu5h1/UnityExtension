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
        IPhysicsParticleSource
    {
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
        }

        protected virtual void OnDisable()
        {
            ReleaseBuffer();
        }

        protected virtual void OnDestroy()
        {
            ReleaseBuffer();
        }
    }
}

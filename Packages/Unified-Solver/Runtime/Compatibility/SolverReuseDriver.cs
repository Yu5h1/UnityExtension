using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib.UnifiedSolver
{
    // Does the two things reuse needs that no single body can do for itself.
    //
    // 1. Applies a request that had to wait. A Respawn, Park or ResetToRest made
    //    before the solver's first step cannot be written yet -- the first upload
    //    would overwrite it -- and a body that was just disabled has no
    //    FixedUpdate to apply it from. So the request is handed here and applied
    //    on the first tick after the solver has stepped.
    //
    // 2. Allocates objects authored inactive. A GameObject that starts inactive
    //    runs none of its Start methods, so its particles would only come into
    //    existence on first activation, which is after the solver's first step
    //    and resets everything alive. Such an object is therefore switched on
    //    when its scene loads, held there just long enough for Start to run, and
    //    switched off again with its bodies parked.
    //
    // Runs after the solver (order 1000, the solver is at 0), so "the solver has
    // stepped" is already true on the tick it first can be.
    [DefaultExecutionOrder(1000)]
    internal sealed class SolverReuseDriver : MonoBehaviour
    {
        sealed class Allocation
        {
            public GameObject owner;
            public ISolverReuseTarget[] targets;
            public Vector3 original;
            public Vector3 relocated;
        }

        static SolverReuseDriver _instance;
        static int _ordinal;

        readonly List<ISolverReuseTarget> _deferred =
            new List<ISolverReuseTarget>();
        readonly HashSet<ISolverReuseTarget> _deferredSet =
            new HashSet<ISolverReuseTarget>();
        readonly List<Allocation> _allocations =
            new List<Allocation>();

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _instance = null;
            _ordinal = 0;
        }

        static SolverReuseDriver Instance
        {
            get
            {
                if (_instance == null)
                {
                    var host = new GameObject("Solver Reuse Driver")
                    {
                        hideFlags = HideFlags.HideAndDontSave
                    };
                    DontDestroyOnLoad(host);
                    _instance =
                        host.AddComponent<SolverReuseDriver>();
                }
                return _instance;
            }
        }

        /// <summary>
        ///   Keeps trying to apply a body's pending request every tick until it
        ///   is applied or the body is gone.
        /// </summary>
        internal static void Defer(ISolverReuseTarget target)
        {
            if (!Application.isPlaying || SolverReuse.IsQuitting)
                return;

            SolverReuseDriver driver = Instance;
            if (driver._deferredSet.Add(target))
                driver._deferred.Add(target);
        }

        /// <summary>
        ///   Brings an object authored inactive to life long enough for its
        ///   Start to allocate, then parks it and switches it off.
        /// </summary>
        internal static void BeginAllocation(
            GameObject owner,
            ISolverReuseTarget[] targets)
        {
            var entry = new Allocation
            {
                owner = owner,
                targets = targets,
                original = owner.transform.position,
                relocated = SolverReuse.AllocationSite(_ordinal++)
            };

            for (int i = 0; i < targets.Length; i++)
                targets[i].AllocationHold = true;

            // Spawned where it is, so first put it somewhere nothing is. Moved
            // back as soon as it has allocated.
            owner.transform.position = entry.relocated;
            owner.SetActive(true);
            Instance._allocations.Add(entry);
        }

        void Start()
        {
            Pump(false);
        }

        void FixedUpdate()
        {
            Pump(
                SolverReuse.HasStepped(SolverManager.Instance));
        }

        void Pump(bool solverStepped)
        {
            for (int i = _allocations.Count - 1; i >= 0; i--)
            {
                Allocation entry = _allocations[i];
                if (entry.owner == null)
                {
                    _allocations.RemoveAt(i);
                    continue;
                }

                bool ready = true;
                for (int t = 0; t < entry.targets.Length; t++)
                {
                    ISolverReuseTarget target = entry.targets[t];
                    if (target.Owner == null ||
                        !target.IsAllocated ||
                        !target.IsSettled)
                    {
                        ready = false;
                        break;
                    }
                }

                // Still waiting is fine until the solver has stepped. After
                // that, waiting longer changes nothing: whatever has not been
                // allocated now will be by an activation that resets the scene.
                if (!ready && !solverStepped)
                    continue;

                if (!ready)
                    WarnNotAllocated(entry);

                Finish(entry);
                _allocations.RemoveAt(i);
            }

            for (int i = _deferred.Count - 1; i >= 0; i--)
            {
                ISolverReuseTarget target = _deferred[i];
                if (target.Owner == null ||
                    target.TryApplyReuse())
                {
                    _deferred.RemoveAt(i);
                    _deferredSet.Remove(target);
                }
            }
        }

        static void Finish(Allocation entry)
        {
            Transform transform = entry.owner.transform;

            // Only if nothing else has moved it. A follower that aligned the
            // object to something during the hold owns its position now.
            if ((transform.position - entry.relocated).sqrMagnitude <
                0.0001f)
            {
                transform.position = entry.original;
            }

            for (int i = 0; i < entry.targets.Length; i++)
            {
                ISolverReuseTarget target = entry.targets[i];
                if (target.Owner == null)
                    continue;

                target.AllocationHold = false;
                target.RequestPark();
            }

            entry.owner.SetActive(false);
        }

        static void WarnNotAllocated(Allocation entry)
        {
            var names = new System.Text.StringBuilder();
            for (int i = 0; i < entry.targets.Length; i++)
            {
                ISolverReuseTarget target = entry.targets[i];
                if (target.Owner != null &&
                    target.IsAllocated &&
                    target.IsSettled)
                {
                    continue;
                }

                if (names.Length > 0)
                    names.Append(", ");
                names.Append(
                    target.Owner == null
                        ? "<destroyed>"
                        : target.Owner.GetType().Name);
            }

            Debug.LogWarning(
                $"'{entry.owner.name}' was authored inactive and could " +
                $"not allocate its solver particles before the solver's " +
                $"first step ({names} did not finish; something may have " +
                "switched the object off during its first frame). Its first " +
                "activation will add them after the solver has stepped, " +
                "which puts every live particle back to the state it was " +
                "created in.",
                entry.owner);
        }
    }
}

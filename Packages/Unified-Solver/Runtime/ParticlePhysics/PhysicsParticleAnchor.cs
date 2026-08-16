using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Yu5h1Lib.Serialization;

namespace Yu5h1Lib.ParticlePhysics
{
    // Holds chosen particles of a body at scene Transforms.
    //
    // One Transform binds many particles, and each keeps its own offset from
    // that Transform. The previous shape bound one Transform to one particle and
    // wrote the Transform's position straight onto it, so anchoring an edge of
    // cloth to a hand collapsed that whole edge onto a single point -- and cost
    // one GameObject per anchored vertex to work around.
    //
    // Backend neutral. It resolves nothing itself: it produces "these particle
    // indices, at these world positions" and hands that to an
    // IPhysicsParticleSource.
    // Nothing here references SolverManager, a ComputeBuffer or the reflection
    // bridge, so the same component serves cloth, rope, and whatever comes next.
    //
    // Runs at -100, before SolverManager at 0, because an anchor is a kinematic
    // constraint: it writes where a particle *is* so the solver starts from
    // there. This is the opposite of SolverParticleModifierRunner at +50, which
    // observes what the solver did and corrects it. Moving this after the solver
    // turns it into a component that drags a finished cloth back every frame.
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    [MovedFrom(false, null, null, "ClothAnchor")]
    public sealed class PhysicsParticleAnchor : MonoBehaviour
    {
        // One bound particle: which one, and where it sits relative to the
        // Transform holding it.
        //
        // The offset is captured when the particle is picked, from the body's
        // rest layout, rather than authored from nothing. That is what preserves
        // the body's shape when several particles share one Transform, and it is
        // why the source has to be able to answer TryGetRestPosition with no
        // solver running.
        [Serializable]
        public sealed class Point
        {
            // Flat index into the source's own particles, never a coordinate.
            // Shape is a display concern; identity is not.
            public int index;
            public Vector3 localOffset;
        }

        [Tooltip("The body whose particles this anchors. Must implement IPhysicsParticleSource.")]
        [SerializeField]
        MonoBehaviour _source;

        // Transform -> the particles it holds. A dictionary rather than a list
        // of pairs because that is what the data is: one Transform appears
        // once, and KeyValuesDrawer says so in the inspector for free.
        [SerializeField]
        KeyValues<Transform, List<Point>> _bindings =
            new KeyValues<Transform, List<Point>>();

        public KeyValues<Transform, List<Point>> Bindings => _bindings;

        // The pre-list shape: one Transform, one node, no offset.
        //
        // Kept as its own field with its original name and layout rather than
        // migrated at load, so opening an old scene without saving it cannot
        // lose the bindings. OnValidate folds it into `_bindings` on first
        // inspection in the editor.
        [Serializable]
        public sealed class Info
        {
            public Transform transform;
            [HideInInspector] public Vector2Int node;
            [HideInInspector] public bool generated;
        }

        [HideInInspector]
        [SerializeField]
        Info[] anchors = Array.Empty<Info>();

        // The flat, allocation-free view FixedUpdate walks. The dictionary's own
        // enumerator, Entries, Keys and Values all allocate, and this runs every
        // physics step.
        //
        // Rebuilt only when the set of Transforms changes -- the Transforms
        // move, which is the whole point of an anchor, but which particles hang
        // off them does not. The point lists are held by reference, so adding a
        // particle to one needs no rebuild.
        struct Group
        {
            public Transform target;
            public List<Point> points;
        }

        Group[] _groups = Array.Empty<Group>();
        int _groupCount;
        bool _groupsDirty = true;

        IPhysicsParticleSource _resolvedSource;
        PhysicsParticleTarget[] _targets =
            Array.Empty<PhysicsParticleTarget>();
        bool _reportedBadSource;

        /// <summary>
        ///   Marks the flat view stale. Adding or removing a Transform through
        ///   the dictionary already does this; replacing the contents of a point
        ///   list obtained from it does not.
        /// </summary>
        public void InvalidateBindings() => _groupsDirty = true;

        public IPhysicsParticleSource Source
        {
            get
            {
                if (_resolvedSource == null)
                {
                    _resolvedSource =
                        _source as IPhysicsParticleSource;
                }
                return _resolvedSource;
            }
        }

        void Reset()
        {
            _source =
                GetComponent<IPhysicsParticleSource>()
                    as MonoBehaviour;
        }

        void OnEnable()
        {
            _resolvedSource = null;
            _groupsDirty = true;
            _bindings.Changed += InvalidateBindings;
        }

        void OnDisable()
        {
            _bindings.Changed -= InvalidateBindings;
        }

        void FixedUpdate()
        {
            IPhysicsParticleSource source = Source;
            if (source == null)
            {
                ReportBadSourceOnce();
                return;
            }

            int count = CollectTargets();
            if (count == 0)
                return;

            source.TryApplyTargets(_targets, count);
        }

        // Drops entries with no Transform and no particles, so the per-step loop
        // has nothing to test. An unassigned key is a normal authoring state,
        // not an error.
        void RebuildGroups()
        {
            _groupsDirty = false;
            _groupCount = 0;

            IReadOnlyList<KeyValue<Transform, List<Point>>> entries =
                _bindings.Entries;

            if (_groups.Length < entries.Count)
                _groups = new Group[entries.Count];

            for (int i = 0; i < entries.Count; i++)
            {
                KeyValue<Transform, List<Point>> entry = entries[i];
                if (entry.Key == null ||
                    entry.Value == null ||
                    entry.Value.Count == 0)
                {
                    continue;
                }

                _groups[_groupCount++] = new Group
                {
                    target = entry.Key,
                    points = entry.Value
                };
            }
        }

        int CollectTargets()
        {
            if (_groupsDirty)
                RebuildGroups();

            int required = 0;
            for (int g = 0; g < _groupCount; g++)
                required += _groups[g].points.Count;

            if (required == 0)
                return 0;

            if (_targets.Length < required)
            {
                _targets =
                    new PhysicsParticleTarget[
                        Mathf.NextPowerOfTwo(required)];
            }

            int count = 0;
            for (int g = 0; g < _groupCount; g++)
            {
                Transform target = _groups[g].target;
                if (target == null)
                {
                    _groupsDirty = true;
                    continue;
                }

                List<Point> points = _groups[g].points;
                for (int p = 0; p < points.Count; p++)
                {
                    Point point = points[p];
                    if (point == null)
                        continue;

                    _targets[count++] =
                        new PhysicsParticleTarget
                        {
                            index = point.index,
                            position =
                                target.TransformPoint(
                                    point.localOffset)
                        };
                }
            }

            return count;
        }

        // The offset is applied on the CPU, so the GPU struct stays at
        // (index, position) and the existing kernel is untouched. At the tens of
        // particles an anchor actually binds, uploading a matrix per Transform
        // and doing this on the GPU would cost more to write than it saves.
        void ReportBadSourceOnce()
        {
            if (_reportedBadSource)
                return;

            Debug.LogWarning(
                _source == null
                    ? "PhysicsParticleAnchor has no Source. Assign the " +
                      "component that owns the particles, such as " +
                      "SolverClothParticles or SolverRopeParticles."
                    : $"PhysicsParticleAnchor's Source " +
                      $"'{_source.GetType().Name}' does not implement " +
                      "IPhysicsParticleSource.",
                this);
            _reportedBadSource = true;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (_source != null &&
                !(_source is IPhysicsParticleSource))
            {
                Debug.LogWarning(
                    $"'{_source.GetType().Name}' does not implement " +
                    "IPhysicsParticleSource; clearing Source.",
                    this);
                _source = null;
            }

            _resolvedSource = null;
            _groupsDirty = true;
            MigrateLegacyAnchors();
        }

        // Groups the old flat list by Transform, which is the whole shape
        // change, and leaves every offset at zero.
        //
        // Zero is deliberate rather than lazy: the old component wrote the
        // Transform's position onto the particle with no offset, so zero is
        // exactly what those anchors already did. Capturing rest offsets here
        // instead would silently move existing scenes.
        //
        // Held back until a Source exists, because the old data is a (x, y)
        // grid coordinate and flattening it needs the source's row width. Doing
        // it without one would either guess a width or drop the data, and this
        // method clears the legacy field when it finishes -- so a wrong guess is
        // unrecoverable. Waiting costs nothing: the inspector prompts for the
        // Source first anyway.
        void MigrateLegacyAnchors()
        {
            if (anchors == null || anchors.Length == 0)
                return;

            IPhysicsParticleSource source = Source;
            if (source == null)
                return;

            int width = source.LayoutSize.x;
            if (width <= 0)
                return;

            for (int i = 0; i < anchors.Length; i++)
            {
                Info info = anchors[i];
                if (info == null || info.transform == null)
                    continue;

                if (!_bindings.TryGetValue(
                        info.transform,
                        out List<Point> points))
                {
                    points = new List<Point>();
                    _bindings[info.transform] = points;
                }

                points.Add(new Point
                {
                    index =
                        info.node.y * width + info.node.x,
                    localOffset = Vector3.zero
                });
            }

            anchors = Array.Empty<Info>();
            _groupsDirty = true;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}

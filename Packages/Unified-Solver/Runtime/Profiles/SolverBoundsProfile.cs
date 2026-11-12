using UnityEngine;

namespace Yu5h1Lib.UnifiedSolver
{
    // What a bounds volume asks of a body.
    public enum SolverBoundsMode
    {
        // The body may only live inside. Several Contain volumes are a union: a
        // body inside any one of them is where it belongs.
        Contain = 0,
        // The body is sent home as soon as it reaches inside. Beats Contain.
        Exclude = 1
    }

    // Sends a body back to where it was born when it reaches somewhere it should
    // not be, and hides the journey.
    //
    // Two separable things, kept separable. The *trigger* is this effect landing
    // on an instance; the *envelope* is the fade that covers the teleport. Not
    // fading is a legitimate setting (both durations 0).
    //
    // The trigger is stated by `Mode` and the rule that combines every bounds
    // volume in the scene is the same for all of them, decided per body centre:
    //
    //   1. Any Exclude that reaches it recycles it. Exclude wins.
    //   2. Otherwise, if any enabled Contain exists and the body is inside none
    //      of them, it is recycled. Contain volumes are a union.
    //   3. Otherwise it lives. With no Contain at all only Exclude is looked at.
    //
    // This replaced a per-effect `actOutside` flag. "Reaches the body" meant
    // "outside" for a flagged bounds, so two flagged bounds were an intersection
    // -- a body had to be inside both -- and two schools living in two separate
    // regions were both recycled the instant they were born. Contain and Exclude
    // are different questions, not two settings of one.
    //
    // Per instance, not per particle. Half a fish is not something that can be
    // recycled, and the test is against the body's centre, so a body is not sent
    // home for dipping one particle over the line.
    //
    // Deliberately not a modifier. A modifier belongs to an emitter and travels
    // with the bodies; a boundary belongs to the scene and applies to whatever
    // wanders into it, which is the same reason a medium is a volume.
    public sealed class SolverBoundsProfile :
        SolverVolumeEffectProfile
    {
        // Bumped when the serialized shape changes. Zero is every asset written
        // before `Mode` existed, whose only statement of intent is `actOutside`.
        const int CurrentSchema = 1;

        // The fade is a visual envelope and nothing more: a shrinking body still
        // collides at full particle radius, because the solver has no idea the
        // renderer is scaling anything. Keep these short enough that a body is
        // not bumping into things it appears too small to reach.
        [Tooltip("Seconds to shrink away before the body is moved. 0 teleports outright.")]
        [Min(0f)]
        public float fadeOut = 0.5f;
        [Tooltip("Seconds to grow back once it has been moved.")]
        [Min(0f)]
        public float fadeIn = 0.5f;

        [Tooltip("Contain: bodies may only live inside (several Contain volumes are a union). Exclude: bodies are recycled on reaching inside, and this always wins.")]
        [SerializeField]
        SolverBoundsMode _mode = SolverBoundsMode.Contain;

        // What `Mode` was before it existed, kept so an old asset can say which
        // it was. Not drawn and not used by the solver any more. Left public
        // and kept equal to `Mode == Contain`, so code written against the old
        // flag still compiles and still reads the right answer; writing to it
        // is only honoured on an asset that has not been migrated yet, which is
        // how a profile created in code and assigned the flag keeps working.
        [HideInInspector]
        public bool actOutside;

        [HideInInspector]
        [SerializeField]
        int _schema;

        public SolverBoundsMode Mode
        {
            get
            {
                EnsureMigrated();
                return _mode;
            }
            set
            {
                EnsureMigrated();
                _mode = value;
                actOutside = value == SolverBoundsMode.Contain;
            }
        }

        public override SolverVolumeEffectType EffectType =>
            SolverVolumeEffectType.Bounds;

        public override SolverVolumeGranularity Granularity =>
            SolverVolumeGranularity.Instance;

        // Contain is the case worth defaulting to: a boundary is a place to be
        // kept within far more often than a place to be kept out of.
        //
        // Also stamps the schema, because a brand new asset and an asset written
        // before `Mode` existed are otherwise identical on disk and would be
        // told apart by nothing.
        void Reset()
        {
            _mode = SolverBoundsMode.Contain;
            actOutside = true;
            _schema = CurrentSchema;
        }

        void OnEnable()
        {
            EnsureMigrated();
        }

        void OnValidate()
        {
            EnsureMigrated();
            actOutside = _mode == SolverBoundsMode.Contain;
        }

        // An old asset had one statement of intent: `actOutside` on meant "act
        // everywhere except inside", which for a boundary is exactly Contain,
        // and off meant "act inside", which is exactly Exclude. So a single
        // bounds in a scene keeps behaving the same, and only scenes that
        // stacked several flagged bounds change -- which was the defect.
        //
        // Done on load rather than in an editor pass so a scene opened and never
        // saved still behaves, and a build, which never sees an editor, does too.
        void EnsureMigrated()
        {
            if (_schema >= CurrentSchema)
                return;

            _mode = actOutside
                ? SolverBoundsMode.Contain
                : SolverBoundsMode.Exclude;
            _schema = CurrentSchema;
#if UNITY_EDITOR
            // So the migrated value is what gets saved. Not in play mode: a
            // change made there is thrown away, and flagging the asset would
            // only ask to save something that is not worth saving.
            if (!Application.isPlaying)
                UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        public override void Write(
            SolverVolume volume,
            ref SolverVolumeGPU entry)
        {
            entry.payloadX = Mathf.Max(0f, fadeOut);
            entry.payloadY = Mathf.Max(0f, fadeIn);
            // Which side of the rule this entry stands on. The shared `invert`
            // slot is left at 0 for a bounds entry: the kernel always asks "is
            // the centre inside" and the rule decides what that means.
            entry.payloadZ = Mode == SolverBoundsMode.Contain
                ? 1f
                : 0f;
        }
    }
}

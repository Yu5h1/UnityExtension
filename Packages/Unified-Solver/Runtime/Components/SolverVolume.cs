using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;
using Yu5h1Lib;

namespace Yu5h1.UnifiedSolver
{
    // A region of space that is different from the rest of the scene.
    //
    // This component answers only *where*. What actually happens there comes
    // from the effects it carries: a medium makes it water, and later effects
    // make it a boundary, a wind, an attractor. Same relationship a collider has
    // with the world, except it changes what the space is made of rather than
    // blocking it.
    //
    // Geometry and behaviour are separate because the geometry is the expensive
    // half to duplicate and it is identical whatever the effect is. Built the
    // other way -- one component per behaviour -- a boundary would arrive with a
    // second copy of the shape code, a second registration list, a second GPU
    // struct and a second inside test, and an aquarium whose water and whose
    // bounds are the same box would have to keep two Transforms in step by hand.
    //
    // Geometry comes from the Transform, so there is nothing to author twice:
    // position is the centre, lossyScale is the full size, and rotation orients
    // it. A box therefore has a flat top, and that top is the waterline.
    //
    // Registration is global rather than per emitter. A volume is a property of
    // the scene, not of whoever happens to be swimming in it, so every emitter
    // reads the same list.
    [MovedFrom(false, null, null, "SolverMediumVolume")]
    [DisallowMultipleComponent]
    public sealed class SolverVolume : MonoBehaviour
    {
        static readonly List<SolverVolume> Active =
            new List<SolverVolume>();

        public static IReadOnlyList<SolverVolume>
            Registered => Active;

        [Tooltip("Box has a flat top and therefore a waterline; an ellipsoid does not.")]
        public SolverVolumeShape shape =
            SolverVolumeShape.Box;

        [Tooltip("What this region does. Several may act on the same space.")]
        [Inline]
        public SolverVolumeEffectProfile[] effects;

        // Carries the single medium this component held before it took a list.
        //
        // Kept as its own field rather than migrated at load, so opening an old
        // scene without saving it cannot lose the reference. OnValidate folds it
        // into the list on first inspection in the editor; until then the
        // accessors below present it as though it were already there, so nothing
        // downstream has to know which of the two it came from.
        [HideInInspector]
        [SerializeField]
        [FormerlySerializedAs("profile")]
        SolverMediumProfile _legacyProfile;

        public Vector3 Center => transform.position;

        public Vector3 HalfExtents =>
            0.5f * AbsoluteScale;

        public Vector3 AxisX => transform.right;
        public Vector3 AxisY => transform.up;
        public Vector3 AxisZ => transform.forward;

        Vector3 AbsoluteScale
        {
            get
            {
                Vector3 scale = transform.lossyScale;
                return new Vector3(
                    Mathf.Abs(scale.x),
                    Mathf.Abs(scale.y),
                    Mathf.Abs(scale.z));
            }
        }

        public int EffectCount =>
            (effects == null ? 0 : effects.Length) +
            (_legacyProfile != null ? 1 : 0);

        public SolverVolumeEffectProfile GetEffect(
            int index)
        {
            int authored =
                effects == null ? 0 : effects.Length;
            return index < authored
                ? effects[index]
                : _legacyProfile;
        }

        // A volume with no usable effect is a shape that does nothing, and an
        // entry for it would still cost an inside test per particle to reach a
        // branch that applies nothing. Filtered here rather than in the kernel.
        public bool IsUsable
        {
            get
            {
                Vector3 half = HalfExtents;
                if (half.x <= 0f ||
                    half.y <= 0f ||
                    half.z <= 0f)
                {
                    return false;
                }

                int count = EffectCount;
                for (int i = 0; i < count; i++)
                {
                    SolverVolumeEffectProfile effect =
                        GetEffect(i);
                    if (effect != null && effect.enabled)
                        return true;
                }
                return false;
            }
        }

        void OnEnable()
        {
            if (!Active.Contains(this))
                Active.Add(this);
        }

        void OnDisable()
        {
            Active.Remove(this);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (_legacyProfile == null)
                return;

            var merged =
                new List<SolverVolumeEffectProfile>();
            if (effects != null)
                merged.AddRange(effects);
            if (!merged.Contains(_legacyProfile))
                merged.Add(_legacyProfile);

            effects = merged.ToArray();
            _legacyProfile = null;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        void OnDrawGizmos()
        {
            Draw(new Color(0.2f, 0.6f, 1f, 0.5f));
        }

        void OnDrawGizmosSelected()
        {
            Draw(new Color(0.3f, 0.8f, 1f, 1f));
        }

        void Draw(Color wire)
        {
            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(
                Center,
                transform.rotation,
                AbsoluteScale);
            Gizmos.color = wire;
            if (shape == SolverVolumeShape.Box)
                Gizmos.DrawWireCube(
                    Vector3.zero, Vector3.one);
            else
                Gizmos.DrawWireSphere(
                    Vector3.zero, 0.5f);
            Gizmos.matrix = previous;
        }
    }
}

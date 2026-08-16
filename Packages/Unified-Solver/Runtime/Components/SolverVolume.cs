using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;
using Yu5h1Lib;

namespace Yu5h1Lib.UnifiedSolver
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

        // Geometry comes from here and from nowhere else -- not from this
        // component's own Transform.
        //
        // One source rather than "a provider, or else my Transform" because the
        // fallback is what makes the failure silent: with two possible sources,
        // dragging this object sometimes moves the region and sometimes does
        // nothing, and nothing on screen says which. A volume with no provider
        // simply does not run, and Reset gives every new one a PrimitiveShape
        // so that state is never where anyone starts.
        [Tooltip("The volume's geometry. Any component implementing IShapeProvider, including a ParticleSystemAddon.")]
        [TypeRestriction(typeof(IShapeProvider))]
        [SerializeField]
        Object _shapeProvider;

        // The enum this component carried before geometry became a provider.
        //
        // Kept with its original name and layout rather than migrated at load,
        // so opening an old scene without saving it cannot lose the shape.
        // OnValidate folds it into an added PrimitiveShape on first inspection;
        // the values were chosen to line up, so Box and Sphere carry across as
        // themselves.
        [HideInInspector]
        [SerializeField]
        [FormerlySerializedAs("shape")]
        SolverVolumeShape _legacyShape = SolverVolumeShape.Box;

        IShapeProvider _resolvedShape;

        // Accepts a GameObject as well as a component, because dragging a
        // provider that lives on another object out of the hierarchy hands over
        // the GameObject, and the difference is invisible in the field.
        public IShapeProvider Shape
        {
            get
            {
                if (_resolvedShape != null)
                    return _resolvedShape;

                _resolvedShape =
                    _shapeProvider as IShapeProvider;

                if (_resolvedShape == null &&
                    _shapeProvider is GameObject owner)
                {
                    _resolvedShape =
                        owner.GetComponent<IShapeProvider>();
                }

                return _resolvedShape;
            }
        }

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

        public ShapeKind Kind =>
            Shape == null ? ShapeKind.Box : Shape.Kind;

        public Vector3 Center =>
            Shape == null
                ? transform.position
                : Shape.Center;

        // Half of the provider's full size. For a cone the depth axis is the
        // narrow-end diameter rather than an extent, and halving it is still
        // correct -- the kernel reads it as the second radius.
        public Vector3 HalfExtents =>
            Shape == null
                ? Vector3.zero
                : 0.5f * Shape.Size;

        // The region's orientation, which is the provider's and not this
        // component's. Anything that means "in the volume's own axes" has to
        // read this -- reading `transform.rotation` looks identical until a
        // provider sits on another GameObject, and then it is silently wrong.
        public Quaternion Rotation =>
            Shape == null
                ? transform.rotation
                : Shape.Rotation;

        public Vector3 AxisX => Rotation * Vector3.right;
        public Vector3 AxisY => Rotation * Vector3.up;
        public Vector3 AxisZ => Rotation * Vector3.forward;

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
                IShapeProvider shape = Shape;
                if (shape == null || !shape.IsUsable)
                    return false;

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

#if UNITY_EDITOR
        /// <summary>
        ///   Prints the exact values this volume would upload for each effect.
        /// </summary>
        /// <remarks>
        ///   Everything between an authored field and the kernel is derived --
        ///   the provider's frame, the effect's payload packing -- and none of
        ///   it is visible once it reaches the GPU. This runs the same code the
        ///   runner does and shows the result, so a wrong direction or a zero
        ///   can be read instead of inferred.
        /// </remarks>
        [ContextMenu("Log Uploaded Values")]
        void LogUploadedValues()
        {
            IShapeProvider shape = Shape;
            var report = new System.Text.StringBuilder();
            report.AppendLine($"{name}: usable={IsUsable}");
            report.AppendLine(
                shape == null
                    ? "  shape provider: NONE"
                    : $"  shape provider: {shape.GetType().Name} " +
                      $"usable={shape.IsUsable} kind={Kind}");
            report.AppendLine(
                $"  center={Center} halfExtents={HalfExtents}");
            report.AppendLine(
                $"  axisX={AxisX} axisY={AxisY} axisZ={AxisZ}");

            int count = EffectCount;
            report.AppendLine($"  effects={count}");

            for (int i = 0; i < count; i++)
            {
                SolverVolumeEffectProfile effect = GetEffect(i);
                if (effect == null)
                {
                    report.AppendLine($"  [{i}] null");
                    continue;
                }

                var entry = new SolverVolumeGPU
                {
                    center = Center,
                    shape = (float)Kind,
                    halfExtents = HalfExtents,
                    effectType = (float)effect.EffectType,
                    axisX = AxisX,
                    invert = effect.actOutside ? 1f : 0f,
                    axisY = AxisY,
                    axisZ = AxisZ
                };
                effect.Write(this, ref entry);

                report.AppendLine(
                    $"  [{i}] {effect.name} ({effect.GetType().Name}) " +
                    $"enabled={effect.enabled} type={effect.EffectType}");
                report.AppendLine(
                    $"       payloadX={entry.payloadX} " +
                    $"payloadY={entry.payloadY} " +
                    $"payloadZ={entry.payloadZ}");
                report.AppendLine(
                    $"       payloadVector={entry.payloadVector} " +
                    $"(magnitude {entry.payloadVector.magnitude})");
            }

            Debug.Log(report.ToString(), this);
        }
#endif

        void Reset()
        {
            _shapeProvider =
                GetComponent<IShapeProvider>() as Object;
        }

        void OnEnable()
        {
            _resolvedShape = null;
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
            _resolvedShape = null;
            EnsureShapeProvider();

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

        // Deferred because Unity refuses AddComponent from inside OnValidate,
        // and guarded on the object surviving the wait so closing a scene or
        // leaving play mode does not trip it.
        //
        // The presence of a provider is itself the "already migrated" marker,
        // so no extra flag has to be kept honest.
        void EnsureShapeProvider()
        {
            if (_shapeProvider != null)
                return;

            SolverVolume owner = this;
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (owner == null ||
                    owner._shapeProvider != null)
                {
                    return;
                }

                var existing =
                    owner.GetComponent<IShapeProvider>();
                if (existing == null)
                {
                    PrimitiveShape added =
                        UnityEditor.Undo.AddComponent<PrimitiveShape>(
                            owner.gameObject);
                    added.kind =
                        (ShapeKind)(int)owner._legacyShape;
                    existing = added;
                }

                owner._shapeProvider = existing as Object;
                owner._resolvedShape = null;
                UnityEditor.EditorUtility.SetDirty(owner);
            };
        }
#endif

        // Drawn from the provider, not from this Transform, so the outline is
        // always where the region actually is even when the provider lives on
        // another GameObject.
        //
        // Only when selected, because a provider such as a ParticleSystem draws
        // its own shape and two outlines in the scene at all times is noise.
        // Kept rather than dropped for that reason, though: this one shows what
        // the solver derived, and the first cone it drew disagreed with the
        // ParticleSystem's own gizmo, which is how a reversed axis was caught
        // before anything simulated it.
        void OnDrawGizmosSelected()
        {
            ShapeGizmos.Draw(
                Shape, new Color(0.3f, 0.8f, 1f, 1f));
        }
    }
}

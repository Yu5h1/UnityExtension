using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    public enum ParticleAnchorToggleResult
    {
        Bound,
        Unbound,
        NoHolder
    }

    /// <summary>
    ///   Edits a <see cref="ParticleAnchor"/>'s bindings for the scene-view authoring tool
    ///   (ParticleExtensions EXT-06) and fills in its source when it is added. Every change is
    ///   recorded for Undo. Kept apart from the inspector so it can be driven without a scene view.
    /// </summary>
    public static class ParticleAnchorAuthoring
    {
        public static int CountPoints(ParticleAnchor anchor)
        {
            int total = 0;
            foreach (ParticleAnchor.Binding binding in anchor.Bindings)
                if (binding != null && binding.points != null)
                    total += binding.points.Count;
            return total;
        }

        /// <summary>
        ///   Unbinds the particle when any holder already holds it, so one particle is never held
        ///   by two Transforms; otherwise binds it to <paramref name="holder"/> at the local
        ///   position of its rest position <paramref name="restPosition"/>.
        /// </summary>
        public static ParticleAnchorToggleResult Toggle(
            ParticleAnchor anchor,
            Transform holder,
            int index,
            Vector3 restPosition)
        {
            foreach (ParticleAnchor.Binding binding in anchor.Bindings)
            {
                if (binding == null || binding.points == null)
                    continue;
                for (int p = 0; p < binding.points.Count; p++)
                {
                    if (binding.points[p] == null || binding.points[p].index != index)
                        continue;
                    Record(anchor, "Unbind Particle");
                    binding.points.RemoveAt(p);
                    Commit(anchor);
                    return ParticleAnchorToggleResult.Unbound;
                }
            }
            if (holder == null)
                return ParticleAnchorToggleResult.NoHolder;
            Record(anchor, "Bind Particle");
            ParticleAnchor.Binding target = FindBinding(anchor, holder);
            if (target == null)
            {
                target = new ParticleAnchor.Binding { target = holder };
                anchor.Bindings.Add(target);
            }
            target.points.Add(new ParticleAnchor.Point
            {
                index = index,
                localOffset = holder.InverseTransformPoint(restPosition)
            });
            Commit(anchor);
            return ParticleAnchorToggleResult.Bound;
        }

        /// <summary>
        ///   Recomputes every point's local position from the holder's current pose and the given
        ///   rest positions, indexed by flat number. Points outside the rest positions keep their
        ///   offset. Returns how many points changed.
        /// </summary>
        public static int RecaptureOffsets(ParticleAnchor anchor, IReadOnlyList<Vector3> restPositions)
        {
            Record(anchor, "Recapture Anchor Offsets");
            int changed = 0;
            foreach (ParticleAnchor.Binding binding in anchor.Bindings)
            {
                if (binding == null || binding.target == null || binding.points == null)
                    continue;
                foreach (ParticleAnchor.Point point in binding.points)
                {
                    if (point == null || point.index < 0 || point.index >= restPositions.Count)
                        continue;
                    point.localOffset = binding.target.InverseTransformPoint(restPositions[point.index]);
                    changed++;
                }
            }
            Commit(anchor);
            return changed;
        }

        public static ParticleAnchor.Binding FindBinding(ParticleAnchor anchor, Transform holder)
        {
            foreach (ParticleAnchor.Binding binding in anchor.Bindings)
                if (binding != null && binding.target == holder)
                {
                    if (binding.points == null)
                        binding.points = new List<ParticleAnchor.Point>();
                    return binding;
                }
            return null;
        }

        /// <summary>
        ///   Sets the source of <paramref name="anchor"/> to the nearest <see cref="PhysicsParticle"/>
        ///   on its GameObject or a parent when it has none. Returns whether it was set.
        /// </summary>
        public static bool ResolveSource(ParticleAnchor anchor)
        {
            if (anchor == null)
                return false;
            var serialized = new SerializedObject(anchor);
            SerializedProperty property = serialized.FindProperty("_source");
            if (property.objectReferenceValue != null)
                return false;
            PhysicsParticle source = anchor.GetComponentInParent<PhysicsParticle>(true);
            if (source == null)
                return false;
            property.objectReferenceValue = source;
            serialized.ApplyModifiedProperties();
            return true;
        }

        [InitializeOnLoadMethod]
        static void HookComponentAdded()
        {
            ObjectFactory.componentWasAdded -= OnComponentAdded;
            ObjectFactory.componentWasAdded += OnComponentAdded;
        }

        static void OnComponentAdded(Component component)
        {
            if (!(component is ParticleAnchor || component is PhysicsParticle))
                return;
            GameObject owner = component.gameObject;
            EditorApplication.delayCall += () =>
            {
                if (owner == null)
                    return;
                foreach (ParticleAnchor anchor in owner.GetComponentsInChildren<ParticleAnchor>(true))
                    ResolveSource(anchor);
            };
        }

        static void Record(ParticleAnchor anchor, string name) =>
            Undo.RegisterCompleteObjectUndo(anchor, name);

        static void Commit(ParticleAnchor anchor)
        {
            EditorUtility.SetDirty(anchor);
            PrefabUtility.RecordPrefabInstancePropertyModifications(anchor);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// Suppresses physics collision between this object's collider(s) and an
    /// ancestor's collider(s) — useful when a child object is parented for
    /// transform inheritance but should not exchange physics forces with the
    /// parent.
    ///
    /// Canonical use case: an XR Origin / Player parented to a boat deck so it
    /// rides along with the moving platform — without this component, the
    /// player's CharacterController would push the boat's Rigidbody every step
    /// and capsize it.
    ///
    /// On Enable: walks up the hierarchy by the chosen <see cref="DetectionMode"/>,
    /// finds the target, and calls Physics.IgnoreCollision for every
    /// (self ↔ target) collider pair. On Disable: restores collision so
    /// toggling the component is safe (e.g. when the player leaves the boat).
    ///
    /// Re-call <see cref="Refresh"/> if the hierarchy changes at runtime.
    /// </summary>
    [DisallowMultipleComponent]
    public class ParentCollisionIgnorer : MonoBehaviour
    {
        public enum DetectionMode
        {
            /// <summary>Walk up the hierarchy until an ancestor with a Collider is found.</summary>
            NearestCollider,
            /// <summary>Walk up the hierarchy until an ancestor with a Rigidbody is found.</summary>
            NearestRigidbody,
            /// <summary>Use transform.root (the topmost ancestor).</summary>
            Root,
            /// <summary>Use a user-specified Transform.</summary>
            Manual,
        }

        [Tooltip("How to find the target whose colliders should be ignored. " +
                 "NearestRigidbody is the typical choice — find the moving platform's body.")]
        [SerializeField] private DetectionMode mode = DetectionMode.NearestRigidbody;

        [Tooltip("Target Transform. Used only in Manual mode.")]
        [SerializeField] private Transform manualTarget;

        [Tooltip("Include all colliders under this object (recursive). " +
                 "Enable when self has nested colliders (e.g. XR Origin with controller colliders).")]
        [SerializeField] private bool includeOwnChildColliders = true;

        [Tooltip("Include all colliders under the target (recursive). " +
                 "Enable when the target hierarchy has colliders on multiple subparts (hull + deck + props).")]
        [SerializeField] private bool includeTargetChildColliders = true;

        // --- runtime ---
        private readonly List<Collider> _selfColliders = new();
        private readonly List<Collider> _targetColliders = new();
        private bool _applied;

        private void OnEnable() => Apply(true);

        private void OnDisable()
        {
            if (_applied) Apply(false);
        }

        private void Apply(bool ignore)
        {
            var target = ResolveTarget();
            if (target == null)
            {
                _applied = false;
                return;
            }

            CollectColliders(_selfColliders, transform, includeOwnChildColliders);
            CollectColliders(_targetColliders, target, includeTargetChildColliders);

            if (_selfColliders.Count == 0 || _targetColliders.Count == 0)
            {
                _applied = false;
                return;
            }

            // Quick lookup so we don't ignore self-against-self when target is
            // an ancestor whose subtree contains us.
            var selfSet = new HashSet<Collider>(_selfColliders);

            for (int i = 0; i < _selfColliders.Count; i++)
            {
                var self = _selfColliders[i];
                if (self == null) continue;

                for (int j = 0; j < _targetColliders.Count; j++)
                {
                    var other = _targetColliders[j];
                    if (other == null || selfSet.Contains(other)) continue;
                    Physics.IgnoreCollision(self, other, ignore);
                }
            }

            _applied = ignore;
        }

        private static void CollectColliders(List<Collider> buffer, Transform root, bool recursive)
        {
            buffer.Clear();
            if (recursive) root.GetComponentsInChildren(true, buffer);
            else root.GetComponents(buffer);
        }

        private Transform ResolveTarget()
        {
            switch (mode)
            {
                case DetectionMode.Manual:
                    return manualTarget;

                case DetectionMode.Root:
                    return transform.root != transform ? transform.root : null;

                case DetectionMode.NearestRigidbody:
                {
                    var t = transform.parent;
                    while (t != null)
                    {
                        if (t.TryGetComponent<Rigidbody>(out _)) return t;
                        t = t.parent;
                    }
                    return null;
                }

                case DetectionMode.NearestCollider:
                default:
                {
                    var t = transform.parent;
                    while (t != null)
                    {
                        if (t.TryGetComponent<Collider>(out _)) return t;
                        t = t.parent;
                    }
                    return null;
                }
            }
        }

        /// <summary>Re-resolve the target and re-apply the ignore set.
        /// Call this if the hierarchy changes at runtime (e.g. re-parented).</summary>
        public void Refresh()
        {
            if (_applied) Apply(false);
            if (isActiveAndEnabled) Apply(true);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (mode == DetectionMode.Manual && manualTarget == transform)
            {
                Debug.LogWarning($"{nameof(ParentCollisionIgnorer)}: manualTarget cannot be self.", this);
                manualTarget = null;
            }
        }
#endif
    }
}

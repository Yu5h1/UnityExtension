using UnityEngine;

namespace Yu5h1Lib
{
    [DisallowMultipleComponent]
    public class ParentSwitcher : MonoBehaviour
    {
        [Tooltip("Transform whose parent will be switched. Null = use this GameObject's transform.")]
        [SerializeField] private Transform target;

        [Tooltip("Available parents. index selects which one becomes the active parent. A null entry logs a warning and unparents to scene root (currentIndex -1).")]
        [SerializeField] private Transform[] parents;

        [Tooltip("SetParent behavior. True = keep world pose. False = keep local pose.")]
        [SerializeField] private bool worldPositionStays = false;

        [SerializeField] private int currentIndex = 0;

        public int CurrentIndex => currentIndex;

        /// <summary>
        /// Switch the target's parent to <c>parents[index]</c>. A null entry unparents to the
        /// scene root. Returns false when the index is out of range or the target is already
        /// parented to the desired transform.
        /// </summary>
        public bool TryApply(int index, bool worldPositionStays)
        {
            if (parents.IsEmpty() || !parents.IsValid(index))
                return false;

            if (parents[index] == null)
            {
                Debug.LogWarning($"[{nameof(ParentSwitcher)}] parents[{index}] is null — unparenting to scene root.", this);
                return Unparent(worldPositionStays);
            }

            return SetTargetParent(parents[index], index, worldPositionStays);
        }

        /// <summary>Detach the target from its parent (move to scene root). currentIndex becomes -1.</summary>
        public bool Unparent(bool worldPositionStays) => SetTargetParent(null, -1, worldPositionStays);

        private bool SetTargetParent(Transform desiredParent, int index, bool worldPositionStays)
        {
            var actualTarget = target != null ? target : transform;

            if (actualTarget.parent == desiredParent)
            {
                currentIndex = index;
                return false;
            }

            actualTarget.SetParent(desiredParent, worldPositionStays);
            if (!worldPositionStays)
            {
                actualTarget.localPosition = Vector3.zero;
                actualTarget.localEulerAngles = Vector3.zero;
            }
            currentIndex = index;
            return true;
        }

        [ContextMenu(nameof(Apply))]
        public void Apply() => TryApply(currentIndex, worldPositionStays);

        [ContextMenu(nameof(Unparent))]
        private void Unparent() => Unparent(worldPositionStays);
    }
}
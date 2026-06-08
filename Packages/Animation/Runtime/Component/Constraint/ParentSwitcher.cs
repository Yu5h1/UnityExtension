using UnityEngine;

namespace Yu5h1Lib
{
    [DisallowMultipleComponent]
    public class ParentSwitcher : MonoBehaviour
    {
        [Tooltip("Transform whose parent will be switched. Null = use this GameObject's transform.")]
        [SerializeField] private Transform target;

        [Tooltip("Available parents. index selects which one becomes the active parent.")]
        [SerializeField] private Transform[] parents;

        [Tooltip("SetParent behavior. True = keep world pose. False = keep local pose.")]
        [SerializeField] private bool worldPositionStays = false;

        [SerializeField] private int currentIndex = 0;

        public int CurrentIndex => currentIndex;

        public bool TryApply(int index,bool worldPositionStays)
        {
            if (parents.IsEmpty() || !parents.IsValid(index))
                return false;

            var desiredParent = parents[index];
            if (desiredParent == null)
                return false;
            var actualTarget = target != null ? target : transform;

            if (actualTarget.parent == desiredParent)
            {
                currentIndex = index;
                return false;
            }

            actualTarget.SetParent(desiredParent, worldPositionStays);
            currentIndex = index;
            return true;
        }
        [ContextMenu(nameof(Apply))]
        public void Apply() => TryApply(currentIndex, worldPositionStays);
    }
}
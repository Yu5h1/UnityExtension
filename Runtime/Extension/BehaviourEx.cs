using UnityEngine;

namespace Yu5h1Lib
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never), System.ComponentModel.Browsable(false)]
    public static class BehaviourEx
    {
        /// <summary>
        /// get component if null
        /// </summary>
        public static T GetComponent<T>(this Behaviour b, ref T component) where T : Component
        {
            if (!component)
                component = b.GetComponent<T>();
            return component;
        }
        public static bool IsAvailable(this Behaviour b) => b != null && b.isActiveAndEnabled;
    }
}

using System.ComponentModel;
using UnityEngine;

namespace Yu5h1Lib
{
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
    public static class AnimatorEx 
    {
        /// <summary>
        /// Set weight of the layer by Name to 1 and others to 0.
        /// </summary>
        /// <param name="c"></param>
        public static void ToggleLayer(this Animator animator,string layerName) 
        {
            if (animator == null) return;

            int layerIndex = animator.IndexOfLayer(layerName);
            if (layerIndex < 0)
                return;
            var weight = animator.GetLayerWeight(layerIndex);
            animator.SetLayerWeight(layerIndex, weight > 0 ? 0 : 1);
        }
        public static int IndexOfLayer(this Animator animator, string layerName)
        {
            if (animator == null) return -1;
            for (int i = 0; i < animator.layerCount; i++)
                if (animator.GetLayerName(i) == layerName) return i;
            return -1;
        }
        public static void SetLayerWeight(this Animator animator, string layerName, float weight) 
        {
            if (animator == null) return;
            int layerIndex = animator.IndexOfLayer(layerName);
            if (layerIndex < 0)
                return;
            animator.SetLayerWeight(layerIndex, weight);
        }


    }

}
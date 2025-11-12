using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never), System.ComponentModel.Browsable(false)]
    public static class AnimatorEx
    {
        public static List<T> GetBehavioursByLayer<T>(this Animator animator, int layerIndex)
            where T : StateMachineBehaviour
        {
            var behavioursInLayer = new List<T>();

            foreach (var behaviour in animator.GetBehaviours<T>())
            {
                var stateInfo = animator.GetCurrentAnimatorStateInfo(layerIndex);

                if (animator.HasState(layerIndex, stateInfo.fullPathHash))
                {
                    behavioursInLayer.Add(behaviour);
                }
            }

            return behavioursInLayer;
        }


        #region Rig
        public static Dictionary<HumanBodyBones, Transform> GetHumanBodyBonesData(this Animator animator)
        {
            Dictionary<HumanBodyBones, Transform> result = new Dictionary<HumanBodyBones, Transform>();

            var bonesName = System.Enum.GetValues(typeof(HumanBodyBones));
            for (int i = 0; i < bonesName.Length - 1; i++)
            {
                HumanBodyBones key = (HumanBodyBones)bonesName.GetValue(i);
                result.Add(key, animator.GetBoneTransform(key));
            }
            return result;
        }
        public static Transform[] GetHumanBodyBones(this Animator animator)
        {
            List<Transform> results = new List<Transform>();
            foreach (var item in GetHumanBodyBonesData(animator))
            {
                if (item.Value != null)
                {
                    results.Add(item.Value);
                }
            }
            return results.ToArray();
        } 
        #endregion

    }
}

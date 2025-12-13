using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace GenerativeMotion.Editor
{
    public static class AnimatorContextMenus
    {
        private const string MENU_PATH = "CONTEXT/Animator/Reset to Bind Pose";

        [MenuItem(MENU_PATH, priority = 100)]
        private static void ResetToBindPose(MenuCommand command)
        {
            Animator animator = command.context as Animator;
            if (animator == null) return;

            if (!animator.isHuman)
            {
                Debug.LogWarning("[ResetToBindPose] Animator is not Humanoid.");
                return;
            }

            Avatar avatar = animator.avatar;
            if (avatar == null)
            {
                Debug.LogWarning("[ResetToBindPose] Animator has no Avatar assigned.");
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(avatar);
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogWarning("[ResetToBindPose] Cannot find source asset for Avatar.");
                return;
            }

            GameObject sourceModel = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (sourceModel == null)
            {
                Debug.LogWarning($"[ResetToBindPose] Cannot load source model from: {assetPath}");
                return;
            }

            var sourceAnimator = sourceModel.GetComponent<Animator>();
            if (sourceAnimator == null || !sourceAnimator.isHuman)
            {
                Debug.LogWarning("[ResetToBindPose] Source model has no Humanoid Animator.");
                return;
            }

            var allBones = GetAllHumanoidTransforms(animator);
            foreach (var bone in allBones)
            {
                if (bone != null)
                {
                    Undo.RecordObject(bone, "Reset to Bind Pose");
                }
            }

            int resetCount = 0;
            for (int i = 0; i < (int)HumanBodyBones.LastBone; i++)
            {
                HumanBodyBones boneType = (HumanBodyBones)i;
                
                Transform targetBone = animator.GetBoneTransform(boneType);
                Transform sourceBone = sourceAnimator.GetBoneTransform(boneType);

                if (targetBone != null && sourceBone != null)
                {
                    targetBone.localPosition = sourceBone.localPosition;
                    targetBone.localRotation = sourceBone.localRotation;
                    targetBone.localScale = sourceBone.localScale;
                    resetCount++;
                }
            }

            Debug.Log($"[ResetToBindPose] Reset {resetCount} bones to bind pose from: {assetPath}");
            
            SceneView.RepaintAll();
        }

        [MenuItem(MENU_PATH, validate = true)]
        private static bool ResetToBindPoseValidate(MenuCommand command)
        {
            Animator animator = command.context as Animator;
            return animator != null && animator.isHuman && animator.avatar != null;
        }

        private static List<Transform> GetAllHumanoidTransforms(Animator animator)
        {
            var result = new List<Transform>();
            
            for (int i = 0; i < (int)HumanBodyBones.LastBone; i++)
            {
                var bone = animator.GetBoneTransform((HumanBodyBones)i);
                if (bone != null)
                {
                    result.Add(bone);
                }
            }

            return result;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>
    ///   Builds a humanoid <see cref="Avatar"/> from an imported character hierarchy. Bones are matched through
    ///   <see cref="HumanoidBoneMap"/>; the node transforms as imported are the base skeleton's T-pose, so they
    ///   are written unchanged as the skeleton pose. A mapped joint that the hierarchy lacks is skipped (finger,
    ///   toe, eye and jaw bones are optional); the Avatar is not valid when a required bone is missing.
    /// </summary>
    public static class HumanoidAvatarBuilder
    {
        public sealed class Result
        {
            public Avatar avatar;
            public readonly List<string> missingJoints = new List<string>();
            public int mappedBones;
            public bool IsHuman => avatar != null && avatar.isValid && avatar.isHuman;
        }

        public static Result Build(GameObject root, HumanoidBoneMap map)
        {
            var result = new Result();
            var transforms = root.GetComponentsInChildren<Transform>(true);
            var byName = new Dictionary<string, Transform>();
            foreach (var transform in transforms)
                if (!byName.ContainsKey(transform.name))
                    byName.Add(transform.name, transform);

            var humanNames = HumanTrait.BoneName;
            var human = new List<HumanBone>();
            foreach (var pair in map.JointOf)
            {
                if (!byName.ContainsKey(pair.Value))
                {
                    result.missingJoints.Add(pair.Value);
                    continue;
                }
                var limit = new HumanLimit { useDefaultValues = true };
                human.Add(new HumanBone { humanName = humanNames[(int)pair.Key], boneName = pair.Value, limit = limit });
            }

            var skeleton = new List<SkeletonBone>(transforms.Length);
            foreach (var transform in transforms)
                skeleton.Add(new SkeletonBone
                {
                    name = transform.name,
                    position = transform.localPosition,
                    rotation = transform.localRotation,
                    scale = transform.localScale
                });

            var description = new HumanDescription
            {
                human = human.ToArray(),
                skeleton = skeleton.ToArray(),
                upperArmTwist = 0.5f,
                lowerArmTwist = 0.5f,
                upperLegTwist = 0.5f,
                lowerLegTwist = 0.5f,
                armStretch = 0.05f,
                legStretch = 0.05f,
                feetSpacing = 0f,
                hasTranslationDoF = false
            };
            result.mappedBones = human.Count;
            result.avatar = AvatarBuilder.BuildHumanAvatar(root, description);
            result.avatar.name = root.name + "Avatar";
            return result;
        }
    }
}

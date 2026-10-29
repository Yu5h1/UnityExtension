using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>
    ///   The fixed table that maps the joint names of the Limb base skeleton to <see cref="HumanBodyBones"/>.
    ///   The table is a byte-identical copy of <c>CharacterBuilder/skeleton/humanoid.bones.json</c> shipped
    ///   beside this file, because the skeleton document owns the joint names; <c>unmapped</c> lists the joints
    ///   that have no human bone (the ground <c>root</c>, hand-held <c>prop_*</c> anchors and end points).
    /// </summary>
    public sealed class HumanoidBoneMap
    {
        public const string PackageAssetPath = "Packages/com.yu5h1.animation/Editor/Humanoid/humanoid.bones.json";

        [Serializable]
        class Entry
        {
            public string bone;
            public string joint;
        }

        [Serializable]
        class Document
        {
            public string format;
            public string version;
            public string skeleton;
            public Entry[] bones;
            public string[] unmapped;
        }

        readonly Dictionary<HumanBodyBones, string> _jointOf = new Dictionary<HumanBodyBones, string>();

        public string Version { get; private set; }
        public IReadOnlyDictionary<HumanBodyBones, string> JointOf => _jointOf;

        public static HumanoidBoneMap Load()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(PackageAssetPath);
            if (asset == null)
                throw new InvalidOperationException($"Humanoid bone map not found at {PackageAssetPath}.");
            return Parse(asset.text);
        }

        public static HumanoidBoneMap Parse(string json)
        {
            var document = JsonUtility.FromJson<Document>(json);
            if (document == null || document.bones == null || document.format != "yu5h1.humanoid-bones.draft")
                throw new FormatException("Not a yu5h1.humanoid-bones document.");
            var map = new HumanoidBoneMap { Version = document.version };
            foreach (var entry in document.bones)
            {
                if (!Enum.TryParse(entry.bone, out HumanBodyBones bone))
                    throw new FormatException($"Unknown human bone '{entry.bone}'.");
                map._jointOf[bone] = entry.joint;
            }
            return map;
        }
    }
}

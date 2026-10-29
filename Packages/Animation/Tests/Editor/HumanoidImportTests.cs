using System;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.TestTools;
using Yu5h1Lib.EditorExtension;
using Object = UnityEngine.Object;

namespace Yu5h1Lib.Tests
{
    public class HumanoidImportTests
    {
        const string Folder = "Assets/__HumanoidImportTests";
        const float Frame = 1f / 60f;

        GameObject _skeleton;

        [SetUp]
        public void SetUp()
        {
            _skeleton = BuildSkeleton();
        }

        [TearDown]
        public void TearDown()
        {
            if (_skeleton != null)
                Object.DestroyImmediate(_skeleton);
            if (AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.DeleteAsset(Folder);
        }

        static GameObject BuildSkeleton()
        {
            var top = new GameObject("Character");
            Transform Add(Transform parent, string name, float x, float y, float z)
            {
                var child = new GameObject(name).transform;
                child.SetParent(parent, false);
                child.localPosition = new Vector3(x, y, z);
                return child;
            }
            var root = Add(top.transform, "root", 0, 0, 0);
            var pelvis = Add(root, "pelvis", 0, 0.95f, 0);
            foreach (var side in new[] { ("l", -1f), ("r", 1f) })
            {
                var hip = Add(pelvis, "hip_" + side.Item1, 0.1f * side.Item2, -0.05f, 0);
                var knee = Add(hip, "knee_" + side.Item1, 0, -0.42f, 0);
                var ankle = Add(knee, "ankle_" + side.Item1, 0, -0.42f, 0);
                Add(ankle, "toe_" + side.Item1, 0, -0.06f, 0.13f);
            }
            var spine = Add(pelvis, "spine", 0, 0.12f, 0);
            var chest = Add(spine, "chest", 0, 0.18f, 0);
            var neck = Add(chest, "neck", 0, 0.2f, 0);
            Add(neck, "head", 0, 0.11f, 0);
            foreach (var side in new[] { ("l", -1f), ("r", 1f) })
            {
                var shoulder = Add(chest, "shoulder_" + side.Item1, 0.17f * side.Item2, 0.14f, 0);
                var elbow = Add(shoulder, "elbow_" + side.Item1, 0.28f * side.Item2, 0, 0);
                Add(elbow, "wrist_" + side.Item1, 0.25f * side.Item2, 0, 0);
            }
            return top;
        }

        static Transform Find(GameObject top, string name)
            => top.GetComponentsInChildren<Transform>(true).First(transform => transform.name == name);

        static HumanoidImportSettings Settings(string node = "root")
        {
            var settings = ScriptableObject.CreateInstance<HumanoidImportSettings>();
            settings.motionNode = node;
            return settings;
        }

        static AnimationClip PathClip(string bonePath, Vector3 from, Vector3 to, float length = 1f)
        {
            var clip = new AnimationClip { name = "walk", frameRate = 30f };
            var axes = new[] { "x", "y", "z" };
            for (var i = 0; i < 3; i++)
                clip.SetCurve(bonePath, typeof(Transform), "localPosition." + axes[i], AnimationCurve.Linear(0f, from[i], length, to[i]));
            return clip;
        }

        static Avatar HumanAvatar(GameObject top)
            => HumanoidAvatarBuilder.Build(top, HumanoidBoneMap.Load()).avatar;

        static AnimationClip Humanoid(GameObject top, AnimationClip generic, HumanoidImportSettings settings)
        {
            var instance = Object.Instantiate(top);
            var avatar = HumanAvatar(top);
            try
            {
                return HumanoidClipBuilder.Build(instance, avatar, generic, settings).clip;
            }
            finally
            {
                Object.DestroyImmediate(instance);
                Object.DestroyImmediate(avatar);
            }
        }

        sealed class Playback : IDisposable
        {
            public readonly GameObject character;
            public readonly Animator animator;
            readonly Avatar _avatar;

            public Playback(GameObject skeleton, AnimationClip clip)
            {
                character = Object.Instantiate(skeleton);
                _avatar = HumanAvatar(skeleton);
                var controller = new AnimatorController();
                controller.AddLayer("Base");
                var machine = controller.layers[0].stateMachine;
                var state = machine.AddState("play");
                state.motion = clip;
                machine.defaultState = state;
                animator = character.GetComponent<Animator>();
                if (animator == null)
                    animator = character.AddComponent<Animator>();
                animator.avatar = _avatar;
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = true;
                animator.Rebind();
                animator.Update(0f);
            }

            public void Advance(float seconds)
            {
                for (var t = 0f; t < seconds - Frame * 0.5f; t += Frame)
                    animator.Update(Frame);
            }

            public Vector3 Bone(string name) => Find(character, name).position;

            public void Dispose()
            {
                Object.DestroyImmediate(character);
                Object.DestroyImmediate(_avatar);
            }
        }

        [Test]
        public void BoneMapMapsEveryHumanBoneOnce()
        {
            var map = HumanoidBoneMap.Load();
            Assert.AreEqual(55, map.JointOf.Count);
            Assert.AreEqual(55, map.JointOf.Values.Distinct().Count());
            Assert.AreEqual("pelvis", map.JointOf[HumanBodyBones.Hips]);
            Assert.AreEqual("clavicle_l", map.JointOf[HumanBodyBones.LeftShoulder]);
            Assert.AreEqual("shoulder_l", map.JointOf[HumanBodyBones.LeftUpperArm]);
        }

        [Test]
        public void BaseSkeletonMakesAValidHumanAvatar()
        {
            var result = HumanoidAvatarBuilder.Build(_skeleton, HumanoidBoneMap.Load());
            try
            {
                Assert.IsTrue(result.avatar.isValid);
                Assert.IsTrue(result.avatar.isHuman);
                Assert.AreEqual(19, result.mappedBones);
                Assert.Contains("clavicle_l", result.missingJoints);
            }
            finally
            {
                Object.DestroyImmediate(result.avatar);
            }
        }

        [Test]
        public void ARequiredBoneMissingMakesNoHumanAvatar()
        {
            Find(_skeleton, "elbow_l").name = "arm_bend_l";
            LogAssert.Expect(LogType.Error, new Regex("Required human bone"));
            var result = HumanoidAvatarBuilder.Build(_skeleton, HumanoidBoneMap.Load());
            try
            {
                Assert.IsFalse(result.IsHuman);
            }
            finally
            {
                Object.DestroyImmediate(result.avatar);
            }
        }

        [Test]
        public void ClipIsHumanoidAndLoopSettingsFollow()
        {
            var settings = Settings();
            settings.loopTime = true;
            settings.loopPose = true;
            var clip = Humanoid(_skeleton, PathClip("root", Vector3.zero, Vector3.forward), settings);
            var clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
            Assert.IsTrue(clip.humanMotion);
            Assert.IsTrue(clip.isLooping);
            Assert.IsTrue(clipSettings.loopTime);
            Assert.IsTrue(clipSettings.loopBlend);
        }

        [Test]
        public void RootNodePathMovesTheGameObject()
        {
            var clip = Humanoid(_skeleton, PathClip("root", Vector3.zero, new Vector3(0f, 0f, 1f)), Settings());
            using (var playback = new Playback(_skeleton, clip))
            {
                playback.Advance(1f);
                Assert.AreEqual(1f, playback.character.transform.position.z, 0.02f);
                Assert.AreEqual(0f, playback.character.transform.position.x, 0.01f);
            }
        }

        [Test]
        public void BakingPlanarMovementKeepsTheGameObjectAndMovesThePose()
        {
            var settings = Settings();
            settings.rootPositionXZ.bakeIntoPose = true;
            var clip = Humanoid(_skeleton, PathClip("root", Vector3.zero, new Vector3(0f, 0f, 1f)), settings);
            using (var playback = new Playback(_skeleton, clip))
            {
                var start = playback.Bone("pelvis");
                playback.Advance(1f);
                Assert.AreEqual(0f, playback.character.transform.position.z, 0.01f);
                Assert.AreEqual(start.z + 1f, playback.Bone("pelvis").z, 0.02f);
            }
        }

        [Test]
        public void BakingHeightKeepsTheGameObjectLevelAndOffsetLiftsThePose()
        {
            var settings = Settings();
            settings.rootPositionY.bakeIntoPose = true;
            settings.rootPositionY.offset = 0.3f;
            var clip = Humanoid(_skeleton, PathClip("root", Vector3.zero, new Vector3(0f, 0.5f, 0f)), settings);
            using (var playback = new Playback(_skeleton, clip))
            {
                var start = playback.Bone("pelvis");
                Assert.AreEqual(0.95f + 0.3f, start.y, 0.02f);
                playback.Advance(1f);
                Assert.AreEqual(0f, playback.character.transform.position.y, 0.01f);
                Assert.AreEqual(0.95f + 0.3f + 0.5f, playback.Bone("pelvis").y, 0.02f);
            }
        }

        [Test]
        public void BakedRotationOffsetTurnsThePoseNotTheGameObject()
        {
            var settings = Settings();
            settings.rootRotation.bakeIntoPose = true;
            settings.rootRotation.offset = 90f;
            var clip = Humanoid(_skeleton, PathClip("root", Vector3.zero, Vector3.zero), settings);
            using (var playback = new Playback(_skeleton, clip))
            {
                var forward = playback.Bone("toe_l") - playback.Bone("ankle_l");
                forward.y = 0f;
                Assert.AreEqual(0f, playback.character.transform.eulerAngles.y, 0.01f);
                Assert.AreEqual(1f, Vector3.Dot(forward.normalized, Vector3.right), 0.02f);
            }
        }

        [Test]
        public void PelvisCanBeTheMotionSourceWhenRootHasNoTrack()
        {
            var clip = Humanoid(_skeleton, PathClip("root/pelvis", new Vector3(0f, 0.95f, 0f), new Vector3(0f, 0.95f, 0.6f)), Settings("pelvis"));
            using (var playback = new Playback(_skeleton, clip))
            {
                playback.Advance(1f);
                Assert.AreEqual(0.6f, playback.character.transform.position.z, 0.02f);
                Assert.AreEqual(0.95f, playback.Bone("pelvis").y, 0.02f);
            }
        }

        [Test]
        public void UnknownMotionNodeIsRefused()
        {
            Assert.Throws<InvalidOperationException>(() => Humanoid(_skeleton, PathClip("root", Vector3.zero, Vector3.forward), Settings("nowhere")));
        }

        [Test]
        public void GeneratingTwiceKeepsTheSameAvatarAsset()
        {
            AssetDatabase.CreateFolder("Assets", "__HumanoidImportTests");
            var prefab = PrefabUtility.SaveAsPrefabAsset(_skeleton, Folder + "/character.prefab");
            var settings = Settings();
            settings.source = prefab;
            AssetDatabase.CreateAsset(settings, Folder + "/character.humanoid.asset");

            Assert.IsTrue(HumanoidImporter.Generate(settings).success);
            var first = settings.avatar;
            Assert.IsNotNull(first);
            Assert.IsTrue(first.isValid && first.isHuman);

            Assert.IsTrue(HumanoidImporter.Generate(settings).success);
            Assert.AreSame(first, settings.avatar);
            Assert.IsTrue(settings.avatar.isValid && settings.avatar.isHuman);
            Assert.AreEqual(1, AssetDatabase.LoadAllAssetsAtPath(Folder + "/character.humanoid.asset").OfType<Avatar>().Count());
        }
    }
}

using System;
using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>
    ///   Converts a generic clip into a humanoid clip. The clip is sampled on the source hierarchy at its own frame
    ///   rate and every sample is read as a human pose: the muscles and the body transform. The movement of the
    ///   motion node is the root path; its unbaked channels become <c>MotionT</c>/<c>MotionQ</c>, the whole body
    ///   transform becomes <c>RootT</c>/<c>RootQ</c>. The Animator removes the motion from the body to get the pose
    ///   and applies the change of the motion to the GameObject, as it does for a clip from ModelImporter.
    ///   <para>
    ///   Playing therefore shows the generic clip as seen from the root path at the first frame: an unbaked heading
    ///   or position starts at the GameObject, and a baked channel stays in the pose as the generic clip has it. An
    ///   offset moves the pose of a baked channel in the GameObject's frame; the offset of a channel that is not
    ///   baked has no effect, because that channel is the motion itself. The sampled instance has no Avatar, because
    ///   an Animator that has a humanoid Avatar ignores the transform curves of its bones. Lengths are stored in
    ///   units of the source skeleton's human scale, which the Animator multiplies by the playing character's scale.
    ///   </para>
    /// </summary>
    public static class HumanoidClipBuilder
    {
        public sealed class Result
        {
            public AnimationClip clip;
            public int frames;
            public Vector3 motionEnd;
            public float motionYawEnd;
        }

        public static Result Build(GameObject instance, Avatar avatar, AnimationClip source, HumanoidImportSettings settings)
        {
            var node = FindNode(instance.transform, settings.motionNode);
            if (node == null)
                throw new InvalidOperationException($"Motion node '{settings.motionNode}' is not in '{instance.name}'.");

            var animator = instance.GetComponent<Animator>();
            if (animator == null)
                animator = instance.AddComponent<Animator>();
            animator.avatar = avatar;
            var humanScale = animator.humanScale;
            animator.avatar = null;

            var restPosition = Relative(instance.transform, node.position);
            var restRotation = Quaternion.Inverse(instance.transform.rotation) * node.rotation;

            var frameRate = source.frameRate > 0f ? source.frameRate : 30f;
            var frames = Mathf.Max(2, Mathf.RoundToInt(source.length * frameRate) + 1);
            var handler = new HumanPoseHandler(avatar, instance.transform);
            var pose = new HumanPose();

            var times = new float[frames];
            var muscles = new float[frames][];
            var bodies = new Matrix4x4[frames];
            var paths = new Vector3[frames];
            var headings = new Quaternion[frames];
            for (var f = 0; f < frames; f++)
            {
                times[f] = Mathf.Min(f / frameRate, source.length);
                source.SampleAnimation(instance, times[f]);
                handler.GetHumanPose(ref pose);
                muscles[f] = (float[])pose.muscles.Clone();
                bodies[f] = Matrix4x4.TRS(pose.bodyPosition * humanScale, pose.bodyRotation, Vector3.one);
                paths[f] = Relative(instance.transform, node.position) - restPosition;
                headings[f] = Heading(Quaternion.Inverse(instance.transform.rotation) * node.rotation * Quaternion.Inverse(restRotation));
            }

            var bakeRotation = settings.rootRotation.bakeIntoPose;
            var bakeHeight = settings.rootPositionY.bakeIntoPose;
            var bakePlanar = settings.rootPositionXZ.bakeIntoPose;

            var motions = new Matrix4x4[frames];
            for (var f = 0; f < frames; f++)
            {
                var motionPosition = new Vector3(bakePlanar ? 0f : paths[f].x, bakeHeight ? 0f : paths[f].y, bakePlanar ? 0f : paths[f].z);
                motions[f] = Matrix4x4.TRS(motionPosition, bakeRotation ? Quaternion.identity : headings[f], Vector3.one);
            }

            var offset = Matrix4x4.TRS(
                new Vector3(bakePlanar ? settings.rootPositionXZ.offset.x : 0f, bakeHeight ? settings.rootPositionY.offset : 0f, bakePlanar ? settings.rootPositionXZ.offset.y : 0f),
                Quaternion.Euler(0f, bakeRotation ? settings.rootRotation.offset : 0f, 0f),
                Vector3.one);
            var correction = motions[0] * offset * motions[0].inverse;

            var muscleCount = HumanTrait.MuscleCount;
            var muscleKeys = new float[muscleCount][];
            for (var m = 0; m < muscleCount; m++)
                muscleKeys[m] = new float[frames];
            var rootT = new float[3][];
            var rootQ = new float[4][];
            var motionT = new float[3][];
            var motionQ = new float[4][];
            for (var i = 0; i < 3; i++) { rootT[i] = new float[frames]; motionT[i] = new float[frames]; }
            for (var i = 0; i < 4; i++) { rootQ[i] = new float[frames]; motionQ[i] = new float[frames]; }

            var previousBody = Quaternion.identity;
            var previousMotion = Quaternion.identity;
            var result = new Result { frames = frames };
            for (var f = 0; f < frames; f++)
            {
                var body = correction * bodies[f];
                var bodyRotation = SameHemisphere(body.rotation, previousBody, f);
                var motionRotation = SameHemisphere(motions[f].rotation, previousMotion, f);
                previousBody = bodyRotation;
                previousMotion = motionRotation;
                var motionPosition = (Vector3)motions[f].GetColumn(3);

                for (var m = 0; m < muscleCount; m++)
                    muscleKeys[m][f] = muscles[f][m];
                Store(rootT, f, (Vector3)body.GetColumn(3) / humanScale);
                Store(rootQ, f, bodyRotation);
                Store(motionT, f, motionPosition / humanScale);
                Store(motionQ, f, motionRotation);
                result.motionEnd = motionPosition;
                result.motionYawEnd = motionRotation.eulerAngles.y;
            }

            var clip = new AnimationClip { name = source.name, frameRate = frameRate };
            var muscleNames = HumanTrait.MuscleName;
            for (var m = 0; m < muscleCount; m++)
                SetCurve(clip, muscleNames[m], times, muscleKeys[m]);
            SetVector(clip, "RootT", rootT, times, "xyz");
            SetVector(clip, "RootQ", rootQ, times, "xyzw");
            SetVector(clip, "MotionT", motionT, times, "xyz");
            SetVector(clip, "MotionQ", motionQ, times, "xyzw");

            var clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
            clipSettings.startTime = 0f;
            clipSettings.stopTime = times[frames - 1];
            clipSettings.loopTime = settings.loopTime;
            clipSettings.loopBlend = settings.loopPose;
            clipSettings.loopBlendOrientation = bakeRotation;
            clipSettings.loopBlendPositionY = bakeHeight;
            clipSettings.loopBlendPositionXZ = bakePlanar;
            AnimationUtility.SetAnimationClipSettings(clip, clipSettings);
            clip.wrapMode = settings.loopTime ? WrapMode.Loop : WrapMode.Default;

            result.clip = clip;
            return result;
        }

        public static Transform FindNode(Transform root, string name)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                if (transform.name == name)
                    return transform;
            return null;
        }

        static Vector3 Relative(Transform root, Vector3 worldPosition) => root.InverseTransformPoint(worldPosition);

        /// <summary>
        ///   The rotation about the vertical axis of <paramref name="rotation"/> (its twist around Y).
        /// </summary>
        public static Quaternion Heading(Quaternion rotation)
        {
            var length = Mathf.Sqrt(rotation.y * rotation.y + rotation.w * rotation.w);
            if (length < 1e-6f)
                return Quaternion.identity;
            return new Quaternion(0f, rotation.y / length, 0f, rotation.w / length);
        }

        static Quaternion SameHemisphere(Quaternion value, Quaternion previous, int frame)
            => frame > 0 && Quaternion.Dot(value, previous) < 0f ? new Quaternion(-value.x, -value.y, -value.z, -value.w) : value;

        static void Store(float[][] channels, int frame, Vector3 value)
        {
            channels[0][frame] = value.x;
            channels[1][frame] = value.y;
            channels[2][frame] = value.z;
        }

        static void Store(float[][] channels, int frame, Quaternion value)
        {
            channels[0][frame] = value.x;
            channels[1][frame] = value.y;
            channels[2][frame] = value.z;
            channels[3][frame] = value.w;
        }

        static void SetVector(AnimationClip clip, string name, float[][] channels, float[] times, string axes)
        {
            for (var i = 0; i < axes.Length; i++)
                SetCurve(clip, name + "." + axes[i], times, channels[i]);
        }

        static void SetCurve(AnimationClip clip, string property, float[] times, float[] values)
        {
            var keys = new Keyframe[times.Length];
            for (var i = 0; i < keys.Length; i++)
            {
                var outgoing = 0f;
                if (i < keys.Length - 1)
                    outgoing = (values[i + 1] - values[i]) / Mathf.Max(1e-6f, times[i + 1] - times[i]);
                var incoming = i > 0 ? (values[i] - values[i - 1]) / Mathf.Max(1e-6f, times[i] - times[i - 1]) : outgoing;
                keys[i] = new Keyframe(times[i], values[i], incoming, outgoing);
            }
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), property), new AnimationCurve(keys));
        }
    }
}

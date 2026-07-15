using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    public sealed class AnimationToolWindow : EditorWindow
    {
        private const string WindowTitle = "Animation Tool";
        private const string MenuPath = "Tools/Yu5h1/Animation/Animation Tool";

        private Animator _animator;
        private AnimationClip _clip;
        private float _time;
        private bool _includeBodyTransform = true;
        private string _status;

        [MenuItem(MenuPath)]
        public static void Open()
        {
            GetWindow<AnimationToolWindow>(WindowTitle);
        }

        private void OnEnable()
        {
            RefreshFromAnimationWindow();
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh From Animation Window"))
                    RefreshFromAnimationWindow();
            }

            EditorGUILayout.Space();

            _clip = (AnimationClip)EditorGUILayout.ObjectField("Animation Clip", _clip, typeof(AnimationClip), false);
            _time = Mathf.Max(0f, EditorGUILayout.FloatField("Time", _time));
            _animator = (Animator)EditorGUILayout.ObjectField("Animator", _animator, typeof(Animator), true);
            _includeBodyTransform = EditorGUILayout.Toggle("Include Body Transform", _includeBodyTransform);

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(!CanCapture()))
            {
                if (GUILayout.Button("Capture Current Pose To Clip"))
                    CaptureCurrentPoseToClip();
            }

            if (!string.IsNullOrEmpty(_status))
                EditorGUILayout.HelpBox(_status, MessageType.Info);
        }

        private bool CanCapture()
        {
            return _animator != null
                && _animator.avatar != null
                && _animator.avatar.isValid
                && _animator.avatar.isHuman
                && _clip != null;
        }

        private void RefreshFromAnimationWindow()
        {
            var animationWindow = AnimationWindowReflection.GetOpenAnimationWindow();
            if (animationWindow != null)
            {
                var state = AnimationWindowReflection.GetState(animationWindow);
                _clip = AnimationWindowReflection.GetActiveAnimationClip(state);
                _time = AnimationWindowReflection.GetCurrentTime(state);

                var root = AnimationWindowReflection.GetActiveRootGameObject(state);
                if (root != null)
                    _animator = root.GetComponentInParent<Animator>();
            }

            if (_animator == null && Selection.activeGameObject != null)
                _animator = Selection.activeGameObject.GetComponentInParent<Animator>();

            _status = _clip == null
                ? "Open Animation Window and select an editable Humanoid AnimationClip."
                : null;

            Repaint();
        }

        private void CaptureCurrentPoseToClip()
        {
            if (!CanCapture())
            {
                _status = "Animator must have a valid Humanoid Avatar, and Animation Window must have an active clip.";
                return;
            }

            if (IsModelEmbeddedClip(_clip))
            {
                _status = "This clip is embedded in a model asset. Extract or duplicate it as .anim before writing keys.";
                return;
            }

            var pose = new HumanPose();
            using (var handler = new HumanPoseHandler(_animator.avatar, _animator.transform))
                handler.GetHumanPose(ref pose);

            try
            {
                Undo.RecordObject(_clip, "Capture Humanoid Pose");

                for (var i = 0; i < HumanTrait.MuscleCount; i++)
                    AddOrMoveKey(_clip, HumanTrait.MuscleName[i], _time, pose.muscles[i]);

                if (_includeBodyTransform)
                {
                    AddOrMoveKey(_clip, "RootT.x", _time, pose.bodyPosition.x);
                    AddOrMoveKey(_clip, "RootT.y", _time, pose.bodyPosition.y);
                    AddOrMoveKey(_clip, "RootT.z", _time, pose.bodyPosition.z);
                    AddOrMoveKey(_clip, "RootQ.x", _time, pose.bodyRotation.x);
                    AddOrMoveKey(_clip, "RootQ.y", _time, pose.bodyRotation.y);
                    AddOrMoveKey(_clip, "RootQ.z", _time, pose.bodyRotation.z);
                    AddOrMoveKey(_clip, "RootQ.w", _time, pose.bodyRotation.w);
                }

                EditorUtility.SetDirty(_clip);
                AssetDatabase.SaveAssets();

                _status = $"Captured current Humanoid pose to {_clip.name} at {_time:0.###}s.";
            }
            catch (Exception exception)
            {
                _status = $"Failed to capture pose: {exception.Message}";
                Debug.LogException(exception);
            }
        }

        private static void AddOrMoveKey(AnimationClip clip, string propertyName, float time, float value)
        {
            var binding = EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), propertyName);
            var curve = AnimationUtility.GetEditorCurve(clip, binding) ?? new AnimationCurve();
            var key = new Keyframe(time, value);
            var index = FindKeyAtTime(curve, time);

            if (index >= 0)
                curve.MoveKey(index, key);
            else
                curve.AddKey(key);

            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }

        private static int FindKeyAtTime(AnimationCurve curve, float time)
        {
            const float tolerance = 0.0001f;
            var keys = curve.keys;
            for (var i = 0; i < keys.Length; i++)
            {
                if (Mathf.Abs(keys[i].time - time) <= tolerance)
                    return i;
            }

            return -1;
        }

        private static bool IsModelEmbeddedClip(AnimationClip clip)
        {
            var path = AssetDatabase.GetAssetPath(clip);
            return AssetDatabase.IsSubAsset(clip) && AssetImporter.GetAtPath(path) is ModelImporter;
        }

        private static class AnimationWindowReflection
        {
            private static readonly Type WindowType = Type.GetType("UnityEditor.AnimationWindow,UnityEditor");
            private static readonly FieldInfo StateField = WindowType?.GetField("m_State", BindingFlags.Instance | BindingFlags.NonPublic);

            public static EditorWindow GetOpenAnimationWindow()
            {
                if (WindowType == null)
                    return null;

                var windows = Resources.FindObjectsOfTypeAll(WindowType);
                return windows.Length > 0 ? windows[0] as EditorWindow : null;
            }

            public static object GetState(EditorWindow window)
            {
                return window == null ? null : StateField?.GetValue(window);
            }

            public static AnimationClip GetActiveAnimationClip(object state)
            {
                return GetPropertyValue<AnimationClip>(state, "activeAnimationClip");
            }

            public static GameObject GetActiveRootGameObject(object state)
            {
                return GetPropertyValue<GameObject>(state, "activeRootGameObject");
            }

            public static float GetCurrentTime(object state)
            {
                return GetNumericPropertyValue(state, "currentTime");
            }

            private static T GetPropertyValue<T>(object target, string propertyName)
            {
                if (target == null)
                    return default;

                var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (property == null)
                    return default;

                var value = property.GetValue(target);
                return value is T typedValue ? typedValue : default;
            }

            private static float GetNumericPropertyValue(object target, string propertyName)
            {
                if (target == null)
                    return 0f;

                var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (property == null)
                    return 0f;

                var value = property.GetValue(target);
                return value is IConvertible ? Convert.ToSingle(value) : 0f;
            }
        }
    }
}

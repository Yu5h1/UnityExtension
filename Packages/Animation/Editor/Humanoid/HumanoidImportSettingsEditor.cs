using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>
    ///   Inspector of <see cref="HumanoidImportSettings"/>: the ModelImporter-style root transform settings, the
    ///   Generate button, and the state of the generated Avatar and clips. The Offset of a channel is enabled only
    ///   while the channel is baked into the pose, as in ModelImporter.
    /// </summary>
    [CustomEditor(typeof(HumanoidImportSettings))]
    public class HumanoidImportSettingsEditor : Editor<HumanoidImportSettings>
    {
        string _message;
        MessageType _messageType;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (var change = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(HumanoidImportSettings.source)));
                DrawMotionNode();

                EditorGUILayout.Space();
                DrawChannel(nameof(HumanoidImportSettings.rootRotation), "Root Transform Rotation");
                DrawChannel(nameof(HumanoidImportSettings.rootPositionY), "Root Transform Position (Y)");
                DrawChannel(nameof(HumanoidImportSettings.rootPositionXZ), "Root Transform Position (XZ)");

                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(HumanoidImportSettings.loopTime)), new GUIContent("Loop Time"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(HumanoidImportSettings.loopPose)), new GUIContent("Loop Pose"));

                if (change.changed)
                    serializedObject.ApplyModifiedProperties();
            }

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(targetObject.source == null))
            {
                if (GUILayout.Button("Generate"))
                    Generate();
            }
            if (!string.IsNullOrEmpty(_message))
                EditorGUILayout.HelpBox(_message, _messageType);

            DrawOutputs();
        }

        void DrawMotionNode()
        {
            var property = serializedObject.FindProperty(nameof(HumanoidImportSettings.motionNode));
            var nodes = NodeNames(targetObject.source);
            if (nodes.Count == 0)
            {
                EditorGUILayout.PropertyField(property);
                return;
            }
            if (!nodes.Contains(property.stringValue))
                nodes.Add(property.stringValue);
            var index = EditorGUILayout.Popup(new GUIContent(property.displayName, property.tooltip), nodes.IndexOf(property.stringValue), nodes.ToArray());
            property.stringValue = nodes[Mathf.Max(0, index)];
        }

        static List<string> NodeNames(GameObject source)
        {
            if (source == null)
                return new List<string>();
            return source.GetComponentsInChildren<Transform>(true)
                .Where(transform => transform != source.transform)
                .Select(transform => transform.name)
                .Where(name => name != null)
                .Distinct()
                .ToList();
        }

        void DrawChannel(string fieldName, string label)
        {
            var channel = serializedObject.FindProperty(fieldName);
            var bake = channel.FindPropertyRelative(nameof(HumanoidImportSettings.RotationChannel.bakeIntoPose));
            var offset = channel.FindPropertyRelative(nameof(HumanoidImportSettings.RotationChannel.offset));
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(bake, new GUIContent("Bake Into Pose"));
                using (new EditorGUI.DisabledScope(!bake.boolValue))
                    EditorGUILayout.PropertyField(offset, new GUIContent("Offset"));
            }
        }

        void DrawOutputs()
        {
            var settings = targetObject;
            if (settings.avatar == null && settings.clips.Count == 0)
                return;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Generated", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                if (settings.avatar != null)
                {
                    EditorGUILayout.ObjectField("Avatar", settings.avatar, typeof(Avatar), false);
                    EditorGUILayout.Toggle("Is Valid", settings.avatar.isValid);
                    EditorGUILayout.Toggle("Is Human", settings.avatar.isHuman);
                }
                foreach (var clip in settings.clips.Where(clip => clip != null))
                    EditorGUILayout.ObjectField(clip.isHumanMotion ? "Humanoid Clip" : "Clip", clip, typeof(AnimationClip), false);
            }
        }

        void Generate()
        {
            var report = HumanoidImporter.Generate(targetObject);
            serializedObject.Update();
            if (!report.success)
            {
                _message = report.error;
                _messageType = MessageType.Error;
                return;
            }
            var lines = new List<string>();
            if (report.avatar != null)
                lines.Add($"Avatar: {report.mappedBones} bones mapped, isValid {report.avatar.isValid}, isHuman {report.avatar.isHuman}.");
            foreach (var clip in report.clips)
                lines.Add($"Clip '{clip.clip.name}': {clip.frames} samples, humanoid {clip.clip.isHumanMotion}, root motion ends at {clip.motionEnd.ToString("F3")}, heading {clip.motionYawEnd:F1}°.");
            _message = string.Join("\n", lines);
            _messageType = MessageType.Info;
        }
    }

    /// <summary>
    ///   Creates the <see cref="HumanoidImportSettings"/> for a selected GLB beside it, or selects the one that is
    ///   already there.
    /// </summary>
    public static class HumanoidImportSettingsMenu
    {
        const string MenuPath = "Assets/Humanoid Import Settings";

        [MenuItem(MenuPath, true)]
        static bool Validate() => IsModel(Selection.activeObject);

        [MenuItem(MenuPath)]
        static void Create()
        {
            var source = (GameObject)Selection.activeObject;
            var sourcePath = AssetDatabase.GetAssetPath(source);
            var path = System.IO.Path.ChangeExtension(sourcePath, ".humanoid.asset");
            var settings = AssetDatabase.LoadAssetAtPath<HumanoidImportSettings>(path);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<HumanoidImportSettings>();
                settings.source = source;
                AssetDatabase.CreateAsset(settings, path);
            }
            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }

        static bool IsModel(Object asset)
        {
            if (!(asset is GameObject))
                return false;
            var extension = System.IO.Path.GetExtension(AssetDatabase.GetAssetPath(asset)).ToLowerInvariant();
            return extension == ".glb" || extension == ".gltf";
        }
    }
}

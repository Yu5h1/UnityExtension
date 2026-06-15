using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using Yu5h1Lib.Animation;
using Yu5h1Lib.Timeline;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>
    /// Inspector for a <see cref="SkipPoint"/> marker. The marker itself carries no event
    /// (it lives in the .playable asset, which can't hold scene references). Instead this editor
    /// reaches the currently inspected <see cref="UnityEngine.Playables.PlayableDirector"/>
    /// (<see cref="TimelineEditor.inspectedDirector"/>), finds its <see cref="PlayableDirectorAddon"/>,
    /// and draws the UnityEvent that the addon maps to this marker — so the event is authored here
    /// but stored (and able to reference scene objects) on the scene-side component.
    /// </summary>
    [CustomEditor(typeof(SkipPoint))]
    public class SkipPointEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // The marker's own serialized fields (time, etc.).
            DrawDefaultInspector();

            EditorGUILayout.Space();

            var marker = (SkipPoint)target;

            var director = TimelineEditor.inspectedDirector;
            if (director == null)
            {
                EditorGUILayout.HelpBox(
                    "Open this Timeline through a PlayableDirector in the scene to author its skip event.",
                    MessageType.Info);
                return;
            }

            var addon = director.GetComponent<PlayableDirectorAddon>();
            if (addon == null)
            {
                EditorGUILayout.HelpBox(
                    $"'{director.name}' has no PlayableDirectorAddon — add one to wire skip events.",
                    MessageType.Info);
                return;
            }

            // Ensure a clean (empty) entry exists before we draw it. Done on the live object so the
            // UnityEvent is default-constructed — avoids InsertArrayElementAtIndex duplicating a
            // neighbouring entry's listeners.
            if (!addon.TryGetSkipEvent(marker, out _))
            {
                Undo.RecordObject(addon, "Add Skip Event");
                addon.GetOrCreateSkipEvent(marker);
                EditorUtility.SetDirty(addon);
            }

            var so = new SerializedObject(addon);
            so.Update();

            var entries = so.FindProperty("_skipPointEvents").FindPropertyRelative("_entries");
            int index = IndexOfKey(entries, marker);
            if (index < 0)
            {
                // Defensive: should never happen after GetOrCreateSkipEvent above.
                EditorGUILayout.HelpBox("Failed to resolve the event entry for this SkipPoint.", MessageType.Warning);
                return;
            }

            var valueProp = entries.GetArrayElementAtIndex(index).FindPropertyRelative("value");

            EditorGUILayout.LabelField($"Skip Event  →  {addon.name}", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(valueProp, GUIContent.none);

            so.ApplyModifiedProperties();
        }

        private static int IndexOfKey(SerializedProperty entries, Object key)
        {
            for (int i = 0; i < entries.arraySize; i++)
                if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("key").objectReferenceValue == key)
                    return i;
            return -1;
        }
    }
}

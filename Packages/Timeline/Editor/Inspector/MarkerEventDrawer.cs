using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>
    /// Draws a single marker's entry inside a host component's serialized
    /// <c>KeyValues&lt;Marker, UnityEvent&gt;</c> map. Shared by SkipPointEditor and EventSignalEditor —
    /// both author a per-marker UnityEvent stored on a scene component; only how they resolve the host
    /// (and the typed ensure-entry step) differs, so that stays in each editor.
    /// The caller must ensure the entry already exists (typed GetOrCreate on the live host) first.
    /// </summary>
    internal static class MarkerEventDrawer
    {
        public static void DrawKeyedEvent(Object host, string mapFieldName, Object markerKey, string label)
        {
            var so = new SerializedObject(host);
            so.Update();

            var entries = so.FindProperty(mapFieldName).FindPropertyRelative("_entries");
            int index = IndexOfKey(entries, markerKey);
            if (index < 0)
            {
                EditorGUILayout.HelpBox("Failed to resolve the event entry for this marker.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(entries.GetArrayElementAtIndex(index).FindPropertyRelative("value"), GUIContent.none);

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

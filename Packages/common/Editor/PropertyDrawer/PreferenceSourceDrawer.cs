using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>
    /// Draws a host's <see cref="PreferenceSourceAttribute"/> field: hidden when the host declares no
    /// <see cref="IPreferences.SourceType"/>, an object field restricted to that type otherwise, followed by a
    /// read-only list of the members the source contributes (key, type, value) and the fields it skips.
    /// </summary>
    [CustomPropertyDrawer(typeof(PreferenceSourceAttribute))]
    public class PreferenceSourceDrawer : PropertyDrawer
    {
        private static System.Type SourceTypeOf(SerializedProperty property)
            => (property.serializedObject.targetObject as IPreferences)?.SourceType;

        private static float Line => EditorGUIUtility.singleLineHeight;
        private static float Spacing => EditorGUIUtility.standardVerticalSpacing;

        private static int RowCount(SerializedProperty property, System.Type sourceType, out List<FieldInfo> members, out List<FieldInfo> skipped)
        {
            members = null;
            skipped = new List<FieldInfo>();
            var source = property.objectReferenceValue;
            if (source == null)
                return 0;
            if (!sourceType.IsInstanceOfType(source))
                return 1;
            members = Preferences.GetSourceFields(source.GetType(), skipped);
            return members.Count + skipped.Count;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var sourceType = SourceTypeOf(property);
            if (sourceType == null)
                return -Spacing;
            var rows = 1 + RowCount(property, sourceType, out _, out _);
            return rows * Line + (rows - 1) * Spacing;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var sourceType = SourceTypeOf(property);
            if (sourceType == null)
                return;

            var row = new Rect(position.x, position.y, position.width, Line);
            EditorGUI.BeginProperty(row, label, property);
            EditorGUI.BeginChangeCheck();
            var picked = EditorGUI.ObjectField(row, label, property.objectReferenceValue, sourceType, false);
            if (EditorGUI.EndChangeCheck())
                property.objectReferenceValue = picked;
            EditorGUI.EndProperty();

            RowCount(property, sourceType, out var members, out var skipped);
            var source = property.objectReferenceValue;
            if (source == null)
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                if (members == null)
                {
                    row.y += Line + Spacing;
                    EditorGUI.HelpBox(row, $"Not a {sourceType.Name}; ignored.", MessageType.Warning);
                    return;
                }
                foreach (var field in members)
                {
                    row.y += Line + Spacing;
                    string value;
                    try { value = PlayerPrefsSerializer.Default.Serialize(field.GetValue(source), field.FieldType); }
                    catch (System.Exception e) { value = $"(cannot convert: {e.GetType().Name})"; }
                    EditorGUI.LabelField(row, new GUIContent(field.Name), new GUIContent($"{field.FieldType.Name}   {value}"), EditorStyles.miniLabel);
                }
                using (new EditorGUI.DisabledScope(true))
                    foreach (var field in skipped)
                    {
                        row.y += Line + Spacing;
                        EditorGUI.LabelField(row, new GUIContent(field.Name), new GUIContent($"{field.FieldType.Name}   skipped: not convertible to one value"), EditorStyles.miniLabel);
                    }
            }
        }
    }
}

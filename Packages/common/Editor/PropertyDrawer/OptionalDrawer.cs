using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    [CustomPropertyDrawer(typeof(Optional<>))]
    public class OptionalDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var enabledProp = property.FindPropertyRelative("enabled");
            var valueProp = property.FindPropertyRelative("value");

            float singleLineHeight = EditorGUIUtility.singleLineHeight;

            float labelEndX = position.x + EditorGUIUtility.labelWidth - (EditorGUI.indentLevel * 15);

            Rect toggleRect = new Rect(
                labelEndX - 20,
                position.y,
                16,
                singleLineHeight
            );

            enabledProp.boolValue = EditorGUI.Toggle(toggleRect, GUIContent.none, enabledProp.boolValue);

            using (new EditorGUI.DisabledScope(!enabledProp.boolValue))
            {
                EditorGUI.PropertyField(position, valueProp, label, true);
            }

            EditorGUI.EndProperty();

        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property.FindPropertyRelative("value"), true);
        }
    }
}
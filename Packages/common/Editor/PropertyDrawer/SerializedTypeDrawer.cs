using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    [CustomPropertyDrawer(typeof(SerializedType))]
    public class SerializedTypeDrawer : PropertyDrawer
    {
        private const string ValuePropertyName = "_assemblyQualifiedName";

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var valueProperty = property.FindPropertyRelative(ValuePropertyName);
            if (valueProperty == null)
            {
                EditorGUI.LabelField(position, label, new GUIContent($"Missing {ValuePropertyName}"));
                return;
            }

            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.PropertyField(position, valueProperty, label);
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var valueProperty = property.FindPropertyRelative(ValuePropertyName);
            return valueProperty == null
                ? EditorGUIUtility.singleLineHeight
                : EditorGUI.GetPropertyHeight(valueProperty, label);
        }
    }
}

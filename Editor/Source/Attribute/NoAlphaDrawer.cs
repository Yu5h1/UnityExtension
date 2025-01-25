using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    [CustomPropertyDrawer(typeof(NoAlphaAttribute))]
    public class NoAlphaDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.Color)
            {
                Color color = property.colorValue;
                color = EditorGUI.ColorField(position, label, color, false, false, false);
                color.a = 1f;
                property.colorValue = color;
            }
            else
            {
                EditorGUI.LabelField(position, label.text, "Use [NoAlpha] with Color.");
            }
        }
    }
}

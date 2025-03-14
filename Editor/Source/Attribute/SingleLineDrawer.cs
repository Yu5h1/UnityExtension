using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    [CustomPropertyDrawer(typeof(SingleLineAttribute))]
    public class SingleLineDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => EditorGUIUtility.singleLineHeight;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.isArray)
            {
                EditorGUI.BeginProperty(position, label, property);
                float widthPerElement = position.width / property.arraySize;

                for (int i = 0; i < property.arraySize; i++)
                {
                    Rect elementRect = new Rect(position.x + (i * widthPerElement), position.y - EditorGUIUtility.singleLineHeight, widthPerElement, position.height);
                    SerializedProperty element = property.GetArrayElementAtIndex(i);

                    EditorGUI.PropertyField(elementRect, element, GUIContent.none);
                }

                EditorGUI.EndProperty();
            }
            else
            {
                EditorGUI.PropertyField(position, property, label);
            }
        }
    } 
}

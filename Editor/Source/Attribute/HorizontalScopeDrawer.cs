using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    [CustomPropertyDrawer(typeof(HorizontalScopeAttribute))]
    public class HorizontalScopeDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => EditorGUIUtility.singleLineHeight;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var horizontal = (HorizontalScopeAttribute)attribute;
            if (property.propertyType == SerializedPropertyType.Generic && property.hasVisibleChildren)
            {
                var child = property.Copy();
                var end = child.GetEndProperty();

                var children = new List<SerializedProperty>();
                while (child.NextVisible(true) && !SerializedProperty.EqualContents(child, end))
                {
                    if (IsPrimitiveLike(child))
                        children.Add(child.Copy());
                    else
                    {
                        // fallback：用預設 GUI 繪製
                        EditorGUI.PropertyField(position, property, label, includeChildren: true);
                        EditorGUI.EndProperty();
                        return;
                    }
                }
                var totalWidth = position.width;
           
                
                if (horizontal.DisplayLabel)
                {
                    var rect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);
                    totalWidth -= rect.width;
                    EditorGUI.LabelField(rect, label);
                }

                float width = totalWidth / children.Count;

                for (int i = 0; i < children.Count; i++)
                {
                    var rect = new Rect(position.x + i * width, position.y, width - 1, position.height);
                    EditorGUI.PropertyField(rect, children[i], horizontal.DisplayLabel ? new GUIContent(children[i].displayName) :  GUIContent.none);
                }
            }
            else
            {
                // 非物件 → 直接顯示
                EditorGUI.PropertyField(position, property, label, includeChildren: true);
            }

            EditorGUI.EndProperty();
        }

        private bool IsPrimitiveLike(SerializedProperty prop)
        {
            return prop.propertyType switch
            {
                SerializedPropertyType.Integer => true,
                SerializedPropertyType.Boolean => true,
                SerializedPropertyType.Float => true,
                SerializedPropertyType.String => true,
                SerializedPropertyType.Enum => true,
                SerializedPropertyType.ObjectReference => true,
                _ => false
            };
        }
    } 
}

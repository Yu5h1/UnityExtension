using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Yu5h1Lib.EditorExtension
{
    [CustomPropertyDrawer(typeof(NotSelfAttribute))]
    public class NotSelfDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // 只處理 ObjectReference 類型
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);
            
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(position, property, label);
            
            if (EditorGUI.EndChangeCheck())
            {
                var self = property.serializedObject.targetObject;
                var target = property.objectReferenceValue;
                
                // 檢查自己參照自己
                if (target == self)
                {
                    property.objectReferenceValue = null;
                    Debug.LogWarning($"[{self.name}.{property.name}] Cannot reference itself!");
                }
                // 檢查循環參照
                else if (target != null && HasCircularReference(self, target, property.name))
                {
                    property.objectReferenceValue = null;
                    Debug.LogWarning($"[{self.name}.{property.name}] Circular reference detected!");
                }
            }
            
            EditorGUI.EndProperty();
        }

        /// <summary>
        /// 檢查是否有循環參照
        /// </summary>
        private bool HasCircularReference(Object self, Object target, string propertyName)
        {
            var visited = new HashSet<Object> { self };
            var current = target;

            while (current != null)
            {
                if (visited.Contains(current))
                    return true;

                visited.Add(current);

                // 取得 current 的同名欄位值
                var so = new SerializedObject(current);
                var prop = so.FindProperty(propertyName);
                
                if (prop == null || prop.propertyType != SerializedPropertyType.ObjectReference)
                    break;

                current = prop.objectReferenceValue;
            }

            return false;
        }
    }
}

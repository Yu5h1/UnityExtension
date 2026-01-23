using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Yu5h1Lib.Serialization.Editor
{
    /// <summary>
    /// KeyValue 的 PropertyDrawer - 水平排列 Key 和 Value
    /// </summary>
    [CustomPropertyDrawer(typeof(KeyValue<,>), true)]
    public class KeyValueDrawer : PropertyDrawer
    {
        private const float KeyWidthRatio = 0.4f;
        private const float Spacing = 4f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var keyProp = property.FindPropertyRelative("key");
            var valueProp = property.FindPropertyRelative("value");

            if (keyProp == null || valueProp == null)
            {
                EditorGUI.LabelField(position, "Error: Cannot find key/value properties");
                EditorGUI.EndProperty();
                return;
            }

            // 檢查是否為重複 key
            bool isDuplicate = IsKeyDuplicate(property);
            
            // 如果是重複 key，顯示紅色背景
            if (isDuplicate)
            {
                var warningRect = position;
                warningRect.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.DrawRect(warningRect, new Color(1f, 0.3f, 0.3f, 0.3f));
            }

            // 計算 Key 和 Value 的寬度
            float totalWidth = position.width;
            float keyWidth = (totalWidth - Spacing) * KeyWidthRatio;
            float valueWidth = totalWidth - keyWidth - Spacing;

            // Key 欄位
            var keyRect = new Rect(position.x, position.y, keyWidth, EditorGUIUtility.singleLineHeight);
            
            // Value 欄位
            var valueRect = new Rect(position.x + keyWidth + Spacing, position.y, valueWidth, position.height);

            // 繪製 Key
            EditorGUI.BeginChangeCheck();
            using (new EditorGUI.IndentLevelScope(-EditorGUI.indentLevel))
            {
                // Key 用不同顏色標示重複
                if (isDuplicate)
                {
                    var originalColor = GUI.color;
                    GUI.color = new Color(1f, 0.5f, 0.5f);
                    EditorGUI.PropertyField(keyRect, keyProp, GUIContent.none);
                    GUI.color = originalColor;
                }
                else
                {
                    EditorGUI.PropertyField(keyRect, keyProp, GUIContent.none);
                }

                // 繪製 Value（處理多行情況）
                if (IsExpandableProperty(valueProp))
                {
                    // 複雜類型：顯示 foldout
                    valueRect.height = EditorGUIUtility.singleLineHeight;
                    EditorGUI.PropertyField(valueRect, valueProp, GUIContent.none, true);
                }
                else
                {
                    EditorGUI.PropertyField(valueRect, valueProp, GUIContent.none);
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var valueProp = property.FindPropertyRelative("value");
            
            if (valueProp != null && IsExpandableProperty(valueProp) && valueProp.isExpanded)
            {
                return EditorGUI.GetPropertyHeight(valueProp, true);
            }
            
            return EditorGUIUtility.singleLineHeight;
        }

        private bool IsExpandableProperty(SerializedProperty property)
        {
            return property.propertyType == SerializedPropertyType.Generic ||
                   property.propertyType == SerializedPropertyType.ManagedReference ||
                   (property.isArray && property.propertyType != SerializedPropertyType.String);
        }

        /// <summary>
        /// 檢查當前 key 是否在同一個 list 中重複
        /// </summary>
        private bool IsKeyDuplicate(SerializedProperty property)
        {
            var keyProp = property.FindPropertyRelative("key");
            if (keyProp == null) return false;

            // 取得父層 list
            string path = property.propertyPath;
            
            // 路徑格式: _entries.Array.data[index]
            int lastBracket = path.LastIndexOf('[');
            if (lastBracket < 0) return false;

            string listPath = path.Substring(0, path.LastIndexOf(".Array.data["));
            int currentIndex = GetArrayIndex(path);
            
            if (currentIndex < 0) return false;

            var listProp = property.serializedObject.FindProperty(listPath);
            if (listProp == null || !listProp.isArray) return false;

            // 取得當前 key 的值
            object currentKeyValue = GetPropertyValue(keyProp);
            if (currentKeyValue == null) return false;

            // 檢查其他項目是否有相同的 key
            for (int i = 0; i < listProp.arraySize; i++)
            {
                if (i == currentIndex) continue;

                var otherElement = listProp.GetArrayElementAtIndex(i);
                var otherKeyProp = otherElement.FindPropertyRelative("key");
                
                if (otherKeyProp == null) continue;

                object otherKeyValue = GetPropertyValue(otherKeyProp);
                
                if (currentKeyValue.Equals(otherKeyValue))
                {
                    return true;
                }
            }

            return false;
        }

        private int GetArrayIndex(string propertyPath)
        {
            int start = propertyPath.LastIndexOf('[');
            int end = propertyPath.LastIndexOf(']');
            
            if (start >= 0 && end > start)
            {
                string indexStr = propertyPath.Substring(start + 1, end - start - 1);
                if (int.TryParse(indexStr, out int index))
                {
                    return index;
                }
            }
            return -1;
        }

        private object GetPropertyValue(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.String:
                    return property.stringValue;
                case SerializedPropertyType.Integer:
                    return property.intValue;
                case SerializedPropertyType.Float:
                    return property.floatValue;
                case SerializedPropertyType.Boolean:
                    return property.boolValue;
                case SerializedPropertyType.Enum:
                    return property.enumValueIndex;
                case SerializedPropertyType.ObjectReference:
                    return property.objectReferenceValue;
                case SerializedPropertyType.Vector2:
                    return property.vector2Value;
                case SerializedPropertyType.Vector3:
                    return property.vector3Value;
                case SerializedPropertyType.Vector4:
                    return property.vector4Value;
                case SerializedPropertyType.Color:
                    return property.colorValue;
                default:
                    // 對於複雜類型，使用 propertyPath 作為識別
                    return property.propertyPath;
            }
        }
    }

    /// <summary>
    /// KeyValues 的 PropertyDrawer - 顯示重複 key 警告
    /// </summary>
    [CustomPropertyDrawer(typeof(KeyValues<,>), true)]
    public class KeyValuesDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var entriesProp = property.FindPropertyRelative("_entries");
            
            if (entriesProp == null)
            {
                EditorGUI.LabelField(position, label.text, "Error: Cannot find _entries");
                EditorGUI.EndProperty();
                return;
            }

            // 檢查是否有重複 key
            var duplicateKeys = FindDuplicateKeys(entriesProp);
            bool hasDuplicates = duplicateKeys.Count > 0;

            // 計算總高度
            float currentY = position.y;

            // 顯示警告（如果有重複）
            if (hasDuplicates)
            {
                var warningRect = new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight * 1.5f);
                EditorGUI.HelpBox(warningRect, $"Duplicate keys: {string.Join(", ", duplicateKeys)}", MessageType.Warning);
                currentY += EditorGUIUtility.singleLineHeight * 1.5f + 2f;
            }

            // 繪製 list
            var listRect = new Rect(position.x, currentY, position.width, position.height - (currentY - position.y));
            EditorGUI.PropertyField(listRect, entriesProp, label, true);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var entriesProp = property.FindPropertyRelative("_entries");
            
            if (entriesProp == null)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            float height = EditorGUI.GetPropertyHeight(entriesProp, label, true);

            // 如果有重複 key，加上警告的高度
            var duplicateKeys = FindDuplicateKeys(entriesProp);
            if (duplicateKeys.Count > 0)
            {
                height += EditorGUIUtility.singleLineHeight * 1.5f + 2f;
            }

            return height;
        }

        private List<string> FindDuplicateKeys(SerializedProperty entriesProp)
        {
            var duplicates = new List<string>();
            var seen = new Dictionary<string, int>(); // key string -> first index

            for (int i = 0; i < entriesProp.arraySize; i++)
            {
                var element = entriesProp.GetArrayElementAtIndex(i);
                var keyProp = element.FindPropertyRelative("key");
                
                if (keyProp == null) continue;

                string keyStr = GetKeyString(keyProp);
                
                if (string.IsNullOrEmpty(keyStr)) continue;

                if (seen.ContainsKey(keyStr))
                {
                    if (!duplicates.Contains(keyStr))
                    {
                        duplicates.Add(keyStr);
                    }
                }
                else
                {
                    seen[keyStr] = i;
                }
            }

            return duplicates;
        }

        private string GetKeyString(SerializedProperty keyProp)
        {
            switch (keyProp.propertyType)
            {
                case SerializedPropertyType.String:
                    return keyProp.stringValue;
                case SerializedPropertyType.Integer:
                    return keyProp.intValue.ToString();
                case SerializedPropertyType.Float:
                    return keyProp.floatValue.ToString();
                case SerializedPropertyType.Boolean:
                    return keyProp.boolValue.ToString();
                case SerializedPropertyType.Enum:
                    return keyProp.enumNames[keyProp.enumValueIndex];
                case SerializedPropertyType.ObjectReference:
                    return keyProp.objectReferenceValue != null 
                        ? keyProp.objectReferenceValue.name 
                        : "null";
                default:
                    return keyProp.propertyPath;
            }
        }
    }
}

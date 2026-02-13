using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    [CustomPropertyDrawer(typeof(CollectionConstraintAttribute))]
    public class CollectionConstraintDrawer : PropertyDrawer
    {
        CollectionConstraintAttribute attr => (CollectionConstraintAttribute)attribute;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var parentArray = FindParentArrayProperty(property);
            int currentIndex = ExtractArrayIndex(property.propertyPath);

            // Unique: 繪製前掃描 — 處理 label 拖拉新增的重複
            if (attr.Unique && parentArray != null && property.objectReferenceValue != null)
            {
                if (IsDuplicateInArray(parentArray, currentIndex, property.objectReferenceValue))
                {
                    var dupName = property.objectReferenceValue.name;
                    property.objectReferenceValue = null;
                    property.serializedObject.ApplyModifiedProperties();
                    Debug.LogWarning(
                        $"[{property.serializedObject.targetObject.name}.{parentArray.name}] " +
                        $"Duplicate reference '{dupName}' removed.");
                }
            }

            EditorGUI.BeginProperty(position, label, property);

            var oldValue = property.objectReferenceValue;

            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(position, property, label);

            if (EditorGUI.EndChangeCheck())
            {
                var newValue = property.objectReferenceValue;

                // Unique 變更檢查
                if (attr.Unique && parentArray != null && newValue != null)
                {
                    if (IsDuplicateInArray(parentArray, currentIndex, newValue))
                    {
                        property.objectReferenceValue = oldValue;
                        Debug.LogWarning(
                            $"[{property.serializedObject.targetObject.name}.{parentArray.name}] " +
                            $"Duplicate reference '{newValue.name}' rejected!");
                    }
                }

                // NotNull 變更檢查
                if (attr.NotNull && property.objectReferenceValue == null && oldValue != null)
                {
                    property.objectReferenceValue = oldValue;
                    Debug.LogWarning(
                        $"[{property.serializedObject.targetObject.name}.{parentArray.name}] " +
                        $"Null is not allowed.");
                }
            }

            EditorGUI.EndProperty();
        }

        static SerializedProperty FindParentArrayProperty(SerializedProperty element)
        {
            var path = element.propertyPath;
            int idx = path.LastIndexOf(".Array.data[");
            if (idx < 0)
                return null;
            return element.serializedObject.FindProperty(path.Substring(0, idx));
        }

        static int ExtractArrayIndex(string propertyPath)
        {
            int start = propertyPath.LastIndexOf('[');
            int end = propertyPath.LastIndexOf(']');
            if (start < 0 || end < 0 || end <= start)
                return -1;
            if (int.TryParse(propertyPath.Substring(start + 1, end - start - 1), out int index))
                return index;
            return -1;
        }

        static bool IsDuplicateInArray(SerializedProperty array, int skipIndex, Object value)
        {
            for (int i = 0; i < array.arraySize; i++)
            {
                if (i == skipIndex)
                    continue;
                if (array.GetArrayElementAtIndex(i).objectReferenceValue == value)
                    return true;
            }
            return false;
        }
    }
}

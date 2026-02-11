using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;

namespace Yu5h1Lib.EditorExtension
{
    [EditorBrowsable(EditorBrowsableState.Never), Browsable(false)]
    public static class SerializedPropertyEx
    {
        public static bool GetParentProperty(this SerializedProperty property, out SerializedProperty parent)
        {
            parent = null;
            var path = property.propertyPath;

            if (!path.Contains("."))
                return false;

            string parentPath;

            if (path.Contains(".Array.data["))
            {
                var arrayIndex = path.IndexOf(".Array.data[");

                var arrayPath = path.Substring(0, arrayIndex);

                var closeBracketIndex = path.IndexOf(']', arrayIndex);
                if (closeBracketIndex + 1 < path.Length && path[closeBracketIndex + 1] == '.')
                {
                    parentPath = path.Substring(0, closeBracketIndex + 1);
                }
                else
                {
                    parentPath = arrayPath;
                }
            }
            else
            {
                var lastDotIndex = path.LastIndexOf('.');
                parentPath = path.Substring(0, lastDotIndex);
            }
            parent = property.serializedObject.FindProperty(parentPath);
            return true;
        }
        public static bool TryGetFieldInfo(this SerializedProperty property, out FieldInfo fieldInfo)
        {
            const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
            string propertyPath = property.propertyPath.Replace(".Array.data", "");
            fieldInfo = null;
            object target = property.serializedObject.targetObject;
            Type targetType = property.serializedObject.targetObject.GetType();

            if (!propertyPath.Contains("."))
            {
                fieldInfo = targetType.GetField(propertyPath, bindingFlags);
                return fieldInfo != null;
            }

            var hierarchys = propertyPath.Split('.');
            for (int i = 0; i < hierarchys.Length; i++)
            {
                var hierarchyLevel = hierarchys[i];
                bool isLastLevel = (i == hierarchys.Length - 1);

                if (hierarchyLevel.Contains("["))
                {
                    var arrayParts = hierarchyLevel.Split('[');
                    string fieldName = arrayParts[0];
                    fieldInfo = targetType.GetField(fieldName, bindingFlags);

                    if (fieldInfo == null)
                        return false;
                    if (isLastLevel)
                        return true;

                    string indexStr = arrayParts[1].TrimEnd(']');
                    if (!int.TryParse(indexStr, out int index) || index < 0)
                        return false;

                    object arrayObject = fieldInfo.GetValue(target);
                    if (arrayObject == null)
                        return false;

                    if (arrayObject is Array array)
                    {
                        if (index >= array.Length)
                            return false;

                        target = array.GetValue(index);
                        targetType = fieldInfo.FieldType.GetElementType();

                        if (targetType == null)
                            return false;
                    }
                    else if (arrayObject is IList list)
                    {
                        if (index >= list.Count)
                            return false;

                        target = list[index];

                        var genericArgs = fieldInfo.FieldType.GetGenericArguments();
                        if (genericArgs.Length == 0)
                            return false;

                        targetType = genericArgs[0];
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    fieldInfo = targetType.GetField(hierarchyLevel, bindingFlags);
                    if (fieldInfo == null)
                        return false;

                    if (isLastLevel)
                        return true;

                    target = fieldInfo.GetValue(target);
                    if (target == null)
                        return false;

                    targetType = fieldInfo.FieldType;
                }
            }

            return false;
        }


        public static object GetValue(this SerializedProperty property)
        {
            return property.propertyType switch
            {
                SerializedPropertyType.Integer => property.intValue,
                SerializedPropertyType.Boolean => property.boolValue,
                SerializedPropertyType.Float => property.floatValue,
                SerializedPropertyType.String => property.stringValue,
                SerializedPropertyType.Color => property.colorValue,
                SerializedPropertyType.ObjectReference => property.objectReferenceValue,
                SerializedPropertyType.LayerMask => property.intValue,
                SerializedPropertyType.Enum => property.enumValueIndex,
                SerializedPropertyType.Vector2 => property.vector2Value,
                SerializedPropertyType.Vector3 => property.vector3Value,
                SerializedPropertyType.Vector4 => property.vector4Value,
                SerializedPropertyType.Rect => property.rectValue,
                SerializedPropertyType.ArraySize => property.arraySize,
                SerializedPropertyType.Character => (char)property.intValue,
                SerializedPropertyType.AnimationCurve => property.animationCurveValue,
                SerializedPropertyType.Bounds => property.boundsValue,
                SerializedPropertyType.Quaternion => property.quaternionValue,
                SerializedPropertyType.ExposedReference => property.exposedReferenceValue,
                SerializedPropertyType.FixedBufferSize => property.fixedBufferSize,
                SerializedPropertyType.Vector2Int => property.vector2IntValue,
                SerializedPropertyType.Vector3Int => property.vector3IntValue,
                SerializedPropertyType.RectInt => property.rectIntValue,
                SerializedPropertyType.BoundsInt => property.boundsIntValue,

                SerializedPropertyType.Generic => GetValueGeneric(property),
                _ => null
            };
        }

        private static object GetValueGeneric(SerializedProperty property)
        {
            if (!property.propertyPath.Contains("."))
            {
                var targetObject = property.serializedObject.targetObject;
                var field = targetObject.GetType().GetField(property.propertyPath,
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                return field?.GetValue(targetObject);
            }

            return GetValueByPath(property.serializedObject.targetObject, property.propertyPath);
        }

        private static object GetValueByPath(object target, string path)
        {
            const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            path = path.Replace(".Array.data", "");

            var pathParts = path.Split('.');
            object currentTarget = target;
            Type currentType = target.GetType();

            foreach (var part in pathParts)
            {
                if (currentTarget == null) return null;

                if (part.Contains("["))
                {
                    var arrayParts = part.Split('[');
                    string fieldName = arrayParts[0];
                    string indexStr = arrayParts[1].TrimEnd(']');

                    if (!int.TryParse(indexStr, out int index))
                        return null;

                    var field = currentType.GetField(fieldName, bindingFlags);
                    if (field == null) return null;

                    var arrayObject = field.GetValue(currentTarget);
                    if (arrayObject == null) return null;

                    if (arrayObject is Array array)
                    {
                        if (index < 0 || index >= array.Length) return null;
                        currentTarget = array.GetValue(index);
                        currentType = field.FieldType.GetElementType() ?? typeof(object);
                    }
                    else if (arrayObject is IList list)
                    {
                        if (index < 0 || index >= list.Count) return null;
                        currentTarget = list[index];
                        var genericArgs = field.FieldType.GetGenericArguments();
                        currentType = genericArgs.Length > 0 ? genericArgs[0] : typeof(object);
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    var field = currentType.GetField(part, bindingFlags);
                    if (field == null) return null;

                    currentTarget = field.GetValue(currentTarget);
                    currentType = field.FieldType;
                }
            }

            return currentTarget;
        }



        public static void SetValue(this SerializedProperty property, object value)
        {
            if (property.isArray && value.GetType().IsArray)
            {
                if (property.arrayElementType == value.GetType().GetElementType().Name)
                {
                    var ArrayValue = value as object[];
                    property.arraySize = ArrayValue.Length;
                    for (int i = 0; i < property.arraySize; i++)
                    {
                        property.GetArrayElementAtIndex(i).SetValue(ArrayValue[i]);
                    }
                }
                else
                {
                    Debug.LogWarning(property.arrayElementType + " can not convert to " + value.GetType().GetElementType().Name);
                }
            }
            else if (property.propertyType == SerializedPropertyType.Generic)
            {
                var refProperties = value.GetType().GetProperties();
                property.Iterate(true, p =>
                {
                    string[] splitNames = p.propertyPath.Split('.');
                    string curPropName = splitNames[splitNames.Length - 1];
                    foreach (var reflectProp in refProperties)
                    {
                        if (curPropName == reflectProp.Name)
                        {
                            SetValue(p, reflectProp.GetValue(value, null));
                            break;
                        }
                    }
                });
            }
            else if (value is int) { property.intValue = (int)value; }
            else if (value is bool) { property.boolValue = (bool)value; }
            else if (value is float) { property.floatValue = (float)value; }
            else if (value is string) { property.stringValue = (string)value; }
            else if (value is long) { property.longValue = (long)value; }
            else if (value is Vector2) { property.vector2Value = (Vector2)value; }
            else if (value is Vector3) { property.vector3Value = (Vector3)value; }
            else if (value is Vector4) { property.vector4Value = (Vector4)value; }
            else if (value is Rect) { property.rectValue = (Rect)value; }
            else if (value is Quaternion) { property.quaternionValue = (Quaternion)value; }
            else if (value is Color) { property.colorValue = (Color)value; }
            else if (value is Bounds) { property.boundsValue = (Bounds)value; }
            else if (value is AnimationCurve) { property.animationCurveValue = (AnimationCurve)value; }
            else if (value is UnityEngine.Object) { property.objectReferenceValue = (UnityEngine.Object)value; }

        }
        public static void Iterate(this SerializedProperty prop, bool enterChildren, System.Action<SerializedProperty> action)
        {
            var iterator = prop.Copy();
            while (iterator.NextVisible(enterChildren))
            {
                action(iterator);
                enterChildren = false; 
            }
        }
        public static IEnumerable<SerializedProperty> GetEnumerator(this SerializedProperty prop, bool enterChildren)
        {
            SerializedProperty iterator = prop.Copy();
            while (iterator.NextVisible(enterChildren))
            {
                yield return iterator.Copy();
                enterChildren = false;
            }
        }


        public static void logPropertiesName(this SerializedProperty property, bool enterChildren)
        {
            property.Iterate(enterChildren, prop => {
                Debug.Log(prop.name);
            });
        }
        public static string FindString(this SerializedProperty property, string name)
        {
            return property.FindPropertyRelative(name).stringValue;
        }
        public static void ResetValue(this SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Generic:
                    break;
                case SerializedPropertyType.Integer:
                    property.intValue = 0;
                    break;
                case SerializedPropertyType.Boolean:
                    property.boolValue = false;
                    break;
                case SerializedPropertyType.Float:
                    if (property.propertyPath.ToLower().Contains("scale")) property.floatValue = 1;
                    else property.floatValue = 0;
                    break;
                case SerializedPropertyType.String:
                    property.stringValue = "";
                    break;
                case SerializedPropertyType.Color:
                    property.colorValue = Color.black;
                    break;
                case SerializedPropertyType.ObjectReference:
                    property.objectReferenceValue = null;
                    break;
                case SerializedPropertyType.LayerMask:
                    break;
                case SerializedPropertyType.Enum:
                    break;
                case SerializedPropertyType.Vector2:
                    property.vector2Value = Vector2.zero;
                    break;
                case SerializedPropertyType.Vector3:
                    if (property.propertyPath.ToLower().Contains("scale")) property.vector3Value = Vector3.one;
                    else property.vector3Value = Vector3.zero;
                    break;
                case SerializedPropertyType.Vector4:
                    property.vector4Value = Vector4.zero;
                    break;
                case SerializedPropertyType.Rect:
                    property.rectValue = Rect.zero;
                    break;
                case SerializedPropertyType.ArraySize:
                    break;
                case SerializedPropertyType.Character:
                    break;
                case SerializedPropertyType.AnimationCurve:
                    break;
                case SerializedPropertyType.Bounds:
                    property.boundsValue = default(Bounds);
                    break;
                case SerializedPropertyType.Gradient:
                    break;
                case SerializedPropertyType.Quaternion:
                    property.quaternionValue = Quaternion.identity;
                    break;
                case SerializedPropertyType.ExposedReference:
                    break;
                case SerializedPropertyType.FixedBufferSize:
                    break;
                case SerializedPropertyType.Vector2Int:
                    property.vector2IntValue = Vector2Int.zero;
                    break;
                case SerializedPropertyType.Vector3Int:
                    property.vector3IntValue = Vector3Int.zero;
                    break;
                case SerializedPropertyType.RectInt:
                    property.rectIntValue = default(RectInt);
                    break;
                case SerializedPropertyType.BoundsInt:
                    property.boundsIntValue = default(BoundsInt);
                    break;
                case SerializedPropertyType.ManagedReference:
                    break;
            }
        }
        public static bool IsDefined<T>(this SerializedProperty property) where T : Attribute
        => property.TryGetFieldInfo(out FieldInfo info) ? info?.IsDefined<T>() == true : false;

        public static bool TryGetElementType(this SerializedProperty property,out Type elementType)
        {
            elementType = null;
            return property.TryGetFieldInfo(out FieldInfo info) ? info.FieldType.TryGetElementType(out elementType) : false;
        }

        public static int GetArrayElementIndex(this SerializedProperty property)
        {
            string path = property.propertyPath;
            int startIndex = path.LastIndexOf('[') + 1;
            int endIndex = path.LastIndexOf(']');

            if (startIndex > 0 && endIndex > startIndex)
            {
                string indexStr = path.Substring(startIndex, endIndex - startIndex);
                if (int.TryParse(indexStr, out int index))
                {
                    return index;
                }
            }

            return -1;
        }

        public static bool TryGetEnumValue<T>(this SerializedProperty property,out T val) where T : Enum
        {
            val = default;
            if (property.propertyType != SerializedPropertyType.Enum)
                return false;
            val = (T)Enum.ToObject(typeof(T), property.enumValueIndex);
            return true;
        }

        public static void CreateInlineInstance(this SerializedProperty prop, Type type)
        {
            if (prop.isArray)
            {
                prop.arraySize++;
                SerializedProperty newElement = prop.GetArrayElementAtIndex(prop.arraySize - 1);
                newElement.CreateAndAssignInstance(type);
            }
            else
                prop.CreateAndAssignInstance(type);

            prop.serializedObject.ApplyModifiedProperties();
        }
        private static ScriptableObject CreateAndAssignInstance(this SerializedProperty prop, Type type)
       {
            if (TryCreateAndAssignInstance(prop, type, out ScriptableObject instance))
                return instance;
            return null;
        }
        private static bool TryCreateAndAssignInstance( this SerializedProperty prop, Type type, out ScriptableObject instance)
        {
            instance = null;

            if (prop.propertyType == SerializedPropertyType.ManagedReference)
            {
                prop.managedReferenceValue = Activator.CreateInstance(type);
                return true;
            }

            if (prop.propertyType != SerializedPropertyType.ObjectReference)
            {
                Debug.LogError($"TryCreateAndAssignInstance: 不支援的 SerializedProperty type: {prop.propertyType}");
                return false;
            }

            if (!typeof(ScriptableObject).IsAssignableFrom(type))
            {
                Debug.LogError($"TryCreateAndAssignInstance: type {type} 不是 ScriptableObject");
                return false;
            }

            instance = ScriptableObject.CreateInstance(type);
            prop.objectReferenceValue = instance;

            var parent = FindNearestAssetParent(prop);
            if (parent != null)
            {
                Undo.RegisterCreatedObjectUndo(instance, "Create Inline SubAsset");
                AssetDatabase.AddObjectToAsset(instance, parent);
                EditorUtility.SetDirty(parent);
                AssetDatabase.SaveAssets();
            }
            prop.objectReferenceValue = instance;

            return true;
        }

        /// <summary>
        /// 從 property path 往上找最近的已存在於 AssetDatabase 的 ObjectReference，
        /// 作為 SubAsset 的 parent。
        ///
        /// 例如：Theme → GenericComponentPresetObject → ParameterObject
        /// 對 ParameterObject 而言，最近的 Asset parent 是 GenericComponentPresetObject（SubAsset），
        /// 而非最頂層的 Theme。
        ///
        /// 若找不到任何中間層的 Asset，fallback 到 serializedObject.targetObject。
        /// </summary>
        private static UnityEngine.Object FindNearestAssetParent(SerializedProperty prop)
        {
            // 從當前 property 往上走，找最近的 ObjectReference 且已在 AssetDatabase 中
            var current = prop;
            while (current.GetParentProperty(out var parentProp))
            {
                current = parentProp;
                if (current.propertyType == SerializedPropertyType.ObjectReference
                    && current.objectReferenceValue != null
                    && AssetDatabase.Contains(current.objectReferenceValue))
                {
                    return current.objectReferenceValue;
                }
            }

            // Fallback: 最頂層的 targetObject
            var root = prop.serializedObject.targetObject;
            if (AssetDatabase.Contains(root))
                return root;

            return null;
        }

        public static bool IsArrayElement(this SerializedProperty prop) => prop.propertyPath.Contains("Array.data[");

        public static bool TryGetArrayElementIndex(this SerializedProperty prop, out int index)
        {
            index = -1;
            if (!prop.IsArrayElement())
                return false;
            var path = prop.propertyPath;
            int start = path.LastIndexOf('[') + 1;
            int end = path.LastIndexOf(']');
            if (start < 0 || end < 0 || end <= start)
                return false;

            return int.TryParse(path.Substring(start, end - start),out index);
        }

    }
}

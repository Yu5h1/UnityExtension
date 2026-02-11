using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Yu5h1Lib.EditorExtension
{
    using static StringOptionsProvider;
    /// <summary>
    /// StringOptions 相關 Drawer 的共用工具
    /// </summary>
    public static class StringOptionsHelper
    {
        
        /// <summary>
        /// 解析最終的 ListKey
        /// 優先順序：明確指定 > Context Map > 上層 Attribute > SO 名稱解析
        /// </summary>
        public static string ResolveListKey(SerializedProperty property, string explicitKey)
        {
            // 1. 有明確指定就用
            if (!string.IsNullOrEmpty(explicitKey))
                return explicitKey;

            // 2. 查詢 Context Map（for ScriptableObject）
            var targetObj = property.serializedObject.targetObject;
            if (targetObj != null)
            {
                var targetID = targetObj.GetInstanceID();
                if (TryGetContextValidated(targetID, out var contextKey))
                    return contextKey;

                // 3. 嘗試從 ScriptableObject 名稱解析
                // 格式：SomeName(ListKey) → 取出 ListKey
                if (targetObj is ScriptableObject so)
                {
                    var parsed = ParseListKeyFromName(so.name);
                    if (!string.IsNullOrEmpty(parsed) && StringOptionsProvider.Contains(parsed))
                    {
                        // 快取到 Context Map
                        StringOptionsProvider.SetContext(targetID, parsed);
                        return parsed;
                    }
                }
            }

            // 4. 往上層搜尋 StringOptionsContextAttribute
            return FindContextFromParent(property);
        }

        /// <summary>
        /// 從名稱解析 ListKey
        /// 格式：SomeName(ListKey) → ListKey
        /// </summary>
        public static string ParseListKeyFromName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            var startIndex = name.LastIndexOf('(');
            var endIndex = name.LastIndexOf(')');

            if (startIndex >= 0 && endIndex > startIndex)
            {
                return name.Substring(startIndex + 1, endIndex - startIndex - 1);
            }

            return null;
        }

        /// <summary>
        /// 遞迴向上搜尋父層的 StringOptionsContextAttribute
        /// </summary>
        private static string FindContextFromParent(SerializedProperty property)
        {
            var parentProperty = GetParentProperty(property);
            if (parentProperty == null)
                return null;

            // 取得父層的 FieldInfo
            var fieldInfo = GetFieldInfo(parentProperty);
            if (fieldInfo != null)
            {
                // 檢查是否有 StringOptionsContextAttribute
                var contextAttr = fieldInfo
                    .GetCustomAttributes(typeof(StringOptionsContextAttribute), true)
                    .FirstOrDefault() as StringOptionsContextAttribute;

                if (contextAttr != null && !string.IsNullOrEmpty(contextAttr.ListKey))
                    return contextAttr.ListKey;

                // 沒找到，繼續往上找
                return FindContextFromParent(parentProperty);
            }

            return null;
        }

        /// <summary>
        /// 取得 SerializedProperty 的父層 Property
        /// </summary>
        public static SerializedProperty GetParentProperty(SerializedProperty property)
        {
            var path = property.propertyPath;
            var lastDot = path.LastIndexOf('.');

            if (lastDot < 0)
                return null;

            // 處理陣列元素：data[0].field → data
            var parentPath = path.Substring(0, lastDot);

            // 移除 Array.data[n] 部分
            if (parentPath.EndsWith("]"))
            {
                var bracketIndex = parentPath.LastIndexOf('[');
                if (bracketIndex > 0)
                {
                    parentPath = parentPath.Substring(0, bracketIndex);
                    // 移除 .Array.data
                    if (parentPath.EndsWith(".Array.data"))
                        parentPath = parentPath.Substring(0, parentPath.Length - ".Array.data".Length);
                }
            }

            return property.serializedObject.FindProperty(parentPath);
        }

        /// <summary>
        /// 從 SerializedProperty 取得 FieldInfo
        /// </summary>
        public static FieldInfo GetFieldInfo(SerializedProperty property)
        {
            var targetType = property.serializedObject.targetObject.GetType();
            var path = property.propertyPath.Replace(".Array.data[", "[");
            var elements = path.Split('.');

            FieldInfo fieldInfo = null;
            var currentType = targetType;

            foreach (var element in elements)
            {
                if (element.Contains("["))
                {
                    var fieldName = element.Substring(0, element.IndexOf("["));
                    fieldInfo = GetFieldRecursive(currentType, fieldName);
                    if (fieldInfo == null) return null;

                    // 取得陣列/List 的元素類型
                    var fieldType = fieldInfo.FieldType;
                    if (fieldType.IsArray)
                        currentType = fieldType.GetElementType();
                    else if (fieldType.IsGenericType)
                        currentType = fieldType.GetGenericArguments()[0];
                }
                else
                {
                    fieldInfo = GetFieldRecursive(currentType, element);
                    if (fieldInfo == null) return null;
                    currentType = fieldInfo.FieldType;
                }
            }

            return fieldInfo;
        }

        private static FieldInfo GetFieldRecursive(System.Type type, string fieldName)
        {
            if (type == null) return null;

            var field = type.GetField(fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (field != null)
                return field;

            // 往父類別找
            return GetFieldRecursive(type.BaseType, fieldName);
        }

        #region Built-in initinalization
        [InitializeOnLoadMethod]
        private static void RegisterBuiltInOptions()
        {
            Register("Tags", target => UnityEditorInternal.InternalEditorUtility.tags);
            Register("Layers", GetLayerNames);
            Register("SortingLayers", GetSortingLayerNames);
            Register("BuildScenes", GetBuildSceneNames);
        }

        private static string[] GetLayerNames(object target)
        {
            var layers = new List<string>();
            for (int i = 0; i < 32; i++)
            {
                var name = LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(name))
                    layers.Add($"{i}: {name}");
            }
            return layers.ToArray();
        }

        private static string[] GetSortingLayerNames(object target)
            => SortingLayer.layers.Select(l => l.name).ToArray();

        private static string[] GetBuildSceneNames(object target)
            => EditorBuildSettings.scenes
                .Select(s => System.IO.Path.GetFileNameWithoutExtension(s.path))
                .ToArray();

        #region Editor Context Validation

        /// <summary>
        /// 嘗試取得物件的 Context，並驗證物件是否仍存在
        /// Editor 專用，會自動清理無效的 Context
        /// </summary>
        public static bool TryGetContextValidated(int instanceID, out string listKey)
        {
            if (_contextMap.TryGetValue(instanceID, out listKey))
            {
                // 驗證物件是否仍然存在
                var obj = EditorUtility.InstanceIDToObject(instanceID);
                if (obj != null)
                    return true;

                // 物件已被刪除，清理 Context
                _contextMap.Remove(instanceID);
            }

            listKey = null;
            return false;
        }

        #endregion
        #endregion
    }
}

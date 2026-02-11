using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    [InitializeOnLoad]
    static class GenericPresetOptionsRegistry
    {
        static GenericPresetOptionsRegistry()
        {
            // 所有 Assembly
            StringOptionsProvider.Register("~Assemblies", (sender, path) =>
            {
                return AppDomain.CurrentDomain.GetAssemblies()
                      .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                      .Where(a =>
                      {
                          var location = a.Location;
                          // Editor 組件通常在 Editor 資料夾或特定路徑
                          return !a.FullName.StartsWith("UnityEditor");
                      })
                      .Select(a => a.GetName().Name)
                      .OrderBy(n => n)
                      .ToArray();
            });

            // 根據 targetAssembly 過濾型別
            StringOptionsProvider.Register("~AssemblyTypes", (sender, path) =>
            {
                if (sender == null || string.IsNullOrEmpty(path))
                    return Array.Empty<string>();

                // path = "value.targetType._assemblyQualifiedName"
                // 目標 = "value.targetAssembly._assemblyName"
                var siblingPath = GetSiblingPath(path, "targetAssembly._assemblyName");
                var assemblyName = GetNestedValue<string>(sender, siblingPath);

                if (string.IsNullOrEmpty(assemblyName))
                    return Array.Empty<string>();

                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == assemblyName);

                if (assembly == null)
                    return Array.Empty<string>();

                return assembly.GetTypes()
                    .Where(t => typeof(UnityEngine.Component).IsAssignableFrom(t) && !t.IsAbstract)
                    .Select(t => t.AssemblyQualifiedName)
                    .OrderBy(n => n)
                    .ToArray();
            });

            // 根據 targetType 過濾屬性
            StringOptionsProvider.Register("~ComponentProperties", (sender, path) =>
            {
                if (sender == null || string.IsNullOrEmpty(path))
                    return Array.Empty<string>();

                // path = "value.properties.Array.data[0].propertyName"
                // 目標 = "value.targetType._assemblyQualifiedName"
                var siblingPath = GetSiblingPath(path, "targetType._assemblyQualifiedName");
                var typeName = GetNestedValue<string>(sender, siblingPath);

                if (string.IsNullOrEmpty(typeName))
                    return Array.Empty<string>();

                var type = Type.GetType(typeName);
                if (type == null)
                    return Array.Empty<string>();

                return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanRead && p.CanWrite)
                    .Select(p => p.Name)
                    .OrderBy(n => n)
                    .ToArray();
            });
        }

        /// <summary>
        /// 從當前欄位的 propertyPath 推算出同層級（或上層）sibling 的路徑
        ///
        /// 策略：
        /// 1. 移除當前欄位名（最後一個 segment）
        /// 2. 如果父路徑是陣列元素（以 ] 結尾），繼續往上跳過整個 Array.data[n] 結構
        /// 3. 再往上一層到容器（跳過子物件名稱，如 targetType）
        /// 4. 拼接 siblingField
        ///
        /// 範例：
        /// "value.targetType._assemblyQualifiedName" + "targetAssembly._assemblyName"
        ///  → 移除 _assemblyQualifiedName → "value.targetType"
        ///  → 往上跳 targetType → "value"
        ///  → 拼接 → "value.targetAssembly._assemblyName"
        ///
        /// "value.properties.Array.data[0].propertyName" + "targetType._assemblyQualifiedName"
        ///  → 移除 propertyName → "value.properties.Array.data[0]"
        ///  → 偵測陣列結尾，跳過 .Array.data[0] → "value.properties"
        ///  → 往上跳 properties → "value"
        ///  → 拼接 → "value.targetType._assemblyQualifiedName"
        /// </summary>
        static string GetSiblingPath(string currentPath, string siblingField)
        {
            if (string.IsNullOrEmpty(currentPath))
                return siblingField;

            // Step 1: 移除當前欄位名
            var lastDot = currentPath.LastIndexOf('.');
            if (lastDot < 0) return siblingField;
            var path = currentPath.Substring(0, lastDot);

            // Step 2: 如果是陣列元素，跳過整個 Array.data[n] 結構
            // 例如 "value.properties.Array.data[0]" → "value.properties"
            if (path.EndsWith("]"))
            {
                // 移除 ".Array.data[n]"
                var arrayPattern = @"\.Array\.data\[\d+\]$";
                path = Regex.Replace(path, arrayPattern, "");
            }

            // Step 3: 再往上一層（跳過當前子物件名稱）
            lastDot = path.LastIndexOf('.');
            if (lastDot < 0) return siblingField;
            var containerPath = path.Substring(0, lastDot);

            // Step 4: 拼接 siblingField
            return containerPath + "." + siblingField;
        }

        /// <summary>
        /// 用 Reflection 取得巢狀欄位值
        /// 支援 Array.data[n] 路徑格式
        /// </summary>
        static T GetNestedValue<T>(object obj, string path)
        {
            if (obj == null || string.IsNullOrEmpty(path))
                return default;

            var parts = path.Split('.');
            object current = obj;

            foreach (var part in parts)
            {
                if (current == null) return default;

                // 處理陣列 "Array.data[0]"
                if (part == "Array") continue;

                if (part.StartsWith("data["))
                {
                    var match = Regex.Match(part, @"data\[(\d+)\]");
                    if (match.Success && current is IList list)
                    {
                        var index = int.Parse(match.Groups[1].Value);
                        if (index < list.Count)
                            current = list[index];
                        else
                            return default;
                        continue;
                    }
                }

                var type = current.GetType();
                var field = type.GetField(part,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (field == null)
                {
                    // 嘗試 property
                    var prop = type.GetProperty(part,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (prop != null)
                        current = prop.GetValue(current);
                    else
                        return default;
                }
                else
                {
                    current = field.GetValue(current);
                }
            }

            return current is T value ? value : default;
        }
    }
}

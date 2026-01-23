using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Yu5h1Lib
{
    public static class RuntimeTypeCache
    {
        private static Dictionary<Type, List<Type>> _attributeCache;

        public static IReadOnlyList<Type> GetTypesWithAttribute<T>() where T : Attribute
        {
            _attributeCache ??= BuildCache();

            return _attributeCache.TryGetValue(typeof(T), out var list)
                ? list
                : Array.Empty<Type>();
        }

        private static Dictionary<Type, List<Type>> BuildCache()
        {
            var cache = new Dictionary<Type, List<Type>>();
            
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var name = assembly.FullName;

                // 跳過系統組件
                if (name.StartsWith("System") ||
                    name.StartsWith("mscorlib") ||
                    name.StartsWith("Mono.") ||
                    name.StartsWith("netstandard") ||
                    name.StartsWith("Microsoft."))
                    continue;

                // Unity 引擎組件跳過，但保留 Unity 專案組件
                if (name.StartsWith("UnityEngine") ||
                    name.StartsWith("UnityEditor"))
                    continue;

                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        foreach (var attr in type.GetCustomAttributes(true))
                        {
                            var attrType = attr.GetType();
                            if (!cache.TryGetValue(attrType, out var list))
                                cache[attrType] = list = new List<Type>();

                            if (!list.Contains(type))
                                list.Add(type);
                        }
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    // 某些組件可能載入失敗，跳過
                }
            }

            return cache;
        }
    }
}

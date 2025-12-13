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
                // 跳過系統組件加速
                if (assembly.FullName.StartsWith("System") ||
                    assembly.FullName.StartsWith("mscorlib") ||
                    assembly.FullName.StartsWith("Unity."))
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

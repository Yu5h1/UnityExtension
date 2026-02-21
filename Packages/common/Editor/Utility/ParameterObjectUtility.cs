using System;
using System.Collections.Generic;
using System.Linq;

namespace Yu5h1Lib.EditorExtension
{
    public static class ParameterObjectUtility
    {
        static Dictionary<Type, Type> _cache = new();

        /// <summary>
        /// Given a value type (e.g. typeof(bool)), find the concrete ParameterObject&lt;T&gt; implementation.
        /// </summary>
        public static Type GetParameterObjectType(Type valueType)
        {
            if (_cache.TryGetValue(valueType, out var cached))
                return cached;

            var baseType = typeof(ParameterObject<>).MakeGenericType(valueType);

            var implType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => {
                    try { return a.GetTypes(); }
                    catch { return Type.EmptyTypes; }
                })
                .FirstOrDefault(t =>
                    !t.IsAbstract &&
                    baseType.IsAssignableFrom(t));

            _cache[valueType] = implType;
            return implType;
        }

        public static bool IsSupported(Type valueType)
            => GetParameterObjectType(valueType) != null;
    }
}

using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Type = System.Type;
using Exception = System.Exception;

namespace Yu5h1Lib
{
    /// <summary>
    /// Shared reflection-based member setter used by both ParameterObject (ScriptableObject) and
    /// ParameterBehaviour (MonoBehaviour). Writes a value into <c>target.[memberName]</c>, matching a public
    /// property setter or single-argument method whose parameter type equals <c>declaredType</c>.
    /// Setters are resolved once and cached globally.
    /// </summary>
    public static class ParameterMember
    {
        private static readonly BindingFlags flags =
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy;

        private static readonly Dictionary<SetterKey, MethodInfo> _setterCache = new();

        private readonly struct SetterKey : System.IEquatable<SetterKey>
        {
            public readonly Type targetType;
            public readonly string memberName;
            public readonly Type declaredType;

            public SetterKey(Type targetType, string memberName, Type declaredType)
            {
                this.targetType = targetType;
                this.memberName = memberName;
                this.declaredType = declaredType;
            }

            public bool Equals(SetterKey other)
                => targetType == other.targetType && memberName == other.memberName && declaredType == other.declaredType;

            public override bool Equals(object obj) => obj is SetterKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    int h = targetType != null ? targetType.GetHashCode() : 0;
                    h = (h * 397) ^ (memberName != null ? memberName.GetHashCode() : 0);
                    h = (h * 397) ^ (declaredType != null ? declaredType.GetHashCode() : 0);
                    return h;
                }
            }
        }

        private static MethodInfo GetSetterCached(Type type, string memberName, Type declaredType)
        {
            var key = new SetterKey(type, memberName, declaredType);
            if (_setterCache.TryGetValue(key, out var cached))
                return cached; // may be null (negative cache)

            MethodInfo setter = null;

            // property first
            var prop = type.GetProperty(memberName, flags);
            if (prop != null && prop.PropertyType == declaredType)
            {
                var setMethod = prop.SetMethod;
                if (setMethod != null && setMethod.IsPublic)
                    setter = setMethod;
            }

            // single-argument method fallback
            if (setter == null)
            {
                var m = type.GetMethod(memberName, flags, null, new[] { declaredType }, null);
                if (m != null && m.IsPublic)
                    setter = m;
            }

            _setterCache[key] = setter; // cache (including null)
            return setter;
        }

        /// <summary>
        /// Reflection-set <paramref name="target"/>.[<paramref name="memberName"/>] = <paramref name="value"/>.
        /// The setter is matched against the value's concrete type (so a base <paramref name="declaredType"/>
        /// like UnityEngine.Object still resolves a derived member such as Transform); when the value is null,
        /// <paramref name="declaredType"/> is used instead. A trailing "a.b.Member" path uses only the last segment.
        /// No-op when the member is absent, or when a null value is assigned to a non-nullable value type.
        /// </summary>
        public static void Apply(Object target, string memberName, object value, Type declaredType)
        {
            if (string.IsNullOrEmpty(memberName) || target == null || declaredType == null)
                return;

            var dot = memberName.LastIndexOf('.');
            if (dot >= 0)
                memberName = memberName.Substring(dot + 1);

            if (value == null && declaredType.IsValueType && System.Nullable.GetUnderlyingType(declaredType) == null)
                return;

            var type = target.GetType();

            // Prefer the value's concrete type so ObjectParameter (declaredType = UnityEngine.Object)
            // still matches a derived property like Transform. Fall back to declaredType when value is null.
            var lookupType = value != null ? value.GetType() : declaredType;

            var setter = GetSetterCached(type, memberName, lookupType);
            if (setter == null)
            {
#if UNITY_EDITOR
                $"No member '{memberName}' ({lookupType.Name}) on {type.Name}".printWarning();
#endif
                return;
            }

            try
            {
                setter.Invoke(target, new object[] { value });
            }
            catch (Exception e)
            {
                ($"Invoke failed: '{type.Name}.{setter.Name}' expects '{lookupType.FullName}', " +
                 $"value '{value?.GetType().FullName ?? "null"}'. {e.GetType().Name}: {e.Message}")
                 .printWarning();
            }
        }
    }
}

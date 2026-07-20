using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Yu5h1Lib
{
    /// <summary>
    /// Resolves and sets public instance properties by name. Resolved setters, including misses,
    /// are cached by target type and property name.
    /// </summary>
    public static class PropertySetter
    {
        private const BindingFlags Flags =
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy;

        private static readonly Dictionary<SetterKey, MethodInfo> SetterCache = new();

        private readonly struct SetterKey : IEquatable<SetterKey>
        {
            public readonly Type targetType;
            public readonly string propertyName;

            public SetterKey(Type targetType, string propertyName)
            {
                this.targetType = targetType;
                this.propertyName = propertyName;
            }

            public bool Equals(SetterKey other)
                => targetType == other.targetType && propertyName == other.propertyName;

            public override bool Equals(object obj)
                => obj is SetterKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((targetType != null ? targetType.GetHashCode() : 0) * 397) ^
                           (propertyName != null ? propertyName.GetHashCode() : 0);
                }
            }
        }

        /// <summary>
        /// Sets a public instance property. The value must be assignable to the property's type.
        /// A dotted name uses only its final segment.
        /// </summary>
        public static bool Set(Object target, string propertyName, object value, Type declaredType)
        {
            if (target == null || string.IsNullOrEmpty(propertyName) || declaredType == null)
                return false;

            propertyName = GetLastSegment(propertyName);
            var setter = GetSetter(target.GetType(), propertyName);
            if (setter == null)
            {
#if UNITY_EDITOR
                $"No public property '{propertyName}' on {target.GetType().Name}".printWarning();
#endif
                return false;
            }

            var propertyType = setter.GetParameters()[0].ParameterType;
            if (!CanAssign(propertyType, value, declaredType))
            {
#if UNITY_EDITOR
                $"Cannot assign '{declaredType.FullName}' to '{target.GetType().Name}.{propertyName}' ({propertyType.FullName})"
                    .printWarning();
#endif
                return false;
            }

            try
            {
                setter.Invoke(target, new[] { value });
                return true;
            }
            catch (Exception exception)
            {
                var cause = exception is TargetInvocationException invocationException &&
                            invocationException.InnerException != null
                    ? invocationException.InnerException
                    : exception;

                ($"Set property failed: '{target.GetType().Name}.{propertyName}'. " +
                 $"{cause.GetType().Name}: {cause.Message}").printWarning();
                return false;
            }
        }

        private static MethodInfo GetSetter(Type targetType, string propertyName)
        {
            var key = new SetterKey(targetType, propertyName);
            if (SetterCache.TryGetValue(key, out var cached))
                return cached;

            var property = targetType.GetProperty(propertyName, Flags);
            var setter = property?.SetMethod;
            if (setter == null || !setter.IsPublic || setter.IsStatic)
                setter = null;

            SetterCache[key] = setter;
            return setter;
        }

        private static bool CanAssign(Type propertyType, object value, Type declaredType)
        {
            if (value != null)
                return propertyType.IsInstanceOfType(value);

            if (propertyType.IsValueType && Nullable.GetUnderlyingType(propertyType) == null)
                return false;

            return propertyType.IsAssignableFrom(declaredType) ||
                   declaredType.IsAssignableFrom(propertyType);
        }

        private static string GetLastSegment(string memberName)
        {
            var dot = memberName.LastIndexOf('.');
            return dot >= 0 ? memberName.Substring(dot + 1) : memberName;
        }
    }
}

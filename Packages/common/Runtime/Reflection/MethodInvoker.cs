using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Yu5h1Lib
{
    /// <summary>
    /// Resolves and invokes public instance methods using ParameterObject declared types.
    /// Resolved methods, including misses, are cached by target type, method name, and signature.
    /// </summary>
    public static class MethodInvoker
    {
        private const BindingFlags Flags =
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy;

        private static readonly Dictionary<MethodKey, MethodInfo> MethodCache = new();

        private readonly struct MethodKey : IEquatable<MethodKey>
        {
            public readonly Type targetType;
            public readonly string methodName;
            public readonly Type[] parameterTypes;

            public MethodKey(Type targetType, string methodName, Type[] parameterTypes)
            {
                this.targetType = targetType;
                this.methodName = methodName;
                this.parameterTypes = parameterTypes;
            }

            public bool Equals(MethodKey other)
            {
                if (targetType != other.targetType || methodName != other.methodName ||
                    parameterTypes.Length != other.parameterTypes.Length)
                    return false;

                for (int i = 0; i < parameterTypes.Length; i++)
                {
                    if (parameterTypes[i] != other.parameterTypes[i])
                        return false;
                }

                return true;
            }

            public override bool Equals(object obj)
                => obj is MethodKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = targetType != null ? targetType.GetHashCode() : 0;
                    hash = (hash * 397) ^ (methodName != null ? methodName.GetHashCode() : 0);
                    for (int i = 0; i < parameterTypes.Length; i++)
                        hash = (hash * 397) ^ (parameterTypes[i] != null ? parameterTypes[i].GetHashCode() : 0);
                    return hash;
                }
            }
        }

        /// <summary>
        /// Invokes a public instance method whose signature exactly matches the declared parameter types.
        /// A dotted name uses only its final segment.
        /// </summary>
        public static void Invoke(
            Object target,
            string methodName,
            IReadOnlyList<ParameterObject> parameters)
            => TryInvoke(target, methodName, parameters);

        /// <summary>
        /// Tries to invoke a public instance method whose signature exactly matches the declared
        /// parameter types. A dotted name uses only its final segment.
        /// </summary>
        public static bool TryInvoke(
            Object target,
            string methodName,
            IReadOnlyList<ParameterObject> parameters)
        {
            if (target == null || string.IsNullOrEmpty(methodName))
                return false;

            methodName = GetLastSegment(methodName);
            if (!TryGetArguments(parameters, out var parameterTypes, out var arguments))
                return false;

            var method = GetMethod(target.GetType(), methodName, parameterTypes);
            if (method == null)
            {
#if UNITY_EDITOR
                $"No public method '{FormatSignature(methodName, parameterTypes)}' on {target.GetType().Name}"
                    .printWarning();
#endif
                return false;
            }

            try
            {
                method.Invoke(target, arguments);
                return true;
            }
            catch (Exception exception)
            {
                var cause = exception is TargetInvocationException invocationException &&
                            invocationException.InnerException != null
                    ? invocationException.InnerException
                    : exception;

                ($"Invoke failed: '{target.GetType().Name}.{FormatSignature(methodName, parameterTypes)}'. " +
                 $"{cause.GetType().Name}: {cause.Message}").printWarning();
                return false;
            }
        }

        private static MethodInfo GetMethod(Type targetType, string methodName, Type[] parameterTypes)
        {
            var key = new MethodKey(targetType, methodName, parameterTypes);
            if (MethodCache.TryGetValue(key, out var cached))
                return cached;

            var method = targetType.GetMethod(methodName, Flags, null, parameterTypes, null);
            if (method != null && (method.IsStatic || method.ContainsGenericParameters || method.IsSpecialName))
                method = null;

            MethodCache[key] = method;
            return method;
        }

        private static bool TryGetArguments(
            IReadOnlyList<ParameterObject> parameters,
            out Type[] parameterTypes,
            out object[] arguments)
        {
            var count = parameters?.Count ?? 0;
            parameterTypes = count == 0 ? Type.EmptyTypes : new Type[count];
            arguments = count == 0 ? Array.Empty<object>() : new object[count];

            for (int i = 0; i < count; i++)
            {
                var parameter = parameters[i];
                if (parameter == null || parameter.DeclaredType == null)
                {
#if UNITY_EDITOR
                    $"Method parameter at index {i} is unassigned".printWarning();
#endif
                    return false;
                }

                parameterTypes[i] = parameter.DeclaredType;
                arguments[i] = parameter.GetValue();
            }

            return true;
        }

        private static string FormatSignature(string methodName, IReadOnlyList<Type> parameterTypes)
        {
            var names = new string[parameterTypes.Count];
            for (int i = 0; i < parameterTypes.Count; i++)
                names[i] = parameterTypes[i]?.Name ?? "null";
            return $"{methodName}({string.Join(", ", names)})";
        }

        private static string GetLastSegment(string memberName)
        {
            var dot = memberName.LastIndexOf('.');
            return dot >= 0 ? memberName.Substring(dot + 1) : memberName;
        }
    }
}

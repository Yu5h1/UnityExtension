using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Yu5h1Lib.EditorExtension
{
    public static class MethodOptionUtility
    {
        private const BindingFlags Flags =
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy;

        public static IReadOnlyList<MethodInfo> GetSupportedMethods(Type targetType)
        {
            if (targetType == null)
                return Array.Empty<MethodInfo>();

            return targetType
                .GetMethods(Flags)
                .Where(method => !method.IsStatic)
                .Where(method => !method.IsSpecialName)
                .Where(method => !method.ContainsGenericParameters)
                .Where(method => method.DeclaringType != typeof(object))
                .Where(method => method.GetParameters().All(IsSupportedParameter))
                .OrderBy(method => method.Name)
                .ThenBy(GetSignature)
                .ToArray();
        }

        public static string GetSignature(MethodInfo method)
        {
            var parameterNames = method
                .GetParameters()
                .Select(parameter => parameter.ParameterType.Name);

            return $"{method.Name} ({string.Join(", ", parameterNames)})";
        }

        private static bool IsSupportedParameter(ParameterInfo parameter)
            => !parameter.ParameterType.IsByRef &&
               ParameterObjectUtility.IsSupported(parameter.ParameterType);
    }
}

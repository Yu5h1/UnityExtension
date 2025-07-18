using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib.Common
{
    public static class TypedStringParser
    {
        public enum ParseType
        {
            String,
            Int,
            Double,
            Bool,
            Float
        }

        private static readonly Dictionary<Type, Func<string, object>> Parsers = new()
        {
            { typeof(string), input => input },
            { typeof(int), input => int.TryParse(input, out int result) ? result : 0 },
            { typeof(decimal), input => decimal.TryParse(input, out decimal result) ? result : 0m },
            { typeof(bool), input => ParseBool(input) },
            { typeof(float), input => float.TryParse(input, out float result) ? result : 0f },
            { typeof(double), input => double.TryParse(input, out double result) ? result : 0d },
            { typeof(long), input => long.TryParse(input, out long result) ? result : 0L },
        };
        public static object Parse(string input, ParseType type) => type switch
        {
            ParseType.Int => Parse(input, typeof(int)),
            ParseType.Double => Parse(input, typeof(double)),
            ParseType.Bool => Parse(input, typeof(bool)),
            _ => input
        };
        public static object Parse(string input, Type targetType)
        {
            if (string.IsNullOrEmpty(input))
                return GetDefaultValue(targetType);

            if (Parsers.TryGetValue(targetType, out var parser))
                return parser(input);

            // ¦^°h¨ì Convert.ChangeType
            try
            {
                return Convert.ChangeType(input, targetType);
            }
            catch
            {
                return GetDefaultValue(targetType);
            }
        }

        public static T Parse<T>(string input)
        {
            return (T)Parse(input, typeof(T));
        }

        public static bool TryParse<T>(string input, out T result)
        {
            try
            {
                result = Parse<T>(input);
                return true;
            }
            catch
            {
                result = default(T);
                return false;
            }
        }

        private static bool ParseBool(string input)
        {
            var normalized = input.ToLowerInvariant().Trim();
            return normalized is "true" or "1" or "yes" or "on" or "enabled";
        }

        private static object GetDefaultValue(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }
    }
}

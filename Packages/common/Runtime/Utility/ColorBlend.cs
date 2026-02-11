using UnityEngine;
using System.Collections.Generic;

namespace Yu5h1Lib.UI.Effects
{
    /// <summary>
    /// 顏色混合運算集
    /// </summary>
    public static class ColorBlend
    {
        public delegate Color Operator(Color baseColor, Color blendColor, float weight);

        private static readonly Dictionary<string, Operator> _modes = new Dictionary<string, Operator>
        {
            ["Replace"]  = (a, b, w) => Color.Lerp(a, b, w),
            ["Multiply"] = (a, b, w) => Color.Lerp(a, a * b, w),
            ["Add"]      = (a, b, w) => Color.Lerp(a, Clamp01(a + b), w),
            ["Screen"]   = (a, b, w) => Color.Lerp(a, Screen(a, b), w),
            ["Overlay"]  = (a, b, w) => Color.Lerp(a, Overlay(a, b), w),
            ["SoftLight"]= (a, b, w) => Color.Lerp(a, SoftLight(a, b), w),
            ["Difference"] = (a, b, w) => Color.Lerp(a, Difference(a, b), w),
            ["Darken"]   = (a, b, w) => Color.Lerp(a, Min(a, b), w),
            ["Lighten"]  = (a, b, w) => Color.Lerp(a, Max(a, b), w),
        };

        public static IReadOnlyDictionary<string, Operator> Modes => _modes;

        public static string[] ModeKeys
        {
            get
            {
                var keys = new string[_modes.Count];
                _modes.Keys.CopyTo(keys, 0);
                return keys;
            }
        }

        /// <summary>
        /// 註冊自訂混合模式
        /// </summary>
        public static void Register(string key, Operator op)
        {
            _modes[key] = op;
        }

        /// <summary>
        /// 取得混合運算子
        /// </summary>
        public static Operator GetOperator(string key)
        {
            return _modes.TryGetValue(key, out var op) ? op : _modes["Multiply"];
        }

        /// <summary>
        /// 執行混合
        /// </summary>
        public static Color Blend(string mode, Color baseColor, Color blendColor, float weight = 1f)
        {
            return GetOperator(mode)(baseColor, blendColor, weight);
        }

        #region Blend Functions

        private static Color Clamp01(Color c) => new Color(
            Mathf.Clamp01(c.r),
            Mathf.Clamp01(c.g),
            Mathf.Clamp01(c.b),
            Mathf.Clamp01(c.a)
        );

        // Screen: 1 - (1-A)(1-B)
        private static Color Screen(Color a, Color b) => new Color(
            1f - (1f - a.r) * (1f - b.r),
            1f - (1f - a.g) * (1f - b.g),
            1f - (1f - a.b) * (1f - b.b),
            1f - (1f - a.a) * (1f - b.a)
        );

        // Overlay: base < 0.5 ? 2AB : 1 - 2(1-A)(1-B)
        private static Color Overlay(Color a, Color b) => new Color(
            a.r < 0.5f ? 2f * a.r * b.r : 1f - 2f * (1f - a.r) * (1f - b.r),
            a.g < 0.5f ? 2f * a.g * b.g : 1f - 2f * (1f - a.g) * (1f - b.g),
            a.b < 0.5f ? 2f * a.b * b.b : 1f - 2f * (1f - a.b) * (1f - b.b),
            a.a * b.a
        );

        // SoftLight: 柔光
        private static Color SoftLight(Color a, Color b) => new Color(
            SoftLightChannel(a.r, b.r),
            SoftLightChannel(a.g, b.g),
            SoftLightChannel(a.b, b.b),
            a.a * b.a
        );

        private static float SoftLightChannel(float a, float b)
        {
            return b < 0.5f
                ? a - (1f - 2f * b) * a * (1f - a)
                : a + (2f * b - 1f) * (D(a) - a);
        }

        private static float D(float x) => x < 0.25f
            ? ((16f * x - 12f) * x + 4f) * x
            : Mathf.Sqrt(x);

        // Difference: |A - B|
        private static Color Difference(Color a, Color b) => new Color(
            Mathf.Abs(a.r - b.r),
            Mathf.Abs(a.g - b.g),
            Mathf.Abs(a.b - b.b),
            a.a * b.a
        );

        // Darken: min(A, B)
        private static Color Min(Color a, Color b) => new Color(
            Mathf.Min(a.r, b.r),
            Mathf.Min(a.g, b.g),
            Mathf.Min(a.b, b.b),
            Mathf.Min(a.a, b.a)
        );

        // Lighten: max(A, B)
        private static Color Max(Color a, Color b) => new Color(
            Mathf.Max(a.r, b.r),
            Mathf.Max(a.g, b.g),
            Mathf.Max(a.b, b.b),
            Mathf.Max(a.a, b.a)
        );

        #endregion

        #region Factory Methods for Custom Operators

        /// <summary>
        /// 建立 Power 混合模式
        /// </summary>
        public static Operator CreatePower(float exponent)
        {
            return (a, b, w) => Color.Lerp(a, new Color(
                Mathf.Pow(a.r, exponent),
                Mathf.Pow(a.g, exponent),
                Mathf.Pow(a.b, exponent),
                a.a
            ) * b, w);
        }

        /// <summary>
        /// 建立 Lerp 混合模式（固定 t 值）
        /// </summary>
        public static Operator CreateLerp(float t)
        {
            return (a, b, w) => Color.Lerp(a, Color.Lerp(a, b, t), w);
        }

        #endregion
    }
}

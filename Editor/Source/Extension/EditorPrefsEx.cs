using System.ComponentModel;
using UnityEditor;
using UnityEngine;
using Yu5h1Lib.EditorExtension;

namespace Yu5h1Lib.EditorPrefsExtension
{
    [EditorBrowsable(EditorBrowsableState.Never), Browsable(false)]
    public static class EditorPrefsEx
    {
        public static string GetPrefsKey<T>(this T obj, string parameterName) where T : ScriptableObject
             => obj switch
             {
                 Editor editor => EditorPrefsUtility.GetPrefsKey<T>(parameterName),
                 EditorWindow window=> EditorPrefsUtility.GetPrefsKey<T>(parameterName),
                 _ => throw new System.NotSupportedException($"NotSupported: {typeof(T)}"),
             };
        public static bool GetBool<T>(this T obj, string parameterName, bool defaultValue = false) where T : ScriptableObject
            => EditorPrefsUtility.GetBool<T>(parameterName, defaultValue);
        public static void SetBool<T>(this T obj, string parameterName,bool value ) where T : ScriptableObject
            => EditorPrefsUtility.SetBool<T>(parameterName, value);
        public static float GetFloat<T>(this T obj, string parameterName, float defaultValue = 0) where T : ScriptableObject
            => EditorPrefsUtility.GetFloat<T>(parameterName, defaultValue);
        public static void SetFloat<T>(this T obj, string parameterName, float value) where T : ScriptableObject
            => EditorPrefsUtility.SetFloat<T>(parameterName, value);
        public static float GetInt<T>(this T obj, string parameterName, int defaultValue = 0) where T : ScriptableObject
            => EditorPrefsUtility.GetInt<T>(parameterName, defaultValue);
        public static void SetInt<T>(this T obj, string parameterName, int value) where T : ScriptableObject
            => EditorPrefsUtility.SetInt<T>(parameterName, value);
    }
}

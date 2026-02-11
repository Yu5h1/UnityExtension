using Type = System.Type;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;


namespace Yu5h1Lib.EditorExtension
{
    public static class EditorPrefsUtility
    {
        public static string GetPrefsKey(Type type,string parameterName) => $"{type.FullName}_{parameterName}";
        public static string GetPrefsKey<T>(string parameterName) => GetPrefsKey(typeof(T),parameterName);

        public static bool GetBool<T>(string parameterName, bool defaultValue = false) where T : ScriptableObject
            => EditorPrefs.GetBool(GetPrefsKey<T>(parameterName), defaultValue);
        public static void SetBool<T>(string parameterName, bool value) where T : ScriptableObject
            => EditorPrefs.SetBool(GetPrefsKey<T>(parameterName), value);
        public static float GetFloat<T>( string parameterName, float defaultValue = 0) where T : ScriptableObject
            => EditorPrefs.GetFloat(GetPrefsKey<T>(parameterName), defaultValue);
        public static void SetFloat<T>( string parameterName, float value) where T : ScriptableObject
            => EditorPrefs.SetFloat(GetPrefsKey<T>(parameterName), value);
        public static int GetInt<T>(string parameterName, int defaultValue = 0) where T : ScriptableObject
            => EditorPrefs.GetInt(GetPrefsKey<T>(parameterName), defaultValue);
        public static void SetInt<T>(string parameterName, int value) where T : ScriptableObject
            => EditorPrefs.SetInt(GetPrefsKey<T>(parameterName), value);
    }
}

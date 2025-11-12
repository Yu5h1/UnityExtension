
using UnityEngine;

namespace Yu5h1Lib.PlayerPrefsExtension
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never), System.ComponentModel.Browsable(false)]
	public static class PlayerPrefsEx
	{
        public static string GetPrefsKey<T>(this T obj, string parameterName) where T : UnityEngine.Object
             => obj switch
             {
                 ScriptableObject sobj => GetPrefsKey<T>(parameterName),
                 MonoBehaviour behaviour => GetPrefsKey<T>(parameterName),
                 _ => throw new System.NotSupportedException($"Type:{typeof(T)} Does NotSupported"),
             };
        private static string GetPrefsKey<T>(string parameterName) => $"{typeof(T).FullName}_{parameterName}";
    }
}
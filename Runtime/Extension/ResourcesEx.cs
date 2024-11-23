using UnityEngine;

namespace Yu5h1Lib
{
    public static class ResourcesEx
    {
        public static bool PrintLog;
        public static bool TryLoad<T>(string path, out T result, bool throwNullReferenceException = true) where T : Object
        {
            result = Resources.Load<T>(path);
#if UNITY_EDITOR
        if (throwNullReferenceException)
        {
            if (!result)
                throw new System.NullReferenceException($"({typeof(T)}) Missing resourecs path : \"{path}\"");
            else if (PrintLog)
                Debug.Log($"Resources.Loaded(\"{path}\")");
        }
#endif
            return result;
        }
    } 
}
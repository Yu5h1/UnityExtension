using UnityEngine;

namespace Yu5h1Lib
{
	public static class ObjectUtility
	{
        public static T FindObject<T>() where T : Object
        {
#if UNITY_6000_4_OR_NEWER
            return GameObject.FindAnyObjectByType<T>();
#else
            return GameObject.FindFirstObjectByType<T>();
#endif
        }
    }
}
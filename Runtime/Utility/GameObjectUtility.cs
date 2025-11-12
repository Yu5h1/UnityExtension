using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    public static class GameObjectUtility
    {
        #region Creation
        public static T Create<T>() where T : Component => new GameObject(typeof(T).Name).AddComponent<T>();

        public static T Create<T>(Transform parent) where T : Component
        {
            var result = Create<T>();
            if (parent)
                result.transform.SetParent(parent, false);
            return result;
        }
        #endregion

        #region Find
        public static bool TryFind<T>(out T result, bool includeInactive = true) where T : Component
            => (result = GameObject.FindFirstObjectByType<T>(includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude)) != null;

        public static GameObject FindOrCreate(string name) => GameObject.Find(name) ?? new GameObject(name);

        public static T FindOrCreate<T>(string name) where T : Component
        {
            GameObject obj = FindOrCreate(name);
            return obj.GetComponent<T>() ?? obj.AddComponent<T>();
        }
        #endregion

        #region Search



        #endregion

    }
}

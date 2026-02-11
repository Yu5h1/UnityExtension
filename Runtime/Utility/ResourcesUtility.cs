using UnityEngine;

namespace Yu5h1Lib
{
    public static class ResourcesUtility
    {
        #region Resources Folder 
        public static bool TryInstantiateFromResources<T>(out T result, string path, Transform parent = null,bool removeCloneSuffix = false) where T : Object
        {
            result = null;
            if (!TryLoad(path, out T source))
                return false;
            result = parent == null ? GameObject.Instantiate(source) : GameObject.Instantiate(source, parent);
            if (removeCloneSuffix)
                result.name = result.GetNameWithOutClone();
            return true;
        }
        public static T InstantiateFromResourecs<T>(string path, Transform parent = null,bool removeCloneSuffix = false) where T : Object
            => TryInstantiateFromResources(out T result, path, parent, removeCloneSuffix) ? result : null;

        public static bool TryInstantiateFromResources<T>(out T result, Transform parent = null) where T : Object
            => TryInstantiateFromResources(out result, typeof(T).Name, parent);

        #endregion

        public static bool TryLoad<T>(string path, out T result) where T : Object
        {
            result = Resources.Load<T>(path);
            return result;
        }
        public static T LoadAsInstance<T>(ref T instance) where T : Object
        {
            if (!instance)
            {
                var typeName = typeof(T).Name;
                instance = Resources.Load<T>(typeName);
                $"{typeName} is not found in the Resources folder. Ensure a resource named '{typeName}' exists at the correct path."
                    .printWarningIf(!instance);
            }
            return instance;
        }
    } 
}
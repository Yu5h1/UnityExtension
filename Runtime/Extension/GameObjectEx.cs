using UnityEngine;

namespace Yu5h1Lib
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never), System.ComponentModel.Browsable(false)]
    public static partial class GameObjectEx
    {
        public static bool InstantiateWithCloneSuffix = false;

        private static string GetNameWithOutClone<T>(this T obj) where T : Object
        {
            var name = obj.name;
            if (name.EndsWith("(Clone)"))
                name = name.Remove(name.Length - "(Clone)".Length);
            return name;
        }

        public static T New<T>() where T : Component => new GameObject(typeof(T).Name).AddComponent<T>();

        public static bool TryFind<T>(out T result, bool includeInactive = true) where T : Component
            => (result = GameObject.FindObjectOfType<T>(includeInactive)) != null;

        public static bool TryInstantiateFromResources<T>(out T result, string path, Transform parent = null, bool throwNullReferenceException = true) where T : Object
        {
            result = null;
            if (!ResourcesEx.TryLoad(path, out T source, throwNullReferenceException))
                return false;
            result = (parent == null) ? GameObject.Instantiate(source) : GameObject.Instantiate(source, parent);
            if (!InstantiateWithCloneSuffix)
                result.name = result.GetNameWithOutClone();
            return true;
        }



        #region Unorgernized
        public static GameObject FindOrCreate(string name)
        {
            GameObject result = GameObject.Find(name);
            if (result == null) result = new GameObject(name);
            return result;
        }
        public static Component GetOrAddComponet(this GameObject gameObject, System.Type type)
        {
            var result = gameObject.GetComponent(type);
            return result ? result : gameObject.AddComponent(type);
        }
        public static T FindOrCreate<T>(string name) where T : Component
        {
            GameObject obj = FindOrCreate(name);
            T result = obj.GetComponent<T>();
            if (result == null) result = obj.AddComponent<T>();
            return result;
        }
        public static T GetComponentInChildren<T>(this GameObject gobj, string name) where T : Component
        {
            foreach (var item in gobj.GetComponentsInChildren<T>(true))
            { if (item.name == name) return item; }
            return null;
        }
        public static T GetComponentInParent<T>(this GameObject gobj, string name) where T : Component
        {
            foreach (var item in gobj.GetComponentsInParent<T>(true))
            { if (item.name == name) return item; }
            return null;
        }
        #endregion

        #region Scene Related

        public static void DestoryOnLoad(this GameObject obj)
        {
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(obj, UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        #endregion
    } 
}

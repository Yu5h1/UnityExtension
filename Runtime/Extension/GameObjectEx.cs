using UnityEngine;

namespace Yu5h1Lib
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never), System.ComponentModel.Browsable(false)]
    public static partial class GameObjectEx
    {

        #region Unorgernized

        public static Component GetOrAddComponet(this GameObject gameObject, System.Type type)
        {
            var result = gameObject.GetComponent(type);
            return result ? result : gameObject.AddComponent(type);
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

        #region Comparision
        public static bool CompareLayer(this GameObject obj, LayerMask layerMask) 
            => ((1 << obj.layer) & layerMask.value) != 0;
        public static bool CompareLayer(this GameObject obj, string layerName)
            => ((1 << obj.layer) & LayerMask.GetMask(layerName)) != 0;
        #endregion
    } 
}

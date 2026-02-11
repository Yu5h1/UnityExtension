using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    public abstract class SingletonEditorWindow<T> : EditorWindow where T : EditorWindow
    {
        private static EditorWindow instance;
        public static T current
        {
            get
            {

                if (instance == null) CreateWindow(typeof(T).Name);
                return (T)instance;
            }
        }
        public static void CreateWindow(string titleContent)
        {
            instance = EditorWindow.GetWindow(typeof(T)) ;
            current.titleContent = new GUIContent(titleContent);
        }
    }
}

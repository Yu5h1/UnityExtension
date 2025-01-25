using UnityEngine;
using UnityEditor;

namespace Yu5h1Lib.EditorExtension
{
    public static class EditorWindowUtil
    {        
        public static EditorWindow[] GetAllEditorWindows() => Resources.FindObjectsOfTypeAll<EditorWindow>();

        public static EditorWindow GetExistsWindow(System.Type type)
        {
            var results = GetAllEditorWindows();
            foreach (var item in results) 
                if (item.GetType() == type) return item;
            $"Type {type.Name} does not exist.".printWarning();
            return null;
        }
        public static bool TryGetExistsWindow(System.Predicate<EditorWindow> expression, out EditorWindow? window)
        {
            window = GetAllEditorWindows().Find(expression);
            return window != null;
        }
        public static EditorWindow GetExistsWindow(string text,System.Func<EditorWindow,bool> expression)
        {
            var allwindows = GetAllEditorWindows();
            foreach (var item in allwindows) if (expression(item)) return item;
            Debug.LogWarning("\""+text +"\" does not found.");
            return null;
        }

        public static EditorWindow GetExistsWindow(string TypeName)
        {
            return GetExistsWindow(TypeName, window => window.GetType().Name == TypeName );
        }
        public static EditorWindow GetExistsWindowByTitle(string title)
        {
            return GetExistsWindow(title, window => window.titleContent.text == title);
        }
        public static Object[] GetEditorWindows(System.Type type)
        {
            return Resources.FindObjectsOfTypeAll(type);
        }
        public static T[] GetEditorWindows<T>() where T : EditorWindow
        {
            return Resources.FindObjectsOfTypeAll<T>();
        }
        public static T GetExistsWindow<T>() where T : EditorWindow
        {
            return (T)GetExistsWindow(typeof(T));
        }
        public static void RepaintAllEditorWindows() {
            foreach (var editorWindow in GetAllEditorWindows())
            {
                editorWindow.Repaint();
            }
        }
    }
}
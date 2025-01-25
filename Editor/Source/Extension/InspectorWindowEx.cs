using Type = System.Type;
using System.Reflection;
using UnityEditor;
using UnityEngine;


namespace Yu5h1Lib.EditorExtension
{
    public static class InspectorWindowEx
    {
        public static Type InternalType { get { return typeof(Editor).Assembly.GetType("UnityEditor.InspectorWindow"); } }

        public static Object[] GetInspectorWindows()
        {
            return Resources.FindObjectsOfTypeAll(InternalType);
        }
        public static object currentInspectorWindow
        {
            get {
                var inspectors = GetInspectorWindows();
                if (inspectors.Length > 0) return inspectors[0];
                return null;
            } 
        }
        public static void RepaintAllInspectors()
        {
            foreach (var item in GetInspectorWindows())
            {
                ((EditorWindow)item).Repaint();
            }
        }
    }
}

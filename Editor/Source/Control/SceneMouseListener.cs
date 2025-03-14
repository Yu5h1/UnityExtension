using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib.EditorExtension
{
    public static class SceneMouseListener
    {
        public static string EnableKey => typeof(SceneMouseListener).FullName;
        public static bool Enable 
        {
            get => EditorPrefs.GetBool(EnableKey, false);
            set {
                if (Enable == value)
                    return;
                EditorPrefs.SetBool(EnableKey, value);
                Register();
            }
        }
        [InitializeOnLoadMethod]
        public static void Register()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            if (!Enable)
                return;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        public static event UnityAction<GameObject, Event> MouseDown;
        public static event UnityAction<GameObject, Event> MouseMove;
        public static event UnityAction<GameObject, Event> MouseDrag;
        public static event UnityAction<GameObject, Event> MouseUp;

        private static void OnSceneGUI(SceneView sceneView)
        {
            var activeObject = Selection.activeGameObject;
            if (activeObject == null)
                return;
            Event e = Event.current;
            if (e.type == EventType.MouseDown)
                MouseDown?.Invoke(Selection.activeGameObject,e);
            else if (e.type == EventType.MouseMove)
                MouseMove?.Invoke(Selection.activeGameObject, e);
            else if (e.type == EventType.MouseDrag)
                MouseDrag?.Invoke(Selection.activeGameObject, e);
            else if (e.type == EventType.MouseUp)
                MouseUp?.Invoke(Selection.activeGameObject, e);
        }
    }
}

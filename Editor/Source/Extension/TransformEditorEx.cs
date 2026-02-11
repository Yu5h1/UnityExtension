using System.ComponentModel;
using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    [EditorBrowsable(EditorBrowsableState.Never), Browsable(false)]
    public static class TransformEditorEx
	{
        public const string BaseLabel = "CONTEXT/Transform/";

        public const string ResetWithoutMovingChildrenLabel = BaseLabel + "more.../Reset without moving children";
        public const string FocusLabel = BaseLabel + "more.../Focus";
        public const string CopyHierarchyPathLabel = BaseLabel + "Copy/path";

        [MenuItem(CopyHierarchyPathLabel)]
        public static void CopyHierarchyPath(MenuCommand command)
        {
            var transform = (Transform)command.context;
            var path = transform.GetHierarchyPath();
            EditorGUIUtility.systemCopyBuffer = path;
            Debug.Log("Copied Path: " + path);
        }

        [MenuItem(ResetWithoutMovingChildrenLabel)]
        public static void ResetWithoutMovingChildren(MenuCommand command)
        {
            var transform = (Transform)command.context;
            Undo.RecordObject(transform, "Reset without moving children");
            var children = transform.GetChildren();
            foreach (Transform child in children)
                child.SetParent(null,true);
            transform.Reset();
            foreach (Transform child in children)
                child.SetParent(transform, true);
        }
        [MenuItem(FocusLabel)]
        public static void Focus(MenuCommand command)
        {
            var transform = (Transform)command.context;
            Undo.RecordObject(transform, "Align Selected to SceneView");
            transform.position = SceneView.lastActiveSceneView.camera.transform.TransformPoint(0,0,10);
        }
    }
}

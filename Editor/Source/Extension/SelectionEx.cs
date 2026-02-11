using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

namespace Yu5h1Lib.EditorExtension
{
    public static class SelectionEx
    {
        public static string SelectedAssetPath => Selection.activeObject != null ?
                                                  AssetDatabase.GetAssetPath(Selection.activeObject) : "";

        public static bool IsSelectedMonoScriptOfType<T>()
            => TryGetSelectedMonoScript(out var script) && script.GetClass().IsDerivedFrom<T>();

        public static bool TryGetSelectedMonoScript(out MonoScript script)
        {
            script = null;
            if (Selection.activeObject is MonoScript s)
            {
                script = s;
                return true;
            }
            return false;
        }



        public static System.Type GetSelectedScriptClass
        {
            get
            {
                if (TryGetSelectedMonoScript(out MonoScript ms))
                    return ms.GetClass();
                return null;
            }
        }
        public static bool IsAnyObjectSelected()
        {
            if (Selection.activeGameObject == null)
            {
                EditorUtility.DisplayDialog("Select an object", "No object selected", "OK");
                return false;
            }
            return true;
        }
        public static bool IsMonoScriptSubclassOf(System.Type type)
        {
            if (!TryGetSelectedMonoScript(out var nonoScript))
                return false;
            return nonoScript.IsSubclassOf(type);
        }
        public static bool IsMonoScriptSubclassOf<T>() => IsMonoScriptSubclassOf(typeof(T));

        private static float _lastMenuCallTimestamp = 0f;
        [MenuItem("GameObject/Group", false, 0)]
        public static void CreateGroup()
        {

            if (Time.unscaledTime.Equals(_lastMenuCallTimestamp)) return;
            var gobjs = Selection.gameObjects;
            if (gobjs.Length == 0)
            {
                Debug.LogWarning("Please Select at least a gameObject");
                return;
            }
            var previousParent = gobjs[0].transform.parent;
            Undo.SetCurrentGroupName("Create new Group");
            int group = Undo.GetCurrentGroup();
            var newGroup = new GameObject("new Group");

            newGroup.transform.SetParent(previousParent);
            Undo.RegisterCreatedObjectUndo(newGroup, "new Group");
            foreach (var item in gobjs)
            {
                Undo.SetTransformParent(item.transform, newGroup.transform, "set parent");
            }
            Undo.CollapseUndoOperations(group);

            EditorGUIUtility.PingObject(newGroup);

            _lastMenuCallTimestamp = Time.unscaledTime;
        }

        [MenuItem("GameObject/Copy Hierarchy Path", false, 0)]
        public static void CopyGameObjectHierarchyPath(MenuCommand command)
        {
            var go = Selection.activeGameObject;
            if (go == null)
                return;
            string path = go.transform.GetHierarchyPath();
            EditorGUIUtility.systemCopyBuffer = path;
            Debug.Log("Copied Path: " + path);
        }
    }
}
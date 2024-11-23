using UnityEngine;
using UnityEditor;

namespace Yu5h1Lib.EditorExtension
{
    public static class SelectionEx
    {
        public static string SelectedAssetPath => Selection.activeObject != null ?
                                                  AssetDatabase.GetAssetPath(Selection.activeObject) : "";

        public static bool IsMonoBehaviourSelected { get { return IsMonoScriptSubclassOf(typeof(MonoBehaviour)); } }
        public static bool IsMonoScriptSelected
        {
            get
            {
                return Selection.activeObject != null &&
                        Selection.activeObject.GetType() == typeof(MonoScript);
            }
        }

        public static bool IsScriptableObjectScriptSelected { get { return IsMonoScriptSubclassOf(typeof(ScriptableObject)); } }

        public static System.Type GetSelectedScriptClass
        {
            get
            {
                if (IsMonoScriptSelected) return ((MonoScript)Selection.activeObject).GetClass();
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
            if (IsMonoScriptSelected)
            {
                var classtype = ((MonoScript)Selection.activeObject).GetClass();
                if (classtype != null)
                {
                    return ((MonoScript)Selection.activeObject).GetClass().IsSubclassOf(type);
                }
            }
            return false;
        }

        private static float _lastMenuCallTimestamp = 0f;
        [MenuItem("GameObject/Group", false, 0)]
        public static void CreateGameObject()
        {

            if (Time.unscaledTime.Equals(_lastMenuCallTimestamp)) return;
            var gobjs = Selection.gameObjects;
            if (gobjs.Length > 0)
            {
                Transform previouseParent = gobjs[0].transform.parent;
                Undo.SetCurrentGroupName("Create new Group");
                int group = Undo.GetCurrentGroup();
                var newGroup = new GameObject("new Group");
                newGroup.transform.SetParent(previouseParent);
                Undo.RegisterCreatedObjectUndo(newGroup, "new Group");
                foreach (var item in gobjs)
                {
                    Undo.SetTransformParent(item.transform, newGroup.transform, "set parent");
                }
                Undo.CollapseUndoOperations(group);
                EditorGUIUtility.PingObject(newGroup);
            }
            else
            {
                Debug.LogWarning("Please Select atleast a gameObject");
            }
            _lastMenuCallTimestamp = Time.unscaledTime;
        }
    }
}
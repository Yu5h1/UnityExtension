using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace Yu5h1Lib.EditorExtension
{
    public class EditorAdvanced : Editor
    {
        public static string UseAdvancedEventsKey = typeof(EditorAdvanced).FullName + "_UseAdvancedEvents";
        public static bool UseAdvancedEvents 
        {
            get => EditorPrefs.GetBool(UseAdvancedEventsKey, false);
            set {
                if (UseAdvancedEvents == value)
                    return;
                EditorPrefs.SetBool(UseAdvancedEventsKey, value);
                RegisterEvents();
            }
        }
        public static List<EditorAdvanced> editors = new List<EditorAdvanced>();

        public static void RegisterAdvancedMethods(EditorAdvanced editor)
        {
            if (!editors.Contains(editor))
                editors.Add(editor);
        }

        public static void UnregisterAdvancedMethods(EditorAdvanced editor)
        {
            editors.Remove(editor);
        }
        protected virtual void HierarchyChanged() { }
        protected virtual void EnteredEditMode() { }
        protected virtual void ExitingEditMode() { }
        protected virtual void EnteredPlayMode() { }
        protected virtual void ExitingPlayMode() { }

        [InitializeOnLoadMethod]
        private static void RegisterEvents()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChange;
            EditorApplication.hierarchyChanged -= EditorApplication_HierarchyChanged;
            if (!UseAdvancedEvents)
                return;
            EditorApplication.playModeStateChanged += OnPlayModeStateChange;
            EditorApplication.hierarchyChanged += EditorApplication_HierarchyChanged;
        }

        private static void EditorApplication_HierarchyChanged()
        {
            foreach (var editor in editors)
                editor.HierarchyChanged();
        }

        private static void OnPlayModeStateChange(PlayModeStateChange state)
        {
            foreach (EditorAdvanced editor in editors)
            {
                switch (state)
                {
                    case PlayModeStateChange.EnteredEditMode:
                        editor.EnteredEditMode();
                        break;
                    case PlayModeStateChange.ExitingEditMode:
                        editor.ExitingEditMode();
                        break;
                    case PlayModeStateChange.EnteredPlayMode:
                        editor.EnteredPlayMode();
                        break;
                    case PlayModeStateChange.ExitingPlayMode:
                        editor.ExitingPlayMode();
                        break;
                    default:
                        break;
                }
            }

        }

    }
    public class Editor<TargetType> : EditorAdvanced where TargetType : Object
    {
        public TargetType targetObject => (TargetType)target;
        public IEnumerable<TargetType> targetObjects => targets.Cast<TargetType>();

        #region custom property context menu
        //[InitializeOnLoadMethod]
        //public static void InitilizeCustomContextualPropertyMenuCallBack()
        //{
        //EditorApplication.contextualPropertyMenu -= OnPropertyContextMenu;
        //EditorApplication.contextualPropertyMenu += OnPropertyContextMenu;
        //}
        //static bool ignoreContextMenuCall = false;
        //static void OnPropertyContextMenu(GenericMenu menu, SerializedProperty property)
        //{

        //if (FindAllInspectors().TryGet(e => e.GetType().IsDerivedFrom<Editor<>>()).Find(e => e.serializedObject == property.serializedObject))
        //{ 

        //}

        //if (ignoreContextMenuCall)
        //{
        //    menu.cl
        //    ignoreContextMenuCall = false;
        //    return;
        //}
        //property.ResetValue();
        //GUI.FocusControl(null);
        //if (Event.current.mousePosition.x > EditorGUIUtility.labelWidth)
        //    ignoreContextMenuCall = true;
        //}

        #endregion
    }
}

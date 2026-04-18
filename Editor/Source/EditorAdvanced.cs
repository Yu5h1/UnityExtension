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

        private Dictionary<string, ReorderableListEnhanced> _reorderableLists = new Dictionary<string, ReorderableListEnhanced>();
        protected Dictionary<string, ReorderableListEnhanced> reorderableLists => _reorderableLists;

        public override void OnInspectorGUI()
        {
            this.Iterate(DrawProperty, DrawMonoScript);
        }

        public virtual void DrawMonoScript()
        {
            var iterator = serializedObject.GetIterator();
            iterator.NextVisible(true);
            using (new EditorGUI.DisabledGroupScope(true))
            {
                EditorGUILayout.PropertyField(iterator);
            }
        }
        public virtual void DrawMonoScript(SerializedProperty property)
        {
            using (new EditorGUI.DisabledGroupScope(true))
            {
                EditorGUILayout.PropertyField(property);
            }
        }
        public virtual void DrawProperty(SerializedProperty property)
        {
            if (TryPrepareList(property,out var list))
                list.DoLayoutList();
            else
                EditorGUILayout.PropertyField(property, true);
        }
        public bool TryPrepareList(SerializedProperty property,out ReorderableListEnhanced list)
        {
            list = null;
            if (!property.isArray || property.propertyType == SerializedPropertyType.String)
                return false;
            if (reorderableLists.TryGetValue(property.propertyPath, out list))
                return true;
            var prop = serializedObject.FindProperty(property.propertyPath);
            list = new ReorderableListEnhanced(serializedObject, prop, true, false);
            reorderableLists[property.propertyPath] = list;
            return true;
        }
        protected virtual void OnDisable()
        {
            _reorderableLists.Clear();
        }

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
        public Object[] FindObjectsByType(System.Type type)
            => FindObjectsByType(type, FindObjectsInactive.Include, FindObjectsSortMode.None);

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

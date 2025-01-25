using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;


namespace Yu5h1Lib.EditorExtension
{
    using EditorBrowsableAttribute = System.ComponentModel.EditorBrowsableAttribute;

    public class Editor<T> : Editor where T : Object
    {
        public static System.Type InspectorType => typeof(Editor).Assembly.GetType("UnityEditor.InspectorWindow");
        public static IEnumerable<Editor> FindAllInspectors() => Resources.FindObjectsOfTypeAll(InspectorType).Cast<Editor>();

        public T targetObject => (T)target;
        public IEnumerable<T> targetObjects => targets.Cast<T>();


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
    [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never), System.ComponentModel.Browsable(false)]
    public static class EditorEx
    {
        public static bool Iterate(this Editor editor, out SerializedProperty changedProperty)
        {
            changedProperty = null;
            bool changed = false;
            var properties = editor.serializedObject.GetIterator();
            #region m_Script
            properties.NextVisible(true);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(properties);
            EditorGUI.EndDisabledGroup(); 
            #endregion
            while (properties.NextVisible(false))
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(properties, true);
                if (EditorGUI.EndChangeCheck())
                {
                    changed = true;
                    changedProperty = properties.Copy();
                }
            }
            if (changed)
                editor.serializedObject.ApplyModifiedProperties();
            return changed;
        }

        public static void Iterate(this Editor editor,System.Action<SerializedProperty> process)
        {
            EditorGUI.BeginChangeCheck();
            var properties = editor.serializedObject.GetIterator();
            #region m_Script
            properties.NextVisible(true);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(properties);
            EditorGUI.EndDisabledGroup();
            #endregion
            while (properties.NextVisible(false))
                process(properties);

            if (EditorGUI.EndChangeCheck())
                editor.serializedObject.ApplyModifiedProperties();
        }

    }
}

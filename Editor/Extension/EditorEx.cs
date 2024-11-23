using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
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
    //[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never), System.ComponentModel.Browsable(false)]
    //public static class EditorEx
    //{


    //}
}

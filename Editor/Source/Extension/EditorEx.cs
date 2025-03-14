using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Yu5h1Lib;


namespace Yu5h1Lib.EditorExtension
{
    using EditorBrowsableAttribute = System.ComponentModel.EditorBrowsableAttribute;
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

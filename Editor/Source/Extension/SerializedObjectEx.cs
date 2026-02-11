using System.Collections.Generic;
using System.ComponentModel;
using UnityEditor;

namespace Yu5h1Lib.EditorExtension
{
    using Predicate = System.Func<SerializedProperty, bool>;
    [EditorBrowsable(EditorBrowsableState.Never), Browsable(false)]
    public static class SerializedObjectEx
    {
        public static void PropertiesAction (this SerializedObject serializedObject, bool enterChildren ,System.Action<SerializedProperty> action)
        {
            var prop = serializedObject.GetIterator();
            prop.Next(true);
            prop.Iterate(enterChildren, p => action(p));
        }
        public static List<string> GetPropertiesName(this SerializedObject serializedObject, bool enterChildren) {
            List<string> result = new List<string>();
            serializedObject.GetIterator().Iterate(enterChildren, prop => result.Add(prop.name));
            return result;
        }
        public static void Revert(this SerializedObject serializedObject,string PropertyName)
        {
            serializedObject.FindProperty(PropertyName).prefabOverride = false;
            serializedObject.ApplyModifiedProperties();
        }
        public static bool TryFindProperty(this SerializedObject serializedObject, string PropertyName,out SerializedProperty prop)
        {
            prop = serializedObject.FindProperty(PropertyName);
            return prop != null;
        }
        public static List<SerializedProperty> FindProperties( this SerializedObject serializedObject,
            Predicate predicate, bool enterChildren = true)
        {
            var results = new List<SerializedProperty>();
            var iterator = serializedObject.GetIterator();

            if (iterator.NextVisible(true))
            {
                do
                {
                    if (predicate(iterator))
                        results.Add(iterator.Copy());
                }
                while (iterator.NextVisible(enterChildren));
            }

            return results;
        }
        public static SerializedProperty FindFirst( this SerializedObject serializedObject, 
            Predicate predicate,bool enterChildren = true)
        {
            var iterator = serializedObject.GetIterator();

            if (iterator.NextVisible(true))
            {
                do
                {
                    if (predicate(iterator))
                        return iterator.Copy();
                }
                while (iterator.NextVisible(enterChildren));
            }

            return null;
        }
        #region EditorGUI
        public static bool TryDrawScriptField(this SerializedObject so)
        {
            var prop = so.FindProperty("m_Script");
            if (prop == null)
                return false;
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(prop);
            }
            return true;
        }
        #endregion
    }
}

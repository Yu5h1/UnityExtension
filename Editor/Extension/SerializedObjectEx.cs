using System.Collections.Generic;
using UnityEditor;

namespace Yu5h1Lib.EditorExtension
{
    public static class SerializedObjectEx
    {
        public static void PropertiesAction (this SerializedObject serializedObject, bool enterChildren ,System.Action<SerializedProperty> action)
        {
            var prop = serializedObject.GetIterator();
            prop.Next(true);
            prop.PropertiesAction(enterChildren, p => action(p));
        }
        public static List<string> GetPropertiesName(this SerializedObject serializedObject, bool enterChildren) {
            List<string> result = new List<string>();
            serializedObject.GetIterator().PropertiesAction(enterChildren, prop => result.Add(prop.name));
            return result;
        }
        public static void Revert(this SerializedObject serializedObject,string PropertyName)
        {
            serializedObject.FindProperty(PropertyName).prefabOverride = false;
            serializedObject.ApplyModifiedProperties();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    public static class ContextualPropertyMenuAdvanced
    {
        [InitializeOnLoadMethod]
        public static void Register()
        {
            EditorApplication.contextualPropertyMenu -= OnPropertyContextMenu;
            EditorApplication.contextualPropertyMenu += OnPropertyContextMenu;
        }

        private static void OnPropertyContextMenu(GenericMenu menu, SerializedProperty property)
        {
            HandleContextMenuAdvanced(menu, property);
        }

        private static void HandleContextMenuAdvanced(GenericMenu menu, SerializedProperty property)
        {
            var attribute = GetAttribute<ContextMenuAdvanced>(property);
            if (attribute == null) return;

            var targetObject = property.serializedObject.targetObject;
            
            menu.AddItem(new GUIContent(attribute.menuName), false, () =>
            {
                MethodInfo method = targetObject.GetType().GetMethod(attribute.methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method != null)
                {
                    Undo.RecordObject(targetObject, attribute.menuName);
                    method.Invoke(targetObject, null);
                    EditorUtility.SetDirty(targetObject);
                    property.serializedObject.ApplyModifiedProperties();
                }
                else
                    $"The method '{attribute.methodName}' does not exist in {targetObject.GetType()}".printError();
            });
        }

        private static T GetAttribute<T>(SerializedProperty property) where T : PropertyAttribute
        {
            if (property == null) return null;
            var field = property.serializedObject.targetObject.GetType().GetField(property.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return field?.GetCustomAttribute<T>();
        }
    }
}

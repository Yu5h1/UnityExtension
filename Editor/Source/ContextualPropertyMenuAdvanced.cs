using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

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
            HandleSerializeReferenceMenu(menu, property);
            HandleObjectReferenceMenu(menu, property);
            HandleContextMenuAdvanced(menu, property);
        }
        private static void HandleSerializeReferenceMenu(GenericMenu menu, SerializedProperty property)
        { 

        }
        private static void HandleObjectReferenceMenu(GenericMenu menu, SerializedProperty property)
        {
            if (property.isArray)
            {
                menu.AddItem(new GUIContent("Clear"), false, () =>
                {
                    property.arraySize = 0;
                    property.serializedObject.ApplyModifiedProperties();
                });
            }
            else
            {
                switch (property.propertyType)
                {
                    case SerializedPropertyType.ObjectReference:
                        if (!AssetDatabase.Contains(property.serializedObject.targetObject))
                        {
                            menu.AddItem(new GUIContent($"Link to This"), false, () =>
                            {
                                property.objectReferenceValue = property.serializedObject.targetObject;
                                property.serializedObject.ApplyModifiedProperties();
                            });
                        }
                        menu.AddItem(new GUIContent("Clear"), false, () =>
                        {
                            property.objectReferenceValue = null;
                            property.serializedObject.ApplyModifiedProperties();
                        });
                        break;
                }
            }
        }
        private static void HandleContextMenuAdvanced(GenericMenu menu, SerializedProperty property)
        {
            if (!TryGetAttribute(property,out ContextMenuAdvanced attribute))
                return;
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

        private static bool TryGetAttribute<T>(SerializedProperty property, out T attribute) where T : PropertyAttribute
        {
            attribute = null;
            if (property == null) return false;
            var field = property.serializedObject.targetObject.GetType().GetField(property.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
                return false; ;
            return (attribute = field.GetCustomAttribute<T>()) != null;
        }
    }
}

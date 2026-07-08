using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    [InitializeOnLoad]
    public static class ObjectPropertyMenu
	{
        static ObjectPropertyMenu()
        {
            EditorApplication.contextualPropertyMenu -= OnPropertyContextMenu;
            EditorApplication.contextualPropertyMenu += OnPropertyContextMenu;
        }
        public static void OnPropertyContextMenu(GenericMenu menu, SerializedProperty property)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
                return;
            if (property.objectReferenceValue == null)
                return;

            menu.AddItem(new GUIContent("Copy.../Name"), false, () =>
            {
                EditorGUIUtility.systemCopyBuffer = property.objectReferenceValue.name;
            });
        } 
    }
}
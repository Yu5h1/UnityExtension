using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Yu5h1Lib.EditorExtension;

namespace Yu5h1Lib
{
    public static class SerializedReferenceMenu 
    {
        [InitializeOnLoadMethod]
        public static void Register()
        {
            EditorApplication.contextualPropertyMenu -= OnPropertyContextMenu;
            EditorApplication.contextualPropertyMenu += OnPropertyContextMenu;
        }
        private static void OnPropertyContextMenu(GenericMenu menu, SerializedProperty property)
        {

            var arrayProperty = property;

            if (!(arrayProperty.isArray && arrayProperty.IsDefined<SerializeReference>()))
                return;
            var baseType = arrayProperty.GetElementType();
            var types = baseType.GetDerivedTypes();
            if (!baseType.IsAbstract)
                menu.AddTypeItem(arrayProperty, baseType);

            foreach (var derivedType in types)
                    menu.AddTypeItem(arrayProperty, derivedType);

        }
        private static void AddTypeItem(this GenericMenu menu,SerializedProperty arrayProperty, Type type)
        {
            menu.AddItem(new GUIContent($"Add/{type.Name}"), false, () =>
            {
                arrayProperty.arraySize++;
                SerializedProperty newElement = arrayProperty.GetArrayElementAtIndex(arrayProperty.arraySize - 1);

                newElement.managedReferenceValue = System.Activator.CreateInstance(type);
                arrayProperty.serializedObject.ApplyModifiedProperties();
            });
        }
    }
}
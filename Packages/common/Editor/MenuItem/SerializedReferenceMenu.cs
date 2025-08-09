using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

            if (!arrayProperty.isArray)
                return;
            if (!arrayProperty.IsDefined<SerializeReference>())
                return;
            if ("ElementType not found ! ".printWarningIf(!arrayProperty.TryGetElementType(out Type baseType)))
                return;

            var types = baseType.GetDerivedTypes().Where(t=>!t.IsAbstract);
            if (!baseType.IsAbstract)
                menu.AddTypeItem(arrayProperty, baseType);

            foreach (var derivedType in types)
                    menu.AddTypeItem(arrayProperty, derivedType);

        }
    }
}
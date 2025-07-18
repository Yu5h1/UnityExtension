using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.CommonEditor
{
    public static class PropertyMenu
    {
        [InitializeOnLoadMethod]
        public static void Register()
        {
            EditorApplication.contextualPropertyMenu -= OnPropertyContextMenu;
            EditorApplication.contextualPropertyMenu += OnPropertyContextMenu;
        }
        private static void OnPropertyContextMenu(GenericMenu menu, SerializedProperty property)
        {
        
        }
    }
}
using System;
using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    public static class GenericMenuEx
    {
        public static void AddItems(this GenericMenu menu,object selectedItem,Type enumType,Action<object> clickItem)
        {
            var items = Enum.GetValues(enumType);
            foreach (var item in items)
                menu.AddItem(new GUIContent(item.ToString()), item.ToString() == selectedItem.ToString(), () => clickItem(item));
        }

        public static void AddTypeItem(this GenericMenu menu, SerializedProperty prop, Type type,string baseItemName = "Add/",Action completed = null)
        {
            menu.AddItem(new GUIContent($"{baseItemName}{type.Name}"), false, () =>
            {
                prop.CreateInlineInstance(type);
                completed?.Invoke();
            });
        } 
    }
}

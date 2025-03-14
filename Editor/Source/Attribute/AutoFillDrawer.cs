using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    [CustomPropertyDrawer(typeof(AutoFillAttribute))]
    public class AutoFillDrawer : PropertyDrawer
    {
        private string[] items;
        private List<string> filtedItems = new List<string>();
        private string[] filteredItemsArray;

        public void Filter(string content){
            if (content.IsEmpty())
            {
                filteredItemsArray = items;
                return;
            }
            filtedItems.Clear();
            foreach(string item in items)
                if (item.Compare(content,StringComparisonStyle.StartsWith))
                    filtedItems.Add(item);
            filteredItemsArray = filtedItems.ToArray();
        }
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            AutoFillAttribute autoFill = (AutoFillAttribute)attribute;

            if (items.IsEmpty())
                items = ((AutoFillAttribute.Items)System.Activator.CreateInstance(autoFill.itemsType)).Get(autoFill.Args);

            position.height = EditorGUI.GetPropertyHeight(property, label, true);
            property.stringValue = EditorGUI.TextField(position, label, property.stringValue);
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.DownArrow)
            {
                Filter(property.stringValue);
                if (filteredItemsArray.Length > 0)
                {
                    var r = position;
                    r.x += EditorGUIUtility.labelWidth;
                    r.y += position.height;
                    Event.current.mousePosition = r.position;
                    var menu = new GenericMenu();
                    foreach (var item in filteredItemsArray)
                    {
                        menu.AddItem(new GUIContent(item), false, () => {
                            property.stringValue = item;
                            property.serializedObject.ApplyModifiedProperties();
                            EditorUtility.SetDirty(property.serializedObject.targetObject);
                            GUI.FocusControl(null);
                            EditorWindow.focusedWindow?.Repaint();
                        });
                    }
                    menu.ShowAsContext();
                    Event.current.Use();
                }
            }
        }
    } 
}

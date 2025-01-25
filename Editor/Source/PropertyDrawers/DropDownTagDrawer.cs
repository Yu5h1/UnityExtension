using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(DropDownTagAttribute))]
public class DropDownTagDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUILayout.HelpBox("DropDownTag is only available for string type.", MessageType.Warning);
            return;
        }
        var ddtAttribute = (DropDownTagAttribute)attribute;

        EditorGUI.BeginProperty(position, label, property);
        
        if (!ddtAttribute.AllowMultiple)
            property.stringValue = EditorGUI.TagField(position, label, property.stringValue);
        else
        {
            var selectedTags = property.stringValue.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
            var selectedTagList = new List<string>(selectedTags);
            string[] allTags = UnityEditorInternal.InternalEditorUtility.tags;

            Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
            Rect buttonRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth, position.height);
            EditorGUI.LabelField(labelRect, label);


            if (EditorGUI.DropdownButton(buttonRect, new GUIContent(string.Join(", ", selectedTagList)), FocusType.Keyboard))
            {
                GenericMenu menu = new GenericMenu();

                foreach (string tag in allTags)
                {
                    bool isSelected = selectedTagList.Contains(tag);
                    menu.AddItem(new GUIContent(tag), isSelected, () =>
                    {
                        if (isSelected)
                            selectedTagList.Remove(tag);
                        else
                            selectedTagList.Add(tag);

                        property.stringValue = string.Join(",", selectedTagList);
                        property.serializedObject.ApplyModifiedProperties();

                    });
                }
                menu.ShowAsContext();
            }
        }

        EditorGUI.EndProperty();
    }
}

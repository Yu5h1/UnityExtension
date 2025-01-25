using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Yu5h1Lib;

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
        
        if (ddtAttribute.AllowMultiple)
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
                        {
                            if (tag == allTags[0])
                            {
                                selectedTagList.Clear();
                                selectedTagList.AddRange(allTags.Skip(1));
                            }else
                            {
                                selectedTagList.Remove(tag);
                                if (selectedTagList.IsEmpty())
                                    selectedTagList.Add(allTags[0]);
                            }
                        }
                        else
                        {
                            if (tag == allTags[0])
                            {
                                selectedTagList.Clear();
                                selectedTagList.Add(allTags[0]);
                            }
                            else
                            {
                                selectedTagList.Add(tag);
                                selectedTagList.Remove(allTags[0]);
                            }
                        }
                            

                        property.stringValue = string.Join(",", selectedTagList);
                        property.serializedObject.ApplyModifiedProperties();

                    });
                }
                menu.ShowAsContext();
            }
        }
        else
            property.stringValue = EditorGUI.TagField(position, label, property.stringValue);


        EditorGUI.EndProperty();
    }
}

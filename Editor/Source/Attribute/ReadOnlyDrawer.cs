using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Yu5h1Lib;

[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        //Color originalColor = GUI.contentColor;
        //GUI.contentColor = new Color(originalColor.r, originalColor.g, originalColor.b, 0.5f); // ¥b³z©ú
        //EditorGUI.HelpBox(position, property.name, MessageType.Warning);
        //var enableGUI = GUI.enabled;
        //GUI.enabled = false;
        ////EditorGUI.PropertyField(position, property, label);
        //GUI.enabled = enableGUI;

        //GUI.contentColor = originalColor;


        EditorGUI.BeginDisabledGroup(true);
        EditorGUI.PropertyField(position, property, label, property.hasVisibleChildren);
        EditorGUI.EndDisabledGroup();
    }
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }
}

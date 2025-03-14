using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Yu5h1Lib;

[CustomPropertyDrawer(typeof(BuildSceneAttribute))]
public class BuildSceneDrawer : PropertyDrawer
{
    public static int[] sceneIndexes { get; private set; }
    public static string[] SceneNames { get; private set; }

    static BuildSceneDrawer()
    {
        UpdateSceneList();
        EditorBuildSettings.sceneListChanged += UpdateSceneList;
    }

    private static void UpdateSceneList()
    {
        List<int> indices = new List<int> { -1 }; 
        List<string> names = new List<string> { "None" }; 
        var scenes = EditorBuildSettings.scenes;
        for (int i = 0; i < scenes.Length; i++)
        {
            if (scenes[i].enabled) 
            {
                indices.Add(i); 
                names.Add(System.IO.Path.GetFileNameWithoutExtension(scenes[i].path)); // 取得場景名稱
            }
        }
        sceneIndexes = indices.ToArray();
        SceneNames = names.ToArray();
    }
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.Integer && property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.LabelField(position, label.text, "Use BuildScene with int or string only.");
            return;
        }
        if (sceneIndexes == null || SceneNames == null)
            UpdateSceneList();

        float labelWidth = EditorGUIUtility.labelWidth;
        Rect labelRect = new Rect(position.x, position.y, labelWidth, position.height);
        EditorGUI.PropertyField(labelRect, property, label, true); 
        Rect popupRect = new Rect(position.x + labelWidth, position.y, position.width - labelWidth, position.height);

        if (property.propertyType == SerializedPropertyType.Integer)
        {
            int currentIndex = System.Array.IndexOf(sceneIndexes, property.intValue);
            if (currentIndex == -1) currentIndex = 0; 

            int newIndex = EditorGUI.Popup(popupRect,currentIndex, SceneNames);

            if (sceneIndexes.IsValid(newIndex))
                property.intValue = sceneIndexes[newIndex];
        }
        else if (property.propertyType == SerializedPropertyType.String)
        {
            int currentNameIndex = System.Array.IndexOf(SceneNames, property.stringValue);
            if (currentNameIndex == -1) currentNameIndex = 0; // 預防找不到的情況

            int newNameIndex = EditorGUI.Popup(popupRect, currentNameIndex, SceneNames);

            if (SceneNames.IsValid(newNameIndex))
                property.stringValue = SceneNames[newNameIndex];
        }
    }
}

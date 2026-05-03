using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Yu5h1Lib.EditorExtension;
using Yu5h1Lib;

[CustomEditor(typeof(LineRendererController2D)), CanEditMultipleObjects]
public class LineRendererControllerEditor : Editor<LineRendererController2D> {

    private string IsConnectingName => nameof(targetObject.IsConnecting);
    protected void OnEnable()
    {
        if (EditorApplication.isPlaying)
            return;
    }
    public override void OnInspectorGUI()
    {

        //DrawDefaultInspector();
        this.Iterate(DrawProperty,null);
    }
    private void BeginDrawProperty()
    {

    }
    public override void DrawProperty(SerializedProperty property)
    {

        if (property.name == $"_{IsConnectingName}")
        {
            targetObject.IsConnecting = EditorGUILayout.Toggle(IsConnectingName, targetObject.IsConnecting);
        }
        else
            base.DrawProperty(property);
    }
    private void OnSceneGUI()
    {
        if (EditorApplication.isPlaying)
            return;
        targetObject.Refresh();
    }
}
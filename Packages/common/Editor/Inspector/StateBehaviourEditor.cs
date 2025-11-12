using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Yu5h1Lib.EditorExtension;

[CustomEditor(typeof(StateBehaviour))]
public class StateBehaviourEditor : Editor<StateBehaviourEditor>
{
    public override void OnInspectorGUI()
    {
        target.name = EditorGUILayout.TextField("State Name", target.name);
        DrawDefaultInspector();
    }
}
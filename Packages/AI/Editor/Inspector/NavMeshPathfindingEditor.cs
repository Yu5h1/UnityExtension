using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Yu5h1Lib.EditorExtension;
using UnityEngine.AI;

[CustomEditor(typeof(NavMeshPathfinding))]
public class NavMeshPathfindingEditor : Editor<NavMeshPathfinding> 
{
    NavMeshPath path => targetObject.NavPath;

   public override void OnInspectorGUI()
   {
      DrawDefaultInspector();
   }
    private void OnSceneGUI()
    {
        if (!EnableDebugMode)
            return;
        for (int i = 0; i < path.corners.Length - 1; i++)
            Debug.DrawLine(path.corners[i], path.corners[i + 1], Color.green);
    }


    public static string EnableDebugModeKey => typeof(NavMeshPathfindingEditor).FullName +"_"+ nameof(EnableDebugMode);
    public static bool EnableDebugMode
    {
        get => EditorPrefs.GetBool(EnableDebugModeKey, false);
        set
        {
            if (EnableDebugMode == value)
                return;
            EditorPrefs.SetBool(EnableDebugModeKey, value);
        }
    }

    public const string ToggleDebugModeLabel = "CONTEXT/" + nameof(NavMeshPathfinding) +"/DebugMode";
    [MenuItem(ToggleDebugModeLabel)]
    private static void ToggleDebugMode(MenuCommand command)
    {
        EnableDebugMode = !EnableDebugMode;
        Menu.SetChecked(ToggleDebugModeLabel, EnableDebugMode);
    }
}
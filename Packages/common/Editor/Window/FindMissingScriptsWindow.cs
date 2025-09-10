using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class FindMissingScriptsWindow : EditorWindow
{
    private List<GameObject> _results = new();
    private Vector2 _scroll;

    [MenuItem("Tools/Find Missing Scripts (Scene)")]
    public static void Open()
    {
        GetWindow<FindMissingScriptsWindow>("Missing Scripts");
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Scan Scene for Missing Scripts"))
        {
            ScanScene();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Found {_results.Count} object(s) with missing script(s)", EditorStyles.boldLabel);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        foreach (var go in _results)
        {
            if (go == null) continue;

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.ObjectField(go, typeof(GameObject), true);

            //if (GUILayout.Button("Ping", GUILayout.Width(50)))
            //{
            //    EditorGUIUtility.PingObject(go);
            //    Selection.activeObject = go;
            //}

            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }

    private void ScanScene()
    {
        _results.Clear();
        
        var allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        foreach (var go in allObjects)
        {
            var components = go.GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp == null)
                {
                    _results.Add(go);
                    break;
                }
            }
        }

        Debug.Log($"[FindMissingScriptsWindow] Found {_results.Count} GameObject(s) with missing script(s).");
    }
}

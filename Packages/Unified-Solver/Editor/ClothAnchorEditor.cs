using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ClothAnchor))]
public sealed class ClothAnchorEditor : Editor
{
    static readonly Color GridColor = new Color(0.35f, 0.7f, 1f, 0.22f);
    static readonly Color AvailableColor = new Color(0.45f, 0.75f, 1f, 0.85f);
    static readonly Color AnchoredColor = new Color(0.25f, 1f, 0.4f, 1f);

    SerializedProperty _anchorsProperty;
    bool _editMode;

    void OnEnable()
    {
        _anchorsProperty = serializedObject.FindProperty("anchors");
    }

    void OnDisable()
    {
        _editMode = false;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var clothAnchor = (ClothAnchor)target;
        var cloth = clothAnchor.GetComponent<ClothGenerator>();

        EditorGUILayout.LabelField("Cloth", cloth != null ? cloth.name : "Missing", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Anchored Vertices", _anchorsProperty.arraySize.ToString());

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(Application.isPlaying || cloth == null))
        {
            bool nextEditMode = GUILayout.Toggle(
                _editMode,
                _editMode ? "Editing Anchors" : "Edit Anchors",
                "Button",
                GUILayout.Height(28f));

            if (nextEditMode != _editMode)
            {
                _editMode = nextEditMode;
                SceneView.RepaintAll();
            }
        }

        if (Application.isPlaying)
            EditorGUILayout.HelpBox("Anchor editing is disabled in Play Mode.", MessageType.Info);
        else if (_editMode)
            EditorGUILayout.HelpBox(
                "Click cloth vertices in the Scene View to toggle anchors. Press Escape to leave edit mode.",
                MessageType.Info);
        else
            EditorGUILayout.HelpBox(
                "Enable Edit Anchors before vertices can be changed.",
                MessageType.None);

        EditorGUILayout.Space();
        DrawAnchorList(cloth);

        serializedObject.ApplyModifiedProperties();
    }

    void DrawAnchorList(ClothGenerator cloth)
    {
        if (_anchorsProperty.arraySize == 0)
        {
            EditorGUILayout.LabelField("No anchored vertices.", EditorStyles.centeredGreyMiniLabel);
            return;
        }

        EditorGUILayout.LabelField("Bindings", EditorStyles.boldLabel);

        int invalidCount = 0;
        for (int i = 0; i < _anchorsProperty.arraySize; i++)
        {
            SerializedProperty info =
                _anchorsProperty.GetArrayElementAtIndex(i);
            SerializedProperty transformProperty =
                info.FindPropertyRelative("transform");
            Vector2Int node =
                info.FindPropertyRelative("node").vector2IntValue;
            bool valid = IsAnchorValid(info, cloth);

            if (!valid)
                invalidCount++;

            Color previousColor = GUI.color;
            if (!valid)
                GUI.color = new Color(1f, 0.55f, 0.45f);

            bool removeRequested = false;
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(
                       !_editMode || Application.isPlaying))
            {
                EditorGUILayout.PropertyField(
                    transformProperty,
                    new GUIContent($"Vertex ({node.x}, {node.y})"));
            }

            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                if (GUILayout.Button(
                        new GUIContent(
                            "\u00d7",
                            valid
                                ? "Remove this anchor."
                                : "Remove this invalid anchor."),
                        GUILayout.Width(26f)))
                {
                    removeRequested = true;
                }
            }

            EditorGUILayout.EndHorizontal();
            GUI.color = previousColor;

            if (removeRequested)
            {
                RemoveAnchor(i, cloth);
                serializedObject.ApplyModifiedProperties();
                GUIUtility.ExitGUI();
            }
        }

        if (invalidCount > 0)
        {
            EditorGUILayout.HelpBox(
                $"{invalidCount} anchor(s) are outside the current cloth " +
                "resolution or have a missing Transform. They are ignored " +
                "at runtime and can be removed safely.",
                MessageType.Warning);

            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                if (GUILayout.Button(
                        $"Remove Invalid Anchors ({invalidCount})"))
                {
                    RemoveInvalidAnchors(cloth);
                    serializedObject.ApplyModifiedProperties();
                    GUIUtility.ExitGUI();
                }
            }
        }

        EditorGUILayout.Space(2f);
        using (new EditorGUI.DisabledScope(Application.isPlaying))
        {
            if (GUILayout.Button("Clear All Anchors"))
            {
                if (EditorUtility.DisplayDialog(
                        "Clear Cloth Anchors",
                        "Remove every ClothAnchor binding? Generated anchor " +
                        "GameObjects that are still unmodified will also be deleted.",
                        "Clear All",
                        "Cancel"))
                {
                    ClearAllAnchors(cloth);
                    serializedObject.ApplyModifiedProperties();
                    GUIUtility.ExitGUI();
                }
            }
        }
    }

    static bool IsAnchorValid(
        SerializedProperty info,
        ClothGenerator cloth)
    {
        if (info == null || cloth == null)
            return false;

        Transform anchorTransform =
            info.FindPropertyRelative("transform").objectReferenceValue
                as Transform;
        Vector2Int node =
            info.FindPropertyRelative("node").vector2IntValue;

        return anchorTransform != null &&
               node.x >= 0 &&
               node.y >= 0 &&
               node.x < cloth.resolutionX &&
               node.y < cloth.resolutionY;
    }

    void RemoveInvalidAnchors(ClothGenerator cloth)
    {
        for (int i = _anchorsProperty.arraySize - 1; i >= 0; i--)
        {
            SerializedProperty info =
                _anchorsProperty.GetArrayElementAtIndex(i);
            if (!IsAnchorValid(info, cloth))
                RemoveAnchor(i, cloth);
        }
    }

    void ClearAllAnchors(ClothGenerator cloth)
    {
        for (int i = _anchorsProperty.arraySize - 1; i >= 0; i--)
            RemoveAnchor(i, cloth);
    }

    void OnSceneGUI()
    {
        if (!_editMode || Application.isPlaying)
            return;

        var clothAnchor = (ClothAnchor)target;
        var cloth = clothAnchor.GetComponent<ClothGenerator>();
        if (cloth == null || cloth.resolutionX <= 0 || cloth.resolutionY <= 0)
            return;

        Event currentEvent = Event.current;
        if (currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.Escape)
        {
            _editMode = false;
            currentEvent.Use();
            Repaint();
            SceneView.RepaintAll();
            return;
        }

        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        serializedObject.Update();
        Dictionary<Vector2Int, int> anchoredNodes = BuildAnchorLookup();

        DrawGrid(clothAnchor, cloth);

        for (int y = 0; y < cloth.resolutionY; y++)
        {
            for (int x = 0; x < cloth.resolutionX; x++)
            {
                var node = new Vector2Int(x, y);
                Vector3 position = clothAnchor.GetNodeWorldPosition(x, y);
                bool anchored = anchoredNodes.TryGetValue(node, out int anchorIndex);

                Handles.color = anchored ? AnchoredColor : AvailableColor;
                float size = HandleUtility.GetHandleSize(position) * (anchored ? 0.075f : 0.055f);

                if (Handles.Button(
                    position,
                    Quaternion.identity,
                    size,
                    size * 1.35f,
                    Handles.DotHandleCap))
                {
                    if (anchored)
                        RemoveAnchor(anchorIndex, cloth);
                    else
                        AddAnchor(node, position, cloth);

                    serializedObject.ApplyModifiedProperties();
                    GUI.changed = true;
                    Repaint();
                    SceneView.RepaintAll();
                    return;
                }
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    Dictionary<Vector2Int, int> BuildAnchorLookup()
    {
        var result = new Dictionary<Vector2Int, int>();
        for (int i = 0; i < _anchorsProperty.arraySize; i++)
        {
            SerializedProperty info = _anchorsProperty.GetArrayElementAtIndex(i);
            Vector2Int node = info.FindPropertyRelative("node").vector2IntValue;
            if (!result.ContainsKey(node))
                result.Add(node, i);
        }
        return result;
    }

    void DrawGrid(ClothAnchor clothAnchor, ClothGenerator cloth)
    {
        Handles.color = GridColor;

        for (int y = 0; y < cloth.resolutionY; y++)
        {
            var points = new Vector3[cloth.resolutionX];
            for (int x = 0; x < cloth.resolutionX; x++)
                points[x] = clothAnchor.GetNodeWorldPosition(x, y);
            Handles.DrawAAPolyLine(1.5f, points);
        }

        for (int x = 0; x < cloth.resolutionX; x++)
        {
            var points = new Vector3[cloth.resolutionY];
            for (int y = 0; y < cloth.resolutionY; y++)
                points[y] = clothAnchor.GetNodeWorldPosition(x, y);
            Handles.DrawAAPolyLine(1.5f, points);
        }
    }

    void AddAnchor(Vector2Int node, Vector3 worldPosition, ClothGenerator cloth)
    {
        string objectName = GetGeneratedName(node);
        var anchorObject = new GameObject(objectName);
        Undo.RegisterCreatedObjectUndo(anchorObject, "Add Cloth Anchor");
        Undo.SetTransformParent(anchorObject.transform, cloth.transform, "Parent Cloth Anchor");
        Undo.RecordObject(anchorObject.transform, "Position Cloth Anchor");
        anchorObject.transform.SetPositionAndRotation(worldPosition, cloth.transform.rotation);

        int index = _anchorsProperty.arraySize;
        _anchorsProperty.arraySize++;

        SerializedProperty info = _anchorsProperty.GetArrayElementAtIndex(index);
        info.FindPropertyRelative("transform").objectReferenceValue = anchorObject.transform;
        info.FindPropertyRelative("node").vector2IntValue = node;
        info.FindPropertyRelative("generated").boolValue = true;
    }

    void RemoveAnchor(int index, ClothGenerator cloth)
    {
        SerializedProperty info = _anchorsProperty.GetArrayElementAtIndex(index);
        Transform anchorTransform =
            info.FindPropertyRelative("transform").objectReferenceValue as Transform;
        bool generated = info.FindPropertyRelative("generated").boolValue;
        Vector2Int node = info.FindPropertyRelative("node").vector2IntValue;

        DeleteArrayElement(_anchorsProperty, index);

        if (CanDeleteGeneratedObject(anchorTransform, generated, node, cloth))
            Undo.DestroyObjectImmediate(anchorTransform.gameObject);
    }

    static void DeleteArrayElement(SerializedProperty array, int index)
    {
        int previousSize = array.arraySize;
        array.DeleteArrayElementAtIndex(index);
        if (array.arraySize == previousSize)
            array.DeleteArrayElementAtIndex(index);
    }

    static bool CanDeleteGeneratedObject(
        Transform anchorTransform,
        bool generated,
        Vector2Int node,
        ClothGenerator cloth)
    {
        if (!generated || anchorTransform == null || cloth == null)
            return false;

        return anchorTransform.parent == cloth.transform &&
               anchorTransform.name == GetGeneratedName(node) &&
               anchorTransform.childCount == 0 &&
               anchorTransform.GetComponents<Component>().Length == 1;
    }

    static string GetGeneratedName(Vector2Int node)
    {
        return $"Cloth Anchor ({node.x}, {node.y})";
    }
}

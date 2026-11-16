using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Yu5h1Lib;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>
    ///   ParticleAnchor inspector and scene-view authoring tool (ParticleExtensions EXT-06): shows
    ///   the source's particle count (and grid columns and rows) and the bound point count, lists
    ///   an error for every point outside the source, and while "Edit Bindings" is on lets the user
    ///   click particles to bind them to, or unbind them from, the Transform being edited. Picking
    ///   state lives in <see cref="ParticleAnchorPicker"/> and the edits in
    ///   <see cref="ParticleAnchorAuthoring"/>.
    /// </summary>
    [CustomEditor(typeof(ParticleAnchor))]
    public sealed class ParticleAnchorEditor : Editor<ParticleAnchor>
    {
        static readonly Color GridColor = new Color(0.35f, 0.7f, 1f, 0.22f);
        static readonly Color HoverColor = new Color(0.45f, 0.75f, 1f, 0.95f);
        static readonly Color EditingColor = new Color(0.25f, 1f, 0.4f, 1f);
        static readonly Color BoundColor = new Color(1f, 0.75f, 0.2f, 0.9f);

        readonly List<PhysicsParticleMessage> _messages = new List<PhysicsParticleMessage>();
        readonly ParticleAnchorPicker _picker = new ParticleAnchorPicker();
        bool _editMode;
        Transform _editing;

        void OnEnable()
        {
            _picker.Invalidate();
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Undo.undoRedoPerformed -= OnUndoRedo;
            _editMode = false;
        }

        void OnUndoRedo()
        {
            _picker.Invalidate();
            Repaint();
            SceneView.RepaintAll();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            base.OnInspectorGUI();
            _messages.Clear();
            targetObject.Validate(_messages);
            foreach (PhysicsParticleMessage message in _messages)
                EditorGUILayout.HelpBox(message.text, MessageType.Error);

            PhysicsParticle source = targetObject.Source;
            if (source == null)
            {
                DrawSourceFixup();
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Particles", DescribeSource(source));
            EditorGUILayout.LabelField(
                "Bound",
                $"{ParticleAnchorAuthoring.CountPoints(targetObject)} in {targetObject.Bindings.Count} bindings");

            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                bool next = GUILayout.Toggle(
                    _editMode,
                    _editMode ? "Editing" : "Edit Bindings",
                    "Button",
                    GUILayout.Height(28f));
                if (next != _editMode)
                {
                    _editMode = next;
                    if (_editMode)
                        PickDefaultEditing();
                    _picker.Invalidate();
                    SceneView.RepaintAll();
                }
            }

            if (Application.isPlaying)
                EditorGUILayout.HelpBox("Binding editing is disabled in Play Mode.", MessageType.Info);
            else if (_editMode)
                DrawEditingTarget();

            if (targetObject.Bindings.Count > 0 && !Application.isPlaying && GUILayout.Button("Recapture Offsets"))
                RecaptureOffsets(source);
        }

        static string DescribeSource(PhysicsParticle source)
        {
            string count = source.FlatParticleCount.ToString();
            if (source.Layout is GridParticleLayout grid)
                return $"{count}  ({grid.Columns} x {grid.Rows})";
            return count;
        }

        void DrawEditingTarget()
        {
            _editing = (Transform)EditorGUILayout.ObjectField("Editing", _editing, typeof(Transform), true);
            EditorGUILayout.HelpBox(
                _editing == null
                    ? "Assign the Transform to bind to, then click particles in the Scene View. "
                      + "Clicking a bound particle unbinds it. Escape leaves edit mode."
                    : $"Click particles in the Scene View to bind them to '{_editing.name}', "
                      + "or click a bound particle to unbind it. Escape leaves edit mode.",
                MessageType.Info);
        }

        void DrawSourceFixup()
        {
            if (targetObject.GetComponentInParent<PhysicsParticle>(true) == null)
                return;
            if (!GUILayout.Button("Add Missing Source"))
                return;
            ParticleAnchorAuthoring.ResolveSource(targetObject);
            serializedObject.Update();
        }

        void PickDefaultEditing()
        {
            if (_editing != null)
                return;
            foreach (ParticleAnchor.Binding binding in targetObject.Bindings)
                if (binding != null && binding.target != null)
                {
                    _editing = binding.target;
                    return;
                }
        }

        void RecaptureOffsets(PhysicsParticle source)
        {
            if (!_picker.Ensure(source))
                return;
            ParticleAnchorAuthoring.RecaptureOffsets(targetObject, _picker.RestPositions);
            serializedObject.Update();
            Repaint();
        }

        void OnSceneGUI()
        {
            if (Application.isPlaying)
                return;
            DrawHolderHandles();
            if (!_editMode)
                return;
            PhysicsParticle source = targetObject.Source;
            if (!_picker.Ensure(source))
                return;

            Event current = Event.current;
            if (current.type == EventType.KeyDown && current.keyCode == KeyCode.Escape)
            {
                _editMode = false;
                current.Use();
                Repaint();
                SceneView.RepaintAll();
                return;
            }

            int control = GUIUtility.GetControlID(FocusType.Passive);
            if (current.type == EventType.Layout)
                HandleUtility.AddDefaultControl(control);

            if (_picker.Process(current.type, current.mousePosition))
                SceneView.RepaintAll();

            if (current.type == EventType.MouseDown && current.button == 0 && !current.alt && _picker.Hovered >= 0)
            {
                Toggle(_picker.Hovered);
                current.Use();
                return;
            }

            if (current.type != EventType.Repaint)
                return;
            DrawGrid();
            DrawMarkers();
        }

        void Toggle(int index)
        {
            ParticleAnchorToggleResult result = ParticleAnchorAuthoring.Toggle(
                targetObject, _editing, index, _picker.RestPositions[index]);
            if (result == ParticleAnchorToggleResult.NoHolder)
                ShowSceneNotification("Assign a Transform to Editing before binding particles.");
            serializedObject.Update();
            Repaint();
            SceneView.RepaintAll();
        }

        static void ShowSceneNotification(string text)
        {
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.ShowNotification(new GUIContent(text));
        }

        void DrawHolderHandles()
        {
            if (_editMode && Event.current.type != EventType.Repaint)
                return;
            foreach (ParticleAnchor.Binding binding in targetObject.Bindings)
            {
                Transform holder = binding?.target;
                if (holder == null)
                    continue;
                using (new Handles.DrawingScope(_editMode && holder == _editing ? EditingColor : BoundColor))
                {
                    Vector3 position = holder.position;
                    float size = HandleUtility.GetHandleSize(position) * 0.10f;
                    if (_editMode)
                    {
                        Handles.DotHandleCap(0, position, Quaternion.identity, size, EventType.Repaint);
                        continue;
                    }
                    SceneView sceneView = SceneView.currentDrawingSceneView;
                    if (sceneView == null)
                        continue;
                    if (!Handles.Button(position, sceneView.camera.transform.rotation, size, size, Handles.DotHandleCap))
                        continue;
                    sceneView.LookAt(position);
                    Selection.activeGameObject = holder.gameObject;
                    return;
                }
            }
        }

        void DrawGrid()
        {
            Handles.color = GridColor;
            foreach (Vector3[] line in _picker.Lines)
                Handles.DrawAAPolyLine(1.5f, line);
        }

        void DrawMarkers()
        {
            foreach (ParticleAnchor.Binding binding in targetObject.Bindings)
            {
                if (binding == null || binding.points == null)
                    continue;
                Handles.color = binding.target == _editing ? EditingColor : BoundColor;
                foreach (ParticleAnchor.Point point in binding.points)
                {
                    if (point == null || point.index < 0 || point.index >= _picker.Count)
                        continue;
                    DrawDot(_picker.RestPositions[point.index], 0.06f);
                }
            }
            if (_picker.Hovered < 0)
                return;
            Handles.color = HoverColor;
            Vector3 hover = _picker.RestPositions[_picker.Hovered];
            DrawDot(hover, 0.09f);
            Handles.Label(hover, "  " + _picker.Describe(_picker.Hovered));
        }

        static void DrawDot(Vector3 position, float size) =>
            Handles.DotHandleCap(
                0, position, Quaternion.identity, HandleUtility.GetHandleSize(position) * size, EventType.Repaint);
    }
}

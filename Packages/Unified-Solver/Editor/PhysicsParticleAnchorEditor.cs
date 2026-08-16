using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Yu5h1Lib.UnifiedSolver;
using Yu5h1Lib.EditorExtension;
using Yu5h1Lib.ParticlePhysics;

// Scene-view particle picking for PhysicsParticleAnchor.
//
// The inspector is not this file's job. `bindings` is a KeyValues, so
// KeyValuesDrawer draws the whole map -- including its duplicate-key warning --
// and the base Editor<T> inspector reaches it with no code here. What is left is
// the one thing no PropertyField can do: say which particles of the body belong
// to the Transform currently being edited.
//
// Picking has to stay cheap. The first version issued one Handles.Button per
// grid node, so a 50x50 cloth paid 2500 control IDs, hit tests and draw calls on
// every repaint -- and OnSceneGUI repaints on mouse move. Three things fix that,
// all from the same observation: the work only has to happen when the mouse
// moves or the layout changes, not on every repaint.
//
//  - Rest positions are cached and rebuilt only when the source's layout or
//    Transform changes. Nothing recomputes them per frame.
//  - Picking is one control ID plus a nearest-particle search, run on mouse
//    events only. Repaint does no picking work at all.
//  - Dots are drawn for bound and hovered particles only. The grid lines already
//    show where every other one is; a dot on all 2500 was never readable.
//
// Edits go straight to the component under Undo.RegisterCompleteObjectUndo
// rather than through SerializedProperty. The data is a dictionary of lists,
// which SerializedProperty addresses only as `_bindings._entries.Array.data[i]`
// -- string paths for something the object already exposes as a typed API.
//
// Particles are identified by flat index everywhere. LayoutSize is consulted
// only to draw lines and to label a particle as (x, y), so a source that is an
// unstructured point cloud still works -- it just draws as points.
[CustomEditor(typeof(PhysicsParticleAnchor))]
public sealed class PhysicsParticleAnchorEditor
    : Editor<PhysicsParticleAnchor>
{
    static readonly Color GridColor =
        new Color(0.35f, 0.7f, 1f, 0.22f);
    static readonly Color HoverColor =
        new Color(0.45f, 0.75f, 1f, 0.95f);
    static readonly Color EditingColor =
        new Color(0.25f, 1f, 0.4f, 1f);
    static readonly Color BoundColor =
        new Color(1f, 0.75f, 0.2f, 0.9f);

    const float PickRadiusPixels = 18f;

    bool _editMode;
    Transform _editing;

    Vector3[] _restPositions;
    Vector3Int _cachedLayout;
    int _cachedCount;
    Matrix4x4 _cachedSourceMatrix;
    Vector3[][] _rows;
    Vector3[][] _columns;

    int _hovered = -1;

    void OnEnable()
    {
        InvalidateCache();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        _editMode = false;
    }

    IPhysicsParticleSource Source => targetObject.Source;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        base.OnInspectorGUI();

        IPhysicsParticleSource source = Source;
        if (source == null)
        {
            EditorGUILayout.HelpBox(
                "Assign a Source that implements IPhysicsParticleSource, " +
                "such as SolverClothParticles or SolverRopeParticles.",
                MessageType.Warning);
            DrawSourceFixup();
            return;
        }

        EditorGUILayout.Space();

        Vector3Int layout = source.LayoutSize;
        EditorGUILayout.LabelField(
            "Particles",
            layout.x > 0 && layout.y > 0
                ? $"{source.ParticleCount}  " +
                  $"({layout.x} x {layout.y})"
                : source.ParticleCount.ToString());
        EditorGUILayout.LabelField(
            "Bound",
            $"{CountPoints()} in " +
            $"{targetObject.Bindings.Count} bindings");

        DrawEmptyKeyWarning();

        using (new EditorGUI.DisabledScope(
                   Application.isPlaying))
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
                InvalidateCache();
                SceneView.RepaintAll();
            }
        }

        if (Application.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "Binding editing is disabled in Play Mode.",
                MessageType.Info);
        }
        else if (_editMode)
        {
            DrawEditingTarget();
        }

        if (targetObject.Bindings.Count > 0 &&
            !Application.isPlaying)
        {
            EditorGUILayout.Space(2f);
            if (GUILayout.Button("Recapture Offsets"))
                RecaptureOffsets();
        }
    }

    void DrawEditingTarget()
    {
        _editing = (Transform)EditorGUILayout.ObjectField(
            "Editing",
            _editing,
            typeof(Transform),
            true);

        EditorGUILayout.HelpBox(
            _editing == null
                ? "Assign the Transform to bind to, then click particles in " +
                  "the Scene View. Clicking a bound particle always unbinds " +
                  "it. Escape leaves edit mode."
                : $"Click particles in the Scene View to bind them to " +
                  $"'{_editing.name}', or click a bound particle to unbind " +
                  "it. Escape leaves edit mode.",
            MessageType.Info);
    }

    // KeyValues drops entries whose key is null every time it deserializes, so
    // a row added through the list's + button and left unassigned disappears on
    // the next domain reload. Saying so beats losing the points silently.
    void DrawEmptyKeyWarning()
    {
        foreach (var entry in targetObject.Bindings.Entries)
        {
            if (entry.Key != null)
                continue;

            EditorGUILayout.HelpBox(
                "A binding has no Transform. Rows without one are dropped " +
                "when the scene reloads.",
                MessageType.Warning);
            return;
        }
    }

    void PickDefaultEditing()
    {
        if (_editing != null)
            return;

        foreach (var entry in targetObject.Bindings.Entries)
        {
            if (entry.Key == null)
                continue;

            _editing = entry.Key;
            return;
        }
    }

    // The manual door, for an anchor that already exists with no Source -- a
    // scene upgraded from ClothAnchor. Anything added from now on is handled by
    // the hook below before anyone sees the inspector.
    void DrawSourceFixup()
    {
        GameObject owner = targetObject.gameObject;
        if (!CanResolveSource(owner))
            return;

        if (GUILayout.Button("Add Missing Source"))
        {
            ResolveSource(owner);
            serializedObject.Update();
        }
    }

    // Adds the backend glue the moment the body can say which one it needs, so
    // the normal path costs no clicks and no reading of an error.
    //
    // In the editor, and only in the editor, for the same reason the button is:
    // PhysicsParticleAnchor is backend neutral by decision and must not name a
    // Unified Solver type. An editor may.
    //
    // All of this dies with the glue if Documentation/Plan/PhysicsParticle.md
    // lands, which is why it is a hook in one editor file rather than anything
    // the runtime knows about.
    [InitializeOnLoadMethod]
    static void HookComponentAdded()
    {
        ObjectFactory.componentWasAdded -= OnComponentAdded;
        ObjectFactory.componentWasAdded += OnComponentAdded;
    }

    // Fires for the generator and the glue too, not just the anchor, so the
    // parts can be added in any order and the last one completes the set.
    //
    // Deferred because the callback runs inside the add that raised it, and
    // because ClothGenerator and the anchor often arrive in the same frame.
    static void OnComponentAdded(Component component)
    {
        if (!(component is PhysicsParticleAnchor ||
              component is SolverParticleSource ||
              component is ClothGenerator ||
              component is RopeGenerator))
        {
            return;
        }

        GameObject owner = component.gameObject;
        EditorApplication.delayCall += () =>
        {
            if (owner != null)
                ResolveSource(owner);
        };
    }

    static bool CanResolveSource(GameObject owner) =>
        owner.GetComponent<SolverParticleSource>() != null ||
        owner.GetComponent<ClothGenerator>() != null ||
        owner.GetComponent<RopeGenerator>() != null;

    static void ResolveSource(GameObject owner)
    {
        var anchor = owner.GetComponent<PhysicsParticleAnchor>();
        if (anchor == null)
            return;

        var serialized = new SerializedObject(anchor);
        SerializedProperty sourceProperty =
            serialized.FindProperty("_source");
        if (sourceProperty.objectReferenceValue != null)
            return;

        SolverParticleSource source =
            owner.GetComponent<SolverParticleSource>();
        if (source == null)
            source = AddMatchingSource(owner);
        if (source == null)
            return;

        sourceProperty.objectReferenceValue = source;
        serialized.ApplyModifiedProperties();
    }

    // Only when the body already says which one it is. Both glue components
    // carry [RequireComponent] for their generator, so guessing here would not
    // fail loudly -- it would silently fabricate a body nobody asked for.
    static SolverParticleSource AddMatchingSource(GameObject owner)
    {
        if (owner.GetComponent<ClothGenerator>() != null)
            return Undo.AddComponent<SolverClothParticles>(owner);

        if (owner.GetComponent<RopeGenerator>() != null)
            return Undo.AddComponent<SolverRopeParticles>(owner);

        return null;
    }

    int CountPoints()
    {
        int total = 0;
        foreach (var entry in targetObject.Bindings.Entries)
        {
            if (entry.Value != null)
                total += entry.Value.Count;
        }
        return total;
    }

    // Re-reads every offset from the body's current rest layout. Needed after
    // the body is moved or its resolution changes, since an offset captured
    // against the old layout would drag the particle somewhere arbitrary.
    void RecaptureOffsets()
    {
        IPhysicsParticleSource source = Source;
        if (source == null)
            return;

        Undo.RegisterCompleteObjectUndo(
            targetObject, "Recapture Anchor Offsets");

        foreach (var entry in targetObject.Bindings.Entries)
        {
            if (entry.Key == null || entry.Value == null)
                continue;

            List<PhysicsParticleAnchor.Point> points = entry.Value;
            for (int p = 0; p < points.Count; p++)
            {
                if (!source.TryGetRestPosition(
                        points[p].index,
                        out Vector3 world))
                {
                    continue;
                }

                points[p].localOffset =
                    entry.Key.InverseTransformPoint(world);
            }
        }

        CommitBindings();
    }

    void CommitBindings()
    {
        targetObject.InvalidateBindings();
        EditorUtility.SetDirty(targetObject);
        serializedObject.Update();
        Repaint();
    }

    void InvalidateCache()
    {
        _restPositions = null;
        _rows = null;
        _columns = null;
        _hovered = -1;
    }

    // Rebuilt only when the layout actually changed, which is what takes the
    // per-repaint matrix work out of the scene view. The source's Transform
    // matrix is part of the key because moving the body moves every particle.
    bool EnsureCache(IPhysicsParticleSource source)
    {
        int count = source.ParticleCount;
        if (count <= 0)
            return false;

        Vector3Int layout = source.LayoutSize;
        Matrix4x4 matrix =
            source is Component component
                ? component.transform.localToWorldMatrix
                : Matrix4x4.identity;

        if (_restPositions != null &&
            _cachedCount == count &&
            _cachedLayout == layout &&
            _cachedSourceMatrix == matrix)
        {
            return true;
        }

        _cachedCount = count;
        _cachedLayout = layout;
        _cachedSourceMatrix = matrix;
        _restPositions = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            source.TryGetRestPosition(
                i, out Vector3 world);
            _restPositions[i] = world;
        }

        BuildGridLines();
        return true;
    }

    // Only meaningful when the source says it is a lattice. Anything else draws
    // as bare points, which is the graceful degradation the flat index buys.
    void BuildGridLines()
    {
        _rows = null;
        _columns = null;

        int width = _cachedLayout.x;
        int height = _cachedLayout.y;
        if (width <= 1 && height <= 1)
            return;
        if (width <= 0 || height <= 0)
            return;
        if (width * height > _cachedCount)
            return;

        _rows = new Vector3[height][];
        for (int y = 0; y < height; y++)
        {
            var row = new Vector3[width];
            for (int x = 0; x < width; x++)
                row[x] = _restPositions[y * width + x];
            _rows[y] = row;
        }

        if (height <= 1)
            return;

        _columns = new Vector3[width][];
        for (int x = 0; x < width; x++)
        {
            var column = new Vector3[height];
            for (int y = 0; y < height; y++)
                column[y] = _restPositions[y * width + x];
            _columns[x] = column;
        }
    }

    void OnSceneGUI()
    {
        if (!_editMode || Application.isPlaying)
            return;

        IPhysicsParticleSource source = Source;
        if (source == null || !EnsureCache(source))
            return;

        Event current = Event.current;

        if (current.type == EventType.KeyDown &&
            current.keyCode == KeyCode.Escape)
        {
            _editMode = false;
            current.Use();
            Repaint();
            SceneView.RepaintAll();
            return;
        }

        HandleUtility.AddDefaultControl(
            GUIUtility.GetControlID(FocusType.Passive));

        if (current.type == EventType.MouseMove ||
            current.type == EventType.MouseDown)
        {
            int found = FindNearest(current.mousePosition);
            if (found != _hovered)
            {
                _hovered = found;
                SceneView.RepaintAll();
            }
        }

        if (current.type == EventType.MouseDown &&
            current.button == 0 &&
            !current.alt &&
            _hovered >= 0)
        {
            Toggle(_hovered);
            current.Use();
            return;
        }

        DrawGrid();
        DrawMarkers();
    }

    int FindNearest(Vector2 mouse)
    {
        int best = -1;
        float bestDistance =
            PickRadiusPixels * PickRadiusPixels;

        for (int i = 0; i < _cachedCount; i++)
        {
            Vector2 gui =
                HandleUtility.WorldToGUIPoint(
                    _restPositions[i]);
            float distance = (gui - mouse).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = i;
            }
        }

        return best;
    }

    void DrawGrid()
    {
        Handles.color = GridColor;
        if (_rows != null)
        {
            for (int y = 0; y < _rows.Length; y++)
                Handles.DrawAAPolyLine(1.5f, _rows[y]);
        }
        if (_columns != null)
        {
            for (int x = 0; x < _columns.Length; x++)
                Handles.DrawAAPolyLine(1.5f, _columns[x]);
        }
    }

    // Only bound particles and the hovered one get a dot. Every other one sits
    // at a visible line intersection already, and drawing all of them was the
    // single most expensive thing the first editor did.
    //
    // The Transform being edited draws in its own colour, because the whole
    // point of the scene view here is to see that one Transform's set.
    void DrawMarkers()
    {
        foreach (var entry in targetObject.Bindings.Entries)
        {
            if (entry.Value == null)
                continue;

            Handles.color =
                entry.Key == _editing ? EditingColor : BoundColor;

            List<PhysicsParticleAnchor.Point> points = entry.Value;
            for (int p = 0; p < points.Count; p++)
            {
                int index = points[p].index;
                if (index < 0 || index >= _cachedCount)
                    continue;

                Vector3 world = _restPositions[index];
                Handles.DotHandleCap(
                    0,
                    world,
                    Quaternion.identity,
                    HandleUtility.GetHandleSize(world) *
                    0.06f,
                    EventType.Repaint);
            }
        }

        if (_hovered < 0)
            return;

        Handles.color = HoverColor;
        Vector3 hoverWorld = _restPositions[_hovered];
        Handles.DotHandleCap(
            0,
            hoverWorld,
            Quaternion.identity,
            HandleUtility.GetHandleSize(hoverWorld) * 0.09f,
            EventType.Repaint);
        Handles.Label(hoverWorld, "  " + Describe(_hovered));
    }

    // Shape is display only, so the label degrades with it: a lattice reads as
    // a coordinate, anything else as the index it actually is.
    string Describe(int index)
    {
        int width = _cachedLayout.x;
        if (width > 0 && _cachedLayout.y > 1)
            return $"({index % width}, {index / width})";
        return index.ToString();
    }

    // Unbinding wins over binding, and it searches every Transform rather than
    // the one being edited, so a particle can never end up held by two
    // Transforms fighting over it -- and so a stray particle can be removed
    // without first hunting down which Transform owns it.
    void Toggle(int index)
    {
        foreach (var entry in targetObject.Bindings.Entries)
        {
            List<PhysicsParticleAnchor.Point> existing = entry.Value;
            if (existing == null)
                continue;

            for (int p = 0; p < existing.Count; p++)
            {
                if (existing[p].index != index)
                    continue;

                Undo.RegisterCompleteObjectUndo(
                    targetObject, "Unbind Particle");
                existing.RemoveAt(p);
                CommitBindings();
                return;
            }
        }

        if (_editing == null)
        {
            Debug.LogWarning(
                "Assign a Transform to Editing before binding particles.",
                targetObject);
            return;
        }

        Undo.RegisterCompleteObjectUndo(
            targetObject, "Bind Particle");

        if (!targetObject.Bindings.TryGetValue(
                _editing,
                out List<PhysicsParticleAnchor.Point> points))
        {
            points = new List<PhysicsParticleAnchor.Point>();
            targetObject.Bindings[_editing] = points;
        }

        // Captured now, against the rest layout, so several particles sharing
        // one Transform keep the body's shape instead of collapsing onto it.
        points.Add(new PhysicsParticleAnchor.Point
        {
            index = index,
            localOffset =
                _editing.InverseTransformPoint(
                    _restPositions[index])
        });

        CommitBindings();
    }
}

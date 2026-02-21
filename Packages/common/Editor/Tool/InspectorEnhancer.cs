using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using System.Linq;

/// <summary>
/// Enhanced Inspector utilities:
/// - Selection history navigation (Alt+Left/Right)
/// - Inspector tab management (lock, add, close, switch)
/// - Keyboard shortcuts for faster workflow
/// </summary>
public class InspectorEnhancer : EditorWindow
{

    public static List<EntityId[]> m_ObjectsRecorder;
    public static List<EntityId[]> ObjectsRecorder
    {
        get
        {
            if (m_ObjectsRecorder == null)
            {
                m_ObjectsRecorder = new List<EntityId[]>();
                var load = EditorPrefs.GetString("InspectorEnhancer_SelectionHistory");
                if (load == "")
                {
                    Record();
                }
                else
                {
                    var elements = load.Split('-');
                    foreach (var ele in elements)
                    {
                        if (ele != "")
                        {
                            var sobjs = ele.Split(',');
                            var objs = new EntityId[sobjs.Length];
                            for (int i = 0; i < sobjs.Length; i++)
                            {
                                if (sobjs[i] != "")
                                {
                                    objs[i] = int.Parse(sobjs[i]);
                                }
                            }
                            if (objs.Length > 0)
                            {
                                m_ObjectsRecorder.Add(objs);
                            }
                        }
                    }
                }
            }

            return m_ObjectsRecorder;
        }
    }
    public static int current;
    static InspectorEnhancer m_window;
    static InspectorEnhancer window
    {
        get
        {
            if (m_window == null)
            {
                InspectorEnhancer.m_window = (InspectorEnhancer)EditorWindow.GetWindow(typeof(InspectorEnhancer));
                m_window.titleContent = new GUIContent("Selection History");
            }
            return m_window;
        }
    }
    static bool disableRecordSelected = false;
    static bool enablePreviouse { get { return ObjectsRecorder.Count > 0 && current > 0; } }
    static bool enableNext { get { return ObjectsRecorder.Count > 0 && current < ObjectsRecorder.Count - 1; } }
    static bool ShowWindow = false;

    // Cached reflection types and methods
    static System.Type _inspectorType;
    static System.Type InspectorType => _inspectorType ??= typeof(Editor).Assembly.GetType("UnityEditor.InspectorWindow");

    static System.Type _dockAreaType;
    static System.Type DockAreaType
    {
        get
        {
            if (_dockAreaType == null)
            {
                _dockAreaType = typeof(Editor).Assembly.GetType("UnityEditor.DockArea");
            }
            return _dockAreaType;
        }
    }

    static List<MethodInfo> DockAreaMethods { get { return DockAreaType?.GetMethods().ToList(); } }

    static List<Object> InspectorAreas
    {
        get
        {
            if (DockAreaType == null || InspectorType == null) return new List<Object>();
            return Resources.FindObjectsOfTypeAll(DockAreaType).ToList().FindAll(d => {
                var so = new SerializedObject(d);
                var panes = so.FindProperty("m_Panes");
                return panes != null && panes.arraySize > 0 &&
                       panes.GetArrayElementAtIndex(0).objectReferenceValue?.GetType() == InspectorType;
            });
        }
    }

    static Object InspectorArea
    {
        get
        {
            var results = InspectorAreas;
            if (results.Count == 0) return null;

            if (EditorWindow.focusedWindow != null && EditorWindow.focusedWindow.GetType() == InspectorType && results.Count > 1)
            {
                foreach (var item in results)
                {
                    var Panes = new SerializedObject(item).FindProperty("m_Panes");
                    for (int i = 0; i < Panes.arraySize; i++)
                    {
                        if (Panes.GetArrayElementAtIndex(i).objectReferenceValue == EditorWindow.focusedWindow)
                        {
                            return item;
                        }
                    }
                }
            }
            return results[0];
        }
    }

    static SerializedProperty Panels
    {
        get
        {
            if (InspectorArea == null) return null;
            return new SerializedObject(InspectorArea).FindProperty("m_Panes");
        }
    }

    static int currentTab
    {
        get
        {
            if (InspectorArea == null) return 0;
            var method = FindDockAreaMethod("Int32 get_selected()");
            if (method == null) return 0;
            return (int)method.Invoke(InspectorArea, null);
        }
    }

    [MenuItem("Edit/Inspector Enhancer/Show Selection History")]
    static void Init()
    {
        ShowWindow = true;
        window.Show();
    }

    void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginDisabledGroup(!enablePreviouse);
        if (GUILayout.Button("<"))
        {
            PreviouseSelected();
        }
        EditorGUI.EndDisabledGroup();
        EditorGUI.BeginDisabledGroup(!enableNext);
        if (GUILayout.Button(">"))
        {
            NextSelected();
        }
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();

        if (ObjectsRecorder != null)
        {
            for (int i = 0; i < ObjectsRecorder.Count; i++)
            {
                var ele = ObjectsRecorder[i];
                string countInfo = "";
                if (ele.Length > 1)
                {
                    countInfo = " Count: " + ele.Length.ToString();
                }
                else if (ele.Length != 0)
                {
                    countInfo = " \tInstanceID: " + ele[0].ToString();
                }
                GUIStyle s = new GUIStyle("label");
                if (current == i)
                {
                    s.richText = true;
                    s.normal.textColor = Color.yellow;
                    GUILayout.Label("<b>" + ("Element." + i.ToString() + countInfo) + "</b>", s);
                }
                else
                {
                    if (GUILayout.Button("Element." + i.ToString() + countInfo, s))
                    {
                        current = i;
                        changeSelectionObjectsFromList();
                    }
                }
            }
        }

        Rect clearPos = new Rect(0, window.position.height - 20, window.position.width, 20);
        if (GUI.Button(clearPos, "Clear"))
        {
            current = 0;
            ObjectsRecorder.Clear();
            Record();
        }
    }

    void OnDisable()
    {
        ShowWindow = false;
    }

    static void Record()
    {
        ObjectsRecorder.Add(Selection.entityIds);
        string save = "";
        foreach (var item in ObjectsRecorder)
        {
            foreach (var obj in item)
            {
                save += obj.ToString() + ",";
            }
            save += "-";
        }
        EditorPrefs.SetString("InspectorEnhancer_SelectionHistory", save);
    }

    static void SelectionChangeEvent()
    {
        if (disableRecordSelected)
        {
            disableRecordSelected = false;
        }
        else
        {
            if (current != ObjectsRecorder.Count - 1)
            {
                ObjectsRecorder.RemoveRange(current + 1, ObjectsRecorder.Count - (current + 1));
            }
            Record();
            if (ObjectsRecorder.Count > 21) { ObjectsRecorder.RemoveAt(0); }
            current = ObjectsRecorder.Count - 1;
        }
        if (ShowWindow)
        {
            window.Repaint();
        }
    }

    [MenuItem("Edit/Inspector Enhancer/Previous Selection &LEFT", true)]
    static bool CheckPreviouseSelected() { return enablePreviouse; }

    [MenuItem("Edit/Inspector Enhancer/Previous Selection &LEFT")]
    static void PreviouseSelected()
    {
        current -= 1;
        changeSelectionObjectsFromList();
    }

    [MenuItem("Edit/Inspector Enhancer/Next Selection &RIGHT", true)]
    static bool CheckNextSelected() { return enableNext; }

    [MenuItem("Edit/Inspector Enhancer/Next Selection &RIGHT")]
    static void NextSelected()
    {
        current += 1;
        changeSelectionObjectsFromList();
    }

    static void changeSelectionObjectsFromList()
    {
        disableRecordSelected = true;
        Object[] objs = new Object[ObjectsRecorder[current].Length];
        for (int i = 0; i < ObjectsRecorder[current].Length; i++)
        {
            objs[i] = EditorUtility.EntityIdToObject(ObjectsRecorder[current][i]);
        }
        Selection.objects = objs;
    }

    [InitializeOnLoadMethod]
    private static void OnScriptsReloaded()
    {
        Selection.selectionChanged += () => InspectorEnhancer.SelectionChangeEvent();
    }

    [MenuItem("Edit/Inspector Enhancer/Add Locked Inspector Tab %&T")]
    static void AddLockInspectorTab()
    {
        if (InspectorType == null)
        {
            Debug.LogWarning("Cannot create Inspector tab - reflection types not found");
            return;
        }
        try
        {
            var newInspector = ScriptableObject.CreateInstance(InspectorType) as EditorWindow;
            var lockProperty = InspectorType.GetProperty("isLocked", BindingFlags.Instance | BindingFlags.Public);
            if (lockProperty != null)
            {
                lockProperty.GetSetMethod()?.Invoke(newInspector, new object[] { true });
            }

            var addTabMethod = FindDockAreaMethod("Void AddTab(UnityEditor.EditorWindow)");
            if (addTabMethod != null && InspectorArea != null)
            {
                addTabMethod.Invoke(InspectorArea, new object[] { newInspector });
            }

            newInspector.Show();
            newInspector.Focus();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to create locked Inspector tab: {e.Message}");
        }
    }

    [MenuItem("Edit/Inspector Enhancer/Add Locked Inspector Window %T")]
    static void AddLockInspectorWindowAtRight()
    {
        if (InspectorType == null || InspectorAreas.Count == 0)
        {
            Debug.LogWarning("Cannot create Inspector window - reflection types not found");
            return;
        }

        try
        {
            var panels = new SerializedObject(InspectorArea).FindProperty("m_Panes");
            if (panels == null || panels.arraySize == 0) return;

            var parentWindow = panels.GetArrayElementAtIndex(0).objectReferenceValue as EditorWindow;
            var newInspector = ScriptableObject.CreateInstance(InspectorType) as EditorWindow;

            var lockProperty = InspectorType.GetProperty("isLocked", BindingFlags.Instance | BindingFlags.Public);
            if (lockProperty != null)
            {
                lockProperty.GetSetMethod()?.Invoke(newInspector, new object[] { true });
            }

            newInspector.Show();
            DockEditorWindow(parentWindow, newInspector);
            newInspector.Focus();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to create docked Inspector window: {e.Message}");
        }
    }

    [MenuItem("Edit/Inspector Enhancer/Close Focused Window %W")]
    static void CloseFocusedWindow()
    {
        if (focusedWindow != null)
        {
            focusedWindow.Close();
        }
    }

    [MenuItem("Edit/Inspector Enhancer/Next Tab %PGUP")]
    static void ChangeNextTab()
    {
        if (InspectorAreas.Count > 0 && Panels != null)
        {
            if (Panels.arraySize > 1)
            {
                int nextTab = currentTab - 1 >= 0 ? currentTab - 1 : Panels.arraySize - 1;
                var window = Panels.GetArrayElementAtIndex(nextTab).objectReferenceValue as EditorWindow;
                window?.Focus();
            }
        }
    }

    [MenuItem("Edit/Inspector Enhancer/Previous Tab %PGDN")]
    static void ChangePreviouseTab()
    {
        if (InspectorAreas.Count > 0 && Panels != null)
        {
            if (Panels.arraySize > 1)
            {
                int nextTab = currentTab + 1 < Panels.arraySize ? currentTab + 1 : 0;
                var window = Panels.GetArrayElementAtIndex(nextTab).objectReferenceValue as EditorWindow;
                window?.Focus();
            }
        }
    }

    [MenuItem("Edit/Inspector Enhancer/Lock/Unlock Inspector %L")]
    static void LockOrUnLockInspector()
    {
        if (InspectorType == null || InspectorAreas.Count == 0 || Panels == null) return;

        try
        {
            var currentTabWindow = Panels.GetArrayElementAtIndex(currentTab).objectReferenceValue;
            var lockProperty = InspectorType.GetProperty("isLocked", BindingFlags.Instance | BindingFlags.Public);
            if (lockProperty == null) return;

            bool currentLockState = (bool)lockProperty.GetValue(currentTabWindow, null);
            lockProperty.GetSetMethod()?.Invoke(currentTabWindow, new object[] { !currentLockState });

            var repaintMethod = InspectorType.GetMethods().ToList().Find(m => m.ToString() == "Void Repaint()");
            repaintMethod?.Invoke(currentTabWindow, null);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to toggle Inspector lock: {e.Message}");
        }
    }

    static MethodInfo FindDockAreaMethod(string val)
    {
        if (DockAreaMethods == null) return null;
        return DockAreaMethods.Find(d => d.ToString().Contains(val));
    }

    public static void DockEditorWindow(EditorWindow parent, EditorWindow child)
    {
        if (parent == null || child == null) return;

        try
        {
            Vector2 screenPoint = parent.position.position + new Vector2(parent.position.width * 0.9f, parent.position.height / 2);

            Assembly assembly = typeof(UnityEditor.EditorWindow).Assembly;
            System.Type sv = assembly.GetType("UnityEditor.SplitView");
            if (sv == null) return;

            var parentField = typeof(EditorWindow).GetField("m_Parent", BindingFlags.NonPublic | BindingFlags.Instance);
            if (parentField == null) return;

            var opArea = parentField.GetValue(parent);
            var ocArea = parentField.GetValue(child);

            var parentProperty = DockAreaType.GetProperty("parent", BindingFlags.Public | BindingFlags.Instance);
            if (parentProperty == null) return;

            var oview = parentProperty.GetValue(opArea, null);

            var dragOverMethod = sv.GetMethod("DragOver", BindingFlags.Public | BindingFlags.Instance);
            if (dragOverMethod == null) return;

            var oDropInfo = dragOverMethod.Invoke(oview, new object[] { child, screenPoint });

            var originalDragSourceField = DockAreaType.GetField("s_OriginalDragSource", BindingFlags.NonPublic | BindingFlags.Static);
            originalDragSourceField?.SetValue(null, ocArea);

            var performDropMethod = sv.GetMethod("PerformDrop", BindingFlags.Public | BindingFlags.Instance);
            performDropMethod?.Invoke(oview, new object[] { child, oDropInfo, null });
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to dock editor window: {e.Message}");
        }
    }
}
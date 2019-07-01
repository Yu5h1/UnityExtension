using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace Yu5h1Tools.Test
{
    public class PivotEditor : EditorWindow
    {
        private static PivotEditor m_PivotEditor;
        private static PivotEditor window
        {
            get
            {
                if (m_PivotEditor == null) Init();
                return m_PivotEditor;
            }
        }
        bool EnableEdit { get { return Selection.activeGameObject != null && meshFilter != null; } }

        bool EditMod = false,
                SnapMod = false;

        MeshFilter meshFilter { get { return Selection.activeGameObject.GetComponent<MeshFilter>(); } }
        MeshRenderer render { get { return Selection.activeGameObject.GetComponent<MeshRenderer>(); } }
        Mesh mesh { get { return meshFilter.sharedMesh; } }
        Bounds bounds { get { return mesh.bounds; } }

        Transform transform { get { return Selection.activeGameObject.transform; } }
        Vector3 targetPosition { get { return transform.position; } }


        Vector3 OriginalPosition;

        object snapPoint = null;

        float detectDistance = 0.1f;
        string warningTxT;
        string snapTxT;
        string AlignTxT;
        bool isChineseLanguage;
        string[] DirTxT;
        [MenuItem("Tools/PivotEditor")]
        public static void Init()
        {
            m_PivotEditor = (PivotEditor)EditorWindow.GetWindow(typeof(PivotEditor));
            window.titleContent = new GUIContent("PivotEditor");
        }
        private void OnEnable()
        {

#if UNITY_2019
        SceneView.duringSceneGui += OnSceneGUI;
#else
            SceneView.onSceneGUIDelegate += OnSceneGUI;
#endif
            Selection.selectionChanged += OnSelectionChanged;
            EditMod = false;
            bool IsChineseLanguage = Application.systemLanguage == SystemLanguage.Chinese;
            warningTxT = IsChineseLanguage ? "請 選擇一個網格過濾器 ~ ♫ " : "P lease Select a Mesh filter ~ ♫";
            warningTxT = warningTxT.PrefixStyle(30, Color.HSVToRGB(0.09f, 1, 1), true).Size(20);
            snapTxT = IsChineseLanguage ?"吸附模式(拖拉物件時)": "Snap Mod (hold shift while drag object)";
            AlignTxT = IsChineseLanguage ? "對齊": "Alignment";
            DirTxT = IsChineseLanguage ?
                new string[] {"中","上","下","左","右","前","後"} :
                new string[] { "Center", "Top", "Buttom", "Left", "Right", "Front", "Back" };




        }
        void OnGUI()
        {
            //using (var scope = new EditorGUI.ChangeCheckScope())
            //{
            //    if (scope.changed)
            //    {
            //        SceneView.RepaintAll();
            //    }
            //}
            if (EnableEdit == false)
            {
                
                var warningTxTstyle = new GUIStyle("HelpBox");
                warningTxTstyle.richText = true;
                GUILayout.Label(warningTxT, warningTxTstyle);
            }
            using (var EnableEditscop = new EditorGUI.DisabledGroupScope(!EnableEdit))
            {
                using (var scope = new EditorGUI.ChangeCheckScope())
                {
                    EditMod = GUILayout.Toggle(EditMod, "Edit Mod", new GUIStyle("button"));
                    if (scope.changed)
                    {
                        if (EditMod)
                        {
                            RecordcurrentSetting();
                            string instanceIDName = transform.gameObject.GetInstanceID().ToString();
                            if (mesh.name.Contains(instanceIDName) == false)
                            {
                                meshFilter.sharedMesh = Instantiate(mesh);
                                meshFilter.sharedMesh.name = instanceIDName;
                            }
                        }
                        else
                        {
                            ResumePreviouseSetting();
                        }
                        SceneView.RepaintAll();
                    }
                }
                GUILayout.Space(20);

                using (var scope = new EditorGUI.DisabledGroupScope(!EditMod))
                {
                    SnapMod = GUILayout.Toggle(SnapMod, snapTxT, new GUIStyle("button"));
                    GUILayout.Space(10);
                    if (GUILayout.Button(AlignTxT))
                    {
                        GenericMenu a = new GenericMenu();
                        a.AddItem(new GUIContent(DirTxT[0]), false, () => AlignTo(bounds.center));
                        a.AddItem(new GUIContent(DirTxT[1]), false, () => AlignTo(bounds.Top()));
                        a.AddItem(new GUIContent(DirTxT[2]), false, () => AlignTo(bounds.Buttom()));
                        a.AddItem(new GUIContent(DirTxT[3]), false, () => AlignTo(bounds.Left()));
                        a.AddItem(new GUIContent(DirTxT[4]), false, () => AlignTo(bounds.Right()));
                        a.AddItem(new GUIContent(DirTxT[5]), false, () => AlignTo(bounds.Front()));
                        a.AddItem(new GUIContent(DirTxT[6]), false, () => AlignTo(bounds.Back()));


                        a.ShowAsContext();
                    }
                }

            }
        }
        void RecordcurrentSetting()
        {
            OriginalPosition = mesh.bounds.center + targetPosition;
        }
        void ResumePreviouseSetting()
        {
            SnapMod = false;
        }

        void AlignTo(Vector3 point)
        {
            transform.position = point + transform.position;
            MoveVertices(mesh.bounds.center + (targetPosition - OriginalPosition));
        }
        void MoveVertices(object vector3) { MoveVertices((Vector3)vector3); }
        void MoveVertices(Vector3 amount)
        {
            Vector3[] newVertices = mesh.vertices;
            //int[] newtriangles = mesh.triangles;
            //Color[] newcolors = mesh.colors;
            //mesh.Clear();

            for (int i = 0; i < newVertices.Length; i++)
            {
                newVertices[i] = newVertices[i] - amount;
            }


            mesh.vertices = newVertices;
            //mesh.triangles = newtriangles;
            //mesh.colors = newcolors;
            mesh.RecalculateBounds();
            //mesh.RecalculateNormals();
        }
        bool IsMouseDrag;
        void OnSceneGUI(SceneView sceneview)
        {

            if (EnableEdit)
            {
                if (EditMod)
                {
                    Tools.pivotMode = PivotMode.Pivot;

                    Vector3 mousePosition = Event.current.mousePosition;
                    mousePosition.x -= 2;
                    mousePosition = HandleUtility.GUIPointToWorldRay(mousePosition).origin;
                    //Draw Mouse Line
                    //if (Event.current.type == EventType.Repaint)
                    //{
                    //    ScreenPoint(sceneview, mousePosition);
                    //}

                    if (targetPosition != OriginalPosition)
                    {
                        MoveVertices(mesh.bounds.center + (targetPosition - OriginalPosition));
                    }

                    if (Event.current.type == EventType.MouseDrag && Event.current.button == 0 && (Event.current.modifiers == EventModifiers.None || Event.current.modifiers == EventModifiers.Shift))
                    {
                        IsMouseDrag = true;
                    }
                    if (IsMouseDrag)
                    {
                        if (Event.current.type == EventType.Layout)
                        {
                            SnapMod = Event.current.modifiers == EventModifiers.Shift;
                        }
                    }

                    if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
                    {
                        if (snapPoint != null)
                        {
                            transform.position = (Vector3)snapPoint;
                            snapPoint = null;
                        }
                        IsMouseDrag = false;
                    }

                    if (Event.current.type == EventType.Repaint && IsMouseDrag && SnapMod)
                    {
                        Vector3 mouseViewportPoint = sceneview.camera.WorldToViewportPoint(mousePosition);
                        float nearestDistance = detectDistance;
                        if (snapPoint != null)
                        {
                            ScreenPoint(sceneview, (Vector3)snapPoint, 0.2f);
                            Vector3 nearestPoint = sceneview.camera.WorldToViewportPoint((Vector3)snapPoint);
                            nearestDistance = Vector2.Distance(nearestPoint, mouseViewportPoint);
                            if (nearestDistance > detectDistance)
                            {
                                snapPoint = null;
                                nearestDistance = 1;
                            }
                        }

                        foreach (var v in mesh.vertices)
                        {
                            var curv = v + targetPosition;
                            Vector3 viewportPoint = sceneview.camera.WorldToViewportPoint(curv);
                            float curDistance = Vector2.Distance(viewportPoint, mouseViewportPoint);
                            if (curDistance < detectDistance)
                            {
                                if (curDistance < nearestDistance)
                                {
                                    snapPoint = curv;
                                }
                            }
                        }



                        //Point(mesh.bounds.center, Color.yellow);
                    }
                    //if (newTargetPosition != OriginalPosition) { OriginalPosition = newTargetPosition; }

                }
            }
            Repaint();
        }
        void OnSelectionChanged()
        {
            if (EnableEdit && EditMod)
            {
                RecordcurrentSetting();
                string instanceIDName = transform.gameObject.GetInstanceID().ToString();
                if (mesh.name.Contains(instanceIDName) == false)
                {
                    meshFilter.sharedMesh = Instantiate(meshFilter.sharedMesh);
                    meshFilter.sharedMesh.name = instanceIDName;
                }
            }
            else
            {
                EditMod = false;
            }
            SnapMod = false;
            Repaint();
        }
        public void Point(Vector3 position, Color color = default(Color), float size = 0.2f)
        {
            if (color == default(Color)) color = Color.yellow;
            var previouseColor = Handles.color;
            Handles.color = color;
            color = (color == default(Color)) ? Color.white : color;
            Handles.DrawLine(position + (Vector3.up * size), position - (Vector3.up * size));
            Handles.DrawLine(position + (Vector3.forward * size), position - (Vector3.forward * size));
            Handles.DrawLine(position + (Vector3.right * size), position - (Vector3.right * size));

        }
        public void ScreenPoint(SceneView sceneview, Vector3 position, float size = 1, Color color = default(Color))
        {
            if (color == default(Color)) color = Color.yellow;
            var previouseColor = Handles.color;
            Handles.color = color;
            Transform cameraTrans = sceneview.camera.transform;

            Vector3 viewportPoint = sceneview.camera.WorldToViewportPoint(position);
            viewportPoint.z += sceneview.camera.nearClipPlane * size;
            Vector2 FrustumSize = new Vector2(0, 2.0f * viewportPoint.z * Mathf.Tan(sceneview.camera.fieldOfView * 0.5f * Mathf.Deg2Rad));
            FrustumSize.x = FrustumSize.y * sceneview.camera.aspect;
            Vector3 FrustumPoint =
                new Vector3((viewportPoint.x - 0.5f) * FrustumSize.x, (viewportPoint.y - 0.5f) * FrustumSize.y, viewportPoint.z);

            Vector3 up = new Vector3(FrustumPoint.x, FrustumPoint.y + size, viewportPoint.z);
            Vector3 down = new Vector3(FrustumPoint.x, FrustumPoint.y - size, viewportPoint.z);
            Vector3 left = new Vector3(FrustumPoint.x + size, FrustumPoint.y, viewportPoint.z);
            Vector3 right = new Vector3(FrustumPoint.x - size, FrustumPoint.y, viewportPoint.z);

            Handles.DrawLine(cameraTrans.TransformPoint(right), cameraTrans.TransformPoint(left));
            Handles.DrawLine(cameraTrans.TransformPoint(up), cameraTrans.TransformPoint(down));

            Handles.color = previouseColor;
        }
        private void OnDisable()
        {
#if UNITY_2019
        SceneView.duringSceneGui -= OnSceneGUI;
#else
            SceneView.onSceneGUIDelegate += OnSceneGUI;
#endif
            Selection.selectionChanged -= OnSelectionChanged;
            ResumePreviouseSetting();
        }
    }
    public static class VectorEx
    {
        public static Vector3 DivideBy(this Vector3 vector, float value)
        {
            return new Vector3(value / vector.x, value / vector.y, value / vector.z);
        }
    }
    public static class StringEx
    {
        public static string Bold(this string contents) { return "<b>" + contents + "</b>"; }
        public static string ChangeColor(this string contents, Color color) { return "<color=#" + ColorUtility.ToHtmlStringRGBA(color) + ">" + contents + "</color>"; }
        public static string Size(this string content, int amount) { return "<size=" + amount.ToString() + ">" + content + "</size>"; }
        public static string PrefixStyle(this string content, int amount, Color color,bool bold) {
            string firstletter = content[0].ToString().ChangeColor(color).Size(amount);
            return (bold ? firstletter.Bold() : firstletter) + content.Remove(0,1);
        }
    }
    public static class BoundsEx
    {
        public static Vector3 Top(this Bounds bounds)
        {
            Vector3 result = bounds.center;
            result.y = bounds.max.y;
            return result;
        }
        public static Vector3 Buttom(this Bounds bounds)
        {
            Vector3 result = bounds.center;
            result.y = bounds.min.y;
            return result;
        }
        public static Vector3 Left(this Bounds bounds)
        {
            Vector3 result = bounds.center;
            result.x = bounds.min.x;
            return result;
        }
        public static Vector3 Right(this Bounds bounds)
        {
            Vector3 result = bounds.center;
            result.x = bounds.max.x;
            return result;
        }
        public static Vector3 Front(this Bounds bounds)
        {
            Vector3 result = bounds.center;
            result.z = bounds.max.z;
            return result;
        }
        public static Vector3 Back(this Bounds bounds)
        {
            Vector3 result = bounds.center;
            result.z = bounds.min.z;
            return result;
        }
    }
}
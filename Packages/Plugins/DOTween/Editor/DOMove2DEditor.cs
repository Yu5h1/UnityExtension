using UnityEngine;
using UnityEditor;
using Yu5h1Lib.EditorExtension;
using System.Runtime.CompilerServices;
using UnityEditorInternal;
using System;

namespace Yu5h1Lib
{
    [CustomEditor(typeof(DOMove2D)),CanEditMultipleObjects]
    public class DOMove2DInspector : Editor<DOMove2D>
    {
        private static bool _ShowAllHandles;
        public static bool ShowAllHandles
        {
            get => _ShowAllHandles;
            set {
                if (_ShowAllHandles == value)
                    return;
                _ShowAllHandles = value;
                if (value)
                    SceneView.duringSceneGui += OnSceneGUI;
                else
                    SceneView.duringSceneGui -= OnSceneGUI;
            }
        }
        private static void OnSceneGUI(SceneView view)
        {
            foreach (var obj in GameObject.FindObjectsOfType<DOMove2D>())
                Handle(obj, false);
        }

        [MenuItem("CONTEXT/DOMove2D/Show all handles")]
        private static void ShowAllHandlesToggle(MenuCommand command)
        {
            ShowAllHandles = !ShowAllHandles;
            Menu.SetChecked("CONTEXT/DOMove2D/Show all handles", ShowAllHandles);
        }

        [InitializeOnLoadMethod]
        static void Init()
        {
            if (_ShowAllHandles)
                SceneView.duringSceneGui += OnSceneGUI;
        }


        Transform transform => targetObject.transform;
        Collider2D collider;
        private Transform lastParent;
        private Vector3 lastPosition;

        private void UpdateCache(){
            lastParent = transform.parent;
            lastPosition = transform.position;
        }

        protected void OnEnable()
        {
            collider = targetObject.GetComponent<Collider2D>();
            UpdateCache();
            RegisterAdvancedMethods(this);
            
        }
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }
         
        private void OnSceneGUI()
        {
            if (!InternalEditorUtility.GetIsInspectorExpanded(target))
                return;
            Handle(targetObject);
        }
      
        protected override void HierarchyChanged()
        {
            if (lastParent != transform.parent || lastPosition != transform.position)
            {
                $"{targetObject.name} changed".print();
            }
            UpdateCache();
        }

        private static void Handle(DOMove2D target,bool editable = true)
        {
            var localMod = target.local && target.transform.parent != null;
            var endValue = localMod ? target.transform.parent.TransformPoint(target.endValue) : target.endValue;
            Handles.DrawDottedLine(target.transform.position, endValue, 3);

            Vector3 newEndValue = endValue;
            if (editable)
                newEndValue = Handles.Slider2D(
                    endValue,
                    Vector3.forward,
                    Vector3.right,
                    Vector3.up,
                    HandleUtility.GetHandleSize(endValue) * 0.1f,
                    Handles.DotHandleCap,
                    Event.current.alt ? 0.1f : 1f
                );

            if (!ShowAllHandles || !editable )
            switch (target.GetComponent<Collider2D>())
            {
                case BoxCollider2D boxCol2D:
                    var matrix = Handles.matrix;
                    Handles.matrix = Matrix4x4.TRS(endValue, target.transform.rotation, Vector3.one);
                    Handles.DrawWireCube(Vector3.zero, boxCol2D.size * boxCol2D.transform.lossyScale);
                    Handles.matrix = matrix;
                    break;
                }

            if (endValue != newEndValue)
            {
                Undo.RecordObject(target, "Move EndValue");
                target.endValue = localMod ? target.transform.parent.InverseTransformPoint(newEndValue) : newEndValue;
                EditorUtility.SetDirty(target);
            }
        }

        private void OnDisable()
        {
            UnregisterAdvancedMethods(this);
        }
    }
}
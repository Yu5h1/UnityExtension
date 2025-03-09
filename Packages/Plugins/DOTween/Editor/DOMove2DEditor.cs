using UnityEngine;
using UnityEditor;
using Yu5h1Lib.EditorExtension;
using System.Runtime.CompilerServices;
using UnityEditorInternal;
using System;
using System.Linq;
using UnityEditor.SceneManagement;

namespace Yu5h1Lib
{
    [CustomEditor(typeof(DOMove2D)),CanEditMultipleObjects]
    public class DOMove2DInspector : Editor<DOMove2D>
    {
        
        public static string ShowAllHandlesKey => typeof(DOMove2DInspector).FullName + "_ShowAllHandles";
        
        public static bool ShowAllHandles
        {
            get => EditorPrefs.GetBool(ShowAllHandlesKey,false);
            set {
                if (ShowAllHandles == value)
                    return;
                EditorPrefs.SetBool(ShowAllHandlesKey, value);
                RegisterEvent();
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
        static DragDropHandler dragdropHandler = new DragDropHandler();

        [InitializeOnLoadMethod]
        static void RegisterEvent()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            if (ShowAllHandles)
                SceneView.duringSceneGui += OnSceneGUI;

            SceneMouseListener.Enable = true;
            SceneMouseListener.Register();
            dragdropHandler.Register();
            dragdropHandler.defaultSet.DragStart    -= DefaultSet_DragStart;
            dragdropHandler.defaultSet.DragBegin    -= DefaultSet_DragBegin; ;
            dragdropHandler.defaultSet.Dragging     -= DefaultSet_Dragging;
            dragdropHandler.defaultSet.DragEnd      -= DefaultSet_DragEnd;

            dragdropHandler.defaultSet.DragVerification += DefaultSet_DragVerification;
            dragdropHandler.defaultSet.DragStart += DefaultSet_DragStart;
            dragdropHandler.defaultSet.DragBegin += DefaultSet_DragBegin; ;
            dragdropHandler.defaultSet.Dragging += DefaultSet_Dragging;
            dragdropHandler.defaultSet.DragEnd += DefaultSet_DragEnd;
        }

        private static bool DefaultSet_DragVerification(object arg1, Event e)
        {
            return e.button == 0;
        }

        private static void DefaultSet_DragStart(object arg1, Event arg2)
        {

        }
        private static void DefaultSet_DragBegin(object arg1, Event arg2)
        {

        }
 
        private static void DefaultSet_Dragging(object arg1, Event arg2)
        {

        }
        private static void DefaultSet_DragEnd(object arg1, Event arg2)
        {

        }
        Transform transform => targetObject.transform;
        Collider2D collider;
  

        protected void OnEnable()
        {
            collider = targetObject.GetComponent<Collider2D>();
            
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

        private static void Handle(DOMove2D target,bool editable = true)
        {
            if (PrefabUtility.IsPartOfPrefabAsset(target.gameObject))
                return;
            var isPlaying = Application.isPlaying;
            var endValue = isPlaying ? target.endValue : 
                                       target.transform.TransformPoint(target.endValue);
            Handles.DrawDottedLine(target.transform.position, endValue, 3);

            
            if (!isPlaying && editable && endValue.Slider2D(out Vector3 newEndValue))
            {
                Undo.RecordObject(target, "Move EndValue");
                target.endValue = target.transform.InverseTransformPoint(newEndValue);
                //EditorUtility.SetDirty(target);
            }

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
        }

        private void OnDisable()
        {
        }
    }
}
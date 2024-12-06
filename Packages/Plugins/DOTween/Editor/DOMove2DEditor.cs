using UnityEngine;
using UnityEditor;
using Yu5h1Lib.EditorExtension;
using System.Runtime.CompilerServices;

namespace Yu5h1Lib
{
    [CustomEditor(typeof(DOMove2D)),CanEditMultipleObjects]
    public class DOMove2DInspector : Editor<DOMove2D>
    {
        Collider2D collider;
        private void OnEnable()
        {
            collider = targetObject.GetComponent<Collider2D>();
        }
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }
         
        private void OnSceneGUI()
        {
            Handles.DrawLine(targetObject.transform.position, targetObject.endValue);
            

            var newValue = Handles.PositionHandle(targetObject.endValue, Quaternion.identity);
            //if (Event.current.shift)
            //    newValue.y = targetObject.endValue.y;

            switch (collider)
            {
                case BoxCollider2D boxCol2D:
                    var matrix = Handles.matrix;
                    Handles.matrix = Matrix4x4.TRS(targetObject.endValue, targetObject.transform.rotation, Vector3.one);
                    Handles.DrawWireCube(Vector3.zero, boxCol2D.size * boxCol2D.transform.lossyScale);
                    Handles.matrix = matrix;
                    break;
            }

            if (GUI.changed)
            {
                Undo.RecordObject(targetObject, "Move EndValue");

                if (newValue != targetObject.endValue)
                    targetObject.endValue = newValue; 

                EditorUtility.SetDirty(targetObject);
            }
            
        }
    }
}
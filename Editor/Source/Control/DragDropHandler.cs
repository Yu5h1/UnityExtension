using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    public class DragDropHandler : CursorControllerBase<Event, MouseCursor, Vector2>
    {
        public override Vector2 DefaultVector => Vector2.zero;
        public override float Delta => Vector2.SignedAngle(Orientation, Direction) / 180f;

        public override Vector2 Add(Vector2 a, Vector2 b) => a + b;
        public override Vector2 Subtract(Vector2 a, Vector2 b) => a - b;
        public override Vector2 Multiply(Vector2 a, Vector2 b) => a * b;
        public override Vector2 Divide(Vector2 a, Vector2 b) => a / b;

        public override Vector2 GetNormal(Vector2 val) => val.normalized;
        public override float GetLength(Vector2 val) => val.magnitude;

        protected override Vector2 GetMouseLocation(object s, Event e) => e.mousePosition;
        protected override int GetButtonIndex(object s, Event e) => e.button;

        public override void ChangeCursor(object sender, MouseCursor cursor)
            => EditorGUIUtility.AddCursorRect(SceneView.currentDrawingSceneView.position, cursor);

        public void Register(){
            Unregister();
            SceneMouseListener.MouseDown += MouseDown;
            SceneMouseListener.MouseDrag += MouseMove;
            SceneMouseListener.MouseUp += MouseUp;
        }
        public void Unregister() {
            SceneMouseListener.MouseDown -= MouseDown;
            SceneMouseListener.MouseDrag -= MouseMove;
            SceneMouseListener.MouseUp   -= MouseUp;
        }
    }

}

using UnityEngine;
using UnityEditor;

namespace Yu5h1Lib.EditorExtension
{
    public static class HandlesEx
    {
        public static GUIStyle LabelStyle = new GUIStyle();
        public static void DrawLabel(Color color, Vector3 position, GUIContent content)
        {
            LabelStyle.normal.textColor = color;
            Handles.Label(position, content, LabelStyle);
        }

        public static void DrawLabel(Color color, Vector3 position, string text) {
            DrawLabel(color, position, new GUIContent(text)); }

        public static bool Slider2D(this Vector3 current,out Vector3 newValue,
            float sizeMultiplier = 0.1f)
        {
            EditorGUI.BeginChangeCheck();
            newValue =  Handles.Slider2D(
                             current,
                             Vector3.forward,
                             Vector3.right,
                             Vector3.up,
                             HandleUtility.GetHandleSize(current) * sizeMultiplier,
                             Handles.DotHandleCap,
                             Event.current.shift ? 1f : 0.5f
                        );
            if(EditorGUI.EndChangeCheck() && current != newValue)
            {
                return true;
            }
            return false;
        }
        public static bool Slider2D(this Vector2 current, out Vector2 newValue, float sizeMultiplier = 0.1f)
        {
            newValue = default;
            if (((Vector3)current).Slider2D(out Vector3 result))
            {
                newValue = result;
                return true;
            }
            return false;
        }
        
    } 
}

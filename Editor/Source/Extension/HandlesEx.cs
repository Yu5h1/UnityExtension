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
    } 
}

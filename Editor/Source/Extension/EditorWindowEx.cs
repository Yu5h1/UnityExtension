using System.ComponentModel;
using UnityEngine;

namespace UnityEditor
{
    [EditorBrowsable(EditorBrowsableState.Never), Browsable(false)]
    public static class EditorWindowEx
    {
        public static void SetRect(this EditorWindow window, Vector2 pos, Vector2 size)
            => window.position = new Rect(pos.x, pos.y, size.x, size.y);
        public static void Center(this EditorWindow window)
        {
            var size = window.position.size;
            window.SetRect(EditorGUIUtility.GetMainWindowPosition().center - (size * 0.5f), size);
        }
        public static void Resize(this EditorWindow window, Vector2 size)
            => window.SetRect(window.position.position, size);
        public static void SetLocation(this EditorWindow window, Vector2 pos)
            => window.SetRect(pos, window.position.size);
    }
}

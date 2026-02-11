using System.ComponentModel;
using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    [EditorBrowsable(EditorBrowsableState.Never), Browsable(false)]
    public static class PopupWindowContentEx
    {
        public static void Show(this PopupWindowContent content,Rect rect)
        {
            PopupWindow.Show(Rect.zero, content);
            content.editorWindow.position = rect;
        }
    }
}

using System.ComponentModel;
using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    [EditorBrowsable(EditorBrowsableState.Never), Browsable(false)]
    public static class PopupWindowContentEx
    {
        public static void Show(this PopupWindowContent content,Rect rect,bool setWindowPosition = true)
        {
            PopupWindow.Show(rect, content);
            if (setWindowPosition)
                content.editorWindow.position = rect;
        }
    }
}

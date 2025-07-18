using System.ComponentModel;
using UnityEngine;
using Yu5h1Lib.Runtime;

namespace Yu5h1Lib.UI
{
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
    public static class TextAnchorEx
    {
        public static Alignment ToAlignment(this TextAnchor anchor) => anchor switch
        {
            TextAnchor.UpperLeft => Alignment.TopLeft,
            TextAnchor.UpperCenter => Alignment.Top,
            TextAnchor.UpperRight => Alignment.TopRight,
            TextAnchor.MiddleLeft => Alignment.Left,
            TextAnchor.MiddleCenter => Alignment.Center,
            TextAnchor.MiddleRight => Alignment.Right,
            TextAnchor.LowerLeft => Alignment.BottomLeft,
            TextAnchor.LowerCenter => Alignment.Bottom,
            TextAnchor.LowerRight => Alignment.BottomRight,
            _ => Alignment.Center
        };
        public static TextAnchor ToTextAnchor(this Alignment anchor) => anchor switch
        {
            Alignment.TopLeft => TextAnchor.UpperLeft,
            Alignment.Top => TextAnchor.UpperCenter,
            Alignment.TopRight => TextAnchor.UpperRight,
            Alignment.Left => TextAnchor.MiddleLeft,
            Alignment.Center => TextAnchor.MiddleCenter,
            Alignment.Right => TextAnchor.MiddleRight,
            Alignment.BottomLeft => TextAnchor.LowerLeft,
            Alignment.Bottom => TextAnchor.LowerCenter,
            Alignment.BottomRight => TextAnchor.LowerRight,
            _ => TextAnchor.MiddleCenter
        };
    } 
}

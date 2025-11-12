using UnityEngine;
using System;

namespace Yu5h1Lib.Runtime
{
    [Flags]
    public enum Alignment
    {
        Center = 0,
        Top             = Direction2D.up,
        Bottom          = Direction2D.down,
        Left            = Direction2D.left,
        Right           = Direction2D.right,


        TopLeft        = Top | Left,
        TopRight       = Top | Right,

        BottomLeft     = Bottom | Left,
        BottomRight    = Bottom | Right,

        /// <summary>Stretch vertically (Top + Bottom)</summary>
        Vertical = Top | Bottom,
        /// <summary>Stretch Horizontally (Top + Bottom)</summary>
        Horizontal = Left | Right,

        VerticalLeft = Top | Bottom | Left,
        VerticalRight = Top | Bottom | Right,

        HorizontalTop = Left | Right | Top,
        HorizontalBottom = Left | Right | Bottom,

        Fill = Vertical | Horizontal,

    }
    public static class AlignmentMethods
    {
        public static Rect CenterScreen(Rect rect) {
            

            rect.x = rect.x += Screen.width * 0.5f - rect.width * 0.5f;
            rect.y = rect.y += Screen.height * 0.5f - rect.height * 0.5f; ;
            return rect;
        }
        public static Rect AligScreen(this Rect rect, Alignment alignment)
        {
            if (alignment == (Alignment.Top | Alignment.Left)) return rect;
            if (alignment.HasAnyFlags(Alignment.Center,Alignment.Fill)) return CenterScreen(rect);

            if (alignment.HasFlag(Alignment.Right))
            {
                if (alignment.HasFlag(Alignment.Left)) rect.x += Screen.width * 0.5f - rect.width * 0.5f;
                else rect.x = (Screen.width - rect.width) - rect.x;                
            }else if (!alignment.HasFlag(Alignment.Left)) {
                rect.x += Screen.width * 0.5f - rect.width * 0.5f;
            }
            if (alignment.HasFlag(Alignment.Bottom)) {
                if (alignment.HasFlag(Alignment.Top)) rect.y += Screen.height * 0.5f - rect.height * 0.5f;
                else rect.y = (Screen.height - rect.height) - rect.y;
            }else if (!alignment.HasFlag(Alignment.Top))
            {
                rect.y += Screen.height * 0.5f - rect.height * 0.5f;
            }
            return rect;
        }
    }
}

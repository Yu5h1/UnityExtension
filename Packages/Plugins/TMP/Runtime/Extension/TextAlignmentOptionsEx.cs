using System.ComponentModel;
using TMPro;
using Yu5h1Lib.Runtime;

namespace Yu5h1Lib.UI
{
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
    public static class TextAlignmentOptionsEx
    {
        public static TextAlignmentOptions ToAlignmentOption(this Alignment alignment)
        {
            return alignment switch
            {
                Alignment.TopLeft => TextAlignmentOptions.TopLeft,
                Alignment.Top => TextAlignmentOptions.Top,
                Alignment.TopRight => TextAlignmentOptions.TopRight,
                Alignment.Left => TextAlignmentOptions.Left,
                Alignment.Center => TextAlignmentOptions.Center,
                Alignment.Right => TextAlignmentOptions.Right,
                Alignment.BottomLeft => TextAlignmentOptions.BottomLeft,
                Alignment.Bottom => TextAlignmentOptions.Bottom,
                Alignment.BottomRight => TextAlignmentOptions.BottomRight,
                Alignment.Fill => TextAlignmentOptions.Justified,
                _ => TextAlignmentOptions.Center 
            };
        }

        public static Alignment ToAlignment(this TextAlignmentOptions tmp)
        {
            return tmp switch
            {
                TextAlignmentOptions.TopLeft => Alignment.TopLeft,
                TextAlignmentOptions.Top => Alignment.Top,
                TextAlignmentOptions.TopRight => Alignment.TopRight,
                TextAlignmentOptions.Left => Alignment.Left,
                TextAlignmentOptions.Center => Alignment.Center,
                TextAlignmentOptions.Right => Alignment.Right,
                TextAlignmentOptions.BottomLeft => Alignment.BottomLeft,
                TextAlignmentOptions.Bottom => Alignment.Bottom,
                TextAlignmentOptions.BottomRight => Alignment.BottomRight,
                TextAlignmentOptions.Justified => Alignment.Fill,
                _ => Alignment.Center 
            };
        }
    }
}
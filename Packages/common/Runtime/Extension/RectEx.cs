using System.ComponentModel;
using UnityEngine;

namespace Yu5h1Lib
{
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
    public static class RectEx
    {
        /// <summary>Every component is finite. Says nothing about size, so a zero-area rect still passes.</summary>
        public static bool IsFinite(this Rect value)
            => float.IsFinite(value.x) && float.IsFinite(value.y)
            && float.IsFinite(value.width) && float.IsFinite(value.height);

        /// <summary>
        /// Finite <em>and</em> big enough to divide by. Use this before normalising a point against a rect;
        /// use <see cref="IsFinite(Rect)"/> when a caller checks the size itself.
        /// </summary>
        public static bool IsValid(this Rect value)
            => value.IsFinite() && value.max.IsFinite() && value.width > 0 && value.height > 0;
    }
}

using System.ComponentModel;
using UnityEngine;

namespace Yu5h1Lib
{
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
    public static class Vector3Ex
    {
        /// <inheritdoc cref="Vector2Ex.IsFinite(Vector2)"/>
        public static bool IsFinite(this Vector3 value)
            => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}

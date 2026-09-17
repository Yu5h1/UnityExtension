using System.ComponentModel;
using UnityEngine;

namespace Yu5h1Lib
{
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
    public static class NumericEx
    {
        public static float GetNormal(this float val, float max, float zeroMaxResult = 0) => max == 0 ? zeroMaxResult : val / max;

        public static float Distance(this float a, float b) => Mathf.Abs(b - a);

        /// <summary>Neither NaN nor infinite. Guards arithmetic fed by pointer deltas, camera state or layout.</summary>
        public static bool IsFinite(this float value) => float.IsFinite(value);
    }
}

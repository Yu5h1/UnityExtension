using System.ComponentModel;
using UnityEngine;

namespace Yu5h1Lib.Runtime
{
    [EditorBrowsable(EditorBrowsableState.Never), Browsable(false)]
    public static class MinMaxEx
    {
        public static void Lerp(this MinMax minMax,float t) => Mathf.Lerp(minMax.Min, minMax.Max,t);
    }
}

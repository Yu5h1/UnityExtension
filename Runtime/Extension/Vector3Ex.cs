using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never), System.ComponentModel.Browsable(false)]
    public static class Vector3Ex
    {
        public static Vector3 GetCenter(this IEnumerable<Vector3> values)
        {
            Vector3 total = Vector3.zero;
            int count = 0;
            foreach (var val in values)
            {
                total += val;
                count++;
            }
            return total / count;
        }
        public static bool IsNaN(this Vector3 v)
                => float.IsNaN(v.x) && float.IsNaN(v.y) && float.IsNaN(v.z);
        public static bool SequenceEqual(this Vector3[] a, Vector3[] b)
        {
            if (a == b) return true;
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                    return false;
            }
            return true;
        }
    }
}

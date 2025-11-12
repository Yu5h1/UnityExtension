using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Yu5h1Lib
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never), System.ComponentModel.Browsable(false)]
    public static class Vector2Ex
    {
        public static bool IsNaN(this Vector2 v) => float.IsNaN(v.x) && float.IsNaN(v.y);
        public static bool HasNegative(this Vector2 v) => v.x < 0 || v.y < 0;
        public static Vector2 GetCenter(this IEnumerable<Vector2> values)
        {
            Vector2 total = Vector2.zero;
            
            int count = 0;
            foreach (var val in values)
            {
                total += val;
                count ++;
            }
            return total / count;
        }
        public static bool SequenceEqual(this Vector2[] a, Vector2[] b)
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

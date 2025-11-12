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

        public static Vector3 Set(this Vector3 v, float? x = null, float? y = null, float? z = null)
        {
            if (x == null && y == null && z == null)
            {
                throw new System.ArgumentException("At least one axis must be set.");
            }
            return new Vector3(
                x.HasValue ? x.Value : v.x,
                y.HasValue ? y.Value : v.y,
                z.HasValue ? z.Value : v.z
            );
        }

        public static Vector3 Add(this Vector3 v, float? x = null, float? y = null, float? z = null)
        {
            return new Vector3(
                v.x + (x ?? 0f),
                v.y + (y ?? 0f),
                v.z + (z ?? 0f)
            );
        }
    }
}

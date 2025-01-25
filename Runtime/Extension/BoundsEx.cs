using System.ComponentModel;
using UnityEngine;

namespace Yu5h1Lib.Runtime
{
    [EditorBrowsable(EditorBrowsableState.Never), Browsable(false)]
    public static class BoundsEx
    {
        public static Vector3 Top(this Bounds bounds)
        {
            Vector3 result = bounds.center;
            result.y = bounds.max.y;            
            return result;
        }
        public static Vector3 Buttom(this Bounds bounds)
        {
            Vector3 result = bounds.center;
            result.y = bounds.min.y;
            return result;
        }
        public static Vector3 Left(this Bounds bounds)
        {
            Vector3 result = bounds.center;
            result.x = bounds.min.x;
            return result;
        }
        public static Vector3 Right(this Bounds bounds)
        {
            Vector3 result = bounds.center;
            result.x = bounds.max.x;
            return result;
        }
        public static Vector3 Front(this Bounds bounds)
        {
            Vector3 result = bounds.center;
            result.z = bounds.max.z;
            return result;
        }
        public static Vector3 Back(this Bounds bounds)
        {
            Vector3 result = bounds.center;
            result.z = bounds.min.z;
            return result;
        }
        public static Vector3[] GetPoints(this Bounds bounds)
        {
            var min = bounds.min;
            var max = bounds.max;
            return new Vector3[] {
            min,
            new Vector3(min.x, min.y, max.z),
            new Vector3(min.x, max.y, min.z),
            new Vector3(max.x, min.y, min.z),
            new Vector3(min.x, max.y, max.z),
            new Vector3(max.x, min.y, max.z),
            max
        };
        }
    } 
}

using UnityEngine;

namespace Yu5h1Lib.Runtime
{
    public static class Gizmo2D
    {
        public static void DrawBox(Vector2 center, Vector2 size, float angle)
        {
            var oldMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.Euler(0, 0, angle), Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, size);
            Gizmos.matrix = oldMatrix;
        }
        //public static void DrawCapsule2D(Vector2 center, Vector2 size, CapsuleDirection2D direction, float angle)
        //{
        //    if (direction == CapsuleDirection2D.Horizontal)
        //    {
        //        size.Set(size.y, size.x);
        //    }
        //    var radius = size.y / 2;
        //    var boxHeight = size.x - 2 * radius;

        //    DrawBox2D(center, size, angle);

        //    Vector2 offset = new Vector2(boxHeight / 2, 0);
        //    offset = Quaternion.Euler(0, 0, angle) * offset;

        //    Vector2 p1 = center + offset,
        //            p2 = center - offset;

        //    Gizmos.DrawWireSphere(p1, radius);
        //    Gizmos.DrawWireSphere(p2, radius);
        //}
    }
}

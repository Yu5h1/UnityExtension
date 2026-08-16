using UnityEngine;

namespace Yu5h1Lib
{
    // Draws whatever an IShapeProvider describes.
    //
    // Shared so a provider and its consumer cannot disagree about where the
    // volume is. A consumer that draws its own outline from its own Transform
    // is drawing a lie the moment a provider on another GameObject supplies the
    // geometry, and that lie is silent.
    public static class ShapeGizmos
    {
        const int RingSegments = 24;

        public static void Draw(
            IShapeProvider shape,
            Color color)
        {
            if (shape == null || !shape.IsUsable)
                return;

            Matrix4x4 previous = Gizmos.matrix;
            Color previousColor = Gizmos.color;

            Gizmos.matrix = Matrix4x4.TRS(
                shape.Center,
                shape.Rotation,
                Vector3.one);
            Gizmos.color = color;

            Vector3 size = shape.Size;
            switch (shape.Kind)
            {
                case ShapeKind.Box:
                    Gizmos.DrawWireCube(Vector3.zero, size);
                    break;
                case ShapeKind.Sphere:
                    DrawEllipsoid(size);
                    break;
                case ShapeKind.Cylinder:
                    DrawTapered(
                        size.z,
                        0.5f * size.x,
                        0.5f * size.x,
                        0.5f * size.x,
                        0.5f * size.x);
                    break;
                case ShapeKind.Cone:
                    DrawTapered(
                        size.z,
                        0.5f * size.y,
                        0.5f * size.y,
                        0.5f * size.x,
                        0.5f * size.x);
                    break;
            }

            Gizmos.matrix = previous;
            Gizmos.color = previousColor;
        }

        // Three orthogonal rings rather than DrawWireSphere, which cannot take
        // a non-uniform radius and would draw a sphere where the volume is an
        // ellipsoid.
        static void DrawEllipsoid(Vector3 size)
        {
            Vector3 r = 0.5f * size;
            DrawRing(Vector3.zero, r.x, r.z, 0);
            DrawRing(Vector3.zero, r.x, r.y, 1);
            DrawRing(Vector3.zero, r.z, r.y, 2);
        }

        // Along +Z, one ring per end plus four struts. A cone is the case where
        // the near radii are zero, so it needs no separate path.
        static void DrawTapered(
            float length,
            float nearX,
            float nearY,
            float farX,
            float farY)
        {
            float half = 0.5f * length;
            Vector3 near = new Vector3(0f, 0f, -half);
            Vector3 far = new Vector3(0f, 0f, half);

            DrawRing(near, nearX, nearY, 1);
            DrawRing(far, farX, farY, 1);

            for (int i = 0; i < 4; i++)
            {
                float angle = i * Mathf.PI * 0.5f;
                float c = Mathf.Cos(angle);
                float s = Mathf.Sin(angle);
                Gizmos.DrawLine(
                    near + new Vector3(
                        c * nearX, s * nearY, 0f),
                    far + new Vector3(
                        c * farX, s * farY, 0f));
            }
        }

        // plane 0 = XZ, 1 = XY, 2 = ZY.
        static void DrawRing(
            Vector3 center,
            float radiusA,
            float radiusB,
            int plane)
        {
            Vector3 previous = RingPoint(
                center, radiusA, radiusB, plane, 0f);

            for (int i = 1; i <= RingSegments; i++)
            {
                float t =
                    i / (float)RingSegments * Mathf.PI * 2f;
                Vector3 point = RingPoint(
                    center, radiusA, radiusB, plane, t);
                Gizmos.DrawLine(previous, point);
                previous = point;
            }
        }

        static Vector3 RingPoint(
            Vector3 center,
            float radiusA,
            float radiusB,
            int plane,
            float angle)
        {
            float a = Mathf.Cos(angle) * radiusA;
            float b = Mathf.Sin(angle) * radiusB;

            switch (plane)
            {
                case 1:
                    return center + new Vector3(a, b, 0f);
                case 2:
                    return center + new Vector3(0f, b, a);
                default:
                    return center + new Vector3(a, 0f, b);
            }
        }
    }
}

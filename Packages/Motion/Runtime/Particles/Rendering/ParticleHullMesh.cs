using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   Builds the flat-shaded convex hull mesh of an entity's rest particles (ParticleRendering
    ///   REND-06), after Unified-Solver's SolverHullMesh: every vertex is pushed away from the
    ///   centroid by its particle radius so the surface reads where the particle spheres collide,
    ///   and every triangle has three vertices of its own with the facet normal so the facets stay
    ///   sharp. The faces come from the points themselves, so a layout variant or a shape-source
    ///   template needs no face table: a tetrahedron gives 4 triangles, an octahedron 8, a cube 12
    ///   (coplanar points are merged into one polygon and fanned). The mesh is in the entity's local
    ///   frame, the frame of the rest positions, and is built once per variant.
    /// </summary>
    public static class ParticleHullMesh
    {
        /// <summary>
        ///   Polygons of the hull, each a list of point indices ordered so that triangles fanned from
        ///   the first index face outward in Unity's winding. Null when there are fewer than four
        ///   points or they all lie in one plane.
        /// </summary>
        public static List<int[]> Faces(IReadOnlyList<Vector3> points)
        {
            int count = points.Count;
            if (count < 4)
                return null;
            Vector3 centroid = Vector3.zero;
            for (int i = 0; i < count; i++)
                centroid += points[i];
            centroid /= count;
            float scale = 0f;
            for (int i = 0; i < count; i++)
                scale = Mathf.Max(scale, (points[i] - centroid).magnitude);
            if (scale <= 1e-9f)
                return null;
            float epsilon = 1e-4f * scale;
            float minimumArea = 1e-6f * scale * scale;

            var faces = new List<int[]>();
            var seen = new HashSet<string>();
            var onPlane = new List<int>();
            for (int i = 0; i < count - 2; i++)
            {
                for (int j = i + 1; j < count - 1; j++)
                {
                    for (int k = j + 1; k < count; k++)
                    {
                        Vector3 normal = Vector3.Cross(points[j] - points[i], points[k] - points[i]);
                        float length = normal.magnitude;
                        if (length <= minimumArea)
                            continue;
                        normal /= length;
                        float offset = Vector3.Dot(normal, points[i]);
                        bool above = false;
                        bool below = false;
                        for (int q = 0; q < count && !(above && below); q++)
                        {
                            float side = Vector3.Dot(normal, points[q]) - offset;
                            above |= side > epsilon;
                            below |= side < -epsilon;
                        }
                        if (above && below)
                            continue;
                        if (!above && !below)
                            return null;
                        if (above)
                        {
                            normal = -normal;
                            offset = -offset;
                        }
                        onPlane.Clear();
                        for (int q = 0; q < count; q++)
                            if (Mathf.Abs(Vector3.Dot(normal, points[q]) - offset) <= epsilon)
                                onPlane.Add(q);
                        if (!seen.Add(string.Join(",", onPlane)))
                            continue;
                        int[] polygon = OrderPolygon(points, onPlane, normal);
                        if (polygon.Length >= 3)
                            faces.Add(polygon);
                    }
                }
            }
            return faces.Count >= 4 ? faces : null;
        }

        public static int TriangleCount(List<int[]> faces)
        {
            int total = 0;
            foreach (int[] polygon in faces)
                total += polygon.Length - 2;
            return total;
        }

        /// <summary>
        ///   The hull mesh of <paramref name="points"/> with each vertex pushed outward by its
        ///   entry of <paramref name="radii"/>, or null when the points have no hull.
        /// </summary>
        public static Mesh Build(IReadOnlyList<Vector3> points, IReadOnlyList<float> radii)
        {
            List<int[]> faces = Faces(points);
            if (faces == null)
                return null;
            int count = points.Count;
            Vector3 centroid = Vector3.zero;
            for (int i = 0; i < count; i++)
                centroid += points[i];
            centroid /= count;

            var inflated = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                Vector3 radial = points[i] - centroid;
                float length = radial.magnitude;
                float radius = i < radii.Count ? radii[i] : 0f;
                inflated[i] = length > 1e-6f ? centroid + radial * ((length + radius) / length) : points[i];
            }

            int corners = TriangleCount(faces) * 3;
            var positions = new Vector3[corners];
            var normals = new Vector3[corners];
            var uvs = new Vector2[corners];
            var triangles = new int[corners];
            int vertex = 0;
            foreach (int[] polygon in faces)
            {
                for (int t = 1; t < polygon.Length - 1; t++)
                {
                    Vector3 a = inflated[polygon[0]];
                    Vector3 b = inflated[polygon[t]];
                    Vector3 c = inflated[polygon[t + 1]];
                    Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
                    Write(vertex++, a, normal, 0);
                    Write(vertex++, b, normal, 1);
                    Write(vertex++, c, normal, 2);
                }
            }

            void Write(int index, Vector3 position, Vector3 normal, int corner)
            {
                positions[index] = position;
                normals[index] = normal;
                uvs[index] = corner == 0 ? new Vector2(0f, 0f) : (corner == 1 ? new Vector2(1f, 0f) : new Vector2(0.5f, 1f));
                triangles[index] = index;
            }

            var mesh = new Mesh
            {
                name = $"ParticleHull_{count}",
                hideFlags = HideFlags.HideAndDontSave,
                vertices = positions,
                normals = normals,
                uv = uvs,
                triangles = triangles
            };
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        static int[] OrderPolygon(IReadOnlyList<Vector3> points, List<int> indices, Vector3 normal)
        {
            Vector3 reference = Mathf.Abs(normal.x) < 0.9f ? Vector3.right : Vector3.up;
            Vector3 u = Vector3.Cross(normal, reference).normalized;
            Vector3 v = Vector3.Cross(normal, u);
            Vector3 origin = points[indices[0]];
            var projected = new List<(Vector2 position, int index)>(indices.Count);
            foreach (int index in indices)
            {
                Vector3 relative = points[index] - origin;
                projected.Add((new Vector2(Vector3.Dot(relative, u), Vector3.Dot(relative, v)), index));
            }
            projected.Sort((a, b) => a.position.x != b.position.x
                ? a.position.x.CompareTo(b.position.x)
                : a.position.y.CompareTo(b.position.y));

            var hull = new List<(Vector2 position, int index)>(projected.Count * 2);
            for (int i = 0; i < projected.Count; i++)
            {
                while (hull.Count >= 2 && Cross(hull[hull.Count - 2].position, hull[hull.Count - 1].position, projected[i].position) <= 0f)
                    hull.RemoveAt(hull.Count - 1);
                hull.Add(projected[i]);
            }
            int lower = hull.Count + 1;
            for (int i = projected.Count - 2; i >= 0; i--)
            {
                while (hull.Count >= lower && Cross(hull[hull.Count - 2].position, hull[hull.Count - 1].position, projected[i].position) <= 0f)
                    hull.RemoveAt(hull.Count - 1);
                hull.Add(projected[i]);
            }
            hull.RemoveAt(hull.Count - 1);

            var result = new int[hull.Count];
            for (int i = 0; i < result.Length; i++)
                result[i] = hull[i].index;
            return result;
        }

        static float Cross(Vector2 o, Vector2 a, Vector2 b) =>
            (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
    }
}

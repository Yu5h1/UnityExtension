using UnityEngine;

namespace Yu5h1.UnifiedSolver
{
    // Index carrier for hull rendering: one mesh per rigid cluster variant,
    // holding no geometry of its own.
    //
    // Every vertex position is read from the solver's rest offsets at draw time,
    // so what the mesh actually supplies is the face list, expanded so that no
    // vertex is shared between two faces. Sharing them would force the corners
    // to interpolate a single normal and round off the facets, and ice is
    // supposed to read as flat and sharp.
    //
    // The three corner indices of a vertex's face, its own first, travel in UV1
    // rather than in NORMAL. Both channels are free here, but a normal is
    // semantically a direction and mesh tooling is entitled to renormalise one;
    // nothing ever rewrites a UV.
    public static class SolverHullMesh
    {
        public static Mesh Build(
            SolverParticleTopology topology)
        {
            int[] faces =
                SolverHullShapes.Faces(topology);
            Vector3[] baseVertices =
                SolverHullShapes.BaseVertices(topology);
            if (faces == null || baseVertices == null)
                return null;

            int count = faces.Length;
            var positions = new Vector3[count];
            var cornerIndices = new Vector3[count];
            var uv = new Vector2[count];
            var triangles = new int[count];

            for (int i = 0; i < count; i += 3)
            {
                int a = faces[i];
                int b = faces[i + 1];
                int c = faces[i + 2];

                // Each of the three gets the same face, rotated so its own
                // corner is first. The winding is preserved by rotating rather
                // than reordering, which is what keeps the computed facet normal
                // pointing outward for all three.
                Write(i, a, b, c);
                Write(i + 1, b, c, a);
                Write(i + 2, c, a, b);
            }

            void Write(
                int vertex,
                int own,
                int next,
                int last)
            {
                // A real position so the mesh has usable bounds in the editor
                // and nothing downstream sees a zero-size mesh. The shader
                // ignores it.
                positions[vertex] = baseVertices[own];
                cornerIndices[vertex] = new Vector3(
                    own,
                    next,
                    last);
                uv[vertex] = FaceUV(vertex % 3);
                triangles[vertex] = vertex;
            }

            var mesh = new Mesh
            {
                name =
                    $"SolverHull_{topology}",
                vertices = positions,
                normals = positions,
                uv = uv,
                triangles = triangles
            };
            mesh.SetUVs(1, cornerIndices);
            mesh.RecalculateBounds();
            return mesh;
        }

        // Flat triangle mapping. A fragment has no meaningful surface
        // parameterisation of its own, and stretching one texture over each
        // facet is what reads as ice rather than as a painted solid.
        static Vector2 FaceUV(int corner)
        {
            switch (corner)
            {
                case 0:
                    return new Vector2(0f, 0f);
                case 1:
                    return new Vector2(1f, 0f);
                default:
                    return new Vector2(0.5f, 1f);
            }
        }
    }
}

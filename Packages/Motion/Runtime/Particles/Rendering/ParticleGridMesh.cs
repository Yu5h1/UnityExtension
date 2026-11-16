using UnityEngine;
using UnityEngine.Rendering;

namespace Yu5h1Lib
{
    /// <summary>
    ///   Builds the mesh of a grid surface (ParticleRendering REND-03), ported from the mesh of
    ///   Unified-Solver's ClothGenerator: one vertex per particle in row-major order (particle
    ///   index = row × columns + column), UV = (column, row) normalized to 0..1, and per cell two
    ///   triangles bl-tl-tr and bl-tr-br, with the reversed pair added when double sided. Vertex
    ///   positions are placeholders; the shader reads the particles.
    /// </summary>
    public static class ParticleGridMesh
    {
        public static int IndexCount(int columns, int rows, bool doubleSided) =>
            (columns - 1) * (rows - 1) * (doubleSided ? 4 : 2) * 3;

        public static Mesh Build(int columns, int rows, bool doubleSided)
        {
            columns = Mathf.Max(2, columns);
            rows = Mathf.Max(2, rows);
            int vertexCount = columns * rows;
            var vertices = new Vector3[vertexCount];
            var uvs = new Vector2[vertexCount];
            for (int y = 0; y < rows; y++)
                for (int x = 0; x < columns; x++)
                    uvs[y * columns + x] = new Vector2((float)x / (columns - 1), (float)y / (rows - 1));

            var triangles = new int[IndexCount(columns, rows, doubleSided)];
            int t = 0;
            for (int y = 0; y < rows - 1; y++)
            {
                for (int x = 0; x < columns - 1; x++)
                {
                    int bl = y * columns + x;
                    int br = bl + 1;
                    int tl = bl + columns;
                    int tr = tl + 1;
                    triangles[t++] = bl; triangles[t++] = tl; triangles[t++] = tr;
                    triangles[t++] = bl; triangles[t++] = tr; triangles[t++] = br;
                    if (!doubleSided)
                        continue;
                    triangles[t++] = bl; triangles[t++] = tr; triangles[t++] = tl;
                    triangles[t++] = bl; triangles[t++] = br; triangles[t++] = tr;
                }
            }

            var mesh = new Mesh
            {
                name = $"ParticleGrid_{columns}x{rows}",
                hideFlags = HideFlags.HideAndDontSave
            };
            if (vertexCount > 65535)
                mesh.indexFormat = IndexFormat.UInt32;
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one);
            return mesh;
        }
    }
}

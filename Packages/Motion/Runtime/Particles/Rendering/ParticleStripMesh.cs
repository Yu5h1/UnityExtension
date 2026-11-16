using UnityEngine;
using UnityEngine.Rendering;

namespace Yu5h1Lib
{
    /// <summary>
    ///   Builds the mesh of a chain strip (ParticleRendering REND-04), ported from the mesh of the
    ///   third-party RopeGenerator: two vertices per particle, left (even) and right (odd), UV =
    ///   (0 or 1, along the chain 0..1), and per segment the front and back triangle pairs. Vertex
    ///   positions are placeholders; the shader reads the particles.
    /// </summary>
    public static class ParticleStripMesh
    {
        public static int IndexCount(int particles) => (particles - 1) * 4 * 3;

        public static Mesh Build(int particles)
        {
            particles = Mathf.Max(2, particles);
            int vertexCount = particles * 2;
            var vertices = new Vector3[vertexCount];
            var uvs = new Vector2[vertexCount];
            for (int s = 0; s < particles; s++)
            {
                float v = (float)s / (particles - 1);
                uvs[s * 2] = new Vector2(0f, v);
                uvs[s * 2 + 1] = new Vector2(1f, v);
            }

            var triangles = new int[IndexCount(particles)];
            int t = 0;
            for (int s = 0; s < particles - 1; s++)
            {
                int bl = s * 2;
                int br = s * 2 + 1;
                int tl = (s + 1) * 2;
                int tr = (s + 1) * 2 + 1;
                triangles[t++] = bl; triangles[t++] = tl; triangles[t++] = tr;
                triangles[t++] = bl; triangles[t++] = tr; triangles[t++] = br;
                triangles[t++] = bl; triangles[t++] = tr; triangles[t++] = tl;
                triangles[t++] = bl; triangles[t++] = br; triangles[t++] = tr;
            }

            var mesh = new Mesh
            {
                name = $"ParticleStrip_{particles}",
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

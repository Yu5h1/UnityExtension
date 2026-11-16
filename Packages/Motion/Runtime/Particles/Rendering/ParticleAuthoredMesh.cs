using UnityEngine;
using UnityEngine.Rendering;

namespace Yu5h1Lib
{
    /// <summary>
    ///   Bakes an authored mesh into the entity's local frame for the rigid drawing (ParticleRendering
    ///   REND-06): the mesh is centred, scaled (fitted to the entity's rest extent and multiplied by
    ///   the visual scale), turned so its forward axis becomes the local Y axis, optionally turned
    ///   end for end, and moved to the centre of the rest particles. The orientation rules are the
    ///   ones the articulated shader uses (ParticleMeshAxis), so both drawings agree. A baked mesh
    ///   is built once per variant.
    /// </summary>
    public static class ParticleAuthoredMesh
    {
        public static Mesh Bake(
            Mesh source,
            Vector3 forwardAxis,
            bool flipForward,
            Vector3 visualScale,
            bool fitToDimensions,
            Vector3 dimensions,
            Vector3 localCenter)
        {
            if (source == null)
                return null;
            int axis = ParticleMeshAxis.Resolve(forwardAxis, flipForward, out bool flipped);
            Bounds bounds = source.bounds;
            Vector3 scale = ParticleMeshAxis.ClampScale(visualScale);
            if (fitToDimensions)
            {
                Vector3 target = ParticleMeshAxis.ToCanonical(dimensions, axis);
                Vector3 size = bounds.size;
                scale = Vector3.Scale(
                    new Vector3(
                        target.x / Mathf.Max(size.x, 0.000001f),
                        target.y / Mathf.Max(size.y, 0.000001f),
                        target.z / Mathf.Max(size.z, 0.000001f)),
                    scale);
            }

            Vector3[] sourceVertices = source.vertices;
            Vector3[] sourceNormals = source.normals;
            bool hasNormals = sourceNormals != null && sourceNormals.Length == sourceVertices.Length;
            var vertices = new Vector3[sourceVertices.Length];
            var normals = hasNormals ? new Vector3[sourceVertices.Length] : null;
            Vector3 inverseScale = new Vector3(
                1f / Mathf.Max(Mathf.Abs(scale.x), 0.0001f),
                1f / Mathf.Max(Mathf.Abs(scale.y), 0.0001f),
                1f / Mathf.Max(Mathf.Abs(scale.z), 0.0001f));
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 canonical = ParticleMeshAxis.ToCanonical(Vector3.Scale(sourceVertices[i] - bounds.center, scale), axis);
                if (flipped)
                {
                    canonical.x = -canonical.x;
                    canonical.y = -canonical.y;
                }
                vertices[i] = localCenter + canonical;
                if (!hasNormals)
                    continue;
                Vector3 normal = ParticleMeshAxis.ToCanonical(Vector3.Scale(sourceNormals[i], inverseScale), axis);
                if (flipped)
                {
                    normal.x = -normal.x;
                    normal.y = -normal.y;
                }
                normals[i] = normal.normalized;
            }

            var mesh = new Mesh
            {
                name = $"{source.name}_Rigid",
                hideFlags = HideFlags.HideAndDontSave
            };
            if (vertices.Length > 65535)
                mesh.indexFormat = IndexFormat.UInt32;
            mesh.vertices = vertices;
            if (hasNormals)
                mesh.normals = normals;
            mesh.uv = source.uv;
            mesh.subMeshCount = source.subMeshCount;
            bool reflects = axis != 1;
            for (int s = 0; s < source.subMeshCount; s++)
            {
                int[] indices = source.GetTriangles(s);
                if (reflects)
                    for (int i = 0; i + 2 < indices.Length; i += 3)
                        (indices[i + 1], indices[i + 2]) = (indices[i + 2], indices[i + 1]);
                mesh.SetTriangles(indices, s);
            }
            mesh.RecalculateBounds();
            if (!hasNormals)
                mesh.RecalculateNormals();
            return mesh;
        }
    }
}

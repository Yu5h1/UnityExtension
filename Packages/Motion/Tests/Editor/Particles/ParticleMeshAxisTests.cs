using NUnit.Framework;
using UnityEngine;

namespace Yu5h1Lib.Tests
{
    /// <summary>
    ///   The mesh-to-body mapping of ParticleRendering REND-05 and REND-06 for X, Y and Z forward
    ///   meshes: a rotation (determinant +1, so winding and handedness survive), forward onto the
    ///   body's Y, and for X and Z forward meshes the mesh's up onto the body's normal Z. The baked
    ///   authored mesh keeps each triangle's winding in agreement with its vertex normals.
    /// </summary>
    public sealed class ParticleMeshAxisTests
    {
        static readonly Vector3[] Forwards = { Vector3.right, Vector3.up, Vector3.forward };

        [Test]
        public void EveryAxisMapsByARotation_ForwardToY_AndUpToTheNormal()
        {
            for (int axis = 0; axis < 3; axis++)
            {
                Vector3 x = ParticleMeshAxis.ToCanonical(Vector3.right, axis);
                Vector3 y = ParticleMeshAxis.ToCanonical(Vector3.up, axis);
                Vector3 z = ParticleMeshAxis.ToCanonical(Vector3.forward, axis);
                Assert.AreEqual(1f, Vector3.Dot(Vector3.Cross(x, y), z), 1e-6f, $"axis {axis} is a rotation");
                Assert.AreEqual(Vector3.up, ParticleMeshAxis.ToCanonical(Forwards[axis], axis), $"axis {axis} forward becomes Y");
                if (axis != 1)
                    Assert.AreEqual(Vector3.forward, y, $"axis {axis}: the mesh's up becomes the body normal");
                Vector3 size = new Vector3(1f, 2f, 3f);
                Vector3 canonical = ParticleMeshAxis.ToCanonical(size, axis);
                Vector3 back = ParticleMeshAxis.ToMeshSize(new Vector3(Mathf.Abs(canonical.x), Mathf.Abs(canonical.y), Mathf.Abs(canonical.z)), axis);
                Assert.AreEqual(size, back, $"axis {axis}: sizes map back to the mesh axes");
            }
        }

        [Test]
        public void BakedAuthoredMeshKeepsWindingForEveryAxis()
        {
            var source = new Mesh
            {
                vertices = new[] { new Vector3(0f, 0f, 0f), new Vector3(1f, 0.2f, 0f), new Vector3(0.3f, 1f, 0f) },
                normals = new[] { Vector3.forward, Vector3.forward, Vector3.forward },
                triangles = new[] { 0, 1, 2 }
            };
            try
            {
                foreach (Vector3 forward in Forwards)
                    foreach (bool flip in new[] { false, true })
                    {
                        Mesh baked = ParticleAuthoredMesh.Bake(source, forward, flip, Vector3.one, false, Vector3.one, Vector3.zero);
                        try
                        {
                            Vector3[] v = baked.vertices;
                            int[] t = baked.triangles;
                            Vector3 face = Vector3.Cross(v[t[1]] - v[t[0]], v[t[2]] - v[t[0]]);
                            Assert.Greater(Vector3.Dot(face, baked.normals[0]), 0f, $"forward {forward}, flip {flip}: winding agrees with the normal");
                        }
                        finally
                        {
                            Object.DestroyImmediate(baked);
                        }
                    }
            }
            finally
            {
                Object.DestroyImmediate(source);
            }
        }
    }
}

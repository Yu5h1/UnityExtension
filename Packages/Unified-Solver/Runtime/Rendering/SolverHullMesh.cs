using UnityEngine;

namespace Yu5h1.UnifiedSolver
{
    // Builds an ordinary Mesh from a template's rest particle positions.
    //
    // Ordinary is the point. The vertices are baked in local space, so the mesh
    // needs no custom shader to place them: a rigid body is a mesh and a matrix,
    // and the solver already hands out that matrix on the CPU every frame.
    // Anything Unity can draw a mesh with will draw these, including a URP or
    // HDRP material taken off the shelf.
    //
    // The earlier version carried no geometry at all, only indices for a vertex
    // shader that read positions out of the solver's buffers. That worked, but
    // it made the look of a fragment the shader's problem, and there was never a
    // reason for it to be.
    public static class SolverHullMesh
    {
        // vertices must be the same positions the emitter used as rest
        // particles, from the same SolverShapeSource.BuildTemplate call, or the
        // fragment collides as one shape and is drawn as another.
        //
        // particleRadius pushes the surface out to where the body actually
        // collides. The hull passes through particle centres, but the fragment
        // collides as the union of spheres around them, so drawn untouched it
        // reads a radius smaller than it behaves. Radial inflation from the
        // centroid is the cheap approximation of that union, and baking it here
        // is only correct because the instance matrix carries no scale that
        // would stretch it afterwards.
        public static Mesh Build(
            SolverParticleTopology topology,
            Vector3[] vertices,
            int vertexCount,
            float particleRadius)
        {
            int[] faces =
                SolverHullShapes.Faces(topology);
            if (faces == null ||
                vertices == null ||
                vertexCount <= 0)
            {
                return null;
            }

            Vector3 centroid = Vector3.zero;
            for (int i = 0; i < vertexCount; i++)
                centroid += vertices[i];
            centroid /= vertexCount;

            var inflated = new Vector3[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                Vector3 radial = vertices[i] - centroid;
                float length = radial.magnitude;
                inflated[i] = length > 1e-6f
                    ? centroid +
                      radial *
                      ((length + particleRadius) / length)
                    : vertices[i];
            }

            // One vertex per face corner, never shared between faces. Sharing
            // would force the corners to interpolate a single normal and round
            // the facets off, and ice is supposed to read as flat and sharp.
            int count = faces.Length;
            var positions = new Vector3[count];
            var normals = new Vector3[count];
            var uv = new Vector2[count];
            var triangles = new int[count];

            for (int i = 0; i < count; i += 3)
            {
                Vector3 a = inflated[faces[i]];
                Vector3 b = inflated[faces[i + 1]];
                Vector3 c = inflated[faces[i + 2]];
                Vector3 normal = Vector3.Cross(
                    b - a,
                    c - a).normalized;

                Write(i, a, normal, 0);
                Write(i + 1, b, normal, 1);
                Write(i + 2, c, normal, 2);
            }

            void Write(
                int vertex,
                Vector3 position,
                Vector3 normal,
                int corner)
            {
                positions[vertex] = position;
                normals[vertex] = normal;
                uv[vertex] = FaceUV(corner);
                triangles[vertex] = vertex;
            }

            var mesh = new Mesh
            {
                name = $"SolverHull_{topology}",
                vertices = positions,
                normals = normals,
                uv = uv,
                triangles = triangles
            };
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        // Flat triangle mapping. A fragment has no meaningful surface
        // parameterisation of its own, and stretching one texture across each
        // facet is what reads as ice rather than as a painted solid. A shader
        // using triplanar or screen-space coordinates ignores this anyway.
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

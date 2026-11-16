using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   Maps a mesh forward axis onto the canonical body axis (+Y, the head direction of
    ///   ParticleBodyFrame.hlsl), the way the articulated shader of Unified-Solver does, so the
    ///   articulated and rigid drawing orient meshes the same way (ParticleRendering REND-05, 06).
    /// </summary>
    internal static class ParticleMeshAxis
    {
        /// <summary>
        ///   Index (0 = X, 1 = Y, 2 = Z) of the dominant component of <paramref name="forward"/>.
        ///   <paramref name="flipped"/> is true when the head is at the negative end: a negative
        ///   dominant component or <paramref name="flipForward"/>, not both.
        /// </summary>
        public static int Resolve(Vector3 forward, bool flipForward, out bool flipped)
        {
            Vector3 magnitude = new Vector3(Mathf.Abs(forward.x), Mathf.Abs(forward.y), Mathf.Abs(forward.z));
            int axis = magnitude.x >= magnitude.y && magnitude.x >= magnitude.z ? 0 : (magnitude.y >= magnitude.z ? 1 : 2);
            bool negative = forward[axis] < 0f;
            flipped = negative != flipForward;
            return axis;
        }

        /// <summary>
        ///   Turns a mesh-space vector into the body's frame (X side, Y forward, Z normal): the
        ///   forward axis becomes Y and, for an X or Z forward mesh, the mesh's up (Y) becomes the
        ///   normal Z. It is a rotation, never a reflection, so triangle winding and handedness are
        ///   kept; the articulated shader's MeshToCanonical is the same mapping.
        /// </summary>
        public static Vector3 ToCanonical(Vector3 value, int axis)
        {
            if (axis == 0)
                return new Vector3(value.z, value.x, value.y);
            if (axis == 1)
                return value;
            return new Vector3(-value.x, value.z, value.y);
        }

        /// <summary>
        ///   Sizes along the mesh's own axes for sizes given along the body's frame, the inverse
        ///   of <see cref="ToCanonical"/> without signs; the shader's CanonicalScaleToMesh.
        /// </summary>
        public static Vector3 ToMeshSize(Vector3 canonical, int axis)
        {
            if (axis == 0)
                return new Vector3(canonical.y, canonical.z, canonical.x);
            if (axis == 1)
                return canonical;
            return new Vector3(canonical.x, canonical.z, canonical.y);
        }

        public static Vector3 ClampScale(Vector3 value) => new Vector3(
            Mathf.Max(0.0001f, Mathf.Abs(value.x)),
            Mathf.Max(0.0001f, Mathf.Abs(value.y)),
            Mathf.Max(0.0001f, Mathf.Abs(value.z)));
    }
}

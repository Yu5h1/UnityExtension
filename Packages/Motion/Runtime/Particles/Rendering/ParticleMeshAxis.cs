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
        ///   Reorders mesh-space components so the forward axis becomes Y.
        /// </summary>
        public static Vector3 ToCanonical(Vector3 value, int axis)
        {
            if (axis == 0)
                return new Vector3(value.y, value.x, value.z);
            if (axis == 1)
                return value;
            return new Vector3(value.x, value.z, value.y);
        }

        public static Vector3 ClampScale(Vector3 value) => new Vector3(
            Mathf.Max(0.0001f, Mathf.Abs(value.x)),
            Mathf.Max(0.0001f, Mathf.Abs(value.y)),
            Mathf.Max(0.0001f, Mathf.Abs(value.z)));
    }
}

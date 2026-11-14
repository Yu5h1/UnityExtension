using System;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   Upper bounds a <see cref="ParticleWorld"/> reserves. Allocation and collider registration
    ///   beyond them are rejected with an error instead of being truncated (ParticleWorld WORLD-04).
    /// </summary>
    [Serializable]
    public sealed class ParticleWorldCapacity
    {
        [Min(1)]
        public int particles = 65536;
        [Min(0)]
        public int constraints = 262144;
        [Min(0)]
        public int rigidGroups = 1024;
        [Min(0)]
        public int sphereColliders = 64;
        [Min(0)]
        public int capsuleColliders = 64;
        [Min(0)]
        public int boxColliders = 64;

        internal int ColliderLimit(ParticleColliderShape shape)
        {
            switch (shape)
            {
                case ParticleColliderShape.Sphere: return sphereColliders;
                case ParticleColliderShape.Capsule: return capsuleColliders;
                default: return boxColliders;
            }
        }
    }
}

using System;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   World-wide numerical settings of the particle solver, with the defaults and ranges of
    ///   ParticleSolver 1.0.0 (Packages/Motion/Documentation/ParticlePhysics/ParticleSolver.md).
    /// </summary>
    [Serializable]
    public sealed class ParticleSolverSettings
    {
        [Min(1)]
        public int substeps = 30;
        public Vector3 gravity = new Vector3(0f, -9.81f, 0f);
        [Min(0f)]
        public float damping = 0f;
        [Range(1f, 2f)]
        public float sor = 1f;
        [Min(0f)]
        public float frictionStatic = 0.4f;
        [Min(0f)]
        public float frictionKinetic = 0.2f;
        [Min(0.0001f)]
        public float maxDepenetrationSpeed = 5f;
        public float groundY = 0f;
        [Tooltip("Particle categories the ground plane pushes (ParticleSolver SOLVER-13).")]
        public uint groundMask = uint.MaxValue;

        /// <summary>
        ///   Returns false and the first violated rule when a value is outside its specified range.
        /// </summary>
        public bool TryValidate(out string error)
        {
            error = null;
            if (substeps < 1)
                error = "substeps must be at least 1.";
            else if (!IsFinite(gravity))
                error = "gravity must be finite.";
            else if (!(damping >= 0f) || float.IsInfinity(damping))
                error = "damping must be a finite value of at least 0.";
            else if (!(sor >= 1f && sor <= 2f))
                error = "sor must be between 1 and 2.";
            else if (!(frictionStatic >= 0f) || !(frictionKinetic >= 0f))
                error = "friction coefficients must be at least 0.";
            else if (!(maxDepenetrationSpeed > 0f) || float.IsInfinity(maxDepenetrationSpeed))
                error = "maxDepenetrationSpeed must be a finite value above 0.";
            else if (float.IsNaN(groundY) || float.IsInfinity(groundY))
                error = "groundY must be finite.";
            return error == null;
        }

        internal static bool IsFinite(Vector3 value)
        {
            return !float.IsNaN(value.x) && !float.IsInfinity(value.x)
                && !float.IsNaN(value.y) && !float.IsInfinity(value.y)
                && !float.IsNaN(value.z) && !float.IsInfinity(value.z);
        }
    }
}

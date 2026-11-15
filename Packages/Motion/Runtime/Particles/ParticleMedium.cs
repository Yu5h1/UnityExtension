using System;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   A box-shaped region of fluid that pushes the particles inside it during the after-step
    ///   stage: buoyancy from <see cref="density"/> relative to each particle, and convergence toward
    ///   <see cref="flow"/> scaled by <see cref="dragCoefficient"/> (ParticleSolver SOLVER-17).
    ///   Density uses the same relative unit as particle mass; a particle of radius r and mass m is
    ///   neutral at density m / ((4/3)·π·r³).
    /// </summary>
    [Serializable]
    public struct ParticleMedium
    {
        public Vector3 center;
        public Vector3 halfExtents;
        public Quaternion rotation;
        public float density;
        public Vector3 flow;
        public float dragCoefficient;
    }
}

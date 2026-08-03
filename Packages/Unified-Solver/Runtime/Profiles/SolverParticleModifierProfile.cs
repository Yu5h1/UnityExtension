using UnityEngine;

namespace Yu5h1.UnifiedSolver
{
    public abstract class SolverParticleModifierProfile :
        ScriptableObject
    {
        [Tooltip("Off silences it without losing its slot or settings.")]
        public bool enabled = true;
    }
}

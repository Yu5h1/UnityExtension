using UnityEngine;

namespace Yu5h1.UnifiedSolver
{
    public abstract class SolverParticleModifierProfile :
        ScriptableObject
    {
        [Tooltip("Dispatch this modifier. Turning it off leaves the profile's list intact, so a modifier can be silenced without losing its slot or its settings.")]
        public bool enabled = true;
    }
}

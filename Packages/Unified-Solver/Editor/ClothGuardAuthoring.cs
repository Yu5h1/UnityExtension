using UnityEditor;
using UnityEngine;
using Yu5h1Lib.UnifiedSolver;

// Adds the solver glue to every ClothGenerator and RopeGenerator the moment it
// is added.
//
// SolverClothParticles is what stops a cloth the solver has no room for before
// the vendored generator throws halfway through building it, and it can only do
// that if it exists. Leaving that to the author would reproduce the original
// gap -- the guard present on anchored cloth and absent on the net that had
// none -- so the editor makes it part of adding a ClothGenerator. The runtime
// bootstrap covers whatever an existing scene already carried.
//
// A decision made by code rather than by the author, which is why it is a hook
// and not a setting: a ClothGenerator without the guard is never what anyone
// wants.
static class ClothGuardAuthoring
{
    [InitializeOnLoadMethod]
    static void Hook()
    {
        ObjectFactory.componentWasAdded -= OnComponentAdded;
        ObjectFactory.componentWasAdded += OnComponentAdded;
    }

    // Deferred because the callback runs inside the add that raised it.
    static void OnComponentAdded(Component component)
    {
        if (!(component is ClothGenerator ||
              component is RopeGenerator))
        {
            return;
        }

        GameObject owner = component.gameObject;
        EditorApplication.delayCall += () =>
        {
            if (owner == null)
                return;

            if (owner.GetComponent<ClothGenerator>() != null &&
                owner.GetComponent<SolverClothParticles>() == null)
            {
                Undo.AddComponent<SolverClothParticles>(owner);
            }

            if (owner.GetComponent<RopeGenerator>() != null &&
                owner.GetComponent<SolverRopeParticles>() == null)
            {
                Undo.AddComponent<SolverRopeParticles>(owner);
            }
        };
    }
}

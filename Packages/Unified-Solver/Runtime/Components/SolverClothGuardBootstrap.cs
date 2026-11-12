using UnityEngine;
using UnityEngine.SceneManagement;

namespace Yu5h1Lib.UnifiedSolver
{
    // Makes sure every ClothGenerator in a loaded scene has a
    // SolverClothParticles beside it, because that component's Start is the only
    // thing that runs ahead of the generator's and can refuse a cloth the solver
    // has no room for.
    //
    // A guard that only existed on anchored cloth would be silent exactly where
    // the failure was first seen: a net with no anchors at all. So presence is
    // not left to the author. The editor adds the component when a
    // ClothGenerator is added; this adds it to whatever a scene or a prefab
    // already carried without one.
    //
    // sceneLoaded fires after Awake and OnEnable and before any Start, so a
    // component added here still runs its Start ahead of the generator's.
    //
    // Not covered, by construction: a prefab that has no SolverClothParticles
    // and is instantiated after its scene has loaded. Nothing can run between
    // that Instantiate and the generator's Start without being on the prefab.
    // Add the component to such a prefab (or call EnsureOn right after
    // instantiating it).
    public static class SolverClothGuardBootstrap
    {
        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ClothGenerator[] cloths =
                ObjectUtility.FindObjects<ClothGenerator>();
            for (int i = 0; i < cloths.Length; i++)
                EnsureOn(cloths[i]);
        }

        /// <summary>
        ///   Adds the capacity guard to a cloth that lacks one. Safe to call
        ///   more than once.
        /// </summary>
        /// <returns>The guard now on the cloth, or null for a null cloth.</returns>
        public static SolverClothParticles EnsureOn(
            ClothGenerator cloth)
        {
            if (cloth == null)
                return null;

            var existing =
                cloth.GetComponent<SolverClothParticles>();
            return existing != null
                ? existing
                : cloth.gameObject
                    .AddComponent<SolverClothParticles>();
        }
    }
}

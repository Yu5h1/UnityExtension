using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Yu5h1Lib.UnifiedSolver
{
    // What has to happen to a scene's solver bodies when it loads, before any of
    // them has started.
    //
    // 1. Every ClothGenerator and RopeGenerator gets its glue component
    //    (SolverClothParticles, SolverRopeParticles). For cloth that component's
    //    Start is the only thing that runs ahead of the generator's and can
    //    refuse a net the solver has no room for; for both it is what parks and
    //    resets the body when its GameObject is disabled and enabled. A guard
    //    that only existed on anchored cloth would be silent exactly where the
    //    failure was first seen -- a net with no anchors at all -- so presence is
    //    not left to the author. The editor adds the component when a generator
    //    is added; this adds it to whatever a scene or a prefab already carried
    //    without one.
    //
    // 2. Every body authored inactive is allocated now. A GameObject that starts
    //    inactive runs no Start, so its particles would first exist when
    //    something activates it, after the solver has stepped, and adding them
    //    then puts every live particle back to its creation state. The object is
    //    switched on, held until its Start has run, and switched off with its
    //    bodies parked (SolverReuseDriver does the holding).
    //
    // sceneLoaded fires after Awake and OnEnable and before any Start, so a
    // component added or an object switched on here still runs its Start ahead
    // of the generator's, and in the same frame, before the first physics step.
    //
    // Not covered, by construction: a prefab instantiated after its scene has
    // loaded. Nothing can run between that Instantiate and the generator's Start
    // without being on the prefab, and its particles are added after the solver
    // has stepped whatever is done. Put the component on the prefab so the
    // capacity guard works, and instantiate such objects before the first step
    // if their particles must not disturb the scene -- or keep them in the scene
    // inactive, which is covered.
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

            RopeGenerator[] ropes =
                ObjectUtility.FindObjects<RopeGenerator>();
            for (int i = 0; i < ropes.Length; i++)
                EnsureOn(ropes[i]);

            AllocateInactive();
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

        /// <summary>
        ///   Adds the reuse glue to a rope that lacks it. Safe to call more than
        ///   once.
        /// </summary>
        public static SolverRopeParticles EnsureOn(
            RopeGenerator rope)
        {
            if (rope == null)
                return null;

            var existing =
                rope.GetComponent<SolverRopeParticles>();
            return existing != null
                ? existing
                : rope.gameObject
                    .AddComponent<SolverRopeParticles>();
        }

        static void AllocateInactive()
        {
            SolverManager solver = SolverManager.Instance;
            if (solver == null || SolverReuse.HasStepped(solver))
                return;

            // Grouped by object: one GameObject can carry a generator, its glue
            // and an emitter, and it is switched on once for all of them.
            var owners = new List<GameObject>();
            CollectInactive<SolverParticleEmitter>(owners);
            CollectInactive<SolverClothParticles>(owners);
            CollectInactive<SolverRopeParticles>(owners);

            for (int i = 0; i < owners.Count; i++)
            {
                GameObject owner = owners[i];
                ISolverReuseTarget[] targets =
                    owner.GetComponents<ISolverReuseTarget>();

                bool pending = false;
                for (int t = 0; t < targets.Length; t++)
                {
                    if (!targets[t].IsAllocated)
                        pending = true;
                }
                if (!pending)
                    continue;

                Transform parent = owner.transform.parent;
                if (parent != null &&
                    !parent.gameObject.activeInHierarchy)
                {
                    Debug.LogWarning(
                        $"'{owner.name}' is authored inactive under an " +
                        $"inactive parent '{parent.name}', so it cannot be " +
                        "switched on by itself to allocate its solver " +
                        "particles before the solver's first step. Its " +
                        "first activation will add them and put every live " +
                        "particle back to its creation state. Keep the " +
                        "parent active and switch this object off instead.",
                        owner);
                    continue;
                }

                SolverReuseDriver.BeginAllocation(owner, targets);
            }
        }

        static void CollectInactive<T>(List<GameObject> into)
            where T : Component
        {
            T[] found = ObjectUtility.FindObjects<T>();
            for (int i = 0; i < found.Length; i++)
            {
                GameObject owner = found[i].gameObject;
                if (!owner.activeInHierarchy &&
                    !into.Contains(owner))
                {
                    into.Add(owner);
                }
            }
        }
    }
}

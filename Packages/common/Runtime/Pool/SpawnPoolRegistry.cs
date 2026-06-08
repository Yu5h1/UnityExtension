using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// Static directory of <see cref="SpawnPool"/> instances by key.
    /// Pools self-register on Awake and unregister on OnDestroy.
    /// <para>Lookup is O(1). Resets automatically on entering Play Mode.</para>
    /// </summary>
    public static class SpawnPoolRegistry
    {
        private static readonly Dictionary<string, SpawnPool> _pools = new Dictionary<string, SpawnPool>();

        // Clear on every Play Mode start so disabled Domain Reload doesn't carry stale entries.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState() => _pools.Clear();

        public static void Register(SpawnPool pool)
        {
            if (pool == null || string.IsNullOrEmpty(pool.Key)) return;

            if (_pools.TryGetValue(pool.Key, out var existing) && existing != null && existing != pool)
                Debug.LogWarning($"[SpawnPoolRegistry] Key '{pool.Key}' already registered by another pool. Overwriting.", pool);

            _pools[pool.Key] = pool;
        }

        public static void Unregister(SpawnPool pool)
        {
            if (pool == null || string.IsNullOrEmpty(pool.Key)) return;

            if (_pools.TryGetValue(pool.Key, out var existing) && existing == pool)
                _pools.Remove(pool.Key);
        }

        /// <summary>True if a pool is registered under <paramref name="key"/>.</summary>
        public static bool Has(string key)
            => !string.IsNullOrEmpty(key) && _pools.ContainsKey(key);

        /// <summary>Lookup a pool by key. Returns null if missing.</summary>
        public static SpawnPool Get(string key)
            => string.IsNullOrEmpty(key) ? null : (_pools.TryGetValue(key, out var p) ? p : null);

        /// <summary>
        /// Try to spawn from the named pool.
        /// Returns false if the pool is missing OR exhausted.
        /// </summary>
        public static bool TrySpawn(string key, Vector3 position, Quaternion rotation, out GameObject spawned)
        {
            spawned = null;
            var pool = Get(key);
            return pool != null && pool.TrySpawn(position, rotation, out spawned);
        }
    }
}

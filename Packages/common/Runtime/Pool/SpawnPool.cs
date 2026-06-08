using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// Fixed-size pool of pre-placed, pre-disabled GameObjects living in the scene.
    /// <para>
    /// Spawning activates an inactive instance at the requested world pose; recycling deactivates it.
    /// If no inactive instance is available, <see cref="TrySpawn"/> returns false —
    /// the pool does NOT grow, instantiate, or evict active instances.
    /// </para>
    /// Setup:
    ///   1. Place identical pre-disabled instances as direct children of this GameObject.
    ///   2. Set a unique <see cref="Key"/> so <see cref="SpawnPoolRegistry"/> can resolve it.
    /// </summary>
    [DisallowMultipleComponent]
    public class SpawnPool : MonoBehaviour
    {
        [Tooltip("Identifier used by SpawnPoolRegistry. Required for proxy lookup.")]
        [SerializeField] private string key;
        public string Key => key;

        [Tooltip("On Awake, gather all direct children as the pool and force them inactive.")]
        [SerializeField] private bool autoCollectChildren = true;

        [Tooltip("Manual list of instances. Appended to children when autoCollectChildren is on; sole source when off.")]
        [SerializeField] private List<GameObject> manualInstances;

        private readonly List<GameObject> _instances = new List<GameObject>();

        /// <summary>Total instances managed by this pool (active + inactive).</summary>
        public int Capacity => _instances.Count;

        /// <summary>Number of currently inactive (spawnable) instances.</summary>
        public int AvailableCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _instances.Count; i++)
                    if (_instances[i] != null && !_instances[i].activeSelf) n++;
                return n;
            }
        }

        private void Awake()
        {
            _instances.Clear();

            if (autoCollectChildren)
            {
                for (int i = 0; i < transform.childCount; i++)
                    _instances.Add(transform.GetChild(i).gameObject);
            }
            if (manualInstances != null)
            {
                for (int i = 0; i < manualInstances.Count; i++)
                {
                    var m = manualInstances[i];
                    if (m != null && !_instances.Contains(m)) _instances.Add(m);
                }
            }

            // Force all collected instances inactive at startup.
            for (int i = 0; i < _instances.Count; i++)
                if (_instances[i] != null) _instances[i].SetActive(false);

            SpawnPoolRegistry.Register(this);
        }

        private void OnDestroy() => SpawnPoolRegistry.Unregister(this);

        /// <summary>
        /// Activate an inactive instance at the given world-space pose.
        /// Returns false if every instance is currently active (pool exhausted) — in that case <paramref name="spawned"/> is null.
        /// </summary>
        public bool TrySpawn(Vector3 position, Quaternion rotation, out GameObject spawned)
        {
            spawned = null;
            for (int i = 0; i < _instances.Count; i++)
            {
                var inst = _instances[i];
                if (inst != null && !inst.activeSelf)
                {
                    inst.transform.SetPositionAndRotation(position, rotation);
                    inst.SetActive(true);
                    spawned = inst;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Return an instance to the pool by deactivating it.
        /// Safe to call on an already-inactive instance (no-op).
        /// </summary>
        public void Recycle(GameObject instance)
        {
            if (instance != null && instance.activeSelf)
                instance.SetActive(false);
        }
    }
}

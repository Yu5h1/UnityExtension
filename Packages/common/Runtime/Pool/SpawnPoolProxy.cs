using UnityEngine;
using UnityEngine.Events;
using Yu5h1Lib.Data;

namespace Yu5h1Lib
{
    /// <summary>
    /// Convenience caller — invokes <see cref="SpawnPool.TrySpawn"/> via <see cref="SpawnPoolRegistry"/>
    /// at this transform's world pose (or an alternative anchor).
    /// <para>Wire <see cref="Spawn"/> from <c>TriggerReceiver._onEnter</c>, a UI Button, or any UnityEvent.</para>
    /// </summary>
    public class SpawnPoolProxy : BaseMonoBehaviour
    {
        [Tooltip("Pool key registered in SpawnPoolRegistry (must match SpawnPool.Key).")]
        [SerializeField] private string poolKey;

        [Tooltip("Use this transform's world pose as the spawn pose. Disable to use the anchor field below.")]
        [SerializeField] private bool useSelfPose = true;

        [Tooltip("Optional alternative spawn anchor. Ignored when useSelfPose is on.")]
        [SerializeField] private Transform _anchor;

        [Tooltip("Fires with the spawned GameObject when a pool instance is activated.")]
        [SerializeField] private UnityEvent<GameObject> _Spawned;

        [Tooltip("Fires when the pool was missing or fully active — proxy did not spawn anything.")]
        [SerializeField] private UnityEvent _PoolExhausted;

        protected override void OnInitializing() {}

        /// <summary>
        /// Try to spawn from the registered pool.
        /// If pool is missing or exhausted, fires <c>_PoolExhausted</c> and returns without spawning.
        /// </summary>
        [ContextMenu(nameof(Spawn))]
        public void Spawn()
        {
            Transform anchor = (!useSelfPose && _anchor != null) ? _anchor : transform;
            Spawn(anchor.position, anchor.rotation, Vector3.one);
        }

        public void Spawn(Vector3 position,Quaternion rotation, Vector3 scale)
        {
            if (SpawnPoolRegistry.TrySpawn(poolKey, position, rotation, out var go))
                _Spawned?.Invoke(go);
            else
                _PoolExhausted?.Invoke();
        }

        public void Spawn(Vector3Object v) => Spawn(v.value, Quaternion.identity,Vector3.one);
        public void Spawn(TransformInfoObject data)
        {
            var t = data.value;
            Vector3 p = transform.position;
            Vector3 e = transform.eulerAngles;
            Vector3 s = transform.localScale; 
            t.position.TryGetValue(out p);
            t.euler.TryGetValue(out e);
            t.scale.TryGetValue(out s);

            Spawn(p, Quaternion.Euler(e),s);
        }

    }
}

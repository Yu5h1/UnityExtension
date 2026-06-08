using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Yu5h1Lib
{
    /// <summary>
    /// Placed on an object (e.g. a boat) that should react when it enters or exits a trigger zone.
    /// Requires a <see cref="Rigidbody"/> so Unity delivers <c>OnTriggerEnter/Exit</c> callbacks
    /// from the zone's isTrigger Collider. The <c>Collider</c> passed to events is the zone's collider.
    /// </summary>
    public class TriggerReceiver : BaseMonoBehaviour
    {
        [SerializeField][FormerlySerializedAs("_onEnter")] private UnityEvent _entered;
        [SerializeField] private UnityEvent _exited;

        public void InvokeEnter(TriggerEvent Triggerer) => _entered?.Invoke();
        public void InvokeExit(TriggerEvent Triggerer) => _exited?.Invoke();

        protected override void OnInitializing() {}
    }
}

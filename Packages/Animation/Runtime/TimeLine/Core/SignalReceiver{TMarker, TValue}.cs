using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

namespace Yu5h1Lib.Timeline
{
    public abstract class SignalReceiver<TMarker, TValue> : BaseMonoBehaviour, INotificationReceiver
where TMarker : SignalMarker<TMarker, TValue>
    {
        [SerializeField] private UnityEvent<TValue> notified;

        protected override void OnInitializing() {}

        public virtual void OnNotify(
            Playable origin,
            INotification notification,
            object context)
        {
            if (notification is not TMarker marker)
                return;

            notified?.Invoke(marker.Value);
        }
    } 
}
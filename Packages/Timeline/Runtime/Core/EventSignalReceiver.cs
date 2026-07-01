using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using Yu5h1Lib.Serialization;

namespace Yu5h1Lib.Timeline
{
    /// <summary>
    /// Receives <see cref="EventSignal"/> markers and invokes a scene-side UnityEvent mapped to each.
    /// The map is keyed by the marker asset; the UnityEvent lives here (scene), so its listeners can
    /// reference scene objects and call any method. One receiver on a single marker track can thus
    /// drive many objects. Events are authored from the EventSignal's own Inspector (EventSignalEditor),
    /// hence the map is <see cref="HideInInspector"/>.
    /// </summary>
    [ExecuteAlways]
    public class EventSignalReceiver : MonoBehaviour, INotificationReceiver
    {
        //[ReadOnly]
        //[HideInInspector]
        [SerializeField] private KeyValues<EventSignal, UnityEvent> _events = new();

        public void OnNotify(Playable origin, INotification notification, object context)
        {
            if (notification is EventSignal signal)
                Invoke(signal);
        }

        /// <summary>
        /// Invoke the UnityEvent mapped to <paramref name="signal"/>, if any. Safe on null and on
        /// unmapped signals (no-op). Lets a marker's event be fired directly — not only through the
        /// Timeline notification path.
        /// </summary>
        public void Invoke(EventSignal signal)
        {
            if (signal != null && _events.TryGetValue(signal, out var evt))
                evt?.Invoke();
        }

        /// <summary>True when <paramref name="signal"/> has a non-null mapped event; outputs it.</summary>
        public bool TryGetEvent(EventSignal signal, out UnityEvent evt)
        {
            evt = null;
            return signal != null && _events.TryGetValue(signal, out evt) && evt != null;
        }

        /// <summary>
        /// Returns the event mapped to <paramref name="signal"/>, creating an empty one if absent.
        /// Used by EventSignalEditor to guarantee a clean entry before drawing it.
        /// </summary>
        public UnityEvent GetOrCreateEvent(EventSignal signal)
        {
            if (signal == null)
                return null;

            if (!_events.TryGetValue(signal, out var evt) || evt == null)
            {
                evt = new UnityEvent();
                _events[signal] = evt;
            }
            return evt;
        }
    }
}

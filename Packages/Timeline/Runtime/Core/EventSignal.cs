using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Yu5h1Lib.Timeline
{
    /// <summary>
    /// A value-less Timeline marker that fires a notification when the playhead passes it.
    /// It carries no data — purely a named trigger point. The response (a scene-side UnityEvent) is
    /// authored on an <see cref="EventSignalReceiver"/>, keyed by this marker.
    /// <para>
    /// Because the UnityEvent lives in the scene, its listeners can reference scene objects and call
    /// any method directly — no ParameterObject, no ExposedReference, no per-target track. One
    /// receiver on a single marker track can drive many objects.
    /// </para>
    /// </summary>
    public class EventSignal : Marker, INotification, INotificationOptionProvider
    {
        public PropertyName id => new PropertyName(nameof(EventSignal));

        [Tooltip("When this notification fires (e.g. in edit mode, retroactively).")]
        [SerializeField]
        private NotificationFlags _flags = NotificationFlags.TriggerInEditMode | NotificationFlags.Retroactive;

        public NotificationFlags flags => _flags;
    }
}

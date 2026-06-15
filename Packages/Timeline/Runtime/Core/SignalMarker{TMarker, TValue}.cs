using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Yu5h1Lib.Timeline
{
    public abstract class SignalMarker<TMarker, TValue> : Marker, INotification , INotificationOptionProvider 
        where TMarker : SignalMarker<TMarker, TValue>
    {
        public PropertyName id => new PropertyName(typeof(TMarker).Name);
        [Inline(false,showLabel:true)]
        public TValue Value;
        public NotificationFlags _NotificationFlags = NotificationFlags.TriggerInEditMode | NotificationFlags.Retroactive;
        public NotificationFlags flags => _NotificationFlags;
    } 
}

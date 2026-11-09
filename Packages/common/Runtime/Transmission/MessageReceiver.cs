// Maps signal keys to UnityEvents without owning a broadcaster subscription.
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Yu5h1Lib.Serialization;

namespace Yu5h1Lib
{
    /// <summary>
    /// Stores signal-to-UnityEvent mappings and invokes them when explicitly called.
    /// </summary>
    [AddComponentMenu("Yu5h1Lib/Message Receiver"), DisallowMultipleComponent]
    public class MessageReceiver : BaseMonoBehaviour
    {
        [FormerlySerializedAs("_signals")]
        [SerializeField] private KeyValues<string, UnityEvent> _events = new();

        public KeyValues<string, UnityEvent> events => _events;

        protected override void OnInitializing() {}

        public void Call(string msg) => TryInvoke(msg, null);

        public bool TryInvoke(string msg) => TryInvoke(msg, null);
        public bool TryInvoke(string msg, params ArgumentInfo[] args)
        {
            if (msg.IsEmpty() || !_events.TryGetValue(msg, out var evt))
                return false;

            if (!args.IsEmpty())
            {
                foreach (var argument in args)
                    evt.LoadArgument(argument);
            }
            evt.Invoke();
            return true;
        }
    }
}

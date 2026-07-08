using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Yu5h1Lib.Serialization;

namespace Yu5h1Lib
{
    /// <summary>
    /// Stores string signal to UnityEvent mappings and registers them with <see cref="Broadcaster"/>.
    /// </summary>
    [AddComponentMenu("Yu5h1Lib/Message Receiver"), DisallowMultipleComponent]
    public class MessageReceiver : BaseMonoBehaviour
    {
        [FormerlySerializedAs("_signals")]
        [SerializeField] private KeyValues<string, UnityEvent> _events = new();

        public KeyValues<string, UnityEvent> events => _events;

        protected override void OnInitializing() {}

        private void OnEnable()
        {
            if (Application.isPlaying)
                Broadcaster.instance.Register(this);
        }

        private void OnDisable()
        {
            if (!Application.isPlaying || !Broadcaster.Exists() || ApplicationInfo.WantsToQuit)
                return;

            Broadcaster.instance.Unregister(this);
        }

        public void RefreshRegistration()
        {
            if (Application.isPlaying)
                Broadcaster.instance.Register(this);
        }

        public bool TryInvoke(string msg)
        {
            if (string.IsNullOrEmpty(msg) || !_events.TryGetValue(msg, out var evt) || evt == null)
                return false;

            evt.Invoke();
            return true;
        }

        public bool TryInvoke(string msg, params ArgumentInfo[] args)
        {
            if (msg.IsEmpty() || !_events.TryGetValue(msg, out var evt))
                return false;

            foreach (var argument in args)
                evt.LoadArgument(argument);

            evt.Invoke();
            return true;
        }
    }
}

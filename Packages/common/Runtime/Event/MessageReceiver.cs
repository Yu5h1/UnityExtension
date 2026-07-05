using UnityEngine;
using UnityEngine.Events;
using Yu5h1Lib.Serialization;

namespace Yu5h1Lib
{
    /// <summary>
    /// Stores string signal to UnityEvent mappings and registers them with <see cref="Broadcaster"/>.
    /// </summary>
    [AddComponentMenu("Yu5h1Lib/Message Receiver"), DisallowMultipleComponent]
    public class MessageReceiver : BaseMonoBehaviour
    {
        [SerializeField] private KeyValues<string, UnityEvent<Object>> _signals = new();

        public KeyValues<string, UnityEvent<Object>> Signals => _signals;

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

        public bool TryInvoke(string signal, Object arg)
        {
            if (string.IsNullOrEmpty(signal) || !_signals.TryGetValue(signal, out var evt) || evt == null)
                return false;

            evt.Invoke(arg);
            return true;
        }
    }
}

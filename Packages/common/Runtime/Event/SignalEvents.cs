using UnityEngine;
using UnityEngine.Events;
using Yu5h1Lib.Serialization;

namespace Yu5h1Lib
{
    /// <summary>
    /// Stores string signal to UnityEvent mappings and registers them with <see cref="SignalDispatcher"/>.
    /// </summary>
    [AddComponentMenu("Yu5h1Lib/Signal Events"), DisallowMultipleComponent]
    public class SignalEvents : BaseMonoBehaviour
    {
        [SerializeField] private KeyValues<string, UnityEvent<Object>> _signals = new();

        public KeyValues<string, UnityEvent<Object>> Signals => _signals;

        protected override void OnInitializing() {}

        private void OnEnable()
        {
            if (Application.isPlaying)
                SignalDispatcher.instance.Register(this);
        }

        private void OnDisable()
        {
            if (!Application.isPlaying || !SignalDispatcher.Exists() || ApplicationInfo.WantsToQuit)
                return;

            SignalDispatcher.instance.Unregister(this);
        }

        public void RefreshRegistration()
        {
            if (Application.isPlaying)
                SignalDispatcher.instance.Register(this);
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

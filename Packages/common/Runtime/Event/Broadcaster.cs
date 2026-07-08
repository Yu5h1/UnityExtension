using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// Runtime signal hub. Dispatches a string signal to every active <see cref="MessageReceiver"/>
    /// currently registered for that signal.
    /// </summary>
    [AddComponentMenu("Yu5h1Lib/Signal Dispatcher")]
    public class Broadcaster : SingletonBehaviour<Broadcaster>
    {
        private readonly Dictionary<string, List<MessageReceiver>> _events = new();
        private readonly List<MessageReceiver> _dispatchBuffer = new();
        private readonly List<string> _emptySignals = new();

        protected override void OnInitializing() {}
        protected override void OnInstantiated() {}

        public void Register(MessageReceiver events)
        {
            if (events == null)
                return;

            Unregister(events);

            foreach (var entry in events.events.Entries)
            {
                if (string.IsNullOrEmpty(entry.Key))
                    continue;

                if (!_events.TryGetValue(entry.Key, out var signalEvents))
                {
                    signalEvents = new List<MessageReceiver>();
                    _events.Add(entry.Key, signalEvents);
                }

                if (!signalEvents.Contains(events))
                    signalEvents.Add(events);
            }
        }

        public void Unregister(MessageReceiver events)
        {
            if (events == null)
                return;

            foreach (var pair in _events)
                pair.Value.Remove(events);

            RemoveEmptySignals();
        }

        public bool Dispatch(string signal)
        {
            if (signal.IsEmpty() || !_events.TryGetValue(signal, out var signalEvents))
                return false;

            var invoked = false;
            _dispatchBuffer.Clear();
            _dispatchBuffer.AddRange(signalEvents);

            foreach (var events in _dispatchBuffer)
            {
                if (events == null || !events.isActiveAndEnabled)
                    continue;

                invoked |= events.TryInvoke(signal);
            }

            _dispatchBuffer.Clear();
            return invoked;
        }

        private void RemoveEmptySignals()
        {
            _emptySignals.Clear();

            foreach (var pair in _events)
            {
                if (pair.Value.Count == 0)
                    _emptySignals.Add(pair.Key);
            }

            if (_emptySignals.Count == 0)
                return;

            foreach (var signal in _emptySignals)
                _events.Remove(signal);

            _emptySignals.Clear();
        }
    }
}

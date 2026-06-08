using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Yu5h1Lib.Data;
using Yu5h1Lib.Serialization;

namespace Yu5h1Lib
{
    public class StringKeyframeEvent : BaseMonoBehaviour
    {
        public KeyValues<string,UnityEvent> _events;
        protected override void OnInitializing() {}
        public void Invoke(string key)
        {
            if (!_events.ContainsKey(key))
                return;
            _events[key].Invoke();
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Yu5h1Lib.Data;
using Yu5h1Lib.Serialization;

namespace Yu5h1Lib
{
    public class IndexKeyframeEvent : BaseMonoBehaviour
    {
        public List<UnityEvent> _events;
        protected override void OnInitializing() {}
        public void Invoke(int index)
        {
            if (!_events.IsValid(index))
                return;
            _events[index].Invoke();
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib
{
    [System.Serializable]
    public class UnityEventEnhanced<T> : UnityEvent<T>
    {
        [SerializeField] private bool _enabled = true;
        public bool enabled { get => _enabled; set => _enabled = value; }

        public event UnityAction<T> action
        {
            add => AddListener(value);
            remove => RemoveListener(value);
        }
        public new void Invoke(T arg)
        {
            if (enabled)
                base.Invoke(arg);
        }

    }

    [System.Serializable]
    public class UnityEventEnhanced : UnityEvent
    {
        [SerializeField] private bool _enabled = true;
        public bool enabled { get => _enabled; set => _enabled = value; }

        public event UnityAction action
        {
            add => AddListener(value);
            remove => RemoveListener(value);
        }

        public new void Invoke()
        {
            if (enabled)
                base.Invoke();
        }
    } 
}
using System;
using UnityEngine;

namespace Yu5h1Lib
{
    [Serializable]
    public struct Optional<T>
    {        
        public bool enabled;
        [SerializeField] private T _value;

        public T value => _value;
        public bool hasValue => enabled;

        public bool TryGetValue(out T result)
        {
            result = _value;
            return enabled;
        }
    }
}
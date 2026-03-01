using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Yu5h1Lib
{
    [Serializable]
    public struct Optional<T>
    {        
        public bool enabled;
        [FormerlySerializedAs("_value")] public T value;
        public bool TryGetValue(out T result)
        {
            result = value;
            return enabled;
        }
    }
}
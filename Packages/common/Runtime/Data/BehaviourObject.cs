using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Yu5h1Lib
{
    public class BehaviourObject : ScriptableObject
    {
        [SerializeField] private bool _enabled = true;
        public bool enabled
        {
            get => _enabled;
            set
            {
                if (_enabled != value)
                {                    
                    _enabled = value;
                    if (value)
                        OnEnabled();
                    else
                        OnDisabled();
                }
            }
        }
        protected virtual void OnEnabled() {}
        protected virtual void OnDisabled() {}

        public bool IsInitialized { get; private set; }

        /// <summary>Run <see cref="Initialize"/> once, on first call. Idempotent and safe to call externally.</summary>
        public void LazyInitialize()
        {
            if (IsInitialized)
                return;
            IsInitialized = true;
            Initialize();
        }

        /// <summary>One-time setup, invoked once by <see cref="LazyInitialize"/>. Override in subclasses.</summary>
        protected virtual void Initialize() {}
    }
    public abstract class BehaviourObject<T> : BehaviourObject
    {
        public T Data;
    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Yu5h1Lib
{
    public class InlineComponentObject : InlineScriptableObject
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
        protected virtual void OnEnabled() { }
        protected virtual void OnDisabled() { }
    }
    public abstract class InlineComponentObject<T> : InlineComponentObject
    {
        public T Data;
    }

}

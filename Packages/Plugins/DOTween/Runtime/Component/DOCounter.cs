using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    public abstract class DOCounter<T> : TweenFloat<T> where T : Component
    {
        public int? _index = null;
        public int index 
        {
            get => _index ?? -1; 
            private set {
                if (_index == value)
                    return;
                _index = value;
                OnIndexChanged();
            }
        } 
        public int endIntValue => Mathf.CeilToInt(endValue);

        protected override float GetFloat() => index;
        protected override void SetFloat(float val) => index = Mathf.RoundToInt(val);
        protected abstract void OnIndexChanged();
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib
{
    public abstract class ObservableValue
    { 
        public abstract void Init();
    }
    [Serializable]
    public abstract class ObservableValue<T> : ObservableValue
    {
        public abstract T GetDefaultValue();
        public T Value
        {
            get => GetValue();
            set => SetValue(value);
        }
        public abstract T GetValue();
        public abstract void SetValueWithoutNotify(T newValue);
        public void SetValue(T newValue){
            if (EqualityComparer<T>.Default.Equals(GetValue(), newValue))
                return;
            SetValueWithoutNotify(newValue);
            _Changed?.Invoke(newValue);
        }
        [SerializeField] private UnityEvent<T> _Init = new UnityEvent<T>();
        public event UnityAction<T> init
        {
            add => _Init.AddListener(value);
            remove => _Init.RemoveListener(value);
        }
        [SerializeField] private UnityEvent<T> _Changed = new UnityEvent<T>();
        public event UnityAction<T> changed
        {
            add => _Changed.AddListener(value);
            remove => _Changed.RemoveListener(value);
        }
        public void Init(bool UseDefaultValue)
        {
            var startupValue = UseDefaultValue ? GetDefaultValue() : GetValue();
            SetValueWithoutNotify(startupValue);
            _Init?.Invoke(startupValue);
        }
        public override void Init() => Init(false);
    }

}

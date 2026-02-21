using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib
{
    public abstract class Logic : ScriptableObject
    {
        public abstract bool Evaluate(object value);
    }

    public abstract class Validator : ScriptableObject
    {
        public UnityEvent<bool> validityChanged;
        private bool _isValid;
        public bool IsValid
        {
            get => _isValid;
            private set
            {
                if (_isValid == value) return;
                _isValid = value;
                validityChanged?.Invoke(value);
            }
        }
        public void Validate() => IsValid = Evaluate();
        protected abstract bool Evaluate();
    }

    public abstract class Validator<T> : Validator where T : Object
    {
        public T target;
        [Inline(true)] public Logic logic;

        protected abstract object GetValue(T target);

        protected override bool Evaluate()
        {
            if (target == null || logic == null) return false;
            return logic.Evaluate(GetValue(target));
        }
    }
}

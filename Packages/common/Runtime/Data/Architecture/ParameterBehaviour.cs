using UnityEngine;

namespace Yu5h1Lib.Parameter
{
    public abstract class ParameterBehaviour : MonoBehaviour, IParameter
    {
        public abstract string memberName { get; protected set; }
        public abstract void ApplyTo(Object target);
        public abstract object GetValue();
    }

    public abstract class ParameterBehaviour<T> : ParameterBehaviour
    {
        [Tooltip("Public property name on the target. Unlike ParameterObject, this is NOT the GameObject name.")]
        [SerializeField] private string _memberName;
        [SerializeField] private T _value;

        public void Reset()
        {
            _memberName = name;
        }

        public override string memberName 
        { 
            get => _memberName; 
            protected set => _memberName = value; 
        }

        public T value { get => _value; set => _value = value; }

        public override object GetValue() => _value;

        public override void ApplyTo(Object target)
            => PropertySetter.Set(target, _memberName, _value, typeof(T));
    }
}

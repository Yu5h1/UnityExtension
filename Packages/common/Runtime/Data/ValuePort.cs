using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib
{

    public abstract class ValuePort : BaseMonoBehaviour, IValuePort
    {
        [SerializeField] private System.StringComparison _searchComparison = System.StringComparison.OrdinalIgnoreCase;
        public System.StringComparison searchComparison { get => _searchComparison; protected set => _searchComparison = value; }

        public virtual string GetFieldName() => gameObject.name;
        public abstract string GetValue();
        public abstract void SetValue(string value, System.StringComparison comparision);
        public void SetValue(string value) => SetValue(value, searchComparison);
        public void SetValue(Object bindable)
        { 
            if (bindable is IValuePort Ibindable)
                SetValue(Ibindable.GetValue());
        }
    }
    public abstract class ValuePort<T> : ValuePort
    {
        public abstract T value { get; set; }
        [SerializeField] private UnityEvent<T> _ValueChanged;
        public event UnityAction<T> valueChanged 
        {
            add { _ValueChanged.AddListener(value); }
            remove { _ValueChanged.RemoveListener(value); }
        }
    }
    public interface IValuePort
    {
        string GetFieldName();
        string GetValue();
        void SetValue(string value);
        void SetValue(Object Ibindable);
    }
}

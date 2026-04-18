using UnityEngine;
using UnityEngine.Events;
using Yu5h1Lib.Serialization;

namespace Yu5h1Lib
{
    public interface IValuePort
    {
        string GetFieldName();
        string GetValue();
        void SetValue(string value);
        void SetValue(Object Ibindable);
    }
    public interface IValuePort<TValue> : IBindable
    {
        TValue value { get; set; }
        bool TryParse(string value, out TValue result);
        void AddListener(UnityAction<TValue> method);
        void RemoveListener(UnityAction<TValue> method);
    }
    public interface IBindable : IValuePort
    {
        void BindTo(DataView other);
        void Unbind();
    }
    public abstract class ValuePort : BaseMonoBehaviour, IValuePort
    {
        [SerializeField] private System.StringComparison _searchComparison = System.StringComparison.OrdinalIgnoreCase;
        public System.StringComparison searchComparison { get => _searchComparison; protected set => _searchComparison = value; }

        protected override void OnInitializing() {}

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
}

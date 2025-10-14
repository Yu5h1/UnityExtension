using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib
{

    public abstract class Bindable : BaseMonoBehaviour, IBindable
    {
        public virtual string GetFieldName() => gameObject.name;
        public abstract string GetValue();
        public abstract void SetValue(string value);        
        public void SetValue(Object bindable)
        { 
            if (bindable is IBindable Ibindable)
                SetValue(Ibindable.GetValue());
        }
    }
    public abstract class Bindable<T> : Bindable
    {
        public abstract T value { get; set; }
        [SerializeField] private UnityEvent<T> _ValueChanged;
        public event UnityAction<T> valueChanged 
        {
            add { _ValueChanged.AddListener(value); }
            remove { _ValueChanged.RemoveListener(value); }
        }
    }
    public interface IBindable
    {
        string GetFieldName();
        string GetValue();
        void SetValue(string value);
        void SetValue(Object Ibindable);
    }
}

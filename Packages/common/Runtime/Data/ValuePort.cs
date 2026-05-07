using UnityEngine;
using UnityEngine.Events;
using Yu5h1Lib.MVVM;

namespace Yu5h1Lib
{
    public interface UValuePort<TValue> : IValuePort<TValue>
    {
        event UnityAction<TValue> ChangedCallback;
    }

    public abstract class ValuePort : BaseMonoBehaviour, IValuePort
    {
        [SerializeField] private System.StringComparison _searchComparison = System.StringComparison.OrdinalIgnoreCase;

        public System.StringComparison searchComparison { get => _searchComparison; protected set => _searchComparison = value; }
        public object Value { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        protected override void OnInitializing() {}

        public virtual string GetFieldName() => gameObject.name;
        public void SetValue(string value) => SetValue(value, searchComparison);

        public abstract string GetValue();
        public abstract void SetValue(string value, System.StringComparison comparision);

        public void SetValue(Object bindable)
        { 
            if (bindable is IValuePort Ibindable)
                SetValue(Ibindable);
        }
        public void SetValue(IValuePort Ibindable)
        {
            Ibindable.GetValue().print();
            SetValue(Ibindable.GetValue());
        }


        public abstract event UnityAction ChangedCallback;
        private UnityAction ReadFromThis;
        public void BindTo(IDataView dataview)
        {
            Unbind();
            ReadFromThis = () => dataview.ReadFrom(this);
            ChangedCallback += ReadFromThis;
        }
        public void Unbind()
        {
            if (ReadFromThis == null) return;
            ChangedCallback -= ReadFromThis;
            ReadFromThis = null;
        }
        protected virtual void OnDestroy() => Unbind();
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

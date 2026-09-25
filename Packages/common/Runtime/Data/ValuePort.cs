using UnityEngine;
using UnityEngine.Events;
using Yu5h1Lib.MVVM;

namespace Yu5h1Lib
{
    public interface UValuePort<TValue> : IValuePort<TValue>
    {
        event UnityAction<TValue> ChangedCallback;
    }

    public abstract class ValuePortBase : BaseMonoBehaviour, IValuePort
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


        private UnityAction ReadFromThis;
        public void BindTo(IDataView dataview)
        {
            Unbind();
            ReadFromThis = () => dataview.ReadFrom(this);
        }
        public void Unbind() => ReadFromThis = null;

        /// <summary>
        /// Call when the value changes so a bound DataView reads it back. Deliberately not named
        /// ChangedCallback: that name belongs to the typed <see cref="UValuePort{TValue}"/> event,
        /// which this non-generic string layer cannot match.
        /// </summary>
        protected void NotifyValueChanged() => ReadFromThis?.Invoke();
        protected virtual void OnDestroy() => Unbind();
    }

    /// <summary>Plain string-valued <see cref="IValuePort"/>. Interactive components own their own type parsing; this only holds the wire value.</summary>
    public class ValuePort : ValuePortBase
    {
        [SerializeField] private string _value;

        public override string GetValue() => _value;
        public override void SetValue(string value, System.StringComparison comparision)
        {
            if (_value == value) return;
            _value = value;
            NotifyValueChanged();
        }
    }
}

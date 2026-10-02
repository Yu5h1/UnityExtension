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
        public void SetValue(IValuePort Ibindable) => SetValue(Ibindable.GetValue());


        private UnityAction ReadFromThis;
        private bool binding;

        /// <summary>Writes the DataView's value into this port, then notifies exactly once. Notifications
        /// raised by the write itself are suppressed so listeners do not hear the bound value twice.</summary>
        public void BindTo(IDataView dataview)
        {
            Unbind();
            ReadFromThis = () => dataview.ReadFrom(this);
            binding = true;
            try { dataview.WriteTo(this); }
            finally { binding = false; }
            NotifyValueChanged();
        }
        public void Unbind() => ReadFromThis = null;


        /// <summary>
        /// Call when the value changes so a bound DataView reads it back. Deliberately not named
        /// ChangedCallback: that name belongs to the typed <see cref="UValuePort{TValue}"/> event,
        /// which this non-generic string layer cannot match.
        /// </summary>
        public void NotifyValueChanged()
        {
            if (binding) return;
            ReadFromThis?.Invoke();
            OnValueChanged();
        }

        /// <summary>Runs after a bound DataView has read the new value; override to raise a component's own event.</summary>
        protected virtual void OnValueChanged() {}
        protected virtual void OnDestroy() => Unbind();
    }

    /// <summary>Plain string-valued <see cref="IValuePort"/>. Interactive components own their own type parsing; this only holds the wire value.
    /// <c>changed</c> plays the role of a control's onValueChanged: it fires on every change and once when bound, after the DataView has read the value.</summary>
    public class ValuePort : ValuePortBase
    {
        [SerializeField] private string _value;
        [SerializeField] private UnityEvent<string> _changed = new UnityEvent<string>();

        public event UnityAction<string> changed
        {
            add => _changed.AddListener(value);
            remove => _changed.RemoveListener(value);
        }

        protected override void OnValueChanged() => _changed?.Invoke(_value);

        public override string GetValue() => _value;
        public override void SetValue(string value, System.StringComparison comparision)
        {
            if (_value == value) return;
            _value = value;
            NotifyValueChanged();
        }
    }
}

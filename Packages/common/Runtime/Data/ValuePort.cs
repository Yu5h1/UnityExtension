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

    /// <summary>
    /// A general-purpose binding that is one value: drop it into a host's bindings and change it from code or a
    /// UnityEvent. <see cref="kind"/> (string by default) fixes the value type; the value is still stored as the
    /// shared codec's string (<see cref="PlayerPrefsSerializer.Default"/>), and input that does not convert to
    /// <see cref="kind"/> is rejected with a warning. <c>changed</c> plays the role of a control's onValueChanged:
    /// it fires on every change and once when bound, after the DataView has read the value. The typed
    /// <c>SetValue</c> overloads let a <c>UnityEvent&lt;T&gt;</c> (e.g. Slider.onValueChanged) drive it directly.
    /// </summary>
    public class ValuePort : ValuePortBase, Preferences.ITypedPort
    {
        public enum ValueKind { String, Bool, Int, Float, Double, Vector2, Vector3, Vector4, Vector2Int, Vector3Int, Quaternion, Color, Rect }

        [SerializeField] private ValueKind _kind = ValueKind.String;
        [SerializeField] private string _value;
        [SerializeField] private UnityEvent<string> _changed = new UnityEvent<string>();

        public ValueKind kind => _kind;
        public System.Type ValueType => TypeOf(_kind);

        public static System.Type TypeOf(ValueKind kind)
        {
            switch (kind)
            {
                case ValueKind.Bool: return typeof(bool);
                case ValueKind.Int: return typeof(int);
                case ValueKind.Float: return typeof(float);
                case ValueKind.Double: return typeof(double);
                case ValueKind.Vector2: return typeof(Vector2);
                case ValueKind.Vector3: return typeof(Vector3);
                case ValueKind.Vector4: return typeof(Vector4);
                case ValueKind.Vector2Int: return typeof(Vector2Int);
                case ValueKind.Vector3Int: return typeof(Vector3Int);
                case ValueKind.Quaternion: return typeof(Quaternion);
                case ValueKind.Color: return typeof(Color);
                case ValueKind.Rect: return typeof(Rect);
                default: return typeof(string);
            }
        }

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
            if (_kind != ValueKind.String && !PlayerPrefsSerializer.Default.TryDeserialize(value, ValueType, out _))
            {
                $"ValuePort {name}: '{value}' is not a {ValueType.Name}; ignored.".printWarning();
                return;
            }
            _value = value;
            NotifyValueChanged();
        }

        public bool TryGet<T>(out T value)
        {
            value = default;
            if (typeof(T) != ValueType)
            {
                $"ValuePort {name} holds {ValueType.Name}, not {typeof(T).Name}.".printWarning();
                return false;
            }
            return PlayerPrefsSerializer.Default.TryDeserialize(_value, out value);
        }

        /// <summary>The value as <typeparamref name="T"/>; default with a warning when <typeparamref name="T"/> is
        /// not <see cref="ValueType"/>, default when the stored string does not convert.</summary>
        public T Get<T>() => TryGet(out T value) ? value : default;

        /// <summary>Sets the value from <typeparamref name="T"/>; rejected with a warning when <typeparamref name="T"/>
        /// is not <see cref="ValueType"/>.</summary>
        public void Set<T>(T value)
        {
            if (typeof(T) != ValueType)
            {
                $"ValuePort {name} holds {ValueType.Name}; a {typeof(T).Name} was rejected.".printWarning();
                return;
            }
            SetValue(PlayerPrefsSerializer.Default.Serialize(value));
        }

        public void SetValue(bool value) => Set(value);
        public void SetValue(int value) => Set(value);
        public void SetValue(float value) => Set(value);
        public void SetValue(double value) => Set(value);
        public void SetValue(Vector2 value) => Set(value);
        public void SetValue(Vector3 value) => Set(value);
        public void SetValue(Vector4 value) => Set(value);
        public void SetValue(Vector2Int value) => Set(value);
        public void SetValue(Vector3Int value) => Set(value);
        public void SetValue(Quaternion value) => Set(value);
        public void SetValue(Color value) => Set(value);
        public void SetValue(Rect value) => Set(value);
    }
}

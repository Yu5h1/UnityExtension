using UnityEngine.Events;
using UnityEngine.UIElements;
using Yu5h1Lib.MVVM;

namespace Yu5h1Lib.UIToolkit
{
    /// <summary>
    /// Wraps a UI Toolkit value control (anything implementing <see cref="INotifyValueChanged{TValue}"/>)
    /// as an <see cref="IValuePort"/>. <paramref name="getValue"/>/<paramref name="setValue"/> already
    /// close over the concrete element, so this class only owns field name, wiring, and the R1-style
    /// unconditional-notify-on-bind pattern shared with <c>ValuePortBase</c>.
    /// </summary>
    internal sealed class VisualElementValuePort<TValue> : IValuePort, Preferences.ITypedPort
    {
        public System.Type ValueType => typeof(TValue);

        private readonly IBindable bindable;
        private readonly INotifyValueChanged<TValue> notifier;
        private readonly System.Func<string> getValue;
        private readonly System.Action<string> setValue;
        private UnityAction readFromThis;
        private EventCallback<ChangeEvent<TValue>> nativeCallback;

        public VisualElementValuePort(IBindable bindable, INotifyValueChanged<TValue> notifier,
            System.Func<string> getValue, System.Action<string> setValue)
        {
            this.bindable = bindable;
            this.notifier = notifier;
            this.getValue = getValue;
            this.setValue = setValue;
        }

        public string GetFieldName() => bindable.bindingPath;
        public string GetValue() => getValue();
        public void SetValue(string value) => setValue(value);
        public void SetValue(IValuePort Ibindable) => SetValue(Ibindable.GetValue());

        public void BindTo(IDataView dataview)
        {
            Unbind();
            readFromThis = () => dataview.ReadFrom(this);
            nativeCallback = _ => readFromThis?.Invoke();
            notifier.RegisterValueChangedCallback(nativeCallback);
            dataview.WriteTo(this);
            NotifyValueChanged();
        }

        public void Unbind()
        {
            if (nativeCallback != null)
                notifier.UnregisterValueChangedCallback(nativeCallback);
            nativeCallback = null;
            readFromThis = null;
        }

        public void NotifyValueChanged() => readFromThis?.Invoke();
    }
}

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
    public interface IUIControl
    {
        RectTransform rectTransform { get; }
    }
    [RequireComponent(typeof(RectTransform))]
    public abstract class UIControl : BaseMonoBehaviour , IUIControl
    {
        [SerializeField, ReadOnly]
        private RectTransform _rectTransform;
        public RectTransform rectTransform => _rectTransform;

        protected virtual void Reset()
        {
            Init();
        }
        protected override void OnInitializing()
        {
            this.GetComponent(ref _rectTransform);
        }
        public static bool TriggerEvent<TEventHandler>(Component ui,ExecuteEvents.EventFunction<TEventHandler> function) where TEventHandler : IEventSystemHandler
        {
            if (ui == null)
                return false;
            ExecuteEvents.Execute(
                ui.gameObject,
                new BaseEventData(EventSystem.current),
                function
            );
            return true;
        }
    }
    public abstract class UIControl<T> : UIControl where T : UIBehaviour
    {
        [SerializeField, ReadOnly] protected T _ui;
        public T ui
        { 
            get
            { 
                if (_ui == null)
                    Get_UIComponent();
                return _ui;
            }

        }

        public virtual void Get_UIComponent() =>_ui = this.GetComponent(ref _ui);

        protected override void OnInitializing()
        {
            base.OnInitializing();
            Get_UIComponent();
        }
        public bool TriggerEvent<TEventHandler>(ExecuteEvents.EventFunction<TEventHandler> function) where TEventHandler : IEventSystemHandler
         => TriggerEvent(ui, function);
    }

    public abstract class UI_Grahpic<T> : UIControl<T> where T : Graphic
    {
        public void SetColor(ColorObject color)
        {
            if (ui == null)
                return;
            ui.color = color;
        }
    }
    public abstract class UIControl<T, TValue> : UIControl<T>, IValuePort<TValue>,IValuePort where T : UIBehaviour
    {
        public abstract TValue value { get; set; }
        public string GetFieldName() => gameObject.name;
        public string GetValue() => value?.ToString() ?? string.Empty;
        public void SetValue(string text) => value = TryParse(text, out TValue result) ? result : default;

        public abstract bool TryParse(string value, out TValue result);

        public void SetValue(Object Ibindable)
        {
            if (Ibindable is IValuePort port)
                SetValue(port.GetValue());
        }

        public abstract void AddListener(UnityAction<TValue> method);
        public abstract void RemoveListener(UnityAction<TValue> method);

        private UnityAction<TValue> ReadFromThis;
        public void BindTo(Serialization.DataView dataview)
        {
            Unbind();
            ReadFromThis = _ => dataview.ReadFrom(this);
            AddListener(ReadFromThis);
        }
        public void Unbind()
        {
            if (ReadFromThis == null) return;
            RemoveListener(ReadFromThis);
            ReadFromThis = null;
        }
    }
}

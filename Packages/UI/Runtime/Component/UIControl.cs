using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Yu5h1Lib.MVVM;

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
        public Selectable selectable => ui as Selectable;

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
}

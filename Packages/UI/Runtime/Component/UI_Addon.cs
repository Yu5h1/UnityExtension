using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
    [RequireComponent(typeof(RectTransform))]
    public abstract class UI_Addon : BaseMonoBehaviour
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
    }

    public abstract class UI_Addon<T> : UI_Addon where T : UIBehaviour
    {
        [SerializeField, ReadOnly] private T _ui;
        public T ui => _ui;

        protected override void OnInitializing()
        {
            base.OnInitializing();
            _ui = this.GetComponent(ref _ui);
        }
        public bool TriggerEvent<TEventHandler>(ExecuteEvents.EventFunction<TEventHandler> function) where TEventHandler : IEventSystemHandler
        {
            if (_ui == null)
                return false;
            ExecuteEvents.Execute(
                _ui.gameObject,
                new BaseEventData(EventSystem.current),
                function
            );
            return true;
        }
    }

    public abstract class UI_GrahpicAddon<T> : UI_Addon<T> where T : Graphic
    {

        public void SetColor(ColorObject color)
        {
            if (ui == null)
                return;
            ui.color = color;
        }
    }
}

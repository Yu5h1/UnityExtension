using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Yu5h1Lib.Common;

namespace Yu5h1Lib.UI
{
    public abstract class SelectableAdapter<TOps> : UI_Adapter<TOps>, ISelectableOps, ISelectHandler, IDeselectHandler
        where TOps : class, ISelectableOps
        
    {
        public Selectable selectable => (Selectable)RawComponent;
        public bool interactable { get => selectable.interactable; set => selectable.interactable = value; }

        private UnityEvent<BaseEventData> _selected;
        public event UnityAction<BaseEventData> selected
        {
            add => _selected.AddListener(value);
            remove => _selected.RemoveListener(value);
        }
        private UnityEvent<BaseEventData> _deselected;
        public event UnityAction<BaseEventData> deselected
        {
            add => _deselected.AddListener(value);
            remove => _deselected.RemoveListener(value);
        }

        protected override void OnInitializing()
        {
            base.OnInitializing();


        }
        public virtual void OnSelect(BaseEventData eventData) => _selected?.Invoke(eventData);
        public virtual void OnDeselect(BaseEventData eventData) => _deselected?.Invoke(eventData);

        protected virtual void OnDisable()
        {
            if ($"{name}".printWarningIf(selectable == null))
                return;
            selectable.Deselect();
        }

     
    } 
}

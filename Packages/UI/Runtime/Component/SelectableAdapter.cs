using System.Collections;
using UnityEngine;
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

        [SerializeField] private UnityEvent<BaseEventData> _selected;
        public event UnityAction<BaseEventData> selected
        {
            add => _selected.AddListener(value);
            remove => _selected.RemoveListener(value);
        }
        [SerializeField] private UnityEvent<BaseEventData> _deselected;
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
        public virtual void OnDeselect(BaseEventData eventData)
        {
            StartCoroutine(DelayInvoke(eventData));
            
        }
        IEnumerator DelayInvoke(BaseEventData eventData)
        {
            yield return null; // Wait for the next frame to ensure the UI is ready
            _deselected?.Invoke(eventData);
        }

        public virtual void ShowAndFocus()
        {
            gameObject.SetActive(true);
            selectable.Select();
        }
        public void Deactivate()
        {
            SetFocus(false);
            gameObject.SetActive(false);
        }
        public void SetFocus(bool focused)
        {
            if (EventSystem.current.currentSelectedGameObject == gameObject)
                EventSystem.current.SetSelectedGameObject(focused ? gameObject : null);
        }

        protected virtual void OnDisable()
        {
            if ($"{name}".printWarningIf(selectable == null))
                return;
            selectable.Deselect();
        }

     
    } 
}

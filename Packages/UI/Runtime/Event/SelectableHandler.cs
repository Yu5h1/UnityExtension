using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Yu5h1Lib;
using Yu5h1Lib.UI;

public class SelectableHandler : UIControl, ISelectHandler, IDeselectHandler//, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField,ReadOnly]
    private Selectable _selectable;
    public Selectable selectable => _selectable;
    [SerializeField]
    private UnityEvent<BaseEventData> _selected;
    public event UnityAction<BaseEventData> selected
    {
        add => _selected.AddListener(value);
        remove => _selected.RemoveListener(value);
    }
    public Selectable next => selectable?.FindSelectableOnDown();

    [SerializeField]
    private UnityEvent<BaseEventData> _deselected;
    public event UnityAction<BaseEventData> deselected
    {
        add => _deselected.AddListener(value);
        remove => _deselected.RemoveListener(value);
    }

    protected override void OnInitializing()
    {
        base.OnInitializing();
        this.GetComponent(ref _selectable);
    }

    public void Select ()
    {
        if (!selectable)
            return;
        selectable.Select();
    }
    public void OnSelect(BaseEventData eventData) => _selected?.Invoke(eventData);
    public void OnDeselect(BaseEventData eventData) => _deselected?.Invoke(eventData);
}

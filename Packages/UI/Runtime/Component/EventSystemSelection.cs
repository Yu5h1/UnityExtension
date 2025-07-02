using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using Yu5h1Lib;

public class EventSystemSelection : SingletonBehaviour<EventSystemSelection>
{
    [SerializeField] private UnityEvent<GameObject, GameObject> _SelectionChanged;
    public static event UnityAction<GameObject, GameObject> SelectionChanged
    { 
        add => instance._SelectionChanged.AddListener(value);
        remove => instance._SelectionChanged.RemoveListener(value);
    }

    public static GameObject lastSelected { get; private set; } = null;
    public static GameObject previousSelected { get; private set; } = null;
    protected override void OnInstantiated()
    {
    }
    protected override void OnInitializing()
    {
    }
    private void Start()
    {
        
    }
    void Update()
    {
        var current = EventSystem.current.currentSelectedGameObject;
        if (current != lastSelected)
        {
            _SelectionChanged?.Invoke(lastSelected, current);
            previousSelected = lastSelected;
            lastSelected = current;
        }
    }
    public void Print(GameObject previous,GameObject current) => $"previous:{previous} current:{current}".print();

}

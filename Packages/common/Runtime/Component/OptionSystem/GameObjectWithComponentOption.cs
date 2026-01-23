using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Yu5h1Lib;

public abstract class GameObjectWithComponentOption<T> : GameObjectOption where T : Component
{
    [SerializeField,ReadOnly] private T _Component;
    public T Component => _Component;

    [SerializeField] private UnityEvent<T> _ComponentChanged;
    public event UnityAction<T> ComponentChanged
    {
        add => _ComponentChanged.AddListener(value);
        remove => _ComponentChanged.RemoveListener(value);
    }

    protected override void OnSelected(GameObject current)
    {
        base.OnSelected(current);
        _Component = current.GetComponent<T>();
        _ComponentChanged?.Invoke(_Component);
        OnComponentChanged(_Component);
    }
    protected virtual void OnComponentChanged(T current) {}
}

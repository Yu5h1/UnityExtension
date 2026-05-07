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

    protected override void OnSelected(int index)
    {
        base.OnSelected(index);
        if (Items[index].TryGetComponent(out T component))
        {
            _Component = component;
            _ComponentChanged?.Invoke(_Component);
            OnComponentChanged(component);
        }
        else
            $"{gameObject.name} {typeof(T)} not found".printWarning();
    }
    protected abstract void OnComponentChanged(T newItem);
}

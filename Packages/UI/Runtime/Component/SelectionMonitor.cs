using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Yu5h1Lib;

public class SelectionMonitor : SingletonBehaviour<SelectionMonitor>
{
    private UnityEvent<GameObject> _selectionChanged;
    public event UnityAction<GameObject> selectionChanged
    { 
        add => _selectionChanged.AddListener(value);
        remove => _selectionChanged.RemoveListener(value);
    }

    public static GameObject lastSelected { get; private set; }

    public bool checkOnUpdate = true;

    protected override void OnInstantiated() { }
    protected override void OnInitializing() { }

    private void Update()
    {
        if (checkOnUpdate)
            CheckAndNotifySelectionChange();
    }

    public void CheckAndNotifySelectionChange()
    {
        var current = EventSystem.current?.currentSelectedGameObject;
        if (current != lastSelected)
        {
            _selectionChanged?.Invoke(current);
            lastSelected = current;

#if UNITY_EDITOR
            UnityEditor.EditorGUIUtility.PingObject(current);
#endif
        }
    }


}

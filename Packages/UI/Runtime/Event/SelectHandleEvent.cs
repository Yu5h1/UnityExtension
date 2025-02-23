using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class SelectHandleEvent : MonoBehaviour, ISelectHandler
{
    [SerializeField]
    private UnityEvent<BaseEventData> select;

    public void OnSelect(BaseEventData eventData)
        => select?.Invoke(eventData);
}

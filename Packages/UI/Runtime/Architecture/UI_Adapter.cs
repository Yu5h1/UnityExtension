using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Yu5h1Lib.Common;

public abstract class UI_Adapter<TOps> : ComponentAdapter<TOps>, IOps where TOps : class, IOps
{
    private RectTransform _rectTransform;
    public RectTransform rectTransform
    {
        get
        {
            if (Object.ReferenceEquals(_rectTransform, null) && RawComponent != null)
            {
                if (RawComponent is Graphic graphic)
                    _rectTransform = graphic.rectTransform;
                else
                    RawComponent.TryGetComponent(out _rectTransform);
            }
            return _rectTransform;
        }
    }
    public bool TriggerEvent<TEventHandler>(ExecuteEvents.EventFunction<TEventHandler> function) where TEventHandler : IEventSystemHandler
    {
        if (RawComponent == null)
            return false;
        ExecuteEvents.Execute(
            RawComponent.gameObject,
            new BaseEventData(EventSystem.current),
            function
        );
        return true;
    }
}

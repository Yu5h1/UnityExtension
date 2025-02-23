using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Yu5h1Lib
{
    public class PointerClickHandlerEvent : BaseEventHandler, IPointerClickHandler
    {
        [SerializeField]
        private UnityEvent<PointerEventData> doubleClick;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.clickCount == 2)
                doubleClick?.Invoke(eventData);
        }
    } 
}

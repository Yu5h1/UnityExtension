using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace Yu5h1Lib
{
    public class PointerClickHandlerEvent : BaseEventHandler, IPointerClickHandler
    {
        [SerializeField,FormerlySerializedAs("doubleClick")]
        private UnityEvent<PointerEventData> _doubleClick;

        [SerializeField]
        private UnityEvent<PointerEventData> _rightClicked;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.clickCount == 1)
            { 
                if (eventData.button == PointerEventData.InputButton.Right)
                    _rightClicked?.Invoke(eventData);
            }
            if (eventData.clickCount == 2)
                _doubleClick?.Invoke(eventData);
        }
    } 
}

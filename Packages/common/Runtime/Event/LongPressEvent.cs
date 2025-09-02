using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.Events;

namespace Yu5h1Lib
{
    public class TouchLongPressHandler : UnityEventEnhanced<PointerEventData>
    {
        //[SerializeField] private float longPressTime = 2f; 
        //private bool _isPressed = false;
        //public bool isPressed
        //{
        //    get => _isPressed;
        //    private set => _isPressed = value;
        //}


        //private Coroutine longPressCoroutine;
        //public void OnPointerDown(IPointerDownHandler handler,PointerEventData eventData)
        //{
        //    isPressed = true;
        //    longPressCoroutine = handler?.StartCoroutine(LongPressCoroutine());
        //}

        //public void OnPointerUp(PointerEventData eventData)
        //{
        //    isPressed = false;
        //    if (longPressCoroutine != null)
        //    {
        //        StopCoroutine(longPressCoroutine);
        //        longPressCoroutine = null;
        //    }
        //}

        //private IEnumerator LongPressCoroutine()
        //{
        //    yield return new WaitForSeconds(longPressTime);

        //    if (isPressed)
        //    {
        //        Debug.Log("長按事件觸發！");
        //        OnLongPress();
        //    }
        //}

        //private void OnLongPress()
        //{
        //   this.Invoke
        //}
    } 
}
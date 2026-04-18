using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Yu5h1Lib.Serialization;

namespace Yu5h1Lib.UI
{
    public class SliderAdvanced : Slider 
    {
        [SerializeField] private bool wrap;

        public override void OnDrag(PointerEventData eventData)
        {
            if (!wrap || !IsActive() || !IsInteractable())
            {
                base.OnDrag(eventData);
                return;
            }

            RectTransform clickRect = handleRect ?? (RectTransform)transform;
            float pixelLength = (direction <= Direction.RightToLeft)
                ? clickRect.rect.width
                : clickRect.rect.height;

            if (pixelLength <= 0) return;

            float delta = (direction <= Direction.RightToLeft)
                ? eventData.delta.x
                : eventData.delta.y;

            if (direction == Direction.RightToLeft || direction == Direction.TopToBottom)
                delta = -delta;

            float range = maxValue - minValue;
            float valueDelta = (delta / pixelLength) * range;
            value = minValue + Mathf.Repeat(value - minValue + valueDelta, range);
        }

        public void UpdateText(Text text)
        {
            text.text = (value * 100.0f).ToString("0.#") + "%";
        }
    }

}


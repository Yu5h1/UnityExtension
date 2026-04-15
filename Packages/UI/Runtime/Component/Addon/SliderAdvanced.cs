using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
    public class SliderAdvanced : Slider, IValuePort
    {
        [SerializeField] private bool wrap;

        #region ValuePort
        public string GetFieldName() => gameObject.name;
        public string GetValue() => value.ToString();
        public void SetValue(string valueText) 
        {
            if (float.TryParse(valueText, out float result))
                value = result;
            else
                $"Failed to parse '{valueText}' as a float for {gameObject.name}".printWarning();
        }

        public void SetValue(Object Ibindable)
        {
            if (Ibindable is IValuePort valuePort)
                SetValue(valuePort.GetValue());
            else
                $"Object {Ibindable.name} does not implement IValuePort".printWarning();
        } 
        #endregion

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


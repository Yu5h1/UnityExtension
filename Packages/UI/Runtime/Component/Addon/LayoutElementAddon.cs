using UnityEngine;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
	[RequireComponent(typeof(LayoutElement))]
	public class LayoutElementAddon : UIControl<LayoutElement>
	{
		[SerializeField] private AspectRatioFitter _aspectRatioFitter;

        protected override void Reset()
        {
            base.Reset();
            TryGetComponent(out _aspectRatioFitter);
            if (!_aspectRatioFitter)
                _aspectRatioFitter = GetComponent<AspectRatioFitter>();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (ui == null || _aspectRatioFitter == null)
                return;

            float ratio = _aspectRatioFitter.aspectRatio;
            if (ratio <= 0f)
                return;

            switch (_aspectRatioFitter.aspectMode)
            {
                case AspectRatioFitter.AspectMode.None:
                    break;

                case AspectRatioFitter.AspectMode.WidthControlsHeight:
                    ui.preferredHeight = rectTransform.rect.width / ratio;
                    break;

                case AspectRatioFitter.AspectMode.HeightControlsWidth:
                    ui.preferredWidth = rectTransform.rect.height * ratio;
                    break;

                case AspectRatioFitter.AspectMode.FitInParent:
                {
                    var parentRect = GetParentRect();
                    float parentAspect = parentRect.height > 0f ? parentRect.width / parentRect.height : ratio;
                    if (ratio >= parentAspect)
                    {
                        ui.preferredWidth  = parentRect.width;
                        ui.preferredHeight = parentRect.width / ratio;
                    }
                    else
                    {
                        ui.preferredHeight = parentRect.height;
                        ui.preferredWidth  = parentRect.height * ratio;
                    }
                    break;
                }

                case AspectRatioFitter.AspectMode.EnvelopeParent:
                {
                    var parentRect = GetParentRect();
                    float parentAspect = parentRect.height > 0f ? parentRect.width / parentRect.height : ratio;
                    if (ratio >= parentAspect)
                    {
                        ui.preferredHeight = parentRect.height;
                        ui.preferredWidth  = parentRect.height * ratio;
                    }
                    else
                    {
                        ui.preferredWidth  = parentRect.width;
                        ui.preferredHeight = parentRect.width / ratio;
                    }
                    break;
                }
            }
        }

        private Rect GetParentRect()
        {
            var parent = rectTransform.parent as RectTransform;
            return parent != null ? parent.rect : rectTransform.rect;
        }
    }
}

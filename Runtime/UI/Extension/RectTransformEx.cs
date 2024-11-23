using System.ComponentModel;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEngine.UI
{
    [EditorBrowsable(EditorBrowsableState.Never), Browsable(false)]
    public static class RectTransformEx
    {
        public static void FitSize(this RectTransform rectTransform)
        {
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, LayoutUtility.GetPreferredSize(rectTransform, (int)RectTransform.Axis.Horizontal));
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, LayoutUtility.GetPreferredSize(rectTransform, (int)RectTransform.Axis.Vertical));
        }
        public static void SetPositionFromScreenPoint(this RectTransform rectTransform, Vector2 position,Camera camera) {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, position, camera, out position);
            rectTransform.localPosition = position;
        }

        public static Vector2 GetLocalPoint(this RectTransform t, Vector2 screenPoint)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(t, screenPoint, null, out Vector2 localPoint);
            return localPoint;
        }
    }
}

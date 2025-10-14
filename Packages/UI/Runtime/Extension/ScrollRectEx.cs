using System.ComponentModel;
using UnityEngine;
using UnityEngine.UI;
using Yu5h1Lib.Runtime;

namespace Yu5h1Lib.UI
{
	[Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
	public static class ScrollRectEx
	{
        public static void ScrollToPosition(this ScrollRect scrollRect, Vector3 worldPosition)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                scrollRect.content,
                RectTransformUtility.WorldToScreenPoint(null, worldPosition),
                null,
                out Vector2 localPos);

            ScrollToLocalPosition(scrollRect, localPos);
        }
        public static void ScrollToLocalPosition(this ScrollRect scrollRect, Vector2 localPosition)
        {
            Vector2 viewportSize = scrollRect.viewport.rect.size;
            Vector2 contentSize = scrollRect.content.rect.size;

            var alignment = scrollRect.content.pivot.y < 0.5f ? Alignment.Bottom : Alignment.Top;

            Vector2 targetAnchoredPos = new Vector2(
                scrollRect.content.anchoredPosition.x,
                  viewportSize.y - (localPosition.y + scrollRect.content.pivot.y * contentSize.y)
            );
            float maxY = Mathf.Max(0, contentSize.y - viewportSize.y);
            var range = (alignment.HasFlag(Alignment.Bottom) ? Vector2.left :  Vector2.up) * maxY;
            targetAnchoredPos.y = Mathf.Clamp(targetAnchoredPos.y, range.x, range.y); 

            scrollRect.content.anchoredPosition = targetAnchoredPos;
        }
    } 
}

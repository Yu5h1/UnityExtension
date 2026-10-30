using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// A target is visible when its RectTransform overlaps this object's RectTransform. Put it with a
    /// <see cref="VisibilityTracker"/> (scope Children) on a scroll view's viewport, so only the viewport's own content is
    /// compared and rects on other canvases never are.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class ViewportVisibility : MonoBehaviour, IVisibilityTest
    {
        private static readonly Vector3[] Corners = new Vector3[4];

        public bool IsVisible(VisibilityTarget target)
            => target.transform is RectTransform rect && WorldRect((RectTransform)transform).Overlaps(WorldRect(rect));

        private static Rect WorldRect(RectTransform rect)
        {
            rect.GetWorldCorners(Corners);
            return Rect.MinMaxRect(Corners[0].x, Corners[0].y, Corners[2].x, Corners[2].y);
        }
    }
}

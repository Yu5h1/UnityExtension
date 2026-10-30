using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// Sees a target whose RectTransform overlaps the perceiver's own RectTransform in world space: put it with a
    /// <see cref="Perceiver"/> on a scroll view's viewport.
    /// </summary>
    public class RectViewportSight : MonoBehaviour, ISightStrategy
    {
        private static readonly Vector3[] Corners = new Vector3[4];

        public bool Check(IPerceiver perceiver, IPerceptible target)
            => perceiver is Component viewer && viewer.transform is RectTransform view
            && target is Component seen && seen.transform is RectTransform rect
            && WorldRect(view).Overlaps(WorldRect(rect));

        private static Rect WorldRect(RectTransform rect)
        {
            rect.GetWorldCorners(Corners);
            return Rect.MinMaxRect(Corners[0].x, Corners[0].y, Corners[2].x, Corners[2].y);
        }
    }
}

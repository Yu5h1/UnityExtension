using System.ComponentModel;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Yu5h1Lib.UI
{
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
    public static class RectTransformEx 
    {
        public static void Align(this RectTransform self, RectTransform target)
        {
            if (self == null || target == null) return;

            var parent = self.parent as RectTransform;
            if (parent == null)
            {
                // No parent RectTransform to convert into; fallback to world position alignment.
                self.position = GetWorldCenter(target);
                return;
            }

            // Find the canvas that drives the parent (needed to know which camera to use).
            var canvas = parent.GetComponentInParent<Canvas>();
            var cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? canvas.worldCamera
                : null;

            var worldCenter = GetWorldCenter(target);
            var screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldCenter);

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, cam, out var localPoint))
            {
                self.anchoredPosition = localPoint;
            }
            else
            {
                // Fallback (very rare): just set world position
                self.position = worldCenter;
            }
        }

        private static Vector3 GetWorldCenter(RectTransform rt)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            // bottom-left (0) + top-right (2) -> center
            return (corners[0] + corners[2]) * 0.5f;
        }
    }
}
using System.ComponentModel;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Yu5h1Lib.Runtime;

namespace Yu5h1Lib.UI
{
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
    public static class RectTransformEx
    {
        public static void FitSize(this RectTransform rectTransform)
        {
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, LayoutUtility.GetPreferredSize(rectTransform, (int)RectTransform.Axis.Horizontal));
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, LayoutUtility.GetPreferredSize(rectTransform, (int)RectTransform.Axis.Vertical));
        }
        public static void SetPositionFromScreenPoint(this RectTransform rectTransform, Vector2 position, Camera camera)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, position, camera, out position);
            rectTransform.localPosition = position;
        }

        public static Vector2 GetLocalPoint(this RectTransform t, Vector2 screenPoint)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(t, screenPoint, null, out Vector2 localPoint);
            return localPoint;
        }
        public static void SetAnchorPreset(this RectTransform rectTransform, Alignment alignment)
        {
            switch (alignment)
            {
                case Alignment.TopLeft: SetFixed(rectTransform, new Vector2(0, 1)); break;
                case Alignment.Top: SetFixed(rectTransform, new Vector2(0.5f, 1)); break;
                case Alignment.TopRight: SetFixed(rectTransform, new Vector2(1, 1)); break;
                case Alignment.Left: break;
                case Alignment.VerticalLeft: SetFixed(rectTransform, new Vector2(0, 0.5f)); break;
                case Alignment.Center: SetFixed(rectTransform, new Vector2(0.5f, 0.5f)); break;
                case Alignment.Right: break;
                case Alignment.VerticalRight: SetFixed(rectTransform, new Vector2(1, 0.5f)); break;
                case Alignment.BottomLeft: SetFixed(rectTransform, new Vector2(0, 0)); break;
                case Alignment.Bottom: SetFixed(rectTransform, new Vector2(0.5f, 0)); break;
                case Alignment.BottomRight: SetFixed(rectTransform, new Vector2(1, 0)); break;
                case Alignment.HorizontalTop: SetStretchHorizontal(rectTransform, 1); break;
                case Alignment.Horizontal: SetStretchHorizontal(rectTransform, 0.5f); break;
                case Alignment.HorizontalBottom: SetStretchHorizontal(rectTransform, 0); break;
                case Alignment.Vertical: SetStretchVertical(rectTransform, 0.5f); break;
                case Alignment.Fill:
                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.one;
                    rectTransform.offsetMin = Vector2.zero;
                    rectTransform.offsetMax = Vector2.zero;
                    rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    break;
                default:
                    if ((int)alignment == -1)
                        ApplyFill(rectTransform);
                    break;
            }
        }
        private static void ApplyFill(RectTransform t)
        {
            t.anchorMin = Vector2.zero;
            t.anchorMax = Vector2.one;
            t.offsetMin = Vector2.zero;
            t.offsetMax = Vector2.zero;
            t.pivot = new Vector2(0.5f, 0.5f);
        }


        #region Private

        // ©T©wÂI preset (9®æ)
        private static void SetFixed(RectTransform rect, Vector2 point)
        {
            rect.anchorMin = point;
            rect.anchorMax = point;
            rect.pivot = point;
            rect.anchoredPosition = Vector2.zero;
        }

        // Stretch Horizontal (Top/Middle/Bottom)
        private static void SetStretchHorizontal(RectTransform rect, float y)
        {
            rect.anchorMin = new Vector2(0, y);
            rect.anchorMax = new Vector2(1, y);
            rect.pivot = new Vector2(0.5f, y);
            rect.offsetMin = new Vector2(0, rect.offsetMin.y);
            rect.offsetMax = new Vector2(0, rect.offsetMax.y);
            rect.anchoredPosition = Vector2.zero;
        }

        // Stretch Vertical (Middle)
        private static void SetStretchVertical(RectTransform rect, float x)
        {
            rect.anchorMin = new Vector2(x, 0);
            rect.anchorMax = new Vector2(x, 1);
            rect.pivot = new Vector2(x, 0.5f);
            rect.offsetMin = new Vector2(rect.offsetMin.x, 0);
            rect.offsetMax = new Vector2(rect.offsetMax.x, 0);
            rect.anchoredPosition = Vector2.zero;
        }
        #endregion



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
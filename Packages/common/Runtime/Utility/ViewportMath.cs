using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// Conversions between a rectangle on screen and the world a camera sees, so 3D objects can be placed
    /// into a UI layout and UI can be asked about a world point.
    /// <para>
    /// Nothing here knows what draws the rectangle. Callers hand in the rect they already have - a
    /// <c>VisualElement.worldBound</c>, a <c>RectTransform</c> rect, a viewport slice - and get plain geometry back.
    /// </para>
    /// </summary>
    public static class ViewportMath
    {
        /// <summary>
        /// World-space height of the camera's view at <paramref name="distance"/> in front of it.
        /// Orthographic cameras ignore the distance; perspective cameras widen with it.
        /// </summary>
        /// <param name="orthographicSize">Half-height for an orthographic camera.</param>
        /// <param name="fieldOfView">Vertical FOV in degrees for a perspective camera.</param>
        public static float ViewHeight(bool orthographic, float orthographicSize, float fieldOfView, float distance)
            => orthographic ? orthographicSize * 2
                            : 2 * distance * Mathf.Tan(fieldOfView * Mathf.Deg2Rad * .5f);

        /// <summary>World-space size of a rectangle that occupies <paramref name="part"/> of <paramref name="panelBounds"/>.</summary>
        public static bool TrySize(Rect panelBounds, Rect part, float viewWidth, float viewHeight, out Vector2 size)
        {
            size = default;
            if (!panelBounds.IsValid() || !part.IsValid() || !viewWidth.IsFinite() || !viewHeight.IsFinite()) return false;
            size = new Vector2(part.width / panelBounds.width * viewWidth,
                               part.height / panelBounds.height * viewHeight);
            return size.IsFinite();
        }

        /// <summary>
        /// The world point that sits under <paramref name="panelPoint"/> at <paramref name="distance"/> from the camera.
        /// Panel space is y-down; camera viewport space is y-up, and the flip happens here.
        /// </summary>
        public static bool TryPanelToWorld(Camera camera, Rect panelBounds, Vector2 panelPoint,
            float distance, out Vector3 world)
        {
            world = default;
            if (camera == null || !panelBounds.IsValid() || !camera.pixelRect.IsValid()
                || !panelPoint.IsFinite() || !distance.IsFinite()) return false;

            var viewport = new Vector3((panelPoint.x - panelBounds.xMin) / panelBounds.width,
                                   1 - (panelPoint.y - panelBounds.yMin) / panelBounds.height,
                                       distance);
            if (!viewport.IsFinite()) return false;
            world = camera.ViewportToWorldPoint(viewport);
            return world.IsFinite();
        }

        /// <summary>
        /// Where <paramref name="world"/> lands inside <paramref name="panelBounds"/>. The inverse of
        /// <see cref="TryPanelToWorld"/>; use it to ask the UI what covers a world point.
        /// </summary>
        public static bool TryWorldToPanel(Camera camera, Rect panelBounds, Vector3 world, out Vector2 panelPoint)
        {
            panelPoint = default;
            if (camera == null || !panelBounds.IsValid() || !world.IsFinite()) return false;

            Vector3 viewport = camera.WorldToViewportPoint(world);
            if (!viewport.IsFinite()) return false;
            panelPoint = new Vector2(panelBounds.xMin + viewport.x * panelBounds.width,
                                     panelBounds.yMin + (1 - viewport.y) * panelBounds.height);
            return panelPoint.IsFinite();
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Yu5h1Lib.UIToolkit
{
    /// <summary>
    /// Makes a UI document and a 3D scene share one stage, in both directions.
    /// <para>
    /// <b>Scene into UI</b> - <see cref="TryPlace"/> turns a rectangle in a screen-space document into a
    /// world position and size, so a 3D object appears exactly where the layout says it should. Layout keeps
    /// deciding where things go; the scene follows. <see cref="IsVisible"/> then answers whether the UI is
    /// covering that object, which is what lets a poke at the screen reach the model only when nothing is
    /// in the way.
    /// </para>
    /// <para>
    /// <b>UI into scene</b> - <see cref="Fill"/> sizes a world-space <see cref="UIDocument"/> so it exactly
    /// fills the camera at a chosen depth, letting a document act as scenery behind or in front of the
    /// objects rather than as an overlay on top of them.
    /// </para>
    /// <para>
    /// The arithmetic lives in <c>Yu5h1Lib.ViewportMath</c>; this adds the UI Toolkit half - reading a
    /// rectangle off an element, and asking a panel what covers a point.
    /// </para>
    /// </summary>
    public static class World
    {
        // Reused because PickAll fills a caller-owned list and this runs per frame per tracked object.
        private static readonly List<VisualElement> picked = new List<VisualElement>();

        /// <summary>
        /// World position under a point of <paramref name="viewport"/>, at <paramref name="distance"/> from
        /// the camera.
        /// </summary>
        public static bool TryProject(VisualElement viewport, Camera camera, Vector2 panelPoint,
            float distance, out Vector3 world)
        {
            world = default;
            return viewport != null && viewport.panel != null
                && ViewportMath.TryPanelToWorld(camera, viewport.worldBound, panelPoint, distance, out world);
        }

        /// <summary>
        /// Where a 3D object must sit, and how big it must be, to land on <paramref name="element"/>.
        /// </summary>
        /// <param name="viewport">Element the layout is measured against - usually the area the scene shows through.</param>
        /// <param name="element">Rectangle the object should occupy.</param>
        /// <param name="distance">Depth in front of the camera to place it at.</param>
        /// <param name="position">World position of the element's centre.</param>
        /// <param name="size">World size the object must be to cover the element.</param>
        public static bool TryPlace(VisualElement viewport, VisualElement element, Camera camera,
            float distance, out Vector3 position, out Vector2 size)
        {
            position = default; size = default;
            if (viewport == null || element == null || viewport.panel == null || camera == null) return false;

            Rect bounds = viewport.worldBound, target = element.worldBound;
            if (!bounds.IsValid() || !target.IsValid()) return false;

            float height = ViewportMath.ViewHeight(camera.orthographic, camera.orthographicSize,
                camera.fieldOfView, distance);
            if (!height.IsFinite() || !camera.aspect.IsFinite() || camera.aspect <= 0) return false;

            return ViewportMath.TrySize(bounds, target, height * camera.aspect, height, out size)
                && ViewportMath.TryPanelToWorld(camera, bounds, target.center, distance, out position);
        }

        /// <summary>
        /// True when nothing the caller counts as an obstacle covers <paramref name="world"/>.
        /// </summary>
        /// <param name="isObstacle">
        /// What blocks, decided by the caller. The package projects and hit-tests; which elements count as
        /// scenery, as chrome, or as see-through is the application's own rule and must not move in here.
        /// </param>
        public static bool IsVisible(VisualElement root, VisualElement viewport, Camera camera,
            Vector3 world, Func<VisualElement, bool> isObstacle)
        {
            return viewport != null && viewport.panel != null
                && ViewportMath.TryWorldToPanel(camera, viewport.worldBound, world, out var panelPoint)
                && IsClear(root, panelPoint, isObstacle);
        }

        /// <summary>
        /// True when nothing the caller counts as an obstacle covers <paramref name="panelPoint"/>. The
        /// panel-space half of <see cref="IsVisible"/>, for a pointer position that needs no projection.
        /// </summary>
        public static bool IsClear(VisualElement root, Vector2 panelPoint, Func<VisualElement, bool> isObstacle)
        {
            if (root == null || root.panel == null || !panelPoint.IsFinite()) return false;
            if (isObstacle == null) return true;

            picked.Clear();
            root.panel.PickAll(panelPoint, picked);
            foreach (var element in picked)
                if (isObstacle(element)) return false;
            return true;
        }

        /// <summary>
        /// Sizes a world-space document so it exactly fills the camera at <paramref name="distance"/>.
        /// <para>
        /// Size only. Where the document sits is the caller's - a backdrop, a floor and a heads-up plane all
        /// want the same size at a given depth and completely different placements.
        /// </para>
        /// </summary>
        /// <param name="pixelsPerUnit">
        /// The world-space panel's scale, matching its <c>PanelSettings</c>. A world-space document lays out
        /// in panel pixels and is then scaled into world units, so the size written here must be in pixels.
        /// </param>
        public static bool Fill(UIDocument document, Camera camera, float distance, float pixelsPerUnit = 100)
        {
            if (document == null || camera == null || !distance.IsFinite() || !pixelsPerUnit.IsFinite()) return false;

            var root = document.rootVisualElement;
            if (root == null || !camera.aspect.IsFinite() || camera.aspect <= 0) return false;

            float height = ViewportMath.ViewHeight(camera.orthographic, camera.orthographicSize,
                camera.fieldOfView, distance);
            float width = height * camera.aspect;
            if (!height.IsFinite() || !width.IsFinite() || height <= 0 || width <= 0) return false;

            float pixelHeight = height * pixelsPerUnit, pixelWidth = width * pixelsPerUnit;
            if (!pixelHeight.IsFinite() || !pixelWidth.IsFinite()) return false;

            root.style.width = pixelWidth;
            root.style.height = pixelHeight;
            return true;
        }
    }
}

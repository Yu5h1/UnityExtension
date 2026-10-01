using UnityEngine;
using UnityEngine.UIElements;

namespace Yu5h1Lib.UIToolkit
{
    /// <summary>
    /// Makes a runtime panel fit the device it runs on: a sensible size for one UI unit, and content kept
    /// clear of notches and home indicators.
    /// <para>
    /// Everything reads <c>UnityEngine.Device</c> rather than <see cref="Application"/>/<see cref="Screen"/>.
    /// On a real device the two are identical; only the Device variants are overridden by the Device
    /// Simulator, so without them the simulator reports the host desktop and nothing here can be checked
    /// in the Editor.
    /// </para>
    /// </summary>
    public static class DevicePanel
    {
        /// <summary>One UI unit on desktop. Desktop OSes already report a logical dpi (96 x the OS scale
        /// factor), so this keeps desktop at the OS's own scaling.</summary>
        public const float DesktopReferenceDpi = 96;

        /// <summary>One UI unit on mobile - Android's dp, close to iOS's point. Phones report physical dpi,
        /// so a 460dpi phone comes out at roughly 2.9x, about 400 units wide.</summary>
        public const float MobileReferenceDpi = 160;

        /// <summary>
        /// Switches <paramref name="settings"/> to <see cref="PanelScaleMode.ConstantPhysicalSize"/> with the
        /// reference dpi for the current platform. Under <see cref="PanelScaleMode.ConstantPixelSize"/> (the
        /// default) a high-density phone draws every unit as one physical pixel, and the whole UI comes out a
        /// fraction of the screen.
        /// </summary>
        public static void ApplyScale(PanelSettings settings)
        {
            settings.scaleMode = PanelScaleMode.ConstantPhysicalSize;
            settings.referenceDpi = settings.fallbackDpi =
                UnityEngine.Device.Application.isMobilePlatform ? MobileReferenceDpi : DesktopReferenceDpi;
        }

        /// <summary>
        /// Keeps <paramref name="element"/>'s padding equal to the device's safe-area insets, re-applied on
        /// every geometry change so rotation picks up the new ones. Meant for an element that covers the whole
        /// screen - typically the document root - so its background still bleeds under the notch while its
        /// content is pushed clear. Any padding set on it some other way is overwritten.
        /// </summary>
        public static void PadToSafeArea(VisualElement element)
        {
            element.RegisterCallback<GeometryChangedEvent>(_ => ApplySafeAreaPadding(element));
        }

        /// <summary>
        /// Insets of <paramref name="safeArea"/> within a screen of <paramref name="screenSize"/>, converted to
        /// panel units. <paramref name="safeArea"/> is in Unity screen coordinates - physical pixels, origin
        /// bottom-left - so the top inset is measured from <c>yMax</c>, not <c>y</c>.
        /// </summary>
        public static (float top, float right, float bottom, float left) SafeAreaInsets(
            Rect safeArea, Vector2 screenSize, float panelUnitsPerPixel)
        {
            return ((screenSize.y - safeArea.yMax) * panelUnitsPerPixel,
                    (screenSize.x - safeArea.xMax) * panelUnitsPerPixel,
                    safeArea.yMin * panelUnitsPerPixel,
                    safeArea.xMin * panelUnitsPerPixel);
        }

        private static void ApplySafeAreaPadding(VisualElement element)
        {
            if (element.panel == null) return;
            var screenSize = new Vector2(UnityEngine.Device.Screen.width, UnityEngine.Device.Screen.height);
            float panelUnitsPerPixel = element.panel.visualTree.layout.width / screenSize.x;
            if (float.IsNaN(panelUnitsPerPixel) || panelUnitsPerPixel <= 0) return;

            var (top, right, bottom, left) =
                SafeAreaInsets(UnityEngine.Device.Screen.safeArea, screenSize, panelUnitsPerPixel);
            element.style.paddingTop = top;
            element.style.paddingRight = right;
            element.style.paddingBottom = bottom;
            element.style.paddingLeft = left;
        }
    }
}

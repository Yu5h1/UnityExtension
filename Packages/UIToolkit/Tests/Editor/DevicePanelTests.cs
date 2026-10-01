using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Yu5h1Lib.UIToolkit.Tests
{
    public class DevicePanelTests
    {
        [Test]
        public void ApplyScaleSwitchesToPhysicalSizeWithAPlatformReferenceDpi()
        {
            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            try
            {
                DevicePanel.ApplyScale(settings);
                Assert.AreEqual(PanelScaleMode.ConstantPhysicalSize, settings.scaleMode);
                Assert.AreEqual(settings.referenceDpi, settings.fallbackDpi);
                Assert.That(settings.referenceDpi,
                    Is.EqualTo(DevicePanel.DesktopReferenceDpi).Or.EqualTo(DevicePanel.MobileReferenceDpi));
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void NotchedPortraitPhoneMeasuresTopFromTheBottomLeftOrigin()
        {
            // A notched phone in portrait: Unity's safe area starts 102px up from the bottom (home indicator)
            // and stops 141px short of the top (notch).
            var (top, right, bottom, left) = DevicePanel.SafeAreaInsets(
                new Rect(0, 102, 1170, 2289), new Vector2(1170, 2532), 1);

            Assert.AreEqual(141, top, .001f);
            Assert.AreEqual(102, bottom, .001f);
            Assert.AreEqual(0, left, .001f);
            Assert.AreEqual(0, right, .001f);
        }

        [Test]
        public void LandscapeInsetsLandOnTheSides()
        {
            var (top, right, bottom, left) = DevicePanel.SafeAreaInsets(
                new Rect(132, 63, 2268, 1107), new Vector2(2532, 1170), 1);

            Assert.AreEqual(132, left, .001f);
            Assert.AreEqual(132, right, .001f);
            Assert.AreEqual(63, bottom, .001f);
            Assert.AreEqual(0, top, .001f);
        }

        [Test]
        public void InsetsAreConvertedToPanelUnits()
        {
            var (top, _, bottom, _) = DevicePanel.SafeAreaInsets(
                new Rect(0, 102, 1170, 2289), new Vector2(1170, 2532), .5f);

            Assert.AreEqual(70.5f, top, .001f);
            Assert.AreEqual(51, bottom, .001f);
        }

        [Test]
        public void AFullScreenSafeAreaHasNoInsets()
        {
            var (top, right, bottom, left) = DevicePanel.SafeAreaInsets(
                new Rect(0, 0, 1920, 1080), new Vector2(1920, 1080), 1);

            Assert.AreEqual(0, top + right + bottom + left, .001f);
        }
    }
}

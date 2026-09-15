using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Yu5h1Lib.UIToolkit.Tests
{
    /// <summary>
    /// Placement and occlusion against a live panel.
    /// <para>
    /// These are PlayMode tests on purpose. A runtime <see cref="UIDocument"/> lays out only while the Game
    /// view is rendering, so in EditMode they pass under <c>-batchmode</c>, which runs the main loop, and
    /// fail inside an unfocused Editor, which does not - leaving every <c>worldBound</c> as NaN. A test that
    /// passes in CI and fails on a desk is worse than no test.
    /// </para>
    /// </summary>
    public class WorldPanelTests
    {
        private const float PanelWidth = 800, PanelHeight = 600;

        private Camera camera;
        private GameObject host;
        private PanelSettings settings;
        private VisualElement viewport, anchor, chrome;

        [SetUp]
        public void SetUp()
        {
            camera = new GameObject("WorldPanelTests.Camera").AddComponent<Camera>();
            camera.transform.position = new Vector3(0, 0, -10);
            camera.orthographic = true;
            camera.orthographicSize = 5;
            camera.aspect = PanelWidth / PanelHeight;

            settings = ScriptableObject.CreateInstance<PanelSettings>();
            host = new GameObject("WorldPanelTests.Document");
            var document = host.AddComponent<UIDocument>();
            document.panelSettings = settings;

            var root = document.rootVisualElement;
            root.style.position = Position.Absolute;
            root.style.width = PanelWidth;
            root.style.height = PanelHeight;

            viewport = Absolute(0, 0, PanelWidth, PanelHeight);
            root.Add(viewport);

            // A 100x100 target, centred horizontally, in the upper half.
            anchor = Absolute(350, 100, 100, 100);
            viewport.Add(anchor);

            // Something that can cover it, added after so it picks first.
            chrome = Absolute(0, 0, PanelWidth, 260);
            chrome.AddToClassList("chrome");
            viewport.Add(chrome);
        }

        [TearDown]
        public void TearDown()
        {
            if (camera != null) Object.DestroyImmediate(camera.gameObject);
            if (host != null) Object.DestroyImmediate(host);
            if (settings != null) Object.DestroyImmediate(settings);
        }

        private static VisualElement Absolute(float x, float y, float w, float h)
        {
            var element = new VisualElement();
            element.style.position = Position.Absolute;
            element.style.left = x; element.style.top = y;
            element.style.width = w; element.style.height = h;
            return element;
        }

        private static IEnumerator Settle() { yield return null; yield return null; }

        private static bool IsChrome(VisualElement element)
        {
            for (var node = element; node != null; node = node.parent)
                if (node.ClassListContains("chrome")) return true;
            return false;
        }

        [UnityTest]
        public IEnumerator LayoutIsResolvedBeforeAnythingIsAsserted()
        {
            yield return Settle();
            Assert.IsTrue(anchor.worldBound.IsValid(), $"anchor has no layout: {anchor.worldBound}");
            Assert.AreEqual(100f, anchor.worldBound.width, 1e-2f);
        }

        [UnityTest]
        public IEnumerator PlacingAnObjectOnTheAnchorGivesItsWorldRect()
        {
            yield return Settle();
            Assert.IsTrue(World.TryPlace(viewport, anchor, camera, distance: 10, out var position, out var size));

            // The anchor is an eighth of the panel wide; the view is 10 units tall, so it is 10/6 units wide.
            Assert.AreEqual(PanelHeight / 100f, 6f, 1e-4f, "guard: the fixture assumes a 6:1 height ratio");
            Assert.AreEqual(10f / 6, size.y, 1e-3f);
            Assert.AreEqual(10f / 6, size.x, 1e-3f, "a square in a square panel area stays square");

            // Centred horizontally on the camera axis, and above centre because it sits in the upper half.
            Assert.AreEqual(0f, position.x, 1e-3f);
            Assert.Greater(position.y, 0f);
        }

        [UnityTest]
        public IEnumerator AnObjectLowerInTheLayoutSitsLowerInTheWorld()
        {
            var lower = Absolute(350, 400, 100, 100);
            viewport.Add(lower);
            yield return Settle();

            Assert.IsTrue(World.TryPlace(viewport, anchor, camera, 10, out var upper, out _));
            Assert.IsTrue(World.TryPlace(viewport, lower, camera, 10, out var below, out _));
            Assert.Greater(upper.y, below.y, "panel space is y-down, world space is y-up");
        }

        [UnityTest]
        public IEnumerator AProjectedPointRoundTripsBackToTheSameElement()
        {
            yield return Settle();
            Assert.IsTrue(World.TryPlace(viewport, anchor, camera, 10, out var position, out _));
            Assert.IsTrue(World.IsVisible(viewport, viewport, camera, position, _ => false));
        }

        [UnityTest]
        public IEnumerator ChromeOverTheObjectHidesIt()
        {
            yield return Settle();
            Assert.IsTrue(World.TryPlace(viewport, anchor, camera, 10, out var position, out _));

            // The anchor's centre is at y=150, inside the chrome band that covers the top 260px.
            Assert.IsFalse(World.IsVisible(viewport, viewport, camera, position, IsChrome),
                "the caller's own rule must be what decides this");
        }

        [UnityTest]
        public IEnumerator AnObjectClearOfTheChromeStaysVisible()
        {
            var lower = Absolute(350, 400, 100, 100);
            viewport.Add(lower);
            yield return Settle();

            Assert.IsTrue(World.TryPlace(viewport, lower, camera, 10, out var position, out _));
            Assert.IsTrue(World.IsVisible(viewport, viewport, camera, position, IsChrome));
        }

        [UnityTest]
        public IEnumerator TheObstacleRuleBelongsEntirelyToTheCaller()
        {
            yield return Settle();
            Assert.IsTrue(World.TryPlace(viewport, anchor, camera, 10, out var position, out _));

            Assert.IsFalse(World.IsVisible(viewport, viewport, camera, position, _ => true), "everything blocks");
            Assert.IsTrue(World.IsVisible(viewport, viewport, camera, position, _ => false), "nothing blocks");
        }

        [UnityTest]
        public IEnumerator IsClearAnswersForAPanelPointWithoutProjecting()
        {
            yield return Settle();
            Assert.IsFalse(World.IsClear(viewport, new Vector2(400, 150), IsChrome), "inside the chrome band");
            Assert.IsTrue(World.IsClear(viewport, new Vector2(400, 450), IsChrome), "below it");
        }

        [UnityTest]
        public IEnumerator ProjectingAPanelPointLandsOnTheCameraAxisAtTheCentre()
        {
            yield return Settle();
            Assert.IsTrue(World.TryProject(viewport, camera, new Vector2(PanelWidth / 2, PanelHeight / 2),
                distance: 10, out var world));
            Assert.AreEqual(0f, world.x, 1e-3f);
            Assert.AreEqual(0f, world.y, 1e-3f);
        }
    }
}

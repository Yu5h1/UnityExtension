using NUnit.Framework;
using UnityEngine;

namespace Yu5h1Lib.Tests
{
    public class ViewportMathTests
    {
        private Camera camera;
        private static readonly Rect Panel = new Rect(0, 0, 800, 600);

        [SetUp]
        public void SetUp()
        {
            camera = new GameObject("ViewportMathTests").AddComponent<Camera>();
            camera.transform.position = new Vector3(0, 0, -10);
            camera.orthographic = true;
            camera.orthographicSize = 5;
            camera.aspect = Panel.width / Panel.height;
        }

        [TearDown]
        public void TearDown()
        {
            if (camera != null) Object.DestroyImmediate(camera.gameObject);
        }

        [Test]
        public void OrthographicViewHeightIgnoresDistance()
        {
            Assert.AreEqual(10f, ViewportMath.ViewHeight(true, 5, 60, 1), 1e-4f);
            Assert.AreEqual(10f, ViewportMath.ViewHeight(true, 5, 60, 500), 1e-4f);
        }

        [Test]
        public void PerspectiveViewHeightGrowsWithDistance()
        {
            // A 90 degree vertical FOV spans exactly twice the distance.
            Assert.AreEqual(20f, ViewportMath.ViewHeight(false, 5, 90, 10), 1e-3f);
            Assert.Greater(ViewportMath.ViewHeight(false, 5, 60, 20), ViewportMath.ViewHeight(false, 5, 60, 10));
        }

        [Test]
        public void PanelAndWorldAreInverses()
        {
            var point = new Vector2(200, 150);
            Assert.IsTrue(ViewportMath.TryPanelToWorld(camera, Panel, point, 10, out var world));
            Assert.IsTrue(ViewportMath.TryWorldToPanel(camera, Panel, world, out var back));
            Assert.AreEqual(point.x, back.x, 1e-2f);
            Assert.AreEqual(point.y, back.y, 1e-2f);
        }

        [Test]
        public void PanelSpaceIsYDown()
        {
            // Top of the panel must map above the bottom of the panel in world space.
            Assert.IsTrue(ViewportMath.TryPanelToWorld(camera, Panel, new Vector2(400, 0), 10, out var top));
            Assert.IsTrue(ViewportMath.TryPanelToWorld(camera, Panel, new Vector2(400, 600), 10, out var bottom));
            Assert.Greater(top.y, bottom.y);
        }

        [Test]
        public void PanelCentreMapsToTheCameraAxis()
        {
            Assert.IsTrue(ViewportMath.TryPanelToWorld(camera, Panel, Panel.center, 10, out var world));
            Assert.AreEqual(0f, world.x, 1e-3f);
            Assert.AreEqual(0f, world.y, 1e-3f);
        }

        [Test]
        public void SizeScalesWithTheShareOfThePanel()
        {
            Assert.IsTrue(ViewportMath.TrySize(Panel, new Rect(0, 0, 400, 300), 16, 10, out var size));
            Assert.AreEqual(8f, size.x, 1e-4f);
            Assert.AreEqual(5f, size.y, 1e-4f);
        }

        [Test]
        public void DegenerateInputIsRejected()
        {
            Assert.IsFalse(ViewportMath.TryPanelToWorld(camera, new Rect(0, 0, 0, 600), Vector2.zero, 10, out _));
            Assert.IsFalse(ViewportMath.TryPanelToWorld(null, Panel, Vector2.zero, 10, out _));
            Assert.IsFalse(ViewportMath.TryPanelToWorld(camera, Panel, new Vector2(float.NaN, 0), 10, out _));
            Assert.IsFalse(ViewportMath.TryWorldToPanel(camera, Panel, new Vector3(0, float.PositiveInfinity, 0), out _));
        }
    }
}

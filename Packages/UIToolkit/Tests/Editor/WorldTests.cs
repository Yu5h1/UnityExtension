using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Yu5h1Lib.UIToolkit.Tests
{
    /// <summary>
    /// The projection arithmetic is covered by <c>ViewportMathTests</c> in <c>com.yu5h1.common</c>. What is
    /// left here is the UI Toolkit half, most of which needs a live panel: an element outside one has no
    /// <c>worldBound</c> worth reading and no panel to hit-test. So these cover the guards and whatever a
    /// document can prove on its own; placement and occlusion against a real panel are verified interactively.
    /// </summary>
    public class WorldTests
    {
        private Camera camera;
        private GameObject host;
        private UIDocument document;
        private PanelSettings settings;

        [SetUp]
        public void SetUp()
        {
            camera = new GameObject("WorldTests.Camera").AddComponent<Camera>();
            camera.transform.position = new Vector3(0, 0, -10);
            camera.orthographic = true;
            camera.orthographicSize = 5;
            camera.aspect = 16f / 9f;

            settings = ScriptableObject.CreateInstance<PanelSettings>();
            host = new GameObject("WorldTests.Document");
            document = host.AddComponent<UIDocument>();
            document.panelSettings = settings;
        }

        [TearDown]
        public void TearDown()
        {
            if (camera != null) Object.DestroyImmediate(camera.gameObject);
            if (host != null) Object.DestroyImmediate(host);
            if (settings != null) Object.DestroyImmediate(settings);
        }

        [Test]
        public void FillSizesTheDocumentToTheCameraView()
        {
            Assert.IsTrue(World.Fill(document, camera, distance: 10));

            // Orthographic size 5 means a 10-unit tall view; at 100 px per unit that is 1000 px.
            var root = document.rootVisualElement;
            Assert.AreEqual(1000f, root.style.height.value.value, 1e-2f);
            Assert.AreEqual(1000f * camera.aspect, root.style.width.value.value, 1e-2f);
        }

        [Test]
        public void FillHonoursThePanelScale()
        {
            Assert.IsTrue(World.Fill(document, camera, distance: 10, pixelsPerUnit: 50));
            Assert.AreEqual(500f, document.rootVisualElement.style.height.value.value, 1e-2f);
        }

        [Test]
        public void PerspectiveFillGrowsWithDepth()
        {
            camera.orthographic = false;
            camera.fieldOfView = 60;

            Assert.IsTrue(World.Fill(document, camera, distance: 5));
            float near = document.rootVisualElement.style.height.value.value;
            Assert.IsTrue(World.Fill(document, camera, distance: 20));
            float far = document.rootVisualElement.style.height.value.value;

            Assert.Greater(far, near, "a document further away must be bigger to fill the same view");
        }

        [Test]
        public void FillRejectsDegenerateInput()
        {
            Assert.IsFalse(World.Fill(null, camera, 10));
            Assert.IsFalse(World.Fill(document, null, 10));
            Assert.IsFalse(World.Fill(document, camera, float.NaN));
            Assert.IsFalse(World.Fill(document, camera, 10, pixelsPerUnit: float.PositiveInfinity));

            camera.aspect = 0;
            Assert.IsFalse(World.Fill(document, camera, 10));
        }

        [Test]
        public void ZeroHeightCameraCannotFill()
        {
            camera.orthographicSize = 0;
            Assert.IsFalse(World.Fill(document, camera, 10));
        }

        [Test]
        public void ElementsOutsideAPanelCannotBePlaced()
        {
            var viewport = new VisualElement();
            var anchor = new VisualElement();
            Assert.IsFalse(World.TryPlace(viewport, anchor, camera, 10, out _, out _));
            Assert.IsFalse(World.TryProject(viewport, camera, Vector2.zero, 10, out _));
            Assert.IsFalse(World.IsVisible(viewport, viewport, camera, Vector3.zero, _ => false));
        }

        [Test]
        public void NullArgumentsAreRejectedRatherThanThrowing()
        {
            Assert.DoesNotThrow(() =>
            {
                Assert.IsFalse(World.TryPlace(null, null, camera, 10, out _, out _));
                Assert.IsFalse(World.TryProject(null, camera, Vector2.zero, 10, out _));
                Assert.IsFalse(World.IsVisible(null, null, camera, Vector3.zero, null));
                Assert.IsFalse(World.IsClear(null, Vector2.zero, _ => true));
            });
        }

        [Test]
        public void IsClearRejectsANonFinitePoint()
        {
            Assert.IsFalse(World.IsClear(new VisualElement(), new Vector2(float.NaN, 0), _ => false));
        }
    }
}

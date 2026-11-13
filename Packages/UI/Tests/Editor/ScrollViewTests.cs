
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Yu5h1Lib.UI.Tests
{
    /// <summary>Exercises ScrollView goal 1 with native layout, pointer drag, wheel and scrollbar dispatch.</summary>
    public sealed class ScrollViewTests
    {
        private GameObject root;
        private EventSystem eventSystem;
        private ScrollView scroll;
        private RectTransform viewport;
        private RectTransform content;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Scroll test", typeof(RectTransform), typeof(Canvas));
            root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            eventSystem = new GameObject("Event system", typeof(EventSystem)).GetComponent<EventSystem>();
            scroll = NewRect("ScrollView", root.transform, 300, 600).gameObject.AddComponent<ScrollView>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.ContentUpdateBehavior = ContentUpdateBehavior.FollowLatest;
            scroll.inertia = false;
            viewport = NewRect("Viewport", scroll.transform, 300, 600);
            content = NewRect("Items", viewport, 300, 600);
            content.anchorMin = Vector2.up;
            content.anchorMax = Vector2.one;
            content.pivot = Vector2.up;
            content.sizeDelta = new Vector2(0f, 600f);
            content.anchoredPosition = Vector2.zero;
            scroll.viewport = viewport;
            scroll.content = content;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            var bar = NewRect("Scrollbar", scroll.transform, 12, 600).gameObject.AddComponent<ScrollViewScrollbar>();
            bar.handleRect = NewRect("Handle", bar.transform, 12, 40);
            bar.direction = Scrollbar.Direction.BottomToTop;
            bar.ScrollView = scroll;
            scroll.verticalScrollbar = bar;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(eventSystem.gameObject);
        }

        [Test]
        public void OverflowClampsAtBothEdgesAndOnViewportResize()
        {
            scroll.StopFollowing();
            SetItems(400);
            Assert.That(scroll.Offset, Is.Zero);
            Assert.That(scroll.verticalScrollbar.gameObject.activeSelf, Is.False);
            Wheel(-100f);
            Assert.That(scroll.Offset, Is.Zero);
            SetItems(1000);
            Assert.That(scroll.MaxOffset, Is.EqualTo(400f));
            Assert.That(scroll.Offset, Is.Zero);
            Assert.That(scroll.verticalScrollbar.gameObject.activeSelf, Is.True);
            scroll.ScrollToOffset(400);
            viewport.sizeDelta = new Vector2(300, 800);
            scroll.RefreshContent();
            Assert.That(scroll.Offset, Is.EqualTo(200f));
            scroll.ScrollToOffset(500f);
            Assert.That(scroll.Offset, Is.EqualTo(200f));
        }

        [Test]
        public void ShortListStartsAtBottomAndFollowsGrowth()
        {
            SetItems(200);
            Assert.That(ItemTop("m1"), Is.EqualTo(400f).Within(.05f));
            Assert.That(scroll.Following, Is.True);
            SetItems(800);
            Assert.That(scroll.Offset, Is.EqualTo(200f));
            Assert.That(ItemTop("m1"), Is.EqualTo(-200f).Within(.05f));
        }

        [Test]
        public void DragDisengagesUntilReleasedAndHostCanResume()
        {
            SetItems(1000);
            var drag = StartDrag();
            MoveDrag(drag, 5f);
            Assert.That(scroll.Following, Is.False);
            Assert.That(scroll.Offset, Is.EqualTo(395f).Within(.05f));
            SetItems(1100);
            Assert.That(scroll.Offset, Is.EqualTo(395f).Within(.05f));
            scroll.OnEndDrag(drag);
            Assert.That(scroll.Following, Is.False);
            scroll.RequestFollow();
            Assert.That(scroll.Offset, Is.EqualTo(500f));
            Assert.That(scroll.Following, Is.True);
        }

        [Test]
        public void HostFollowDiscardsTheOldDragOrigin()
        {
            SetItems(1000);
            var drag = StartDrag();
            MoveDrag(drag, 50f);
            scroll.RequestFollow();
            MoveDrag(drag, 70f);
            Assert.That(scroll.Following, Is.True);
            Assert.That(scroll.Offset, Is.EqualTo(400f));
            scroll.OnEndDrag(drag);
        }

        [Test]
        public void PointerBelowNativeDragThresholdKeepsFollowing()
        {
            SetItems(1000);
            var press = new PointerEventData(eventSystem);
            ExecuteEvents.Execute(scroll.gameObject, press, ExecuteEvents.initializePotentialDrag);
            Assert.That(scroll.Following, Is.True);
            var drag = StartDrag();
            Assert.That(scroll.Following, Is.False);
            MoveDrag(drag, 4f);
            Assert.That(scroll.Offset, Is.EqualTo(396f).Within(.05f));
        }

        [Test]
        public void WheelOnlyDisengagesWhenContentOverflows()
        {
            SetItems(400);
            Wheel(40f);
            Assert.That(scroll.Following, Is.True);
            Assert.That(scroll.Offset, Is.Zero);
            SetItems(1000);
            Wheel(40f);
            Assert.That(scroll.Offset, Is.EqualTo(360f).Within(.05f));
            Assert.That(scroll.Following, Is.False);
        }

        [Test]
        public void RebuildAndResizePreserveTopVisibleItemAndOffsetFallback()
        {
            viewport.sizeDelta = new Vector2(300, 500);
            SetItems(200, 150, 150, 250, 250);
            scroll.StopFollowing();
            scroll.ScrollToOffset(500);
            Assert.That(ItemTop("m4"), Is.Zero.Within(.05f));
            SetItems(200, 150, 150, 250, 250, 120);
            Assert.That(scroll.Offset, Is.EqualTo(500f));
            Assert.That(ItemTop("m4"), Is.Zero.Within(.05f));
            SetItems(200, 150, 150, 250, 250, 120);
            Assert.That(scroll.Offset, Is.EqualTo(500f));
            Assert.That(ItemTop("m4"), Is.Zero.Within(.05f));
            SetItems(200, 200, 150, 250, 250, 120);
            Assert.That(scroll.Offset, Is.EqualTo(550f));
            Assert.That(ItemTop("m4"), Is.Zero.Within(.05f));
            content.Find("m4").name = "replacement";
            scroll.RefreshContent();
            Assert.That(scroll.Offset, Is.EqualTo(550f));
        }

        [Test]
        public void UserDragAndWheelResumeWithinBottomTolerance()
        {
            SetItems(1000);
            scroll.StopFollowing();
            scroll.ScrollToOffset(300f);
            var drag = StartDrag();
            MoveDrag(drag, -90f);
            scroll.OnEndDrag(drag);
            Assert.That(scroll.Offset, Is.EqualTo(390f).Within(.05f));
            Assert.That(scroll.Following, Is.False);
            drag = StartDrag();
            MoveDrag(drag, -5f);
            Assert.That(scroll.Following, Is.False);
            scroll.OnEndDrag(drag);
            Assert.That(scroll.Offset, Is.EqualTo(395f).Within(.05f));
            Assert.That(scroll.Following, Is.True);
            SetItems(1050);
            Assert.That(scroll.Offset, Is.EqualTo(450f));
            Wheel(20f);
            Assert.That(scroll.Following, Is.False);
            Wheel(-12f);
            Assert.That(scroll.Offset, Is.EqualTo(442f).Within(.05f));
            Assert.That(scroll.Following, Is.True);
        }

        [Test]
        public void ScrollbarUserGestureDisengagesWithoutCountingProgrammaticScroll()
        {
            SetItems(1000);
            scroll.ScrollToOffset(350f);
            Assert.That(scroll.Following, Is.True);
            var bar = scroll.verticalScrollbar;
            var pointer = new PointerEventData(eventSystem) { position = Vector2.zero };
            bar.OnPointerDown(pointer);
            bar.value = .5f;
            bar.OnPointerUp(pointer);
            Assert.That(scroll.Following, Is.False);
            Assert.That(scroll.Offset, Is.EqualTo(200f).Within(.05f));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void LoopWrapsBothWaysAndReusesPrefabInstances(bool horizontal)
        {
            const int count = 24;
            const float size = 100f;
            const float cycle = count * size;
            scroll.horizontal = horizontal;
            scroll.vertical = !horizontal;
            scroll.Loop = true;
            var bar = (ScrollViewScrollbar)scroll.verticalScrollbar;
            if (horizontal)
            {
                scroll.verticalScrollbar = null;
                scroll.horizontalScrollbar = bar;
                bar.direction = Scrollbar.Direction.LeftToRight;
            }
            Object.DestroyImmediate(content.GetComponent<VerticalLayoutGroup>());
            var template = NewRect("Item", root.transform, size, size);
            var path = AssetDatabase.GenerateUniqueAssetPath("Assets/ScrollViewTestItem.prefab");
            try
            {
                var prefab = (RectTransform)PrefabUtility.SaveAsPrefabAsset(template.gameObject, path).transform;
                scroll.SetItems(prefab, count, size, (item, index) => item.name = "Item " + index);
                scroll.ScrollToOffset(cycle - 20f);
                Assert.That(content.Find("Item 23"), Is.Not.Null);
                Assert.That(content.Find("Item 0"), Is.Not.Null);
                Assert.That(scroll.Following, Is.False);
                scroll.RequestFollow();
                Assert.That(scroll.Offset, Is.EqualTo(cycle - 20f).Within(.05f));

                void WheelBy(float delta) => scroll.OnScroll(new PointerEventData(eventSystem)
                {
                    scrollDelta = horizontal ? new Vector2(-delta, 0f) : new Vector2(0f, -delta),
                });

                WheelBy(60f);
                Assert.That(scroll.Offset, Is.EqualTo(40f).Within(.05f));
                WheelBy(-60f);
                Assert.That(scroll.Offset, Is.EqualTo(cycle - 20f).Within(.05f));

                var created = scroll.CreatedItemCount;
                var original = new System.Collections.Generic.HashSet<Transform>();
                foreach (Transform child in content)
                    original.Add(child);
                foreach (Transform child in scroll.transform)
                    if (!child.gameObject.activeSelf)
                        original.Add(child);
                for (int i = 0; i < 18; i++)
                    WheelBy(cycle / 2f);
                Assert.That(scroll.Offset, Is.EqualTo(cycle - 20f).Within(.05f));
                Assert.That(scroll.CreatedItemCount, Is.EqualTo(created));
                Assert.That(content.childCount, Is.LessThan(count));
                foreach (Transform child in content)
                    Assert.That(original.Contains(child), Is.True);

                Canvas.ForceUpdateCanvases();
                Assert.That(bar.value, Is.EqualTo(horizontal ? (cycle - 20f) / cycle : 20f / cycle).Within(.001f));
                var overlay = bar.GetComponent<CanvasGroup>();
                var pointer = new PointerEventData(eventSystem);
                Assert.That(overlay.alpha, Is.Zero);
                bar.OnPointerEnter(pointer);
                Assert.That(overlay.alpha, Is.EqualTo(1f));
                bar.value = .5f;
                Assert.That(scroll.Offset, Is.EqualTo(cycle / 2f).Within(.05f));
                Assert.That(content.Find("Item 12"), Is.Not.Null);
                bar.OnPointerExit(pointer);
                Assert.That(overlay.alpha, Is.Zero);
                eventSystem.SetSelectedGameObject(bar.gameObject);
                Assert.That(overlay.alpha, Is.EqualTo(1f));
                eventSystem.SetSelectedGameObject(null);
                Assert.That(overlay.alpha, Is.Zero);

                scroll.ScrollToOffset(cycle - 20f);
                var drag = StartDrag();
                drag.position = horizontal ? new Vector2(-60f, 0f) : new Vector2(0f, 60f);
                scroll.OnDrag(drag);
                scroll.OnEndDrag(drag);
                Assert.That(scroll.Offset, Is.EqualTo(40f).Within(.05f));

                scroll.Loop = false;
                scroll.ScrollToOffset(cycle + 20f);
                Assert.That(scroll.Offset, Is.EqualTo(cycle - (horizontal ? viewport.rect.width : viewport.rect.height)).Within(.05f));
            }
            finally
            {
                AssetDatabase.DeleteAsset(path);
            }
        }

        [Test]
        public void PreservePositionDoesNotResumeAtTheEnd()
        {
            scroll.ContentUpdateBehavior = ContentUpdateBehavior.PreservePosition;
            SetItems(1000f);
            Wheel(-400f);
            Assert.That(scroll.Offset, Is.EqualTo(400f));
            Assert.That(scroll.Following, Is.False);
            SetItems(1000f, 100f);
            scroll.RequestFollow();
            Assert.That(scroll.Offset, Is.EqualTo(400f));
        }

        private void SetItems(params float[] heights)
        {
            for (int i = content.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(content.GetChild(i).gameObject);
            for (int i = 0; i < heights.Length; i++)
                NewRect("m" + (i + 1), content, 300, heights[i]).gameObject
                    .AddComponent<LayoutElement>().preferredHeight = heights[i];
            scroll.RefreshContent();
            Canvas.ForceUpdateCanvases();
        }

        private float ItemTop(string key)
        {
            var item = (RectTransform)content.Find(key);
            return viewport.rect.yMax - viewport.InverseTransformPoint(item.TransformPoint(new Vector3(0, item.rect.yMax))).y;
        }

        private void Wheel(float delta) => scroll.OnScroll(new PointerEventData(eventSystem) { scrollDelta = new Vector2(0, delta) });

        private PointerEventData StartDrag()
        {
            var drag = new PointerEventData(eventSystem) { pointerId = 1, position = Vector2.zero };
            scroll.OnInitializePotentialDrag(drag);
            scroll.OnBeginDrag(drag);
            return drag;
        }

        private void MoveDrag(PointerEventData drag, float dy)
        {
            drag.position = new Vector2(0, -dy);
            scroll.OnDrag(drag);
        }

        private static RectTransform NewRect(string name, Transform parent, float width, float height)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }
    }
}

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Yu5h1Lib.Tests
{
    public class VisibilityTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _created)
                Object.DestroyImmediate(go);
            _created.Clear();
        }

        private RectTransform Rect(string name, Transform parent, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent == null)
                _created.Add(go);
            else
                go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = size;
            return rect;
        }

        [Test]
        public void VisibleStaysTrueUntilTheLastTrackerLosesSight()
        {
            var target = Rect("target", null, Vector2.one).gameObject.AddComponent<VisibilityTarget>();
            var a = Rect("a", null, Vector2.one).gameObject.AddComponent<VisibilityTracker>();
            var b = Rect("b", null, Vector2.one).gameObject.AddComponent<VisibilityTracker>();
            var changes = new List<bool>();
            target.visibleChanged.AddListener(changes.Add);

            target.Seen(a);
            target.Seen(b);
            target.Seen(a);
            target.Unseen(a);
            Assert.IsTrue(target.visible);
            target.Unseen(b);

            Assert.IsFalse(target.visible);
            CollectionAssert.AreEqual(new[] { true, false }, changes);
        }

        [Test]
        public void ViewportSeesOnlyItsOwnOverlappingContent()
        {
            var viewport = Rect("viewport", null, new Vector2(100f, 100f));
            viewport.gameObject.AddComponent<ViewportVisibility>();
            var tracker = viewport.gameObject.AddComponent<VisibilityTracker>();
            var inside = Rect("inside", viewport, new Vector2(20f, 20f)).gameObject.AddComponent<VisibilityTarget>();
            var below = Rect("below", viewport, new Vector2(20f, 20f));
            below.anchoredPosition = new Vector2(0f, -200f);
            var scrolledOut = below.gameObject.AddComponent<VisibilityTarget>();
            var elsewhere = Rect("elsewhere", null, new Vector2(20f, 20f)).gameObject.AddComponent<VisibilityTarget>();

            Assert.IsTrue(tracker.Sees(inside));
            Assert.IsFalse(tracker.Sees(scrolledOut));
            Assert.IsFalse(tracker.Sees(elsewhere), "a target outside the viewport's children is out of scope");
            tracker.scope = VisibilityTracker.Scope.Scene;
            Assert.IsTrue(tracker.Sees(elsewhere));
        }
    }
}

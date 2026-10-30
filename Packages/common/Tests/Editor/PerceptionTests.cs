using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Yu5h1Lib.Tests
{
    public class PerceptionTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _created)
                Object.DestroyImmediate(go);
            _created.Clear();
        }

        private T Make<T>(string name) where T : Component
        {
            var go = new GameObject(name, typeof(RectTransform));
            _created.Add(go);
            return go.AddComponent<T>();
        }

        [Test]
        public void SeenStaysTrueUntilTheLastPerceiverLosesSight()
        {
            var target = Make<Perceptible>("target");
            var a = Make<Perceiver>("a");
            var b = Make<Perceiver>("b");
            var changes = new List<bool>();
            target.seenChanged.AddListener(changes.Add);

            target.Spotted(a);
            target.Spotted(b);
            target.Spotted(a);
            target.LostSight(a);
            Assert.IsTrue(target.seen);
            target.LostSight(b);

            Assert.IsFalse(target.seen);
            CollectionAssert.AreEqual(new[] { true, false }, changes);
        }

        [Test]
        public void ViewportSeesOnlyOverlappingRects()
        {
            var viewport = Make<RectViewportSight>("viewport");
            var perceiver = viewport.gameObject.AddComponent<Perceiver>();
            ((RectTransform)viewport.transform).sizeDelta = new Vector2(100f, 100f);
            var inside = Make<Perceptible>("inside");
            ((RectTransform)inside.transform).sizeDelta = new Vector2(20f, 20f);
            var outside = Make<Perceptible>("outside");
            ((RectTransform)outside.transform).sizeDelta = new Vector2(20f, 20f);
            outside.transform.position = new Vector3(0f, 200f, 0f);

            Assert.IsTrue(viewport.Check(perceiver, inside));
            Assert.IsFalse(viewport.Check(perceiver, outside));
        }
    }
}

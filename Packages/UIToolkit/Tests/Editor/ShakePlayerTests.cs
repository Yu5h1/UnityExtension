using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Yu5h1Lib.UIToolkit.Tests
{
    public class ShakePlayerTests
    {
        private GameObject host;
        private PanelSettings settings;
        private VisualElement root;
        private ShakePlayer player;

        [SetUp]
        public void SetUp()
        {
            settings = ScriptableObject.CreateInstance<PanelSettings>();
            host = new GameObject("ShakePlayerTests");
            var document = host.AddComponent<UIDocument>();
            document.panelSettings = settings;
            root = document.rootVisualElement;
            player = new ShakePlayer();
        }

        [TearDown]
        public void TearDown()
        {
            if (host != null) Object.DestroyImmediate(host);
            if (settings != null) Object.DestroyImmediate(settings);
        }

        private VisualElement Attached()
        {
            var element = new VisualElement();
            root.Add(element);
            return element;
        }

        [Test]
        public void PlayingTracksTheElement()
        {
            player.Play(Attached(), Reaction.Shake.Settings.Default, reduceMotion: false);
            Assert.AreEqual(1, player.Count);
        }

        [Test]
        public void ReduceMotionPlaysNothingAtAll()
        {
            player.Play(Attached(), Reaction.Shake.Settings.Default, reduceMotion: true);
            Assert.AreEqual(0, player.Count);
        }

        [Test]
        public void ANullElementIsIgnored()
        {
            player.Play(null, Reaction.Shake.Settings.Default, false);
            Assert.AreEqual(0, player.Count);
        }

        [Test]
        public void OneElementNeverGetsTwoWobbles()
        {
            var element = Attached();
            player.Play(element, Reaction.Shake.Settings.Default, false);
            player.Play(element, Reaction.Shake.Settings.Default, false);
            Assert.AreEqual(1, player.Count, "a second catch must build on the swing, not start a rival one");
        }

        [Test]
        public void SeparateElementsWobbleSeparately()
        {
            player.Play(Attached(), Reaction.Shake.Settings.Default, false);
            player.Play(Attached(), Reaction.Shake.Settings.Default, false);
            Assert.AreEqual(2, player.Count);
        }

        [Test]
        public void TickingReachesTheElementStyle()
        {
            var element = Attached();
            player.Play(element, Reaction.Shake.Settings.Default, false);
            player.Tick(1f / 60);

            Assert.AreEqual(StyleKeyword.Undefined, element.style.rotate.keyword, "rotation should now be set");
            Assert.AreNotEqual(0f, element.style.rotate.value.angle.value);
        }

        [Test]
        public void AFinishedWobbleRestoresTheElementAndIsDropped()
        {
            var element = Attached();
            Assert.AreEqual(StyleKeyword.Null, element.style.rotate.keyword);

            player.Play(element, Reaction.Shake.Settings.Default, false);
            for (int i = 0; i < 1200 && player.Count > 0; i++) player.Tick(1f / 60);

            Assert.AreEqual(0, player.Count, "the player must let go once the wobble ends");
            Assert.AreEqual(StyleKeyword.Null, element.style.rotate.keyword, "the element must be handed back untouched");
            Assert.AreEqual(StyleKeyword.Null, element.style.scale.keyword);
        }

        [Test]
        public void StopAllRestoresEverythingAtOnce()
        {
            var a = Attached();
            var b = Attached();
            player.Play(a, Reaction.Shake.Settings.Default, false);
            player.Play(b, Reaction.Shake.Settings.Default, false);
            player.Tick(1f / 60);

            player.StopAll();
            Assert.AreEqual(0, player.Count);
            Assert.AreEqual(StyleKeyword.Null, a.style.rotate.keyword);
            Assert.AreEqual(StyleKeyword.Null, b.style.rotate.keyword);
        }

        [UnityTest]
        public IEnumerator AnElementThatLeavesThePanelIsDropped()
        {
            var element = Attached();
            player.Play(element, Reaction.Shake.Settings.Default, false);
            yield return null;

            element.RemoveFromHierarchy();
            yield return null;

            player.Tick(1f / 60);
            Assert.AreEqual(0, player.Count, "nothing is left to restore it to");
        }

        [Test]
        public void TickingWithNothingPlayingIsHarmless()
        {
            Assert.DoesNotThrow(() => player.Tick(1f / 60));
            Assert.DoesNotThrow(() => player.StopAll());
        }
    }
}

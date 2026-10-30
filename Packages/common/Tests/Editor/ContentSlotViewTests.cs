using NUnit.Framework;
using UnityEngine;

namespace Yu5h1Lib.Tests
{
    /// <summary>
    /// ContentSlotView's events follow the slot: the indicator's three states are separate switches, the percent is the
    /// slot's rounded-down whole percent, and the placeholder tells waiting (with progress) from fallback (BGL-PROGRESS,
    /// BGL-PLACEHOLDER, BGL-FALLBACK). The slot's own decisions are the neutral cases in <see cref="BackgroundLoadCaseTests"/>.
    /// </summary>
    public class ContentSlotViewTests
    {
        private class Seen
        {
            public bool indicator, percentShown, indeterminate, preparing, placeholder, waiting, fallback, notice;
            public int percent = -1;
        }

        private GameObject _host;

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_host);

        private Seen Watch(ContentSlot slot)
        {
            _host = new GameObject(nameof(ContentSlotViewTests));
            var view = _host.AddComponent<ContentSlotView>();
            var seen = new Seen();
            view.indicatorVisible.AddListener(v => seen.indicator = v);
            view.percentVisible.AddListener(v => seen.percentShown = v);
            view.percent.AddListener(v => seen.percent = v);
            view.indeterminateVisible.AddListener(v => seen.indeterminate = v);
            view.preparingVisible.AddListener(v => seen.preparing = v);
            view.placeholderVisible.AddListener(v => seen.placeholder = v);
            view.waitingVisible.AddListener(v => seen.waiting = v);
            view.fallbackVisible.AddListener(v => seen.fallback = v);
            view.noticeVisible.AddListener(v => seen.notice = v);
            view.Bind(slot);
            return seen;
        }

        [Test]
        public void NothingLoadingShowsNothing()
        {
            var seen = Watch(new ContentSlot(new[] { "a" }));
            Assert.IsFalse(seen.indicator);
            Assert.IsFalse(seen.placeholder);
            Assert.IsFalse(seen.notice);
        }

        [Test]
        public void IndicatorStatesAreSeparateSwitches()
        {
            var slot = new ContentSlot(new[] { "a" });
            var seen = Watch(slot);
            slot.Start("a");
            Assert.IsTrue(seen.indicator);
            Assert.IsTrue(seen.indeterminate);
            Assert.IsFalse(seen.percentShown);

            slot.Report("a", 0.999f);
            Assert.IsTrue(seen.percentShown);
            Assert.IsFalse(seen.indeterminate);
            Assert.AreEqual(99, seen.percent);

            slot.Report("a", 1f);
            Assert.IsTrue(seen.preparing);
            Assert.IsFalse(seen.percentShown);
        }

        [Test]
        public void WaitingPlaceholderCarriesProgress()
        {
            var slot = new ContentSlot(new[] { "a" });
            var seen = Watch(slot);
            slot.Select("a");
            slot.Report("a", 0.5f);
            Assert.IsTrue(seen.placeholder);
            Assert.IsTrue(seen.waiting);
            Assert.IsFalse(seen.fallback);
            Assert.AreEqual(50, seen.percent);
        }

        [Test]
        public void FailedSelectionIsFallbackWithNotice()
        {
            var slot = new ContentSlot(new[] { "a" });
            var seen = Watch(slot);
            slot.Select("a");
            slot.Complete("a", false);
            Assert.IsTrue(seen.placeholder);
            Assert.IsFalse(seen.waiting);
            Assert.IsTrue(seen.fallback);
            Assert.IsTrue(seen.notice);
            Assert.IsFalse(seen.indicator);
        }

        [Test]
        public void ReadyContentHidesThePlaceholder()
        {
            var slot = new ContentSlot(new[] { "a" });
            var seen = Watch(slot);
            slot.Select("a");
            slot.Complete("a", true);
            Assert.IsFalse(seen.placeholder);
            Assert.IsFalse(seen.indicator);
        }
    }
}

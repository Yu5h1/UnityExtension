using NUnit.Framework;

namespace Yu5h1Lib.Tests
{
    /// <summary>
    /// The placeholder reads the selection's progress while it waits and nothing as the fallback (BGL-PLACEHOLDER,
    /// BGL-FALLBACK); the indicator's three texts are the host's (BGL-PROGRESS). The slot's own decisions are the neutral
    /// cases in <see cref="BackgroundLoadCaseTests"/>.
    /// </summary>
    public class ContentSlotPresentationTests
    {
        private static readonly ContentSlotPresentation Texts = new ContentSlotPresentation("{0} pct", "waiting", "almost");

        [Test]
        public void HiddenIndicatorHasNoText()
        {
            var slot = new ContentSlot(new[] { "a" });
            Assert.IsFalse(Texts.IndicatorVisible(slot));
            Assert.AreEqual(string.Empty, Texts.ProgressText(slot));
            Assert.IsFalse(Texts.PlaceholderVisible(slot));
        }

        [Test]
        public void IndicatorUsesTheHostTexts()
        {
            var slot = new ContentSlot(new[] { "a" });
            slot.Start("a");
            Assert.AreEqual("waiting", Texts.ProgressText(slot));
            slot.Report("a", 0.345f);
            Assert.AreEqual("34 pct", Texts.ProgressText(slot));
            slot.Report("a", 1f);
            Assert.AreEqual("almost", Texts.ProgressText(slot));
            Assert.IsTrue(Texts.IndicatorVisible(slot));
        }

        [Test]
        public void WaitingPlaceholderReadsTheSelectionsProgress()
        {
            var slot = new ContentSlot(new[] { "a" });
            slot.Select("a");
            slot.Report("a", 0.5f);
            Assert.IsTrue(Texts.PlaceholderVisible(slot));
            Assert.AreEqual("50 pct", Texts.PlaceholderText(slot));
            Assert.IsFalse(Texts.NoticeVisible(slot));
        }

        [Test]
        public void FallbackReadsNothingAndRaisesTheNotice()
        {
            var slot = new ContentSlot(new[] { "a" });
            slot.Select("a");
            slot.Complete("a", false);
            Assert.IsTrue(Texts.PlaceholderVisible(slot));
            Assert.AreEqual(string.Empty, Texts.PlaceholderText(slot));
            Assert.IsTrue(Texts.NoticeVisible(slot));
        }

        [Test]
        public void ReadyContentHidesThePlaceholder()
        {
            var slot = new ContentSlot(new[] { "a" });
            slot.Select("a");
            slot.Complete("a", true);
            Assert.IsFalse(Texts.PlaceholderVisible(slot));
            Assert.IsFalse(Texts.IndicatorVisible(slot));
        }
    }
}

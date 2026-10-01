using NUnit.Framework;

namespace Yu5h1Lib.UIToolkit.Tests
{
    public class ChatReplyPerformanceTests
    {
        [Test]
        public void BeforeAnyPushItShowsThinkingWithOneDot()
        {
            var performance = new ChatReplyPerformance();
            Assert.AreEqual("思考中.", performance.Tick(0.1f, reduceMotion: false));
        }

        [Test]
        public void DotsCycleThroughOneToThreeEverySecond()
        {
            var performance = new ChatReplyPerformance();
            Assert.AreEqual("思考中.", performance.Tick(0.5f, false));    // elapsed .5 -> 0 dots-index
            Assert.AreEqual("思考中..", performance.Tick(1f, false));    // elapsed 1.5 -> 1
            Assert.AreEqual("思考中...", performance.Tick(1f, false));   // elapsed 2.5 -> 2
            Assert.AreEqual("思考中.", performance.Tick(1f, false));     // elapsed 3.5 -> 0 again
        }

        [Test]
        public void AppendsElapsedSecondsOnceItReachesFive()
        {
            var performance = new ChatReplyPerformance();
            performance.Tick(4.9f, false);
            string atFour = performance.Tick(0f, false);
            StringAssert.DoesNotContain("s", atFour);

            string atFive = performance.Tick(0.2f, false);
            StringAssert.Contains("5s", atFive);
        }

        [Test]
        public void PushWithoutTickingStillReportsNotDone()
        {
            var performance = new ChatReplyPerformance();
            performance.Push("hello");
            Assert.IsFalse(performance.Done);
        }

        [Test]
        public void TickRevealsTextGraduallyAtAboutThirtyPerSecond()
        {
            var performance = new ChatReplyPerformance();
            performance.Complete("abcdefghij"); // 10 characters, complete so the whole thing is revealable

            string afterOneFrame = performance.Tick(1f / 30, false); // one character's worth of credit
            Assert.AreEqual(1, afterOneFrame.Length);
            Assert.IsFalse(performance.Done, "a single frame at 30/s should not reveal 10 characters at once");

            // Credit banks at most 8 characters per Tick, so a long-held key needs more than one more call -
            // loop rather than assume one big dt finishes it.
            string revealed = afterOneFrame;
            for (int i = 0; i < 10 && !performance.Done; i++) revealed = performance.Tick(1f, false);

            Assert.AreEqual("abcdefghij", revealed);
            Assert.IsTrue(performance.Done);
        }

        [Test]
        public void ReduceMotionRevealsTheCompletedTextImmediately()
        {
            var performance = new ChatReplyPerformance();
            performance.Complete("a whole sentence with no delay");
            Assert.AreEqual("a whole sentence with no delay", performance.Tick(0f, reduceMotion: true));
        }

        [Test]
        public void EvenReduceMotionHoldsBackTheLastElementUntilComplete()
        {
            // A mid-stream snapshot's last text element may still be cut short by the next snapshot, so it
            // is never shown early - reduceMotion skips the pacing, not this safety margin.
            var performance = new ChatReplyPerformance();
            performance.Push("not finished");
            string revealed = performance.Tick(0f, reduceMotion: true);
            Assert.AreEqual("not finishe", revealed);
        }

        [Test]
        public void AnExtendingPushKeepsWhatWasAlreadyRevealed()
        {
            var performance = new ChatReplyPerformance();
            performance.Push("hello");
            string revealedBefore = performance.Tick(1f, reduceMotion: true);
            Assume.That(revealedBefore, Is.Not.Empty);

            performance.Push("hello world"); // extends the same reply
            string revealedAfter = performance.Tick(0f, reduceMotion: false);

            // Nothing here forces a reset: progress only ever grows on an extending push, it never drops
            // back to what a restart from zero would give.
            StringAssert.StartsWith(revealedBefore, revealedAfter);
        }

        [Test]
        public void ADifferentSnapshotResetsTheReveal()
        {
            var performance = new ChatReplyPerformance();
            performance.Push("first reply");
            performance.Tick(1f, reduceMotion: true);
            performance.Push("an unrelated second reply"); // does not start with "first reply"'s revealed prefix
            string revealed = performance.Tick(0f, reduceMotion: false);
            StringAssert.DoesNotContain("first", revealed);
        }

        [Test]
        public void RevealCompleteBeforeFinishingOnlyReturnsWhatHasBeenShown()
        {
            var performance = new ChatReplyPerformance();
            performance.Push("not finished yet");
            string partial = performance.RevealComplete();
            Assert.AreEqual("", partial); // nothing ticked yet, so nothing shown
        }

        [Test]
        public void RevealCompleteAfterCompletingJumpsToTheFullText()
        {
            var performance = new ChatReplyPerformance();
            performance.Complete("the whole thing");
            Assert.AreEqual("the whole thing", performance.RevealComplete());
            Assert.IsTrue(performance.Done);
        }

        [Test]
        public void DoesNotSplitAFlagEmojiAcrossTwoTicks()
        {
            // The Taiwan flag: two regional-indicator surrogate pairs (U+1F1F9, U+1F1FC) forming one
            // grapheme cluster. C# string literals only support \uXXXX (UTF-16), so each codepoint is
            // written as its own surrogate pair rather than a \U escape.
            const string flag = "🇹🇼";
            var performance = new ChatReplyPerformance();
            performance.Complete("hi " + flag);

            string first = performance.Tick(1f / 30, false); // one credit - "h"
            Assert.AreEqual("h", first);

            // Keep ticking; whenever the flag itself becomes part of the revealed text, both halves must
            // appear together - never just one surrogate pair of the pair.
            string revealed = first;
            for (int i = 0; i < 10 && revealed.Length < ("hi " + flag).Length; i++)
                revealed = performance.Tick(1f, false);

            Assert.AreEqual("hi " + flag, revealed);
        }

        [Test]
        public void BeginClearsEverything()
        {
            var performance = new ChatReplyPerformance();
            performance.Complete("done already");
            performance.Tick(10f, true);
            Assert.IsTrue(performance.Done);

            performance.Begin();
            Assert.IsFalse(performance.Done);
            Assert.AreEqual("思考中.", performance.Tick(0f, false));
        }
    }
}

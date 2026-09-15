using NUnit.Framework;
using UnityEngine.UIElements;

namespace Yu5h1Lib.UIToolkit.Tests
{
    /// <summary>
    /// Covers what a detached element can prove. The hold itself runs on the panel scheduler, so its timing,
    /// the move-abandon threshold and the swallowed click need a live panel and are verified interactively.
    /// </summary>
    public class GestureLongPressTests
    {
        private VisualElement element;
        private Gesture.LongPress hold;

        [SetUp]
        public void SetUp()
        {
            element = new VisualElement();
            hold = new Gesture.LongPress();
            element.AddManipulator(hold);
        }

        [Test]
        public void DefaultsMatchTheBehaviourBeingReplaced()
        {
            Assert.AreEqual(400, hold.Duration, "both HealthAI call sites held for 400ms");
            Assert.AreEqual(6f, hold.MoveTolerance);
        }

        [Test]
        public void NothingIsHeldBeforeAPressHappens()
        {
            Assert.IsFalse(hold.IsHolding);
        }

        [Test]
        public void MoveToleranceCanBeDisabled()
        {
            // The colour picker slides straight from the hold into choosing a swatch, so wandering must not
            // abandon the gesture there.
            hold.MoveTolerance = 0;
            Assert.AreEqual(0f, hold.MoveTolerance);
        }

        [Test]
        public void ReleaseClearsTheHoldSoTheNextPressStartsClean()
        {
            hold.Release();
            Assert.IsFalse(hold.IsHolding);
        }

        [Test]
        public void AbandonIsSafeWithNothingInFlight()
        {
            Assert.DoesNotThrow(() => hold.Abandon());
            Assert.IsFalse(hold.IsHolding);
        }

        [Test]
        public void DetachingTheManipulatorAbandonsRatherThanLeaking()
        {
            Assert.DoesNotThrow(() => element.RemoveManipulator(hold));
            Assert.IsFalse(hold.IsHolding);
        }

        [Test]
        public void ItCanBeReattachedToAnotherElement()
        {
            element.RemoveManipulator(hold);
            var other = new VisualElement();
            Assert.DoesNotThrow(() => other.AddManipulator(hold));
        }
    }
}

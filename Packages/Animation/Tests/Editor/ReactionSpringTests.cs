using NUnit.Framework;

namespace Yu5h1Lib.Tests
{
    public class ReactionSpringTests
    {
        private const float Frame = 1f / 60;

        private static Reaction.Spring Settled(float at)
        {
            var spring = new Reaction.Spring(at);
            Assert.IsFalse(spring.IsMoving);
            return spring;
        }

        private static void Run(Reaction.Spring spring, int frames)
        {
            for (int i = 0; i < frames && spring.IsMoving; i++) spring.Tick(Frame, false);
        }

        [Test]
        public void StartsWhereItWasPutAndStaysThere()
        {
            var spring = Settled(5);
            Assert.AreEqual(5f, spring.Value);
            Assert.AreEqual(5f, spring.Target);
            Assert.AreEqual(0f, spring.Velocity);
        }

        [Test]
        public void ReachesItsTargetAndStops()
        {
            var spring = Settled(0);
            spring.MoveTo(10);
            Run(spring, 600);
            Assert.IsFalse(spring.IsMoving, "should settle rather than oscillate forever");
            Assert.AreEqual(10f, spring.Value, 1e-3f);
        }

        [Test]
        public void RetargetingKeepsTheVelocity()
        {
            // The whole reason this is a Reaction and not a Transition: interrupting must not drop speed.
            var spring = Settled(0);
            spring.MoveTo(100);
            Run(spring, 12);
            float speed = spring.Velocity;
            Assert.Greater(speed, 0);

            spring.MoveTo(0);
            Assert.AreEqual(speed, spring.Velocity, 1e-6f, "retarget must carry the velocity through");
        }

        [Test]
        public void SnapDropsEverything()
        {
            var spring = Settled(0);
            spring.MoveTo(100);
            Run(spring, 12);

            spring.Snap(3);
            Assert.AreEqual(3f, spring.Value);
            Assert.AreEqual(3f, spring.Target);
            Assert.AreEqual(0f, spring.Velocity);
            Assert.IsFalse(spring.IsMoving);
        }

        [Test]
        public void ReleaseThrowsItPastTheTarget()
        {
            var spring = Settled(0);
            spring.Release(0, 400);
            spring.Tick(Frame, false);
            Assert.Greater(spring.Value, 0, "a throw must overshoot before coming back");
            Run(spring, 600);
            Assert.AreEqual(0f, spring.Value, 1e-3f);
        }

        [Test]
        public void ReduceMotionArrivesImmediately()
        {
            var spring = Settled(0);
            spring.MoveTo(10);
            spring.Tick(Frame, true);
            Assert.AreEqual(10f, spring.Value);
            Assert.IsFalse(spring.IsMoving);
        }

        [Test]
        public void OneLongFrameDoesNotExplode()
        {
            // Integrated in a single step a 2-second frame would diverge instead of settling.
            var spring = Settled(0);
            spring.MoveTo(10);
            spring.Tick(2f, false);
            Assert.LessOrEqual(spring.Value, 12f);
            Assert.GreaterOrEqual(spring.Value, 0f);
        }

        [Test]
        public void NegativeOrZeroDeltaTimeChangesNothing()
        {
            var spring = Settled(0);
            spring.MoveTo(10);
            spring.Tick(0, false);
            spring.Tick(-1, false);
            Assert.AreEqual(0f, spring.Value);
            Assert.AreEqual(0f, spring.Velocity);
        }

        [Test]
        public void StifferSpringArrivesSooner()
        {
            var slow = Settled(0); slow.MoveTo(10);
            var fast = Settled(0); fast.MoveTo(10);
            for (int i = 0; i < 10; i++)
            {
                slow.Tick(Frame, false, stiffness: 60, damping: 29);
                fast.Tick(Frame, false, stiffness: 400, damping: 29);
            }
            Assert.Greater(fast.Value, slow.Value);
        }
    }
}

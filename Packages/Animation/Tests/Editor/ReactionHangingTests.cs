using NUnit.Framework;
using UnityEngine;

namespace Yu5h1Lib.Tests
{
    public class ReactionHangingTests
    {
        private const float Frame = 1f / 60;
        private static Reaction.Hanging.Settings Defaults => new Reaction.Hanging.Settings();

        private static Reaction.Hanging Knocked(float impulse, Reaction.Hanging.Settings settings = null)
        {
            var hanging = new Reaction.Hanging();
            hanging.Impulse(impulse);
            hanging.Tick(Frame, settings ?? Defaults);
            return hanging;
        }

        [Test]
        public void StartsUprightAndStill()
        {
            var hanging = new Reaction.Hanging();
            Assert.AreEqual(0f, hanging.Angle);
            Assert.IsFalse(hanging.IsMoving);
        }

        [Test]
        public void AKnockStartsItSwinging()
        {
            var hanging = Knocked(60);
            Assert.AreNotEqual(0f, hanging.Angle);
            Assert.IsTrue(hanging.IsMoving);
        }

        [Test]
        public void ItSettlesBackUpright()
        {
            var hanging = Knocked(60);
            for (int i = 0; i < 1200 && hanging.IsMoving; i++) hanging.Tick(Frame, Defaults);
            Assert.IsFalse(hanging.IsMoving, "a swing must die down");
            Assert.AreEqual(0f, hanging.Angle, 1e-2f);
        }

        [Test]
        public void AViolentKnockStillRespectsTheAngleBound()
        {
            var settings = Defaults;
            var hanging = new Reaction.Hanging();
            hanging.Impulse(float.MaxValue);
            for (int i = 0; i < 600; i++)
            {
                hanging.Tick(Frame, settings);
                Assert.LessOrEqual(Mathf.Abs(hanging.Angle), settings.maximumAngle + 1e-3f,
                    "the element must never fold past its limit");
            }
        }

        [Test]
        public void RepeatedKnocksAccumulate()
        {
            var one = Knocked(30);
            var many = new Reaction.Hanging();
            for (int i = 0; i < 3; i++) many.Impulse(30);
            many.Tick(Frame, Defaults);
            Assert.Greater(Mathf.Abs(many.Angle), Mathf.Abs(one.Angle));
        }

        [Test]
        public void ResetReturnsItUprightImmediately()
        {
            var hanging = Knocked(60);
            hanging.Reset();
            Assert.AreEqual(0f, hanging.Angle);
            Assert.IsFalse(hanging.IsMoving);
        }

        [Test]
        public void NonFiniteImpulseIsIgnored()
        {
            var hanging = new Reaction.Hanging();
            hanging.Impulse(float.NaN);
            hanging.Impulse(float.PositiveInfinity);
            hanging.Tick(Frame, Defaults);
            Assert.AreEqual(0f, hanging.Angle);
        }

        [Test]
        public void MissingSettingsDoesNotThrow()
        {
            var hanging = Knocked(60);
            Assert.DoesNotThrow(() => hanging.Tick(Frame, null));
        }

        [Test]
        public void TorqueGrowsWithTheLeverArm()
        {
            var bounds = new Rect(0, 0, 200, 300);
            var drag = new Vector2(0, 500);
            float atCentre = Reaction.Hanging.Torque(bounds, bounds.center, drag, gain: .16f);
            float atEdge = Reaction.Hanging.Torque(bounds, new Vector2(bounds.xMax, bounds.center.y), drag, .16f);
            Assert.Greater(Mathf.Abs(atEdge), Mathf.Abs(atCentre));
        }

        [Test]
        public void TorqueFlipsWithTheSideGrabbed()
        {
            var bounds = new Rect(0, 0, 200, 300);
            var drag = new Vector2(0, 500);
            float left = Reaction.Hanging.Torque(bounds, new Vector2(bounds.xMin, bounds.center.y), drag, .16f);
            float right = Reaction.Hanging.Torque(bounds, new Vector2(bounds.xMax, bounds.center.y), drag, .16f);
            Assert.Less(left * right, 0, "opposite sides must swing opposite ways");
        }

        [Test]
        public void ASliverThinElementCannotProduceEnormousTorque()
        {
            var sliver = new Rect(0, 0, 1, 1);
            float torque = Reaction.Hanging.Torque(sliver, new Vector2(1, 1), new Vector2(0, 1000), .16f);
            Assert.Less(Mathf.Abs(torque), 100f);
        }
    }
}

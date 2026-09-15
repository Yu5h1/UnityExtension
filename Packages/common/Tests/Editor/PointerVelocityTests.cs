using NUnit.Framework;
using UnityEngine;

namespace Yu5h1Lib.Tests
{
    public class PointerVelocityTests
    {
        [Test]
        public void SampleMovesTowardTheInstantaneousReading()
        {
            // 100 units in 0.1s is 1000/s; from rest the estimate lands part of the way there, not all of it.
            var v = PointerVelocity.Sample(Vector2.zero, new Vector2(100, 0), .1f);
            Assert.Greater(v.x, 0);
            Assert.Less(v.x, 1000f, "a single sample must not snap to the raw reading");
        }

        [Test]
        public void RepeatedSamplesConverge()
        {
            var v = Vector2.zero;
            for (int i = 0; i < 40; i++) v = PointerVelocity.Sample(v, new Vector2(10, 0), .01f);
            Assert.AreEqual(1000f, v.x, 1f, "steady dragging should settle on the real speed");
        }

        [Test]
        public void ShortFrameCannotProduceASpike()
        {
            // Without the interval floor this would read as 10 / 0.00001 = 1,000,000 per second.
            var v = PointerVelocity.Sample(Vector2.zero, new Vector2(10, 0), .00001f);
            Assert.LessOrEqual(v.magnitude, 1000f);
        }

        [Test]
        public void ClampIsHonouredAndScalable()
        {
            var v = Vector2.zero;
            for (int i = 0; i < 60; i++) v = PointerVelocity.Sample(v, new Vector2(1000, 0), .01f, maximumSpeed: 50);
            Assert.LessOrEqual(v.magnitude, 50f + 1e-3f);
        }

        [Test]
        public void UnusableSampleReturnsZeroRatherThanKeepingStaleSpeed()
        {
            var moving = new Vector2(500, 0);
            Assert.AreEqual(Vector2.zero, PointerVelocity.Sample(moving, new Vector2(10, 0), 0));
            Assert.AreEqual(Vector2.zero, PointerVelocity.Sample(moving, new Vector2(10, 0), -1));
            Assert.AreEqual(Vector2.zero, PointerVelocity.Sample(moving, new Vector2(float.NaN, 0), .01f));
        }
    }
}

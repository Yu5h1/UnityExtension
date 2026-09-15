using NUnit.Framework;
using UnityEngine;

namespace Yu5h1Lib.Tests
{
    public class SweptContactTests
    {
        private static readonly Rect Target = new Rect(10, 10, 20, 20);

        [Test]
        public void CrossingInOneStepStillHits()
        {
            // The whole point of sweeping: both endpoints are outside, the travel is not.
            Assert.IsTrue(SweptContact.SegmentHit(Target, new Vector2(0, 20), new Vector2(40, 20), out var contact));
            Assert.AreEqual(10f, contact.x, 1e-4f, "should enter at the left edge");
            Assert.AreEqual(20f, contact.y, 1e-4f);
        }

        [Test]
        public void StartingInsideContactsAtTheStart()
        {
            Assert.IsTrue(SweptContact.SegmentHit(Target, new Vector2(20, 20), new Vector2(100, 20), out var contact));
            Assert.AreEqual(new Vector2(20, 20), contact);
        }

        [Test]
        public void TravelThatMissesReportsNoContact()
        {
            Assert.IsFalse(SweptContact.SegmentHit(Target, new Vector2(0, 100), new Vector2(40, 100), out var contact));
            Assert.AreEqual(new Vector2(0, 100), contact, "contact is left at the start on a miss");
        }

        [Test]
        public void StoppingShortOfTheTargetIsNotAHit()
        {
            Assert.IsFalse(SweptContact.SegmentHit(Target, new Vector2(0, 20), new Vector2(5, 20), out _));
        }

        [Test]
        public void ParallelTravelAlongAnEdgeStillClips()
        {
            Assert.IsTrue(SweptContact.SegmentHit(Target, new Vector2(0, 15), new Vector2(40, 15), out var contact));
            Assert.AreEqual(10f, contact.x, 1e-4f);
        }

        [Test]
        public void NonFiniteInputIsRejectedRatherThanPropagated()
        {
            Assert.IsFalse(SweptContact.SegmentHit(Target, new Vector2(float.NaN, 0), new Vector2(40, 20), out _));
            Assert.IsFalse(SweptContact.SegmentHit(Target, Vector2.zero, new Vector2(float.PositiveInfinity, 20), out _));
        }

        [Test]
        public void EmptyTargetCannotBeHit()
        {
            Assert.IsFalse(SweptContact.SegmentHit(new Rect(10, 10, 0, 20), new Vector2(0, 20), new Vector2(40, 20), out _));
        }
    }
}

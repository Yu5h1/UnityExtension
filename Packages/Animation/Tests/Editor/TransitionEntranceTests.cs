using System;
using NUnit.Framework;
using UnityEngine;

namespace Yu5h1Lib.Tests
{
    public class TransitionEntranceTests
    {
        private static readonly Rect Frame = new Rect(0, 0, 1000, 800);
        private static readonly Rect Home = new Rect(450, 350, 100, 100);

        private sealed class Probe : Drive.ISpatial
        {
            public bool IsAlive => true;
            public Rect Rect { get; set; }
            public float Opacity { get; set; } = 1;
        }

        private static readonly Func<float, float> Linear = t => t;
        private static Transition.ScaleTrack Flat => Transition.ScaleTrack.From(t => t);

        private static Transition.Entrance Build(Probe probe, bool absorb = false,
            float start = 0, float end = 1, Vector2 origin = default, float scale = 1)
            => new Transition.Entrance(probe, () => Home, absorb, origin, scale, start, end,
                restSize: 100, Flat, Linear, Linear);

        // --- scheduling ------------------------------------------------------

        [Test]
        public void ASingleItemNeedsNoStagger()
        {
            Assert.AreEqual(0f, Transition.Entrance.Interval(1, 2, .5f));
        }

        [Test]
        public void TheAuthoredGapIsHonouredWhileItFits()
        {
            Assert.AreEqual(.2f, Transition.Entrance.Interval(2, 2, .1f), 1e-4f);
        }

        [Test]
        public void TheGapIsCompressedRatherThanOverrunningTheEnd()
        {
            // Twenty items with a generous gap would push the last launch past the end of the performance.
            float interval = Transition.Entrance.Interval(20, 1, .5f);
            float lastStart = interval * 19;
            Assert.LessOrEqual(lastStart, 1 - Transition.Entrance.MinTripShare + 1e-4f,
                "the last item must always keep time to travel");
        }

        [Test]
        public void TheWholeSetAlwaysFinishesAtTheDuration()
        {
            for (int i = 0; i < 5; i++)
            {
                Transition.Entrance.Schedule(i, 5, 2, .1f, .5f, out float start, out float end);
                Assert.LessOrEqual(end, 2 + 1e-4f);
                Assert.Less(start, end);
            }
        }

        [Test]
        public void LandingTogetherIsIndependentOfSettingOffTogether()
        {
            Transition.Entrance.Schedule(0, 4, 2, .2f, 1, out float firstStart, out float firstEnd);
            Transition.Entrance.Schedule(3, 4, 2, .2f, 1, out float lastStart, out float lastEnd);

            Assert.Less(firstStart, lastStart, "launches are still staggered");
            Assert.AreEqual(firstEnd, lastEnd, 1e-4f, "yet they land together");
        }

        [Test]
        public void StaggeredArrivalsKeepEqualTripLengths()
        {
            Transition.Entrance.Schedule(0, 4, 2, .2f, 0, out float aStart, out float aEnd);
            Transition.Entrance.Schedule(3, 4, 2, .2f, 0, out float bStart, out float bEnd);
            Assert.AreEqual(aEnd - aStart, bEnd - bStart, 1e-4f);
        }

        // --- planning --------------------------------------------------------

        [Test]
        public void APlannedStartSitsOutsideTheFrame()
        {
            var random = new System.Random(1234);
            for (int i = 0; i < 20; i++)
            {
                Transition.Entrance.Plan(Frame, random, 100, Transition.ScaleRange.Default,
                    new Vector2(-1, -1), 90, 1.15f, out var origin, out _);
                Assert.IsFalse(Frame.Contains(origin), $"{origin} was not off-screen");
            }
        }

        [Test]
        public void APlannedStartIsOversized()
        {
            var random = new System.Random(7);
            Transition.Entrance.Plan(Frame, random, 100, Transition.ScaleRange.Default,
                Vector2.right, 0, 1, out _, out float scale);
            Assert.GreaterOrEqual(scale, Transition.ScaleRange.Default.Min);
            Assert.LessOrEqual(scale, Transition.ScaleRange.Default.Max);
        }

        [Test]
        public void AZeroBearingSpreadsAllTheWayRound()
        {
            var random = new System.Random(99);
            bool left = false, right = false;
            for (int i = 0; i < 60; i++)
            {
                Transition.Entrance.Plan(Frame, random, 50, Transition.ScaleRange.Default,
                    Vector2.zero, 90, 1, out var origin, out _);
                left |= origin.x < Frame.center.x;
                right |= origin.x > Frame.center.x;
            }
            Assert.IsTrue(left && right, "a zero bearing must ignore the angle range entirely");
        }

        [Test]
        public void ScaleRangeToleratesReversedFields()
        {
            var reversed = new Transition.ScaleRange { min = 9, max = 2 };
            Assert.AreEqual(2f, reversed.Min, 1e-4f);
            Assert.GreaterOrEqual(reversed.Max, reversed.Min);
        }

        [Test]
        public void ScaleRangeNeverStartsBelowLifeSize()
        {
            var tiny = new Transition.ScaleRange { min = .1f, max = .2f };
            Assert.GreaterOrEqual(tiny.Min, 1f);
        }

        // --- the scale-track convention --------------------------------------

        [Test]
        public void AScaleTrackNormalizesItsShapeAndKeepsItsEndValue()
        {
            var track = Transition.ScaleTrack.From(t => 5 + t * 3);   // 5 -> 8
            Assert.AreEqual(0f, track.Shape(0), 1e-4f);
            Assert.AreEqual(1f, track.Shape(1), 1e-4f);
            Assert.AreEqual(8f, track.EndMultiplier, 1e-4f);
        }

        [Test]
        public void AFlatCurveStillGivesAUsableTrack()
        {
            var track = Transition.ScaleTrack.From(_ => 2);
            Assert.AreEqual(2f, track.EndMultiplier, 1e-4f);
            Assert.AreEqual(1f, track.Shape(1), 1e-4f);
        }

        [Test]
        public void AMissingCurveArrivesAtLifeSize()
        {
            var track = Transition.ScaleTrack.From(null);
            Assert.AreEqual(1f, track.EndMultiplier, 1e-4f);
        }

        [Test]
        public void ALargeStartNeverDrivesTheSizeNegative()
        {
            // The trap the convention exists to avoid: reading the curve as an absolute size.
            var track = Transition.ScaleTrack.From(t => t * 1.2f);
            for (float t = 0; t <= 1.0001f; t += .05f)
            {
                var pose = Transition.Entrance.At(t, 0, 1, Vector2.zero, 100, scale: 10, Home,
                    track, Linear, Linear, absorb: false);
                Assert.GreaterOrEqual(pose.Size, 0f, $"size went negative at t={t}");
            }
        }

        // --- the trip --------------------------------------------------------

        [Test]
        public void ItStartsAtItsOriginAndEndsAtItsDestination()
        {
            var origin = new Vector2(-500, -500);
            var first = Transition.Entrance.At(0, 0, 1, origin, 100, 1, Home, Flat, Linear, Linear, false);
            var last = Transition.Entrance.At(1, 0, 1, origin, 100, 1, Home, Flat, Linear, Linear, false);

            Assert.AreEqual(origin, first.Centre);
            Assert.AreEqual(Home.center, last.Centre);
        }

        [Test]
        public void ALandingItemNeverFades()
        {
            var pose = Transition.Entrance.At(.5f, 0, 1, Vector2.zero, 100, 1, Home, Flat, Linear, Linear, absorb: false);
            Assert.AreEqual(1f, pose.Opacity, 1e-4f);
        }

        [Test]
        public void AnAbsorbedItemFades()
        {
            var pose = Transition.Entrance.At(1, 0, 1, Vector2.zero, 100, 1, Home, Flat, Linear, t => 1 - t, absorb: true);
            Assert.AreEqual(0f, pose.Opacity, 1e-4f);
        }

        [Test]
        public void TickingDrivesTheTargetAndFinishesOnTime()
        {
            var probe = new Probe();
            var entrance = Build(probe, origin: new Vector2(-300, -300));

            entrance.Tick(0);
            Assert.AreNotEqual(Rect.zero, probe.Rect);
            Assert.IsFalse(entrance.Done);

            entrance.Tick(1);
            Assert.IsTrue(entrance.Done);
            Assert.AreEqual(Home.center, probe.Rect.center);
        }

        [Test]
        public void CompletedReportsWhetherItArrivedOrWasSkipped()
        {
            bool? arrived = null;
            var entrance = Build(new Probe());
            entrance.Completed += reached => arrived = reached;

            entrance.Finish(false);
            Assert.IsFalse(arrived.Value, "a skipped opening must not claim an arrival");
        }

        [Test]
        public void CompletedFiresOnce()
        {
            int fired = 0;
            var entrance = Build(new Probe());
            entrance.Completed += _ => fired++;

            entrance.Tick(2);
            entrance.Finish(true);
            entrance.Finish(true);
            Assert.AreEqual(1, fired);
        }

        [Test]
        public void ADestinationWithoutALayoutIsWaitedForRatherThanFlownAt()
        {
            var probe = new Probe();
            var entrance = new Transition.Entrance(probe, () => new Rect(0, 0, 0, 0), false,
                Vector2.zero, 1, 0, 1, 100, Flat, Linear, Linear);

            entrance.Tick(.5f);
            Assert.AreEqual(Rect.zero, probe.Rect, "nothing should be written against a rect with no size");
            Assert.IsFalse(entrance.Done);
        }

        [Test]
        public void ANullTargetIsInertRatherThanFatal()
        {
            var entrance = new Transition.Entrance(null, () => Home, false,
                Vector2.zero, 1, 0, 1, 100, Flat, Linear, Linear);
            Assert.DoesNotThrow(() => { entrance.Tick(.5f); entrance.Finish(true); });
        }
    }
}

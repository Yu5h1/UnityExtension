using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Yu5h1Lib.Tests
{
    public class TransitionOpeningTests
    {
        private static readonly Rect Home = new Rect(450, 350, 100, 100);
        private static readonly Func<float, float> Linear = t => t;

        private sealed class Probe : Drive.ISpatial
        {
            public bool IsAlive => true;
            public Rect Rect { get; set; }
            public float Opacity { get; set; } = 1;
        }

        private static Transition.Entrance Item(float start, float end, Func<Rect> home = null)
            => new Transition.Entrance(new Probe(), home ?? (() => Home), false,
                new Vector2(-300, -300), 1, start, end, 100,
                Transition.ScaleTrack.From(Linear), Linear, Linear);

        private static List<Transition.Entrance> Staggered(int count, float duration)
        {
            var items = new List<Transition.Entrance>();
            for (int i = 0; i < count; i++)
            {
                Transition.Entrance.Schedule(i, count, duration, .1f, .35f, out float start, out float end);
                items.Add(Item(start, end));
            }
            return items;
        }

        private Transition.Opening opening;

        [SetUp]
        public void SetUp() => opening = new Transition.Opening();

        [Test]
        public void NothingIsPlayingToBeginWith()
        {
            Assert.IsFalse(opening.IsPlaying);
            Assert.AreEqual(-1f, opening.Clock);
            Assert.IsFalse(opening.Tick(1f / 60));
        }

        [Test]
        public void BeginningStartsTheClock()
        {
            opening.Begin(Staggered(4, 1), timeout: 2);
            Assert.IsTrue(opening.IsPlaying);
            Assert.AreEqual(4, opening.Count);
            Assert.AreEqual(0f, opening.Clock);
        }

        [Test]
        public void AnEmptySetNeverStarts()
        {
            opening.Begin(new List<Transition.Entrance>(), 2);
            Assert.IsFalse(opening.IsPlaying);
            opening.Begin(null, 2);
            Assert.IsFalse(opening.IsPlaying);
        }

        [Test]
        public void OneClockIsSharedByEveryItem()
        {
            opening.Begin(Staggered(4, 1), 2);
            opening.Tick(.25f);
            opening.Tick(.25f);
            Assert.AreEqual(.5f, opening.Clock, 1e-4f, "each item is read against the same time");
        }

        [Test]
        public void ItRunsUntilEveryoneHasArrived()
        {
            var items = Staggered(4, 1);
            opening.Begin(items, 3);

            int ticks = 0;
            while (opening.Tick(1f / 60) && ticks < 1000) ticks++;

            Assert.IsFalse(opening.IsPlaying);
            foreach (var item in items) Assert.IsTrue(item.Done);
        }

        [Test]
        public void SkippingPutsEverythingWhereItBelongsAtOnce()
        {
            var items = Staggered(4, 5);
            opening.Begin(items, 6);
            opening.Tick(.1f);

            opening.Skip();
            Assert.IsFalse(opening.IsPlaying);
            foreach (var item in items) Assert.IsTrue(item.Done);
        }

        [Test]
        public void SkippingDoesNotClaimAnArrival()
        {
            bool? arrived = null;
            var item = Item(0, 5);
            item.Completed += reached => arrived = reached;

            opening.Begin(new[] { item }, 6);
            opening.Tick(.1f);
            opening.Skip();

            Assert.IsFalse(arrived.Value);
        }

        [Test]
        public void RunningToTheEndDoesClaimArrivals()
        {
            bool? arrived = null;
            var item = Item(0, .2f);
            item.Completed += reached => arrived = reached;

            opening.Begin(new[] { item }, 2);
            while (opening.Tick(1f / 60)) { }

            Assert.IsTrue(arrived.Value);
        }

        [Test]
        public void ADestinationThatNeverGainsASizeCannotHoldTheOpeningOpen()
        {
            // Without the timeout this would wait for a layout that never comes.
            var stuck = new Transition.Entrance(new Probe(), () => new Rect(0, 0, 0, 0), false,
                Vector2.zero, 1, 0, 1, 100, Transition.ScaleTrack.From(Linear), Linear, Linear);

            opening.Begin(new[] { stuck }, timeout: .5f);
            int ticks = 0;
            while (opening.Tick(1f / 60) && ticks < 1000) ticks++;

            Assert.IsFalse(opening.IsPlaying, "the timeout must end it");
            Assert.Less(ticks, 1000);
            Assert.IsTrue(stuck.Done);
        }

        [Test]
        public void BeginningAgainSkipsWhateverWasRunning()
        {
            var first = Staggered(3, 5);
            opening.Begin(first, 6);
            opening.Tick(.1f);

            opening.Begin(Staggered(2, 1), 2);
            Assert.AreEqual(2, opening.Count);
            foreach (var item in first) Assert.IsTrue(item.Done, "two openings must never fight over items");
        }

        [Test]
        public void SkippingWhenNothingIsRunningIsHarmless()
        {
            Assert.DoesNotThrow(() => opening.Skip());
            Assert.IsFalse(opening.IsPlaying);
        }

        [Test]
        public void ADeadFrameDoesNotAdvanceTheClock()
        {
            opening.Begin(Staggered(2, 1), 2);
            opening.Tick(float.NaN);
            opening.Tick(-1);
            Assert.AreEqual(0f, opening.Clock, 1e-4f);
            Assert.IsTrue(opening.IsPlaying);
        }
    }
}

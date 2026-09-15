using NUnit.Framework;
using UnityEngine;

namespace Yu5h1Lib.Tests
{
    public class ReactionThrowTests
    {
        private const float Frame = 1f / 60;
        private static readonly Rect Area = new Rect(0, 0, 1000, 800);

        private Reaction.Throw thrown;
        private Reaction.Throw.Settings settings;

        [SetUp]
        public void SetUp()
        {
            thrown = new Reaction.Throw();
            settings = new Reaction.Throw.Settings();
        }

        private int Run(int frames = 600, float radius = 20)
        {
            int ticks = 0;
            while (ticks < frames && thrown.Tick(Frame, Area, radius, settings)) ticks++;
            return ticks;
        }

        [Test]
        public void NothingFliesUntilItIsLaunched()
        {
            Assert.IsFalse(thrown.IsFlying);
            Assert.IsFalse(thrown.Tick(Frame, Area, 20, settings));
        }

        [Test]
        public void LaunchingStartsItTravelling()
        {
            thrown.Launch(Area.center, new Vector2(500, 0));
            Assert.IsTrue(thrown.IsFlying);

            thrown.Tick(Frame, Area, 20, settings);
            Assert.Greater(thrown.Position.x, Area.center.x);
        }

        [Test]
        public void ItSlowsDownAndComesToRest()
        {
            thrown.Launch(Area.center, new Vector2(600, 0));
            int ticks = Run();
            Assert.IsFalse(thrown.IsFlying, "a throw must end");
            Assert.Greater(ticks, 1, "but not on the first frame");
        }

        [Test]
        public void TheLifetimeIsAHardCeiling()
        {
            // Almost no drag and a huge speed: only the lifetime can stop this.
            settings.drag = 0;
            settings.restingSpeed = 0;
            thrown.Launch(Area.center, new Vector2(400, 0), lifetime: .2f);
            Run(frames: 10000);
            Assert.IsFalse(thrown.IsFlying);
            Assert.LessOrEqual(thrown.Remaining, 0);
        }

        [Test]
        public void ItNeverLeavesItsWorld()
        {
            thrown.Launch(Area.center, new Vector2(4000, 3000));
            const float radius = 20;
            while (thrown.Tick(Frame, Area, radius, settings))
            {
                Assert.GreaterOrEqual(thrown.Position.x, Area.xMin + radius - 1e-3f);
                Assert.LessOrEqual(thrown.Position.x, Area.xMax - radius + 1e-3f);
                Assert.GreaterOrEqual(thrown.Position.y, Area.yMin + radius - 1e-3f);
                Assert.LessOrEqual(thrown.Position.y, Area.yMax - radius + 1e-3f);
            }
        }

        [Test]
        public void HittingAnEdgeReversesThatAxisOnly()
        {
            thrown.Launch(new Vector2(950, 400), new Vector2(3000, 0));
            thrown.Tick(Frame, Area, 20, settings);
            Assert.Less(thrown.Velocity.x, 0, "it must come back off the wall");
            Assert.AreEqual(0f, thrown.Velocity.y, 1e-3f, "the other axis is untouched");
        }

        [Test]
        public void ABouncierSettingKeepsMoreSpeed()
        {
            var dull = new Reaction.Throw();
            dull.Launch(new Vector2(950, 400), new Vector2(3000, 0));
            dull.Tick(Frame, Area, 20, new Reaction.Throw.Settings { edgeBounce = .1f });

            thrown.Launch(new Vector2(950, 400), new Vector2(3000, 0));
            thrown.Tick(Frame, Area, 20, new Reaction.Throw.Settings { edgeBounce = .9f });

            Assert.Greater(thrown.Velocity.magnitude, dull.Velocity.magnitude);
        }

        [Test]
        public void AStrikeSendsItBack()
        {
            thrown.Strikes = (_, _, _) => true;
            thrown.Launch(Area.center, new Vector2(600, 0));
            thrown.Tick(Frame, Area, 20, settings);

            Assert.IsTrue(thrown.HasStruck);
            Assert.Less(thrown.Velocity.x, 0);
        }

        [Test]
        public void ItOnlyEverStrikesOnce()
        {
            int asked = 0;
            thrown.Strikes = (_, _, _) => { asked++; return true; };
            thrown.Launch(Area.center, new Vector2(600, 0));
            Run();
            Assert.AreEqual(1, asked, "a rebounding object re-crossing the same target must not strike it again");
        }

        [Test]
        public void TheStrikeTestSeesTheWholeStep()
        {
            Vector2 seenStart = default, seenEnd = default;
            thrown.Strikes = (a, b, _) => { seenStart = a; seenEnd = b; return false; };
            thrown.Launch(Area.center, new Vector2(600, 0));
            thrown.Tick(Frame, Area, 20, settings);

            Assert.AreEqual(Area.center, seenStart);
            Assert.AreNotEqual(seenStart, seenEnd, "a swept test needs both ends of the travel");
            Assert.AreEqual(thrown.Position, seenEnd);
        }

        [Test]
        public void NoStrikeTestMeansNothingIsEverHit()
        {
            thrown.Launch(Area.center, new Vector2(600, 0));
            Run();
            Assert.IsFalse(thrown.HasStruck);
        }

        [Test]
        public void StopEndsItImmediately()
        {
            thrown.Launch(Area.center, new Vector2(600, 0));
            thrown.Stop();
            Assert.IsFalse(thrown.IsFlying);
            Assert.AreEqual(Vector2.zero, thrown.Velocity);
        }

        [Test]
        public void RelaunchingReplacesTheFlightInProgress()
        {
            thrown.Launch(Area.center, new Vector2(600, 0));
            thrown.Strikes = (_, _, _) => true;
            thrown.Tick(Frame, Area, 20, settings);
            Assert.IsTrue(thrown.HasStruck);

            thrown.Launch(Area.center, new Vector2(0, 600));
            Assert.IsFalse(thrown.HasStruck, "a new throw starts with a clean slate");
            Assert.AreEqual(new Vector2(0, 600), thrown.Velocity);
        }

        [Test]
        public void DegenerateInputIsRefusedRatherThanPropagated()
        {
            thrown.Launch(new Vector2(float.NaN, 0), Vector2.one);
            Assert.IsFalse(thrown.IsFlying);

            thrown.Launch(Area.center, new Vector2(float.PositiveInfinity, 0));
            Assert.IsFalse(thrown.IsFlying);

            thrown.Launch(Area.center, Vector2.one * 500, lifetime: 0);
            Assert.IsFalse(thrown.IsFlying);
        }

        [Test]
        public void MissingSettingsOrDeadTimeChangesNothing()
        {
            thrown.Launch(Area.center, new Vector2(600, 0));
            var before = thrown.Position;

            thrown.Tick(Frame, Area, 20, null);
            thrown.Tick(0, Area, 20, settings);
            thrown.Tick(-1, Area, 20, settings);
            Assert.AreEqual(before, thrown.Position);
        }

        [Test]
        public void AWorldTooSmallToFitTheObjectSimplyDoesNotConfineIt()
        {
            thrown.Launch(Vector2.zero, new Vector2(600, 0));
            Assert.DoesNotThrow(() => thrown.Tick(Frame, new Rect(0, 0, 10, 10), radius: 50, settings));
        }
    }
}

using NUnit.Framework;
using UnityEngine;

namespace Yu5h1Lib.Tests
{
    public class ReactionShakeTests
    {
        private const float Frame = 1f / 60;

        private sealed class Probe : Drive.IPivot
        {
            public bool Alive = true;
            public bool IsAlive => Alive;
            public float Rotation { get; set; }
            public Vector2 Scale { get; set; } = Vector2.one;
            public Vector2 Pivot { get; set; }
        }

        private Probe probe;
        private Reaction.Shake shake;

        [SetUp]
        public void SetUp()
        {
            probe = new Probe();
            shake = new Reaction.Shake(probe);
        }

        [Test]
        public void NothingMovesUntilItIsPlayed()
        {
            Assert.IsFalse(shake.IsMoving);
            shake.Tick(Frame);
            Assert.AreEqual(0f, probe.Rotation);
        }

        [Test]
        public void PlayingStartsItAndMovesTheTarget()
        {
            shake.Play(Reaction.Shake.Settings.Default);
            Assert.IsTrue(shake.IsMoving);
            shake.Tick(Frame);
            Assert.AreNotEqual(0f, probe.Rotation);
        }

        [Test]
        public void PlayingSetsThePivotFromTheSettings()
        {
            // The default hangs from the bottom edge: y up -0.5 becomes y down 1.
            shake.Play(Reaction.Shake.Settings.Default);
            Assert.AreEqual(.5f, probe.Pivot.x, 1e-4f);
            Assert.AreEqual(1f, probe.Pivot.y, 1e-4f);
        }

        [Test]
        public void HangingFromAboveFlipsThePivotToTheTop()
        {
            shake.Play(new Reaction.Shake.Settings { anchorOffset = new Vector2(0, .5f), amplitude = .22f });
            Assert.AreEqual(0f, probe.Pivot.y, 1e-4f);
        }

        [Test]
        public void ItSettlesAndStopsWriting()
        {
            shake.Play(Reaction.Shake.Settings.Default);
            for (int i = 0; i < 1200 && shake.IsMoving; i++) shake.Tick(Frame);

            Assert.IsFalse(shake.IsMoving, "a one-shot wobble must end");
            float last = probe.Rotation;
            shake.Tick(Frame);
            Assert.AreEqual(last, probe.Rotation, "a settled shake must stop touching its target");
        }

        [Test]
        public void TheSwingNeverExceedsThePeakTheAmplitudeAsksFor()
        {
            var settings = Reaction.Shake.Settings.Default;
            float peak = settings.PeakAngle();
            shake.Play(settings);
            for (int i = 0; i < 600 && shake.IsMoving; i++)
            {
                shake.Tick(Frame);
                Assert.LessOrEqual(Mathf.Abs(probe.Rotation), peak + 1e-3f);
            }
        }

        [Test]
        public void ScalePulsesAroundLifeSizeAndStaysUniform()
        {
            shake.Play(Reaction.Shake.Settings.Default);
            for (int i = 0; i < 60 && shake.IsMoving; i++)
            {
                shake.Tick(Frame);
                Assert.AreEqual(probe.Scale.x, probe.Scale.y, 1e-6f, "the pulse must not distort the target");
                Assert.Greater(probe.Scale.x, .5f);
                Assert.Less(probe.Scale.x, 1.5f);
            }
        }

        [Test]
        public void ReplayingBuildsOnTheSwingAlreadyThere()
        {
            shake.Play(Reaction.Shake.Settings.Default);
            for (int i = 0; i < 20; i++) shake.Tick(Frame);
            float once = Mathf.Abs(probe.Rotation);

            shake.Play(Reaction.Shake.Settings.Default);
            shake.Tick(Frame);
            Assert.IsTrue(shake.IsMoving);
            Assert.Greater(Mathf.Abs(probe.Rotation) + once, 0f);
        }

        [Test]
        public void StopEndsItImmediately()
        {
            shake.Play(Reaction.Shake.Settings.Default);
            shake.Stop();
            Assert.IsFalse(shake.IsMoving);
        }

        [Test]
        public void AmplitudeDrivesThePeakAngleAndIsBounded()
        {
            var gentle = new Reaction.Shake.Settings { anchorOffset = new Vector2(0, -.5f), amplitude = .05f };
            var hard = new Reaction.Shake.Settings { anchorOffset = new Vector2(0, -.5f), amplitude = 2f };
            Assert.Less(gentle.PeakAngle(), hard.PeakAngle());
            Assert.GreaterOrEqual(gentle.PeakAngle(), 1f);
            Assert.LessOrEqual(hard.PeakAngle(), 30f);
        }

        [Test]
        public void ADistantPivotNeedsASmallerAngleForTheSameSwing()
        {
            var near = new Reaction.Shake.Settings { anchorOffset = new Vector2(0, -.5f), amplitude = .22f };
            var far = new Reaction.Shake.Settings { anchorOffset = new Vector2(0, -2f), amplitude = .22f };
            Assert.Less(far.PeakAngle(), near.PeakAngle());
        }

        [Test]
        public void ANullTargetIsInertRatherThanFatal()
        {
            var orphan = new Reaction.Shake(null);
            Assert.DoesNotThrow(() =>
            {
                orphan.Play(Reaction.Shake.Settings.Default);
                orphan.Tick(Frame);
                orphan.Stop();
            });
        }
    }
}

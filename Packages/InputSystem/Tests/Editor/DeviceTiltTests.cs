using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
// UnityEngine also has a Gyroscope - the legacy one. Say which.
using Gyroscope = UnityEngine.InputSystem.Gyroscope;

namespace Yu5h1Lib.Tests
{
    /// <summary>
    /// Driven by a virtual gyroscope, so the rate limiting and thresholds are exercised on a desktop that
    /// has no sensor of its own.
    /// </summary>
    public class DeviceTiltTests
    {
        private Gyroscope sensor;
        private DeviceTilt tilt;
        private DeviceTilt.Settings settings;

        /// <summary>Past the settling time, so the first read is not swallowed by it.</summary>
        private float Clock => Time.unscaledTime + 10;

        [SetUp]
        public void SetUp()
        {
            sensor = InputSystem.AddDevice<Gyroscope>();
            tilt = new DeviceTilt();
            settings = new DeviceTilt.Settings();
        }

        [TearDown]
        public void TearDown()
        {
            tilt.Dispose();
            if (sensor != null && sensor.added) InputSystem.RemoveDevice(sensor);
        }

        private void Turn(Vector3 radiansPerSecond)
        {
            // The state struct itself is internal to the Input System, so the value goes to the control.
            InputSystem.QueueDeltaStateEvent(sensor.angularVelocity, radiansPerSecond);
            InputSystem.Update();
        }

        [Test]
        public void ItStartsInactive()
        {
            Assert.IsFalse(tilt.IsActive);
        }

        [Test]
        public void NothingIsReportedWhileInactive()
        {
            Turn(new Vector3(0, 0, 5));
            Assert.AreEqual(0f, tilt.ReadImpulse(Clock, settings));
        }

        [Test]
        public void ActivatingEnablesTheSensorItTurnedOn()
        {
            InputSystem.DisableDevice(sensor);
            tilt.SetActive(true);

            Assert.IsTrue(tilt.IsActive);
            Assert.IsTrue(sensor.enabled);
        }

        [Test]
        public void ASensorSomeoneElseEnabledIsLeftOn()
        {
            InputSystem.EnableDevice(sensor);
            tilt.SetActive(true);
            tilt.SetActive(false);

            Assert.IsTrue(sensor.enabled, "turning off a sensor this object did not turn on would break someone else");
        }

        [Test]
        public void ASensorItEnabledIsHandedBack()
        {
            InputSystem.DisableDevice(sensor);
            tilt.SetActive(true);
            tilt.SetActive(false);

            Assert.IsFalse(sensor.enabled);
        }

        [Test]
        public void ADefiniteTurnBecomesAnImpulse()
        {
            tilt.SetActive(true);
            Turn(new Vector3(0, 0, 3));

            float impulse = tilt.ReadImpulse(Clock, settings);
            Assert.AreNotEqual(0f, impulse);
        }

        [Test]
        public void TheImpulseOpposesTheTurn()
        {
            tilt.SetActive(true);

            Turn(new Vector3(0, 0, 3));
            float positive = tilt.ReadImpulse(Clock, settings);
            Turn(new Vector3(0, 0, -3));
            float negative = tilt.ReadImpulse(Clock + 1, settings);

            Assert.Less(positive * negative, 0, "turning the other way must swing the other way");
        }

        [Test]
        public void AGentleTurnIsIgnored()
        {
            tilt.SetActive(true);
            Turn(new Vector3(0, 0, .1f));
            Assert.AreEqual(0f, tilt.ReadImpulse(Clock, settings));
        }

        [Test]
        public void TheBusiestAxisWins()
        {
            tilt.SetActive(true);
            Turn(new Vector3(.3f, 4, .3f));
            Assert.AreNotEqual(0f, tilt.ReadImpulse(Clock, settings));
        }

        [Test]
        public void ReadingAgainTooSoonReportsNothing()
        {
            tilt.SetActive(true);
            Turn(new Vector3(0, 0, 4));

            float first = tilt.ReadImpulse(Clock, settings);
            float second = tilt.ReadImpulse(Clock, settings);

            Assert.AreNotEqual(0f, first);
            Assert.AreEqual(0f, second, "a sensor read every frame would drive the motion instead of knocking it");
        }

        [Test]
        public void WaitingLongEnoughAllowsAnother()
        {
            tilt.SetActive(true);
            Turn(new Vector3(0, 0, 4));

            Assert.AreNotEqual(0f, tilt.ReadImpulse(Clock, settings));
            Assert.AreNotEqual(0f, tilt.ReadImpulse(Clock + 1, settings));
        }

        [Test]
        public void AViolentTurnIsStillBounded()
        {
            tilt.SetActive(true);
            Turn(new Vector3(0, 0, 1000));

            float impulse = Mathf.Abs(tilt.ReadImpulse(Clock, settings));
            Assert.LessOrEqual(impulse, 6f * 60f + 1e-3f);
        }

        [Test]
        public void MoreGainMeansMoreImpulse()
        {
            tilt.SetActive(true);
            Turn(new Vector3(0, 0, 3));

            float gentle = Mathf.Abs(tilt.ReadImpulse(Clock, new DeviceTilt.Settings { gain = 5 }));
            float hard = Mathf.Abs(tilt.ReadImpulse(Clock + 1, new DeviceTilt.Settings { gain = 50 }));
            Assert.Greater(hard, gentle);
        }

        [Test]
        public void MissingSettingsOrDeadClockReportNothing()
        {
            tilt.SetActive(true);
            Turn(new Vector3(0, 0, 4));

            Assert.AreEqual(0f, tilt.ReadImpulse(Clock, null));
            Assert.AreEqual(0f, tilt.ReadImpulse(float.NaN, settings));
        }

        [Test]
        public void DisposingStopsIt()
        {
            tilt.SetActive(true);
            tilt.Dispose();
            Assert.IsFalse(tilt.IsActive);
        }

        [Test]
        public void WithoutASensorItIsSimplyQuiet()
        {
            InputSystem.RemoveDevice(sensor);

            var orphan = new DeviceTilt();
            Assert.DoesNotThrow(() => orphan.SetActive(true));
            Assert.AreEqual(0f, orphan.ReadImpulse(Clock, settings));
            Assert.DoesNotThrow(() => orphan.Dispose());
        }
    }
}

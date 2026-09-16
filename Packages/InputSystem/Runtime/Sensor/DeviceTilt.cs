using System;
using UnityEngine;
using UnityEngine.InputSystem;
// UnityEngine also has a Gyroscope - the legacy one. Say which.
using Gyroscope = UnityEngine.InputSystem.Gyroscope;

namespace Yu5h1Lib
{
    /// <summary>
    /// Turns tilting the device into occasional nudges, for motion that should answer to the phone itself.
    /// <para>
    /// Two things make a gyroscope usable here rather than raw. It is rate-limited, because a sensor that
    /// reports every frame would drive whatever it feeds continuously instead of knocking it; and it reads
    /// as a single strongest axis, because a hanging or swinging thing has one degree of freedom and does
    /// not care which way the phone was turned.
    /// </para>
    /// <para>
    /// It only ever turns off a gyroscope it turned on itself. Something else in the application may want
    /// the sensor too, and silently disabling it would break that at a distance.
    /// </para>
    /// </summary>
    public sealed class DeviceTilt : IDisposable
    {
        /// <summary>Settling time after activating, so picking the phone up is not read as a knock.</summary>
        private const float SettleSeconds = .2f;

        /// <summary>Quietest gap between nudges. Below this the motion is driven rather than knocked.</summary>
        private const float Interval = .16f;

        /// <summary>Ceiling on the rate actually used, in radians per second.</summary>
        private const float MaximumRate = 6;

        /// <summary>How a tilt becomes a nudge.</summary>
        [Serializable]
        public sealed class Settings
        {
            [Range(0, 60), Tooltip("How much of the turn rate becomes an impulse.")]
            public float gain = 20;

            [Range(.2f, 5), Tooltip("Turn rate below which nothing is reported, in radians per second.")]
            public float threshold = 1;
        }

        private bool ownsSensor;
        private float nextImpulse;

        /// <summary>True while this is listening. False on any device without a gyroscope.</summary>
        public bool IsActive { get; private set; }

        /// <summary>The gyroscope, or null where there is none - desktop included.</summary>
        private static Gyroscope Sensor => Gyroscope.current;

        /// <summary>
        /// Starts or stops listening. Enabling a sensor that was already on leaves it on afterwards.
        /// </summary>
        public void SetActive(bool value)
        {
            if (IsActive == value) return;
            IsActive = value;

            var sensor = Sensor;
            if (sensor == null) return;

            if (value)
            {
                ownsSensor = !sensor.enabled;
                if (ownsSensor) InputSystem.EnableDevice(sensor);
                nextImpulse = Time.unscaledTime + SettleSeconds;
            }
            else if (ownsSensor)
            {
                InputSystem.DisableDevice(sensor);
                ownsSensor = false;
            }
        }

        /// <summary>
        /// The nudge this moment's tilt is worth, or zero when there is nothing to report - no sensor, not
        /// listening, still settling, too soon after the last one, or too gentle to count.
        /// </summary>
        /// <param name="clock">The caller's own unscaled clock, so a paused application stays still.</param>
        public float ReadImpulse(float clock, Settings settings)
        {
            var sensor = Sensor;
            if (!IsActive || sensor == null || settings == null || !clock.IsFinite() || clock < nextImpulse)
                return 0;

            Vector3 rate = sensor.angularVelocity.ReadValue();

            // One axis, the busiest: what is being driven swings one way, and which way the phone turned to
            // cause it does not matter.
            float strongest = Mathf.Abs(rate.x) > Mathf.Abs(rate.y) ? rate.x : rate.y;
            if (Mathf.Abs(rate.z) > Mathf.Abs(strongest)) strongest = rate.z;

            if (!strongest.IsFinite() || Mathf.Abs(strongest) < Mathf.Clamp(settings.threshold, .2f, 5)) return 0;

            nextImpulse = clock + Interval;
            return -Mathf.Clamp(strongest, -MaximumRate, MaximumRate) * Mathf.Clamp(settings.gain, 0, 60);
        }

        /// <summary>Stops listening and hands back the sensor if it was this object that turned it on.</summary>
        public void Dispose() => SetActive(false);
    }
}

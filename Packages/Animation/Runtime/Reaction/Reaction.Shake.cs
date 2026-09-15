using System;
using UnityEngine;

namespace Yu5h1Lib
{
    public static partial class Reaction
    {
        /// <summary>
        /// A one-shot wobble: whatever catches a dropped item rattles briefly and settles.
        /// <para>
        /// Built on <see cref="Hanging"/> rather than a second angular spring, because an entrance swing and
        /// a catch are the same event - one impulse decaying back to rest. What differs is only how hard and
        /// about what point, and both are settings.
        /// </para>
        /// </summary>
        public sealed class Shake
        {
            /// <summary>Per-site shape of the wobble.</summary>
            [Serializable]
            public struct Settings
            {
                [Tooltip("Pivot offset from the centre, in target sizes, y up. (0,-0.5) is the bottom edge; (0,1) hangs it from above.")]
                public Vector2 anchorOffset;

                [Range(0, 2), Tooltip("How far the centre swings sideways, as a fraction of the target's own size.")]
                public float amplitude;

                public static Settings Default => new Settings { anchorOffset = new Vector2(0, -.5f), amplitude = .22f };

                /// <summary>
                /// Peak angle that swings the centre sideways by <see cref="amplitude"/> of the target's size.
                /// Size cancels out, so one setting reads the same on a small icon and a large card, and a
                /// distant pivot automatically needs a smaller angle than a close one.
                /// </summary>
                public float PeakAngle()
                {
                    float reach = Mathf.Max(.1f, anchorOffset.magnitude);
                    return Mathf.Clamp(Mathf.Atan2(Mathf.Max(0, amplitude), reach) * Mathf.Rad2Deg, 1, 30);
                }

                /// <summary>
                /// <see cref="anchorOffset"/> as a normalized pivot. Authoring reads y up; targets are y down,
                /// so the flip happens here rather than in every adapter.
                /// </summary>
                public Vector2 Pivot => new Vector2(.5f + anchorOffset.x, .5f - anchorOffset.y);
            }

            // Stiff and lightly damped: a quick rattle rather than a pendulum. Not authored per site.
            private const float Stiffness = 90, Damping = 6, ScalePulse = .5f;

            private readonly Hanging hanging = new Hanging();
            private readonly Hanging.Settings physics = new Hanging.Settings { stiffness = Stiffness, damping = Damping };
            private float peak = 1;

            public Shake(Drive.IPivot target) => Target = target;

            /// <summary>What this wobble drives.</summary>
            public Drive.IPivot Target { get; }

            /// <summary>True while the wobble is still worth ticking.</summary>
            public bool IsMoving => hanging.IsMoving;

            /// <summary>
            /// Starts, or restarts, the wobble. Re-playing while one is running builds on the swing already
            /// there rather than resetting it, so a flurry of catches reads as a flurry.
            /// </summary>
            public void Play(Settings settings)
            {
                if (Target == null) return;
                peak = settings.PeakAngle();
                physics.maximumAngle = peak;
                Target.Pivot = settings.Pivot;
                // Kick it past the limit on purpose: the spring's own bound is what pins the peak to the amplitude.
                hanging.Impulse(Hanging.MaximumImpulse);
            }

            /// <summary>Advances the wobble and writes the pose. Harmless once it has settled.</summary>
            public void Tick(float deltaTime)
            {
                if (Target == null || !hanging.IsMoving) return;
                hanging.Tick(deltaTime, physics);

                float angle = hanging.Angle;
                Target.Rotation = angle;
                // Scale pulses with the swing, scaled by the peak so a gentle wobble does not also pulse hard.
                float pulse = 1 + angle / Mathf.Max(1e-3f, peak) * ScalePulse * peak / 90;
                Target.Scale = new Vector2(pulse, pulse);
            }

            /// <summary>Stops immediately, leaving the pose for the caller to restore.</summary>
            public void Stop() => hanging.Reset();
        }
    }
}

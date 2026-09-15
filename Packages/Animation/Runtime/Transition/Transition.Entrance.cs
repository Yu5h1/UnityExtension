using System;
using UnityEngine;

namespace Yu5h1Lib
{
    public static partial class Transition
    {
        /// <summary>
        /// Carries one item from off-screen to where it belongs.
        /// <para>
        /// Two endings. An item that owns a place lands on it at life size. An item that lives inside
        /// something else shrinks and fades into it instead, so an opening shows the viewer where every
        /// feature went rather than merely revealing a finished screen.
        /// </para>
        /// <para>
        /// Every calculation here is static and takes plain numbers, so a whole arrival can be checked
        /// without a panel, a scene or a clock.
        /// </para>
        /// </summary>
        public sealed class Entrance
        {
            /// <summary>Share of the duration the last item keeps to travel, however tight the interval is set.</summary>
            public const float MinTripShare = .25f;

            private readonly Drive.ISpatial target;
            private readonly Func<Rect> destination;
            private readonly Vector2 origin;
            private readonly float scale, start, end, restSize;
            private readonly ScaleTrack track;
            private readonly Func<float, float> position, fade;

            /// <param name="target">What is being carried.</param>
            /// <param name="destination">Where it belongs, asked again each frame so a moving layout still wins.</param>
            /// <param name="absorb">True for an item that disappears into its destination rather than landing on it.</param>
            /// <param name="origin">Off-screen start, usually from <see cref="Plan"/>.</param>
            /// <param name="scale">Oversized start multiplier, usually from <see cref="Plan"/>.</param>
            /// <param name="start">Clock reading at which it sets off, usually from <see cref="Schedule"/>.</param>
            /// <param name="end">Clock reading by which it must be home.</param>
            /// <param name="restSize">Size an absorbed item starts from; a landing one uses its destination instead.</param>
            public Entrance(Drive.ISpatial target, Func<Rect> destination, bool absorb,
                Vector2 origin, float scale, float start, float end, float restSize,
                ScaleTrack track, Func<float, float> position, Func<float, float> fade)
            {
                this.target = target; this.destination = destination; Absorb = absorb;
                this.origin = origin; this.scale = scale; this.start = start; this.end = end;
                this.restSize = restSize; this.track = track; this.position = position; this.fade = fade;
            }

            /// <summary>True for an item that vanishes into its destination instead of landing on it.</summary>
            public bool Absorb { get; }

            /// <summary>True once it has arrived or been cut short.</summary>
            public bool Done { get; private set; }

            /// <summary>
            /// Raised once. The argument is false when the opening was cut short, which is how a caller
            /// tells an arrival worth celebrating from one that was skipped. An absorbed item still needs
            /// removing by whoever owns it - this class never touches a hierarchy.
            /// </summary>
            public event Action<bool> Completed;

            /// <summary>Advances to the shared clock reading. Harmless once done.</summary>
            public void Tick(float clock)
            {
                if (Done || destination == null) return;
                Rect home = destination();
                // A destination with no size yet has not been laid out; wait rather than fly at nothing.
                if (home.width <= 0 || home.height <= 0) return;

                Apply(At(clock, start, end, origin, Absorb ? restSize : home.width, scale, home,
                    track, position, fade, Absorb));
                if (clock >= end) Finish(true);
            }

            /// <summary>Puts the item where it belongs at once.</summary>
            /// <param name="reachedTheEnd">False when the opening was skipped, so arrival feedback stays quiet.</param>
            public void Finish(bool reachedTheEnd)
            {
                if (Done) return;
                Done = true;

                if (!Absorb && destination != null)
                {
                    Rect home = destination();
                    if (home.width > 0 && home.height > 0) Apply(new Pose(home.center, home.width, 1));
                }
                Completed?.Invoke(reachedTheEnd);
            }

            private void Apply(Pose pose)
            {
                if (target == null) return;
                target.Rect = new Rect(pose.Centre.x - pose.Size / 2, pose.Centre.y - pose.Size / 2,
                                       pose.Size, pose.Size);
                if (Absorb || pose.Opacity < 1) target.Opacity = pose.Opacity;
            }

            /// <summary>
            /// The gap actually used between launches. The authored gap is honoured while it fits and
            /// compressed when it does not: without that, adding one more item pushes the last launches past
            /// the end of the performance and they travel for a negative length of time, silently.
            /// </summary>
            public static float Interval(int count, float duration, float startInterval)
            {
                if (count < 2 || duration <= 0) return 0;
                return Mathf.Min(duration * Mathf.Clamp01(startInterval),
                    duration * (1 - MinTripShare) / (count - 1));
            }

            /// <summary>
            /// When one item sets off and when it must be home. The whole set always finishes at
            /// <paramref name="duration"/>, whatever the other two dials say.
            /// <para>
            /// The two dials are independent ends of the performance: <paramref name="startInterval"/> only
            /// moves the launches apart, <paramref name="endTogether"/> only pulls the arrivals in. Landing
            /// together says nothing about setting off together - staggered launches simply mean the later
            /// items travel faster.
            /// </para>
            /// </summary>
            public static void Schedule(int index, int count, float duration, float startInterval,
                float endTogether, out float start, out float end)
            {
                float interval = Interval(count, duration, startInterval);
                float lastStart = interval * Mathf.Max(0, count - 1);
                start = interval * index;
                end = Mathf.Lerp(start + duration - lastStart, duration, Mathf.Clamp01(endTogether));
            }

            /// <summary>
            /// Chooses a bearing and an oversized start, off the edge the bearing points at.
            /// </summary>
            /// <param name="direction">Bearing written y up, the way a person reads it. Zero means a full spread.</param>
            /// <param name="clearance">Extra margin past the edge, so nothing is caught peeking in on its first frame.</param>
            public static void Plan(Rect frame, System.Random random, float baseSize, ScaleRange range,
                Vector2 direction, float angleRange, float clearance, out Vector2 origin, out float scale)
            {
                scale = Mathf.Lerp(range.Min, range.Max, (float)random.NextDouble());
                float size = baseSize * scale;

                // A bearing is written y up; target space runs the other way, so it flips on the way in.
                bool radial = direction.sqrMagnitude < 1e-6f;
                float centre = radial ? 0 : Mathf.Atan2(-direction.y, direction.x);
                float width = radial ? Mathf.PI * 2 : Mathf.Clamp(angleRange, 0, 360) * Mathf.Deg2Rad;
                float angle = centre + ((float)random.NextDouble() - .5f) * width;
                var bearing = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                // Out far enough that the whole item clears the edge it leaves by, whatever its start size.
                float reach = size / (2 * Mathf.Max(1e-4f, Mathf.Max(Mathf.Abs(bearing.x), Mathf.Abs(bearing.y))))
                    * Mathf.Max(1, clearance);
                origin = Anchor(frame, direction) + bearing * reach;
            }

            /// <summary>Items leave from the corner or edge their bearing points at, not from the middle of the frame.</summary>
            private static Vector2 Anchor(Rect frame, Vector2 direction)
            {
                if (direction.sqrMagnitude < 1e-6f) return frame.center;
                return new Vector2(
                    direction.x < 0 ? frame.xMin : direction.x > 0 ? frame.xMax : frame.center.x,
                    direction.y > 0 ? frame.yMin : direction.y < 0 ? frame.yMax : frame.center.y);
            }

            /// <summary>The pose at a given clock reading. Pure, so a whole arrival can be checked without a panel.</summary>
            public static Pose At(float clock, float start, float end, Vector2 origin, float restSize,
                float scale, Rect target, ScaleTrack track, Func<float, float> position,
                Func<float, float> fade, bool absorb)
            {
                float t = end > start ? Mathf.Clamp01((clock - start) / (end - start)) : 1;
                return new Pose(
                    Vector2.LerpUnclamped(origin, target.center, position(t)),
                    Mathf.Max(0, restSize * Mathf.LerpUnclamped(scale, track.EndMultiplier, track.Shape(t))),
                    absorb ? Mathf.Clamp01(fade(t)) : 1);
            }
        }
    }
}

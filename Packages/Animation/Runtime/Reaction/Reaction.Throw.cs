using System;
using UnityEngine;

namespace Yu5h1Lib
{
    public static partial class Reaction
    {
        /// <summary>
        /// An object let go of with speed: it coasts, bounces off the edges of its world, may strike
        /// something on the way, and comes to rest.
        /// <para>
        /// This is not a transition and cannot be written as one. There is no destination to interpolate
        /// toward - where it ends up is whatever the physics produce, and a strike mid-flight sends it
        /// somewhere else entirely. What it carries is velocity.
        /// </para>
        /// <para>
        /// Geometry only, in whatever space the caller works in. What counts as a strike, and what happens
        /// when it lands, stay with the caller.
        /// </para>
        /// </summary>
        public sealed class Throw
        {
            /// <summary>Longest step integrated at once; a longer frame is split rather than tunnelled through.</summary>
            private const float MaximumStep = .033f;

            /// <summary>How a thrown object behaves in flight.</summary>
            [Serializable]
            public sealed class Settings
            {
                [Range(0, 1), Tooltip("Share of speed kept when it bounces off an edge. 0 sticks, 1 is perfectly elastic.")]
                public float edgeBounce = .55f;

                [Range(0, 1), Tooltip("Share of speed kept when it strikes something, reversed.")]
                public float hitRebound = .3f;

                [Min(0), Tooltip("How quickly it slows down. Higher stops it sooner.")]
                public float drag = 4;

                [Min(0), Tooltip("Speed below which it is considered at rest, in the caller's own units.")]
                public float restingSpeed = 20;
            }

            /// <summary>Where it is now.</summary>
            public Vector2 Position { get; private set; }

            /// <summary>How fast, and which way, it is travelling.</summary>
            public Vector2 Velocity { get; private set; }

            /// <summary>Seconds of flight left before it is forced to rest.</summary>
            public float Remaining { get; private set; }

            /// <summary>True while it is still travelling.</summary>
            public bool IsFlying { get; private set; }

            /// <summary>True once it has struck something during this flight. It only strikes once.</summary>
            public bool HasStruck { get; private set; }

            /// <summary>
            /// Asked once per step whether the travel from the first point to the second, at that speed, hit
            /// anything. Return true to make it rebound. The caller decides what can be hit and converts the
            /// points into whatever space that test needs.
            /// </summary>
            public Func<Vector2, Vector2, float, bool> Strikes { get; set; }

            /// <summary>Lets go of it. Any flight already in progress is replaced.</summary>
            public void Launch(Vector2 position, Vector2 velocity, float lifetime = .75f)
            {
                if (!position.IsFinite() || !velocity.IsFinite() || !lifetime.IsFinite() || lifetime <= 0) return;
                Position = position;
                Velocity = velocity;
                Remaining = lifetime;
                HasStruck = false;
                IsFlying = true;
            }

            /// <summary>Stops it where it is.</summary>
            public void Stop() { IsFlying = false; Velocity = Vector2.zero; Remaining = 0; }

            /// <summary>
            /// Advances the flight.
            /// </summary>
            /// <param name="bounds">The world it is confined to - the real edge, not the panels drawn inside it.
            /// An object is flown over chrome, not bounced off it.</param>
            /// <param name="radius">Half the object's size, so it bounces when its edge meets the wall rather than its centre.</param>
            /// <returns>False once it has come to rest.</returns>
            public bool Tick(float deltaTime, Rect bounds, float radius, Settings settings)
            {
                if (!IsFlying || settings == null || !deltaTime.IsFinite() || deltaTime <= 0) return IsFlying;

                float step = Mathf.Min(deltaTime, MaximumStep);
                Vector2 start = Position;
                Vector2 desired = start + Velocity * step;

                if (bounds.IsValid() && bounds.width > radius * 2 && bounds.height > radius * 2)
                {
                    var free = Rect.MinMaxRect(bounds.xMin + radius, bounds.yMin + radius,
                                               bounds.xMax - radius, bounds.yMax - radius);
                    var velocity = Velocity;
                    if (desired.x < free.xMin || desired.x > free.xMax) velocity.x *= -settings.edgeBounce;
                    if (desired.y < free.yMin || desired.y > free.yMax) velocity.y *= -settings.edgeBounce;
                    Velocity = velocity;
                    desired.x = Mathf.Clamp(desired.x, free.xMin, free.xMax);
                    desired.y = Mathf.Clamp(desired.y, free.yMin, free.yMax);
                }

                Position = desired;

                // Once only: a rebounding object re-crossing the same target must not strike it again.
                if (!HasStruck && Strikes != null && Strikes(start, desired, Velocity.magnitude))
                {
                    HasStruck = true;
                    Velocity *= -settings.hitRebound;
                }

                Velocity *= Mathf.Exp(-Mathf.Max(0, settings.drag) * step);
                Remaining -= step;

                if (Remaining <= 0 || Velocity.magnitude < settings.restingSpeed) Stop();
                return IsFlying;
            }
        }
    }
}

using System;
using UnityEngine;

namespace Yu5h1Lib
{
    public static partial class Reaction
    {
        /// <summary>
        /// An element hung from a point: knock it and it swings, then settles upright.
        /// <para>
        /// A bare spring is not enough. Left alone it would swing past any angle you like, and a rig that
        /// folds over on a hard flick reads as broken rather than lively. This bounds the angle and bleeds
        /// speed when it hits that bound, so a violent input still produces a believable swing.
        /// </para>
        /// </summary>
        public sealed class Hanging
        {
            /// <summary>Authoring surface for one hung element.</summary>
            [Serializable]
            public sealed class Settings
            {
                [Range(1, 30), Tooltip("How far it may swing from upright, in degrees.")]
                public float maximumAngle = 18;

                [Range(10, 100), Tooltip("How hard it pulls back to upright. Higher is snappier.")]
                public float stiffness = 45;

                [Range(2, 20), Tooltip("How quickly the swing dies down. Higher settles sooner.")]
                public float damping = 5;

                [Range(0, .5f), Tooltip("How much of a drag's speed becomes swing.")]
                public float pointerGain = .16f;
            }

            /// <summary>Ceiling on a single impulse, so one absurd sample cannot launch the element.</summary>
            public const float MaximumImpulse = 180;

            private readonly Spring spring = new Spring();

            /// <summary>Current angle from upright, in degrees.</summary>
            public float Angle => spring.Value;

            /// <summary>True while it is still worth ticking.</summary>
            public bool IsMoving => spring.IsMoving;

            /// <summary>Returns it upright immediately, with no swing.</summary>
            public void Reset() => spring.Snap(0);

            /// <summary>
            /// Adds angular speed - a knock. Accumulates onto whatever swing is already happening, so
            /// repeated taps build up rather than replacing each other.
            /// </summary>
            public void Impulse(float velocity)
            {
                if (!velocity.IsFinite()) return;
                spring.Release(0, Mathf.Clamp(spring.Velocity + velocity, -MaximumImpulse, MaximumImpulse));
            }

            /// <summary>Advances the swing and enforces the angle bound.</summary>
            public void Tick(float deltaTime, Settings settings)
            {
                if (settings == null) return;
                spring.Tick(deltaTime, false,
                    Mathf.Clamp(settings.stiffness, 10, 100),
                    Mathf.Clamp(settings.damping, 2, 20));

                float limit = Mathf.Clamp(settings.maximumAngle, 1, 30);
                if (Mathf.Abs(spring.Value) <= limit) return;

                // At the bound: pin the angle, then either bounce back at a fraction of the speed if it was
                // still travelling outward, or keep the speed if it was already on its way home. Killing the
                // speed outright would make the element stick to the limit instead of rebounding.
                float angle = Mathf.Clamp(spring.Value, -limit, limit);
                float velocity = spring.Velocity;
                spring.Snap(angle);
                spring.Release(0, angle * velocity > 0 ? -velocity * .2f : velocity);
            }

            /// <summary>
            /// Turns a drag across an element into the swing it should produce. Grabbing near the bottom,
            /// or far from the centre, gives a longer lever and therefore a bigger swing - the cross product
            /// of that lever and the drag is the torque.
            /// </summary>
            /// <param name="bounds">The element being dragged.</param>
            /// <param name="contact">Where the pointer is, in the same space as <paramref name="bounds"/>.</param>
            /// <param name="velocity">Pointer speed, e.g. from <c>PointerVelocity.Sample</c>.</param>
            /// <param name="gain">Usually <see cref="Settings.pointerGain"/>.</param>
            public static float Torque(Rect bounds, Vector2 contact, Vector2 velocity, float gain)
            {
                // The 20 floors stop a sliver-thin element producing an enormous lever arm.
                var arm = new Vector2((contact.x - bounds.center.x) / Mathf.Max(20, bounds.width * .5f),
                                      (contact.y - bounds.yMin) / Mathf.Max(20, bounds.height));
                return (arm.x * velocity.y - arm.y * velocity.x) * gain;
            }
        }
    }
}

using System;

namespace Yu5h1Lib
{
    public static partial class Reaction
    {
        /// <summary>
        /// One damped spring chasing a target value.
        /// <para>
        /// Retargeting keeps the current position <em>and</em> velocity, which is what a duration-based
        /// transition cannot do: cancelling and restarting one drops the velocity and the motion visibly
        /// stutters. Drive anything that should survive being interrupted - a sheet the user keeps grabbing,
        /// a layout that moves while an item is still travelling.
        /// </para>
        /// </summary>
        public sealed class Spring
        {
            /// <summary>Where it is now.</summary>
            public float Value { get; private set; }

            /// <summary>How fast it is moving, in units per second. Survives retargeting.</summary>
            public float Velocity { get; private set; }

            /// <summary>Where it is heading.</summary>
            public float Target { get; private set; }

            /// <summary>
            /// False once it is close enough and slow enough to stop ticking. The thresholds are
            /// deliberately loose: chasing the last thousandth costs frames and shows nothing.
            /// </summary>
            public bool IsMoving => Math.Abs(Target - Value) > 0.001f || Math.Abs(Velocity) > 0.01f;

            public Spring(float initial = 0) => Snap(initial);

            /// <summary>Jumps there and stops dead. Use for a reset, not for a move.</summary>
            public void Snap(float value) { Value = Target = value; Velocity = 0; }

            /// <summary>Heads for a new target, keeping whatever speed it already had.</summary>
            public void MoveTo(float target) => Target = target;

            /// <summary>Hands it a target and a starting speed at once - a throw.</summary>
            public void Release(float target, float velocity) { Target = target; Velocity = velocity; }

            /// <summary>
            /// Advances by <paramref name="deltaTime"/>.
            /// </summary>
            /// <param name="reduceMotion">
            /// When the platform asks for reduced motion, arrive immediately rather than animating.
            /// Whether the user asked for that is the caller's to know.
            /// </param>
            public void Tick(float deltaTime, bool reduceMotion, float stiffness = 240, float damping = 29)
            {
                if (reduceMotion) { Snap(Target); return; }

                // Clamp the frame, then integrate in fixed sub-steps: one long frame integrated in a single
                // step overshoots and the spring explodes instead of settling.
                float remaining = Math.Min(0.032f, Math.Max(0, deltaTime));
                while (remaining > 0)
                {
                    float step = Math.Min(remaining, 1f / 120f);
                    Velocity += ((Target - Value) * stiffness - Velocity * damping) * step;
                    Value += Velocity * step;
                    remaining -= step;
                }
                if (!IsMoving) Snap(Target);
            }
        }
    }
}

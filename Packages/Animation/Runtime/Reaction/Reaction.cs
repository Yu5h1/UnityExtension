using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// Motion that answers to events rather than to a clock: a spring chasing a target, a hung element
    /// swinging after a knock, a wobble after a drop.
    /// <para>
    /// The distinction from <c>Transition</c> is the whole reason both exist. A transition is told where to
    /// finish and how long to take, so interrupting it means cancelling it. A reaction has no end time and
    /// carries its velocity, so a new target mid-flight bends the path instead of restarting it - which is
    /// what makes a grabbed, released and re-grabbed element feel continuous.
    /// </para>
    /// <para>
    /// Nothing here knows what it drives. Callers either read the value each frame, or hand in a
    /// <see cref="ITarget"/> implementation; the UI Toolkit one lives in <c>com.yu5h1.uitoolkit</c>.
    /// </para>
    /// </summary>
    public static partial class Reaction
    {
        /// <summary>Something a reaction can drive. Dropped as soon as it stops being alive.</summary>
        public interface ITarget
        {
            /// <summary>False once the thing is gone - detached, destroyed or recycled.</summary>
            bool IsAlive { get; }
        }

        /// <summary>A target placed and faded by rectangle. Used by travel, not by rotation.</summary>
        public interface ISpatial : ITarget
        {
            /// <summary>Position and size in the caller's own space.</summary>
            Rect Rect { get; set; }

            /// <summary>0 invisible, 1 opaque.</summary>
            float Opacity { get; set; }
        }

        /// <summary>A target turned and scaled about a point. Used by swing and wobble.</summary>
        public interface IPivot : ITarget
        {
            /// <summary>Degrees, clockwise.</summary>
            float Rotation { get; set; }

            /// <summary>Multiplier per axis; <see cref="Vector2.one"/> is life size.</summary>
            Vector2 Scale { get; set; }

            /// <summary>
            /// Point everything turns about, normalized over the target: (0,0) top-left, (.5,.5) centre,
            /// (1,1) bottom-right. The same motion about a different pivot reads as a different performance.
            /// </summary>
            Vector2 Pivot { get; set; }
        }
    }
}

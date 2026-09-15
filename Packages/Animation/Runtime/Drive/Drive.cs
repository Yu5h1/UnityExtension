using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// What motion is allowed to touch. One adapter per UI technology implements these, and every kind of
    /// motion - <see cref="Reaction"/> and <see cref="Transition"/> alike - drives them without learning
    /// what is on the other side.
    /// <para>
    /// They are deliberately small and split by what they write rather than by what writes them. A wobble
    /// needs a pivot and no rectangle; travel needs a rectangle and no pivot. An adapter is free to
    /// implement both, and usually does.
    /// </para>
    /// </summary>
    public static class Drive
    {
        /// <summary>Something motion can drive. Dropped as soon as it stops being alive.</summary>
        public interface ITarget
        {
            /// <summary>False once the thing is gone - detached, destroyed or recycled.</summary>
            bool IsAlive { get; }
        }

        /// <summary>A target placed and faded by rectangle.</summary>
        public interface ISpatial : ITarget
        {
            /// <summary>Position and size in the caller's own space.</summary>
            Rect Rect { get; set; }

            /// <summary>0 invisible, 1 opaque.</summary>
            float Opacity { get; set; }
        }

        /// <summary>A target turned and scaled about a point.</summary>
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

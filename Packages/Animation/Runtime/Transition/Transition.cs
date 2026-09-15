using System;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// Motion with an end: told where to finish and how long to take, so interrupting it means cancelling it.
    /// The opposite of <see cref="Reaction"/>, which carries velocity and has no destination at all.
    /// <para>
    /// Note what is <em>not</em> here. A thrown object belongs to <see cref="Reaction"/>, however much it
    /// looks like travel, because nothing decides where it ends up: the physics do.
    /// </para>
    /// </summary>
    public static partial class Transition
    {
        /// <summary>How much bigger than its resting size an arriving item starts.</summary>
        [Serializable]
        public struct ScaleRange
        {
            [Tooltip("Smallest start size, as a multiple of the item's own size.")]
            public float min;

            [Tooltip("Largest start size, as a multiple of the item's own size.")]
            public float max;

            /// <summary>Never below life size, and tolerant of the two fields being entered the wrong way round.</summary>
            public float Min => Mathf.Max(1, Mathf.Min(min, max));

            /// <inheritdoc cref="Min"/>
            public float Max => Mathf.Max(Min, max);

            public static ScaleRange Default => new ScaleRange { min = 3, max = 10 };
        }

        /// <summary>
        /// A size curve read by convention: the shape is the curve normalized to its own value range, and
        /// the curve's END value is the final multiplier.
        /// <para>
        /// Splitting the two matters because the start comes from a random draw, not from the curve. Reading
        /// the curve as an absolute size instead would let a large random start drive the size negative -
        /// <c>Lerp(10, 1, 1.2)</c> is <c>-0.8</c>, and the bigger the draw the sooner it happens.
        /// </para>
        /// </summary>
        public readonly struct ScaleTrack
        {
            /// <summary>Normalized 0..1 shape over the trip.</summary>
            public readonly Func<float, float> Shape;

            /// <summary>Size at the end, as a multiple of the resting size.</summary>
            public readonly float EndMultiplier;

            public ScaleTrack(Func<float, float> shape, float endMultiplier)
            { Shape = shape; EndMultiplier = endMultiplier; }

            /// <summary>Reads a curve by the convention above. A missing curve becomes a straight arrival at life size.</summary>
            public static ScaleTrack From(Func<float, float> curve)
            {
                if (curve == null) return new ScaleTrack(t => t, 1);
                float first = curve(0), last = curve(1), span = last - first;
                if (Mathf.Abs(span) < 1e-6f) return new ScaleTrack(t => t, last);
                return new ScaleTrack(t => (curve(t) - first) / span, last);
            }
        }

        /// <summary>Where an item is at one moment of its trip. Free of UI types, so arrivals can be checked headlessly.</summary>
        public readonly struct Pose
        {
            public readonly Vector2 Centre;
            public readonly float Size, Opacity;
            public Pose(Vector2 centre, float size, float opacity)
            { Centre = centre; Size = size; Opacity = opacity; }
        }
    }
}

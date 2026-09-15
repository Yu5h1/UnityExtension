using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// Turns per-frame pointer movement into a usable throw velocity.
    /// <para>
    /// A raw delta divided by a raw interval is unusable: one short frame produces a spike, and the last
    /// sample before release is often the smallest. Smoothing across samples and clamping the interval is
    /// what makes a flick read as the gesture the user performed rather than as its final frame.
    /// </para>
    /// </summary>
    public static class PointerVelocity
    {
        /// <summary>Shortest interval trusted as real; below it the sample is treated as this long.</summary>
        private const float MinimumInterval = .008f;

        /// <summary>How much of the new reading replaces the running estimate. Higher follows, lower steadies.</summary>
        private const float Smoothing = .55f;

        /// <summary>
        /// The running velocity estimate after one more sample, in the same units as
        /// <paramref name="delta"/> per second. Returns zero rather than a spike when the sample is unusable.
        /// </summary>
        /// <param name="previous">Estimate carried from the last sample.</param>
        /// <param name="delta">Movement since that sample.</param>
        /// <param name="elapsed">Seconds since that sample.</param>
        /// <param name="maximumSpeed">Clamp for the instantaneous reading. Scale it to the caller's coordinate space.</param>
        public static Vector2 Sample(Vector2 previous, Vector2 delta, float elapsed, float maximumSpeed = 1000)
        {
            if (!elapsed.IsFinite() || elapsed <= 0 || !delta.IsFinite()) return Vector2.zero;
            var instant = Vector2.ClampMagnitude(delta / Mathf.Max(MinimumInterval, elapsed), maximumSpeed);
            return Vector2.Lerp(previous, instant, Smoothing);
        }
    }
}

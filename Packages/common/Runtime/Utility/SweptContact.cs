using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// Swept collision between a moving point and a rectangle, for pointer-driven interaction.
    /// <para>
    /// A pointer sampled once per frame can cross a small target entirely between two samples, so testing
    /// only the endpoints misses the hit. This clips the whole travel segment against the rectangle instead.
    /// </para>
    /// </summary>
    public static class SweptContact
    {
        /// <summary>
        /// Where the segment from <paramref name="start"/> to <paramref name="end"/> first enters
        /// <paramref name="bounds"/>. False when the travel misses it entirely, or when any input is not finite.
        /// </summary>
        /// <param name="contact">First point inside the rectangle; left at <paramref name="start"/> on a miss.</param>
        public static bool SegmentHit(Rect bounds, Vector2 start, Vector2 end, out Vector2 contact)
        {
            contact = start;
            if (bounds.width <= 0 || bounds.height <= 0 || !start.IsFinite() || !end.IsFinite()) return false;

            Vector2 delta = end - start;
            float enter = 0, exit = 1;

            // Liang-Barsky: clip the segment's parameter range against each of the four edges in turn.
            bool Clip(float direction, float distance)
            {
                if (Mathf.Abs(direction) < .00001f) return distance >= 0;   // parallel: inside or nothing to clip
                float time = distance / direction;
                if (direction < 0) enter = Mathf.Max(enter, time); else exit = Mathf.Min(exit, time);
                return enter <= exit;
            }

            if (!Clip(-delta.x, start.x - bounds.xMin) || !Clip(delta.x, bounds.xMax - start.x)
             || !Clip(-delta.y, start.y - bounds.yMin) || !Clip(delta.y, bounds.yMax - start.y)) return false;

            contact = start + delta * enter;
            return true;
        }
    }
}

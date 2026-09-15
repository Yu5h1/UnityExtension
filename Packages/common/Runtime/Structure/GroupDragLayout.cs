using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// Positions a set of named items inside a bounded area, keeps them clear of obstacles, and lets the
    /// whole group be dragged and thrown as one body.
    /// <para>
    /// Two arrangements: scattered, where an item keeps wherever the user left it, and sorted, where items
    /// fill a grid. Dragging translates and rotates the group about its own centre, then coasts to rest.
    /// </para>
    /// <para>
    /// Geometry only - it never touches a <c>VisualElement</c>, <c>Transform</c> or <c>Camera</c>. Obstacles
    /// arrive as plain rectangles, so what counts as an obstacle stays with the caller.
    /// </para>
    /// </summary>
    public sealed class GroupDragLayout
    {
        /// <summary>Ratios preserved from the tuned 62/44 pt desktop icons. They track the icon size
        /// rather than standing as separate settings: neither is a choice independent of the size.</summary>
        private const float ScatterRadiusRatio = 32f / 62f, ScatterClearanceRatio = 34f / 62f;
        private const float SortedRadiusRatio = 28f / 44f, SortedClearanceRatio = 33f / 44f;

        /// <summary>Share of the shorter bound used as the seed ring's radius.</summary>
        private const float RadialScale = .32f;

        /// <summary>Resolution of the fallback search when the wanted spot is occupied.</summary>
        private const int ProjectionSteps = 32;

        private readonly float scatterSize, sortedSize, sortedStride;
        private readonly float scatterRadius, scatterClearance, sortedRadius, sortedClearance;

        private readonly Dictionary<string, Vector2> cache = new Dictionary<string, Vector2>();
        private bool hasUserPlacement;
        private Vector2? seedOrigin;
        private Vector2 groupVelocity;
        private float angularVelocity;

        /// <summary>Resolved position per item id, in the same space as the bounds passed to <see cref="Resolve"/>.</summary>
        public readonly Dictionary<string, Vector2> Positions = new Dictionary<string, Vector2>();

        /// <summary>Area the items are confined to.</summary>
        public Rect Bounds { get; private set; }

        /// <summary>True while the sorted grid is in use.</summary>
        public bool Compact { get; private set; }

        /// <summary>Item size for the current arrangement.</summary>
        public float Size { get; private set; }

        /// <summary>True while the group still carries enough momentum to be worth ticking.</summary>
        public bool IsMoving => groupVelocity.sqrMagnitude > 1 || Mathf.Abs(angularVelocity) > .01f;

        /// <param name="scatterSize">Item size while scattered.</param>
        /// <param name="sortedSize">Item size in the sorted grid.</param>
        /// <param name="sortedStride">Grid pitch. Deliberately independent of <paramref name="sortedSize"/>:
        /// how much air sits between sorted items is a layout choice, not a function of the item.</param>
        public GroupDragLayout(float scatterSize = 62, float sortedSize = 44, float sortedStride = 72)
        {
            this.scatterSize = scatterSize;
            this.sortedSize = sortedSize;
            this.sortedStride = sortedStride;
            scatterRadius = scatterSize * ScatterRadiusRatio;
            scatterClearance = scatterSize * ScatterClearanceRatio;
            sortedRadius = sortedSize * SortedRadiusRatio;
            sortedClearance = sortedSize * SortedClearanceRatio;
        }

        /// <summary>Drops the momentum without moving anything.</summary>
        public void StopMotion() { groupVelocity = Vector2.zero; angularVelocity = 0; }

        /// <summary>Forgets every remembered placement, so the next <see cref="Resolve"/> seeds from scratch.</summary>
        public void ResetScatter() { StopMotion(); cache.Clear(); hasUserPlacement = false; }

        private Vector2 GroupCenter()
        {
            Vector2 center = Vector2.zero;
            foreach (var position in Positions.Values) center += position;
            return center / Mathf.Max(1, Positions.Count);
        }

        /// <summary>
        /// Moves the group with the pointer. The cross product of the grab arm and the drag gives the group
        /// a twist, so grabbing off-centre rotates it the way a real object would.
        /// </summary>
        public void DragGroup(Vector2 cursor, Vector2 delta, float deltaTime, IReadOnlyList<Rect> obstacles)
        {
            if (Compact || Positions.Count == 0 || deltaTime <= 0 || !delta.IsFinite()) return;
            Vector2 radius = cursor - GroupCenter();
            float angle = Mathf.Clamp((radius.x * delta.y - radius.y * delta.x)
                / Mathf.Max(6400, radius.sqrMagnitude), -.12f, .12f);
            Vector2 translation = delta - new Vector2(-radius.y, radius.x) * angle;
            float blend = 1 - Mathf.Exp(-deltaTime * 25);
            groupVelocity = Vector2.Lerp(groupVelocity, Vector2.ClampMagnitude(translation / deltaTime, 700), blend);
            angularVelocity = Mathf.Lerp(angularVelocity, Mathf.Clamp(angle / deltaTime, -2, 2), blend);
            TransformGroup(translation, angle, obstacles);
        }

        /// <summary>Coasts the group after release. Fixed sub-steps keep the decay frame-rate independent.</summary>
        public void AdvanceGroup(float deltaTime, IReadOnlyList<Rect> obstacles)
        {
            if (!IsMoving || Compact || !deltaTime.IsFinite() || deltaTime <= 0) return;
            float remaining = Mathf.Min(deltaTime, .05f);
            while (remaining > 0)
            {
                float step = Mathf.Min(remaining, 1f / 120);
                TransformGroup(groupVelocity * step, angularVelocity * step, obstacles);
                float damping = Mathf.Exp(-3 * step);
                groupVelocity *= damping; angularVelocity *= damping;
                remaining -= step;
            }
            if (!IsMoving) StopMotion();
        }

        private void TransformGroup(Vector2 translation, float angle, IReadOnlyList<Rect> obstacles)
        {
            Vector2 center = GroupCenter();
            float cosine = Mathf.Cos(angle), sine = Mathf.Sin(angle);
            bool collision = false;
            foreach (var id in new List<string>(Positions.Keys))
            {
                Vector2 old = Positions[id], offset = old - center;
                Vector2 desired = center + translation + new Vector2(offset.x * cosine - offset.y * sine,
                    offset.x * sine + offset.y * cosine);
                var occupied = new List<Rect>(obstacles);
                foreach (var pair in Positions)
                    if (pair.Key != id)
                        occupied.Add(new Rect(pair.Value - Vector2.one * scatterRadius,
                            Vector2.one * scatterRadius * 2));
                if (TryPlace(desired, Bounds, scatterClearance, occupied, out var projected)
                    && Vector2.Distance(projected, desired) <= 4)
                { Positions[id] = projected; collision |= (projected - desired).sqrMagnitude > .01f; }
                else collision = true;
            }
            // Bleeding speed on contact is what stops the group grinding along an obstacle forever.
            if (collision) { groupVelocity *= .85f; angularVelocity *= .85f; }
            Commit();
        }

        /// <summary>
        /// Lays out <paramref name="ids"/> inside <paramref name="bounds"/>, avoiding <paramref name="obstacles"/>.
        /// </summary>
        /// <param name="compact">True for the sorted grid, false to scatter.</param>
        /// <param name="active">Item being dragged right now; it is placed first so the rest yield to it.</param>
        /// <param name="desired">Where the active item wants to be.</param>
        /// <param name="origin">Centre of the seed ring for items with no remembered position.</param>
        public void Resolve(IReadOnlyList<string> ids, Rect bounds, IReadOnlyList<Rect> obstacles,
            bool compact, string active = null, Vector2? desired = null, Vector2? origin = null)
        {
            if (!hasUserPlacement && (bounds != Bounds || origin != seedOrigin)) cache.Clear();
            seedOrigin = origin;
            Bounds = bounds;
            Positions.Clear();
            Compact = compact;
            Size = scatterSize;
            if (!bounds.IsValid()) return;
            foreach (var obstacle in obstacles)
                if (!obstacle.IsFinite()) return;

            if (!compact)
            {
                var occupied = new List<Rect>(obstacles);
                if (active != null && desired.HasValue)
                    Place(active, desired.Value, occupied);
                for (int i = 0; i < ids.Count; i++)
                {
                    string id = ids[i];
                    if (id == active) continue;
                    float angle = (ids.Count == 1 ? 90 : -30 + 240f * i / (ids.Count - 1)) * Mathf.Deg2Rad;
                    Vector2 center = origin ?? bounds.center;
                    if (!center.IsFinite()) center = bounds.center;
                    float radius = Mathf.Max(scatterSize, Mathf.Min(bounds.width, bounds.height) * RadialScale);
                    Vector2 candidate = cache.TryGetValue(id, out var saved) && saved.IsFinite()
                        ? bounds.min + Vector2.Scale(saved, bounds.size)
                        : center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    Place(id, candidate, occupied);
                }
                // Only remember the arrangement when nothing is being dragged, or the drag pollutes the memory.
                if (active == null)
                    foreach (var pair in Positions)
                        if (!cache.ContainsKey(pair.Key))
                            cache[pair.Key] = new Vector2((pair.Value.x - bounds.xMin) / bounds.width,
                                (pair.Value.y - bounds.yMin) / bounds.height);
                return;
            }

            Compact = true;
            Positions.Clear();
            Size = sortedSize;
            var slots = new List<Rect>(obstacles);
            int columns = Mathf.Max(1, Mathf.FloorToInt(bounds.width / sortedStride));
            for (int i = 0; i < ids.Count; i++)
            {
                Vector2 slot = bounds.min + new Vector2(sortedClearance + i % columns * sortedStride,
                                                        sortedClearance + i / columns * sortedStride);
                if (TryPlace(slot, bounds, sortedClearance, slots, out var position))
                {
                    Positions[ids[i]] = position;
                    slots.Add(new Rect(position - Vector2.one * sortedRadius, Vector2.one * (sortedRadius * 2)));
                }
            }
        }

        private bool Place(string id, Vector2 desired, List<Rect> obstacles)
        {
            if (!TryPlace(desired, Bounds, scatterClearance, obstacles, out var position)) return false;
            Positions[id] = position;
            obstacles.Add(new Rect(position - Vector2.one * scatterRadius, Vector2.one * (scatterRadius * 2)));
            return true;
        }

        /// <summary>Remembers the current arrangement as the user's own, so later resolves keep it.</summary>
        public void Commit()
        {
            if (Compact || !Bounds.IsValid()) return;
            hasUserPlacement = true;
            foreach (var pair in Positions)
                cache[pair.Key] = new Vector2((pair.Value.x - Bounds.xMin) / Bounds.width,
                    (pair.Value.y - Bounds.yMin) / Bounds.height);
        }

        /// <summary>
        /// Nearest free spot to <paramref name="desired"/> that keeps <paramref name="radius"/> clear of every
        /// obstacle and stays inside <paramref name="bounds"/>. Tries the wanted spot first and only then
        /// sweeps a grid, so an uncontested placement costs nothing.
        /// </summary>
        public static bool TryPlace(Vector2 desired, Rect bounds, float radius,
            IReadOnlyList<Rect> obstacles, out Vector2 result)
        {
            result = desired;
            if (!bounds.IsFinite() || !desired.IsFinite() || !radius.IsFinite()) return false;
            foreach (var obstacle in obstacles)
                if (!obstacle.IsFinite()) return false;
            if (bounds.width < radius * 2 || bounds.height < radius * 2) return false;

            var area = Rect.MinMaxRect(bounds.xMin + radius, bounds.yMin + radius,
                bounds.xMax - radius, bounds.yMax - radius);
            var clamped = new Vector2(Mathf.Clamp(desired.x, area.xMin, area.xMax),
                Mathf.Clamp(desired.y, area.yMin, area.yMax));
            if (Clear(clamped, radius, obstacles)) { result = clamped; return true; }

            float best = float.PositiveInfinity;
            for (int y = 0; y <= ProjectionSteps; y++)
                for (int x = 0; x <= ProjectionSteps; x++)
                {
                    var candidate = new Vector2(Mathf.Lerp(area.xMin, area.xMax, x / (float)ProjectionSteps),
                        Mathf.Lerp(area.yMin, area.yMax, y / (float)ProjectionSteps));
                    float distance = (candidate - desired).sqrMagnitude;
                    if (distance >= best || !Clear(candidate, radius, obstacles)) continue;
                    best = distance; result = candidate;
                }
            return !float.IsPositiveInfinity(best);
        }

        private static bool Clear(Vector2 position, float radius, IReadOnlyList<Rect> obstacles)
        {
            foreach (var rect in obstacles)
            {
                var nearest = new Vector2(Mathf.Clamp(position.x, rect.xMin, rect.xMax),
                    Mathf.Clamp(position.y, rect.yMin, rect.yMax));
                if ((position - nearest).sqrMagnitude < radius * radius) return false;
            }
            return true;
        }
    }
}

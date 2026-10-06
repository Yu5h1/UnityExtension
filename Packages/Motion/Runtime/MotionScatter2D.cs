// Unity value adapter for native ordered placement and attraction; the host owns capture and scheduling.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>One ordered item result. A null position means no available placement.</summary>
    public readonly struct MotionPlacement
    {
        public readonly int Id;
        public readonly Vector2? Position;
        internal MotionPlacement(int id, Vector2? position) { Id = id; Position = position; }
    }

    /// <summary>Owns a native scatter cache. IDs are stable integers. Use on one thread and dispose explicitly.</summary>
    public sealed class MotionScatter2D : IDisposable
    {
        private readonly MotionNativeHandle handle;
        private int[] ids = Array.Empty<int>();
        public static float DefaultSize { get { MotionRuntime.EnsureAvailable(); return NativeMethods.motion_scatter_default(0); } }
        public static float DefaultSpread { get { MotionRuntime.EnsureAvailable(); return NativeMethods.motion_scatter_default(1); } }
        public MotionScatter2D(float? size = null)
        {
            float value = size ?? DefaultSize;
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(size));
            handle = new MotionNativeHandle(true, value);
        }
        public void SetItems(IReadOnlyList<int> items)
        {
            handle.EnsureAlive();
            if (items == null) throw new ArgumentNullException(nameof(items));
            var copy = new int[items.Count];
            var unique = new HashSet<int>();
            for (int i = 0; i < items.Count; i++)
            {
                copy[i] = items[i];
                if (!unique.Add(copy[i])) throw new ArgumentException("Motion item IDs must be unique.", nameof(items));
            }
            NativeMethods.motion_scatter_clear_items(handle);
            foreach (int id in copy) NativeMethods.motion_scatter_add_item(handle, id);
            ids = copy;
        }
        public void SetObstacles(IReadOnlyList<Rect> obstacles)
        {
            handle.EnsureAlive();
            if (obstacles == null) throw new ArgumentNullException(nameof(obstacles));
            var copy = new Rect[obstacles.Count];
            for (int i = 0; i < copy.Length; i++) copy[i] = obstacles[i];
            NativeMethods.motion_scatter_clear_obstacles(handle);
            foreach (var rect in copy) NativeMethods.motion_scatter_add_obstacle(handle, rect.x, rect.y, rect.width, rect.height);
        }
        public void Reset() { handle.EnsureAlive(); NativeMethods.motion_scatter_reset(handle); }
        public void Commit() { handle.EnsureAlive(); NativeMethods.motion_scatter_commit(handle); }
        public void SetSpread(float spread) { handle.EnsureAlive(); NativeMethods.motion_scatter_set_spread(handle, spread); }
        public MotionPlacement[] Resolve(Rect bounds, float? spread = null, Vector2? origin = null)
        {
            handle.EnsureAlive();
            Vector2 point = origin ?? default;
            NativeMethods.motion_scatter_resolve(handle, bounds.x, bounds.y, bounds.width, bounds.height,
                spread ?? DefaultSpread, origin.HasValue ? 1 : 0, point.x, point.y);
            return Positions;
        }
        /// <summary>Returns whether a solve was attempted, not whether the item found a free position.</summary>
        public bool Pull(int id, Vector2 current, Vector2 target, float radius, float force, float dt, Vector2? origin = null)
        {
            handle.EnsureAlive();
            if (Array.IndexOf(ids, id) < 0) return false;
            Vector2 point = origin ?? default;
            return NativeMethods.motion_scatter_pull(handle, id, current.x, current.y, target.x, target.y,
                radius, force, dt, origin.HasValue ? 1 : 0, point.x, point.y) != 0;
        }
        public MotionPlacement[] Positions
        {
            get
            {
                handle.EnsureAlive();
                var result = new MotionPlacement[ids.Length];
                for (int i = 0; i < ids.Length; i++)
                    result[i] = new MotionPlacement(ids[i], NativeMethods.motion_scatter_read(handle, i, 0) != 0
                        ? new Vector2(NativeMethods.motion_scatter_read(handle, i, 1), NativeMethods.motion_scatter_read(handle, i, 2))
                        : (Vector2?)null);
                return result;
            }
        }
        public void Dispose() => handle.Dispose();
    }
}

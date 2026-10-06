// Unity value adapter for the shared Throw service; callers supply seconds and local-plane units.
using System;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>Throw parameters whose defaults are read from the native core.</summary>
    [Serializable]
    public struct MotionThrowSettings
    {
        public float edgeBounce, drag, restingSpeed, lifetime;
        public static MotionThrowSettings Default
        {
            get
            {
                MotionRuntime.EnsureAvailable();
                return new MotionThrowSettings {
                    edgeBounce = NativeMethods.motion_throw_default(0), drag = NativeMethods.motion_throw_default(1),
                    restingSpeed = NativeMethods.motion_throw_default(2), lifetime = NativeMethods.motion_throw_default(3)
                };
            }
        }
    }

    /// <summary>A copied snapshot; modifying it never changes native state.</summary>
    public readonly struct MotionThrowState
    {
        public readonly Vector2 Position, Velocity;
        public readonly float Remaining;
        public readonly bool IsFlying, HasStruck;
        internal MotionThrowState(Vector2 position, Vector2 velocity, float remaining, bool flying, bool struck)
        { Position = position; Velocity = velocity; Remaining = remaining; IsFlying = flying; HasStruck = struck; }
    }

    /// <summary>Owns one native 2D flight. Use on one thread and dispose when its host unloads.</summary>
    public sealed class MotionThrow2D : IDisposable
    {
        private readonly MotionNativeHandle handle = new MotionNativeHandle(false);
        public void Launch(Vector2 position, Vector2 velocity, float? lifetime = null)
        {
            handle.EnsureAlive();
            NativeMethods.motion_throw_launch(handle, position.x, position.y, velocity.x, velocity.y,
                lifetime ?? NativeMethods.motion_throw_default(3));
        }
        public void Stop() { handle.EnsureAlive(); NativeMethods.motion_throw_stop(handle); }
        public bool Tick(float dt, Rect bounds, float radius) => Tick(dt, bounds, radius, MotionThrowSettings.Default);
        /// <summary>Null settings preserve the flight without advancing, matching the source contract.</summary>
        public bool Tick(float dt, Rect bounds, float radius, MotionThrowSettings? settings)
        {
            handle.EnsureAlive();
            var value = settings ?? default;
            return NativeMethods.motion_throw_tick(handle, dt, bounds.x, bounds.y, bounds.width, bounds.height,
                radius, value.edgeBounce, value.drag, value.restingSpeed, settings.HasValue ? 1 : 0) != 0;
        }
        public MotionThrowState State
        {
            get
            {
                handle.EnsureAlive();
                return new MotionThrowState(new Vector2(Read(0), Read(1)), new Vector2(Read(2), Read(3)),
                    Read(4), Read(5) != 0, Read(6) != 0);
            }
        }
        private float Read(int field) => NativeMethods.motion_throw_read(handle, field);
        public void Dispose() => handle.Dispose();
    }
}

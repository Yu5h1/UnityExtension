// Native capability checks and ownership; no numerical solver is implemented in C#.
using System;
using System.Runtime.InteropServices;

namespace Yu5h1Lib
{
    /// <summary>Validates the platform and the complete Motion ABI before allocating native state.</summary>
    public static class MotionRuntime
    {
        public const uint AbiVersion = 1;

        public static void EnsureAvailable()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || (UNITY_ANDROID && !UNITY_EDITOR) || MOTION_STANDALONE_TEST
            try
            {
                uint actual = NativeMethods.motion_abi_version();
                if (actual != AbiVersion) throw new NotSupportedException($"Motion ABI {actual} is incompatible with {AbiVersion}.");
                NativeMethods.motion_scatter_default(0);
            }
            catch (DllNotFoundException error) { throw new InvalidOperationException("MotionNative plugin is missing for this platform. Check the package delivery manifest and Plugin Inspector.", error); }
            catch (EntryPointNotFoundException error) { throw new InvalidOperationException("MotionNative plugin is incomplete. Install the matching DLL/SO and C# binding from the same delivery.", error); }
            catch (BadImageFormatException error) { throw new InvalidOperationException("MotionNative plugin architecture does not match this process.", error); }
#else
            throw new PlatformNotSupportedException("Motion supports Windows x64 and Android arm64-v8a. This platform has no validated native plugin.");
#endif
        }
    }

    internal sealed class MotionNativeHandle : SafeHandle
    {
        private readonly bool scatter;
        internal MotionNativeHandle(bool scatter, float size = 0) : base(IntPtr.Zero, true)
        {
            MotionRuntime.EnsureAvailable();
            this.scatter = scatter;
            SetHandle(scatter ? NativeMethods.motion_scatter_create(size) : NativeMethods.motion_throw_create());
            if (IsInvalid) throw new OutOfMemoryException("Motion native allocation failed.");
        }
        public override bool IsInvalid => handle == IntPtr.Zero;
        internal void EnsureAlive()
        {
            if (IsClosed || IsInvalid) throw new ObjectDisposedException(scatter ? nameof(MotionScatter2D) : nameof(MotionThrow2D));
        }
        protected override bool ReleaseHandle()
        {
            if (scatter) NativeMethods.motion_scatter_destroy(handle);
            else NativeMethods.motion_throw_destroy(handle);
            return true;
        }
    }
}

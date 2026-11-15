using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace Yu5h1Lib
{
    /// <summary>
    ///   GPU state of particles held by Transforms (ParticleExtensions EXT-05), driving the kernels
    ///   in Resources/Yu5h1LibParticleHold. Owners hand it each step's holder matrices and the
    ///   entries to hold; it never decides which particles are held.
    /// </summary>
    internal sealed class ParticleHoldSet : IDisposable
    {
        const string ShaderName = "Yu5h1LibParticleHold";
        static readonly int HoldersId = Shader.PropertyToID("_Holders");
        static readonly int EntriesId = Shader.PropertyToID("_HoldEntries");
        static readonly int CountId = Shader.PropertyToID("_HoldCount");
        static readonly int EntryCountId = Shader.PropertyToID("_HoldEntryCount");
        static readonly int RadiusId = Shader.PropertyToID("_HoldRadius");

        [StructLayout(LayoutKind.Sequential)]
        struct GpuHolder
        {
            public Matrix4x4 previous;
            public Matrix4x4 current;
            public Matrix4x4 previousInverse;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Entry
        {
            public uint particle;
            public uint holder;
            public uint held;
            public float originalInvMass;
            public Vector4 localOffset;
        }

        static ComputeShader s_shader;
        static int s_apply, s_capture, s_release;

        ComputeBuffer _holders;
        ComputeBuffer _entries;
        readonly ComputeBuffer _count;
        GpuHolder[] _holderData = new GpuHolder[1];
        readonly uint[] _zero = { 0u };

        public ParticleHoldSet()
        {
            _holders = new ComputeBuffer(1, Marshal.SizeOf<GpuHolder>());
            _entries = new ComputeBuffer(1, Marshal.SizeOf<Entry>());
            _count = new ComputeBuffer(1, sizeof(uint));
        }

        public int EntryCount { get; private set; }

        /// <summary>
        ///   Loads the hold kernels, or returns false with the reason.
        /// </summary>
        public static bool TryLoadShader(out string error)
        {
            error = null;
            if (s_shader != null)
                return true;
            if (!SystemInfo.supportsComputeShaders)
            {
                error = "This platform has no compute shaders.";
                return false;
            }
            s_shader = Resources.Load<ComputeShader>(ShaderName);
            if (s_shader == null)
            {
                error = $"Compute shader Resources/{ShaderName} is missing.";
                return false;
            }
            s_apply = s_shader.FindKernel("ApplyHolds");
            s_capture = s_shader.FindKernel("CaptureHolds");
            s_release = s_shader.FindKernel("ReleaseHolds");
            return true;
        }

        /// <summary>
        ///   Uploads holder i's matrices at the start and end of this step.
        /// </summary>
        public void SetHolders(Matrix4x4[] previous, Matrix4x4[] current, int count)
        {
            if (_holderData.Length < count)
                _holderData = new GpuHolder[Mathf.NextPowerOfTwo(count)];
            for (int i = 0; i < count; i++)
                _holderData[i] = new GpuHolder
                {
                    previous = previous[i],
                    current = current[i],
                    previousInverse = previous[i].inverse
                };
            Ensure(ref _holders, count, Marshal.SizeOf<GpuHolder>());
            if (count > 0)
                _holders.SetData(_holderData, 0, 0, count);
        }

        public void SetEntries(Entry[] entries, int count)
        {
            Ensure(ref _entries, count, Marshal.SizeOf<Entry>());
            if (count > 0)
                _entries.SetData(entries, 0, 0, count);
            EntryCount = count;
        }

        public void Apply(ParticleExtensionContext context) => Run(context, s_apply);

        /// <summary>
        ///   Holds every free entry within <paramref name="radius"/> of holder 0's position at the
        ///   start of the step and counts them for <see cref="RequestCount"/>.
        /// </summary>
        public void Capture(ParticleExtensionContext context, float radius)
        {
            _count.SetData(_zero);
            s_shader.SetFloat(RadiusId, radius);
            Run(context, s_capture);
        }

        public void RequestCount(Action<int> onComplete)
        {
            AsyncGPUReadback.Request(_count, request =>
                onComplete(request.hasError ? -1 : (int)request.GetData<uint>()[0]));
        }

        /// <summary>
        ///   Gives every held entry its recorded inverse mass back, immediately. Returns false when
        ///   the world has no GPU data.
        /// </summary>
        public bool Release(ParticleWorld world)
        {
            if (EntryCount == 0 || world == null || s_shader == null || !world.TryBind(s_shader, s_release))
                return false;
            BindOwn(s_release);
            ParticleExtensionContext.Dispatch(s_shader, s_release, EntryCount);
            return true;
        }

        public void Dispose()
        {
            _holders?.Release();
            _entries?.Release();
            _count?.Release();
            _holders = null;
            _entries = null;
        }

        void Run(ParticleExtensionContext context, int kernel)
        {
            if (EntryCount == 0)
                return;
            context.Bind(s_shader, kernel);
            BindOwn(kernel);
            ParticleExtensionContext.Dispatch(s_shader, kernel, EntryCount);
        }

        void BindOwn(int kernel)
        {
            s_shader.SetBuffer(kernel, HoldersId, _holders);
            s_shader.SetBuffer(kernel, EntriesId, _entries);
            s_shader.SetBuffer(kernel, CountId, _count);
            s_shader.SetInt(EntryCountId, EntryCount);
        }

        static void Ensure(ref ComputeBuffer buffer, int count, int stride)
        {
            if (count <= buffer.count)
                return;
            buffer.Release();
            buffer = new ComputeBuffer(Mathf.NextPowerOfTwo(count), stride);
        }
    }
}

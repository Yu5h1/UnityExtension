using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   Binds a world's particle data to a solver backend's buffers (ParticleExtensions EXT-03).
    /// </summary>
    internal interface IParticleDataBinder
    {
        void Bind(ComputeShader shader, int kernel);

        void Bind(MaterialPropertyBlock block);

        void BindMedia(ComputeShader shader, int kernel);

        void BindSleep(ComputeShader shader, int kernel);
    }

    /// <summary>
    ///   What an extension sees of the step it runs in: timing, the substep it is called for, and
    ///   access to the world's particle buffers as declared in Shaders/ParticleWorldData.hlsl
    ///   (ParticleExtensions EXT-02, EXT-03).
    /// </summary>
    public sealed class ParticleExtensionContext
    {
        static readonly int SubstepIndexId = Shader.PropertyToID("_SubstepIndex");
        static readonly int SubstepCountId = Shader.PropertyToID("_SubstepCount");
        static readonly int SubstepId = Shader.PropertyToID("_Substep");
        static readonly int StepTimeId = Shader.PropertyToID("_StepTime");
        static readonly int GravityId = Shader.PropertyToID("_Gravity");

        internal ParticleExtensionContext(ParticleWorld world)
        {
            World = world;
        }

        public ParticleWorld World { get; }

        /// <summary>
        ///   The world's step number, as in <see cref="ParticleReadback.Step"/>.
        /// </summary>
        public int Step { get; internal set; }

        public float StepTime { get; internal set; }

        public float Substep { get; internal set; }

        public int SubstepIndex { get; internal set; }

        public int SubstepCount { get; internal set; }

        public Vector3 Gravity { get; internal set; }

        /// <summary>
        ///   Fraction of the step at the start of the current substep, s / n.
        /// </summary>
        public float FractionStart => SubstepCount > 0 ? (float)SubstepIndex / SubstepCount : 0f;

        /// <summary>
        ///   Fraction of the step at the end of the current substep, (s + 1) / n.
        /// </summary>
        public float FractionEnd => SubstepCount > 0 ? (float)(SubstepIndex + 1) / SubstepCount : 1f;

        internal IParticleDataBinder Binder { get; set; }

        /// <summary>
        ///   True when <paramref name="entity"/> belongs to this world, is alive and not parked.
        /// </summary>
        public bool IsSimulated(ParticleEntity entity) =>
            entity.World == World && entity.IsValid && !entity.IsParked;

        /// <summary>
        ///   Binds the particle buffers, the step's media list (_Media, _MediumCount of
        ///   Shaders/ParticleWorldMedia.hlsl) and the sleep push buffer (_SleepPush of
        ///   Shaders/ParticleWorldSleep.hlsl), and sets the step uniforms on
        ///   <paramref name="kernel"/>. Names a kernel does not declare are ignored.
        /// </summary>
        public void Bind(ComputeShader shader, int kernel)
        {
            Binder?.Bind(shader, kernel);
            Binder?.BindMedia(shader, kernel);
            Binder?.BindSleep(shader, kernel);
            shader.SetInt(SubstepIndexId, SubstepIndex);
            shader.SetInt(SubstepCountId, SubstepCount);
            shader.SetFloat(SubstepId, Substep);
            shader.SetFloat(StepTimeId, StepTime);
            shader.SetVector(GravityId, Gravity);
        }

        /// <summary>
        ///   Dispatches enough groups of <paramref name="kernel"/>'s thread size to cover
        ///   <paramref name="threads"/>; does nothing for zero threads.
        /// </summary>
        public static void Dispatch(ComputeShader shader, int kernel, int threads)
        {
            if (threads <= 0)
                return;
            shader.GetKernelThreadGroupSizes(kernel, out uint size, out _, out _);
            shader.Dispatch(kernel, (threads + (int)size - 1) / (int)size, 1, 1);
        }
    }
}

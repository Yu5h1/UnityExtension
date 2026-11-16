using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Yu5h1Lib
{
    /// <summary>
    ///   The post-step extension of one <see cref="ParticleEmitter"/>: it owns the GPU buffers of the
    ///   emitter's bodies and dispatches the behaviour kernels of Resources/Yu5h1LibParticleBodyBehaviour
    ///   in the order of Unified-Solver's SolverParticleModifierRunner (ParticleExtensions EXT-12,
    ///   EXT-13, EXT-14). Sleep decisions are made on the GPU and reach the CPU through one
    ///   asynchronous readback of a command per body; the CPU applies them with
    ///   <see cref="ParticleEntity.SetSleeping"/> at the next step boundary and then resets the body's
    ///   GPU sleep state. Nothing here reads the GPU synchronously.
    /// </summary>
    internal sealed class ParticleBodyRunner : IParticleExtension, IDisposable
    {
        const string ShaderName = "Yu5h1LibParticleBodyBehaviour";
        const float SleepAwake = 0f;
        const float SleepSleeping = 2f;
        const uint CommandSleep = 1u;
        const uint CommandWake = 2u;

        static readonly string[] KernelNames =
        {
            "ApplySpeedLimit", "ApplyMediumState", "ApplyRollDamping", "ApplyOscillation",
            "ApplySurfaceImpulse", "ApplyLocomotion", "ApplyBounds", "ApplySleep"
        };

        const int SpeedLimit = 0, MediumState = 1, RollDamping = 2, Oscillation = 3,
            SurfaceImpulse = 4, Locomotion = 5, Bounds = 6, Sleep = 7;

        [StructLayout(LayoutKind.Sequential)]
        struct GpuBounds
        {
            public Vector4 center;
            public Vector4 halfExtents;
            public Vector4 axisX;
            public Vector4 axisY;
            public Vector4 axisZ;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct GpuTarget
        {
            public Vector3 position;
            public float mode;
            public Vector3 direction;
            public float radius;
        }

        /// <summary>
        ///   Called with the kernel's name before each dispatch; tests use it to check the order
        ///   (ParticleExtensions EXT-12).
        /// </summary>
        internal Action<string> onKernel;

        readonly ParticleEmitter _emitter;
        readonly ComputeShader _shader;
        readonly int[] _kernels = new int[KernelNames.Length];
        readonly int _capacity;
        readonly ComputeBuffer _bodies;
        readonly ComputeBuffer _lifecycle;
        readonly ComputeBuffer _mediumState;
        readonly ComputeBuffer _sleepState;
        readonly ComputeBuffer _sleepCommands;
        ComputeBuffer _bounds;
        ComputeBuffer _targets;
        readonly ParticleBodyData[] _bodyData;
        readonly bool[] _bodyDirty;
        readonly bool[] _stateDirty;
        readonly bool[] _lifecycleDirty;
        readonly int[] _epoch;
        readonly Vector2[] _stateScratch;
        readonly List<GpuBounds> _boundsData = new List<GpuBounds>();
        readonly List<GpuTarget> _targetData = new List<GpuTarget>();
        int _count;
        int _boundsCount;
        int _targetCount;
        float _time;
        bool _commandsRequested;
        bool _disposed;
        bool _reportedSpawnOutsideContain;
        bool _reportedSpawnInsideExclude;

        public ParticleBodyRunner(ParticleEmitter emitter, ComputeShader shader, int capacity)
        {
            _emitter = emitter;
            _capacity = Mathf.Max(1, capacity);
            _shader = Object.Instantiate(shader);
            _shader.hideFlags = HideFlags.HideAndDontSave;
            for (int k = 0; k < KernelNames.Length; k++)
                _kernels[k] = shader.FindKernel(KernelNames[k]);
            _bodies = new ComputeBuffer(_capacity, ParticleBodyData.Stride);
            _lifecycle = Zeroed(_capacity, 16);
            _mediumState = Zeroed(_capacity, 16);
            _sleepState = Zeroed(_capacity, 8);
            _sleepCommands = Zeroed(_capacity, 4);
            _bounds = new ComputeBuffer(4, Marshal.SizeOf<GpuBounds>());
            _targets = new ComputeBuffer(4, Marshal.SizeOf<GpuTarget>());
            _bodyData = new ParticleBodyData[_capacity];
            _bodyDirty = new bool[_capacity];
            _stateDirty = new bool[_capacity];
            _lifecycleDirty = new bool[_capacity];
            _epoch = new int[_capacity];
            _stateScratch = new Vector2[_capacity];
        }

        public ComputeBuffer Bodies => _bodies;
        public ComputeBuffer Lifecycle => _lifecycle;

        /// <summary>
        ///   Per body float4: submersion, drift, lift, medium-to-body density ratio. Written every
        ///   step by ApplyMediumState; read by sleep and locomotion, and by tests.
        /// </summary>
        internal ComputeBuffer MediumStateBuffer => _mediumState;
        public int Version { get; private set; }
        public int Count => _count;

        /// <summary>
        ///   Seconds of simulated steps the kernels have seen: the sum of the step times of every step
        ///   this extension prepared. The behaviours' rhythms run on it, so they do not depend on the
        ///   engine's clock.
        /// </summary>
        public float Time => _time;

        public ParticleExtensionStage Stage => ParticleExtensionStage.PostStep;
        public ParticleExtensionWrites Writes => ParticleExtensionWrites.Positions | ParticleExtensionWrites.Velocities;

        public static bool TryLoadShader(out ComputeShader shader, out string error)
        {
            error = null;
            shader = Resources.Load<ComputeShader>(ShaderName);
            if (shader == null)
                error = $"compute shader Resources/{ShaderName} is missing.";
            return shader != null;
        }

        static ComputeBuffer Zeroed(int count, int stride)
        {
            var buffer = new ComputeBuffer(count, stride);
            buffer.SetData(new byte[count * stride]);
            return buffer;
        }

        /// <summary>
        ///   Adds or replaces the body at <paramref name="index"/>; indices follow the emitter's
        ///   entity list.
        /// </summary>
        public void SetBody(int index, ParticleBodyData data, uint colorBits)
        {
            data.color = colorBits;
            _bodyData[index] = data;
            _bodyDirty[index] = true;
            _stateDirty[index] = true;
            _lifecycleDirty[index] = true;
            _count = Mathf.Max(_count, index + 1);
            Version++;
        }

        /// <summary>
        ///   Forgets lifecycle, sleep state and pending sleep commands of every body, so a respawned
        ///   body starts awake and fully visible (ParticleExtensions EXT-11, EXT-13).
        /// </summary>
        public void ResetState()
        {
            for (int i = 0; i < _count; i++)
            {
                _stateDirty[i] = true;
                _lifecycleDirty[i] = true;
                _epoch[i]++;
            }
            Version++;
        }

        public void Prepare(ParticleExtensionContext context)
        {
            if (_count == 0)
                return;
            _time += context.StepTime;
            SyncFlags();
            UploadDirty();
            UploadBounds();
            UploadTargets();
        }

        public void Execute(ParticleExtensionContext context)
        {
            if (_count == 0)
                return;
            ParticleBehaviourProfile behaviour = _emitter.EffectiveBehaviour;
            Transform transform = _emitter.transform;
            Vector3 half = _emitter.SpawnVolume * 0.5f;
            Vector3 gravity = context.Gravity;

            _shader.SetInt("_BodyCount", _count);
            _shader.SetInt("_BoundsCount", _boundsCount);
            _shader.SetInt("_MotionTargetCount", _targetCount);
            _shader.SetFloat("_Time", _time);
            _shader.SetFloat("_SubDeltaTime", context.Substep);
            _shader.SetVector("_UpAxis", gravity.sqrMagnitude > 1e-8f ? -gravity.normalized : Vector3.up);
            _shader.SetVector("_SpawnCenter", transform.position);
            _shader.SetVector("_SpawnAxisX", transform.right * half.x);
            _shader.SetVector("_SpawnAxisY", transform.up * half.y);
            _shader.SetVector("_SpawnAxisZ", transform.forward * half.z);

            float speedLimit = Mathf.Max(0f, behaviour.SpeedLimit);
            if (speedLimit > 0f)
            {
                _shader.SetFloat("_SpeedLimit", speedLimit);
                _shader.SetFloat("_SpeedDecayRate", Mathf.Max(0.01f, behaviour.SpeedDecayRate));
                Run(context, SpeedLimit);
            }

            Run(context, MediumState);

            _shader.SetFloat("_RollDamping", Mathf.Clamp01(behaviour.RollDamping));
            Run(context, RollDamping);

            ParticleBehaviourModifier[] modifiers = behaviour.Modifiers;
            if (modifiers != null)
                foreach (ParticleBehaviourModifier modifier in modifiers)
                {
                    if (modifier == null || !modifier.Enabled)
                        continue;
                    if (modifier is ParticleOscillationProfile oscillation)
                        RunOscillation(context, oscillation);
                    else if (modifier is ParticleSurfaceImpulseProfile surface)
                        RunSurfaceImpulse(context, surface);
                    else if (modifier is ParticleLocomotionProfile locomotion)
                        RunLocomotion(context, locomotion);
                }

            Run(context, Bounds);

            float sleepSpeed = Mathf.Max(0f, behaviour.SleepSpeed);
            if (sleepSpeed > 0f)
            {
                _shader.SetFloat("_SleepSpeed", sleepSpeed);
                _shader.SetFloat("_SleepDelay", Mathf.Max(0f, behaviour.SleepDelay));
                _shader.SetFloat("_WakeDistance", Mathf.Max(0.0001f, behaviour.WakeDistance));
                _shader.SetFloat("_KeepAwake", behaviour.KeepsAwake ? 1f : 0f);
                Run(context, Sleep);
                RequestCommands();
            }
        }

        void RunOscillation(ParticleExtensionContext context, ParticleOscillationProfile profile)
        {
            _shader.SetFloat("_OscillationAcceleration", profile.acceleration);
            _shader.SetFloat("_OscillationFrequency", profile.frequency);
            _shader.SetFloat("_OscillationDuration", Mathf.Max(0f, profile.duration));
            _shader.SetFloat("_OscillationDurationRandomness", Mathf.Clamp01(profile.durationRandomness));
            _shader.SetFloat("_OscillationRandomness", profile.frequencyRandomness);
            _shader.SetFloat("_OscillationDirectionAngle", profile.directionAngle);
            _shader.SetFloat("_OscillationDirectionRandomness", profile.directionRandomness);
            _shader.SetFloat("_OscillationStiffness", Mathf.Clamp01(profile.stiffness));
            _shader.SetFloat("_OscillationMuscleTension", Mathf.Clamp01(profile.muscleTension));
            _shader.SetFloat("_OscillationTensionRandomness", profile.tensionRandomness);
            _shader.SetFloat("_OscillationTailBias", Mathf.Clamp01(profile.tailBias));
            _shader.SetFloat("_OscillationVitality", Mathf.Clamp01(profile.vitality));
            Run(context, Oscillation);
        }

        void RunSurfaceImpulse(ParticleExtensionContext context, ParticleSurfaceImpulseProfile profile)
        {
            float step = Mathf.Max(1e-5f, context.StepTime);
            float spread = Mathf.Max(1f, profile.impactSpread) * step;
            float period = 1f / Mathf.Max(1e-4f, profile.frequency);
            _shader.SetFloat("_SurfaceSpreadSeconds", Mathf.Max(step, Mathf.Min(spread, period * 0.9f)));
            _shader.SetFloat("_SurfaceImpulseSpeed", profile.impulseSpeed);
            _shader.SetFloat("_SurfaceContactSpeed", profile.fallSpeedLimit);
            _shader.SetFloat("_SurfaceFrequency", profile.frequency);
            _shader.SetFloat("_SurfaceRandomness", profile.frequencyRandomness);
            Run(context, SurfaceImpulse);
        }

        void RunLocomotion(ParticleExtensionContext context, ParticleLocomotionProfile profile)
        {
            _shader.SetFloat("_LocomotionSpeed", Mathf.Max(0f, profile.speed));
            _shader.SetFloat("_LocomotionFrequency", Mathf.Max(0f, profile.frequency));
            _shader.SetFloat("_LocomotionDuration", Mathf.Max(0.01f, profile.duration));
            _shader.SetFloat("_LocomotionRandomness", Mathf.Clamp01(profile.randomness));
            _shader.SetFloat("_LocomotionTurnRate", Mathf.Max(0f, profile.turnRate) * Mathf.Deg2Rad);
            _shader.SetFloat("_LocomotionUprightRate", Mathf.Max(0f, profile.uprightRate) * Mathf.Deg2Rad);
            _shader.SetFloat("_LocomotionMediumThrust", Mathf.Max(0f, profile.mediumThrust));
            _shader.SetFloat("_LocomotionHeadingSpread", Mathf.Clamp(profile.headingSpread, 0f, 89f) * Mathf.Deg2Rad);
            Run(context, Locomotion);
        }

        void Run(ParticleExtensionContext context, int kernelIndex)
        {
            onKernel?.Invoke(KernelNames[kernelIndex]);
            int kernel = _kernels[kernelIndex];
            context.Bind(_shader, kernel);
            _shader.SetBuffer(kernel, "_Bodies", _bodies);
            _shader.SetBuffer(kernel, "_Lifecycle", _lifecycle);
            _shader.SetBuffer(kernel, "_MediumState", _mediumState);
            _shader.SetBuffer(kernel, "_SleepState", _sleepState);
            _shader.SetBuffer(kernel, "_SleepCommands", _sleepCommands);
            _shader.SetBuffer(kernel, "_Bounds", _bounds);
            _shader.SetBuffer(kernel, "_MotionTargets", _targets);
            ParticleExtensionContext.Dispatch(_shader, kernel, _count);
        }

        void SyncFlags()
        {
            IReadOnlyList<ParticleEntity> entities = _emitter.Entities;
            for (int i = 0; i < _count && i < entities.Count; i++)
            {
                ParticleEntity entity = entities[i];
                uint flags = 0u;
                if (entity.IsValid && !entity.IsParked)
                    flags = entity.IsSleeping ? 2u : 1u;
                if (_bodyData[i].flags == flags)
                    continue;
                _bodyData[i].flags = flags;
                _bodyDirty[i] = true;
                _stateDirty[i] = true;
                _epoch[i]++;
                Version++;
            }
        }

        void UploadDirty()
        {
            UploadRuns(_bodyDirty, UploadBodies);
            UploadRuns(_stateDirty, (start, length) =>
            {
                for (int i = start; i < start + length; i++)
                    _stateScratch[i] = new Vector2(_bodyData[i].flags == 2u ? SleepSleeping : SleepAwake, 0f);
                _sleepState.SetData(_stateScratch, start, start, length);
                _sleepCommands.SetData(new uint[length], 0, start, length);
            });
            UploadRuns(_lifecycleDirty, (start, length) => _lifecycle.SetData(new Vector4[length], 0, start, length));
        }

        void UploadBodies(int start, int length) => _bodies.SetData(_bodyData, start, start, length);

        static void UploadRuns(bool[] dirty, Action<int, int> upload)
        {
            int i = 0;
            while (i < dirty.Length)
            {
                if (!dirty[i])
                {
                    i++;
                    continue;
                }
                int start = i;
                while (i < dirty.Length && dirty[i])
                {
                    dirty[i] = false;
                    i++;
                }
                upload(start, i - start);
            }
        }

        /// <summary>
        ///   Gathers the bounds volumes that apply to this emitter. A spawn box whose centre is outside
        ///   every Contain volume ignores all Contain volumes, and one inside an Exclude volume ignores
        ///   that volume, each with one warning, because recycling would put a body straight back where
        ///   it is recycled again (ParticleExtensions EXT-14).
        /// </summary>
        void UploadBounds()
        {
            _boundsData.Clear();
            IReadOnlyList<ParticleVolume> volumes = _emitter.BoundWorld != null ? _emitter.BoundWorld.Volumes : null;
            Vector3 spawnCenter = _emitter.transform.position;
            if (volumes != null)
            {
                bool ignoreContain = SpawnOutsideEveryContain(volumes, spawnCenter);
                foreach (ParticleVolume volume in volumes)
                {
                    if (volume == null)
                        continue;
                    foreach (ParticleVolumeEffect effect in volume.Effects)
                    {
                        if (!(effect is ParticleBoundsEffect bounds) || !bounds.Enabled)
                            continue;
                        bool contain = bounds.Mode == ParticleBoundsMode.Contain;
                        if (contain && ignoreContain)
                            continue;
                        if (!contain && volume.ContainsPoint(spawnCenter))
                        {
                            ReportSpawnInsideExclude(volume, spawnCenter);
                            continue;
                        }
                        Quaternion rotation = volume.Rotation;
                        Vector3 center = volume.Center;
                        Vector3 extents = volume.HalfExtents;
                        Vector3 x = rotation * Vector3.right;
                        Vector3 y = rotation * Vector3.up;
                        Vector3 z = rotation * Vector3.forward;
                        _boundsData.Add(new GpuBounds
                        {
                            center = new Vector4(center.x, center.y, center.z, contain ? 1f : 0f),
                            halfExtents = new Vector4(extents.x, extents.y, extents.z, Mathf.Max(0f, bounds.FadeOut)),
                            axisX = new Vector4(x.x, x.y, x.z, Mathf.Max(0f, bounds.FadeIn)),
                            axisY = new Vector4(y.x, y.y, y.z, 0f),
                            axisZ = new Vector4(z.x, z.y, z.z, 0f)
                        });
                    }
                }
            }
            _boundsCount = _boundsData.Count;
            Grow(ref _bounds, _boundsCount, Marshal.SizeOf<GpuBounds>());
            if (_boundsCount > 0)
                _bounds.SetData(_boundsData);
        }

        bool SpawnOutsideEveryContain(IReadOnlyList<ParticleVolume> volumes, Vector3 spawnCenter)
        {
            int contains = 0;
            foreach (ParticleVolume volume in volumes)
            {
                if (volume == null)
                    continue;
                foreach (ParticleVolumeEffect effect in volume.Effects)
                {
                    if (!(effect is ParticleBoundsEffect bounds) || !bounds.Enabled || bounds.Mode != ParticleBoundsMode.Contain)
                        continue;
                    contains++;
                    if (volume.ContainsPoint(spawnCenter))
                        return false;
                }
            }
            if (contains == 0)
                return false;
            if (!_reportedSpawnOutsideContain)
            {
                _reportedSpawnOutsideContain = true;
                Debug.LogWarning(
                    $"ParticleEmitter '{_emitter.name}': its spawn box centre {spawnCenter} is outside all {contains} Contain bounds "
                    + "volume(s), so Contain is not applied to this emitter's bodies. Recycling them would place them back outside "
                    + "and they would fade and respawn for ever. Move the emitter inside a Contain volume, or the volume over the emitter.",
                    _emitter);
            }
            return true;
        }

        void ReportSpawnInsideExclude(ParticleVolume volume, Vector3 spawnCenter)
        {
            if (_reportedSpawnInsideExclude)
                return;
            _reportedSpawnInsideExclude = true;
            Debug.LogWarning(
                $"ParticleEmitter '{_emitter.name}': its spawn box centre {spawnCenter} is inside the Exclude bounds volume "
                + $"'{volume.name}', so that volume is not applied to this emitter's bodies. Recycling them would place them back "
                + "inside it and they would fade and respawn for ever. Move the emitter or the volume.",
                _emitter);
        }

        void UploadTargets()
        {
            _targetData.Clear();
            IReadOnlyList<ParticleMotionTarget> targets = _emitter.BoundWorld != null ? _emitter.BoundWorld.MotionTargets : null;
            if (targets != null)
                foreach (ParticleMotionTarget target in targets)
                    if (target != null)
                        _targetData.Add(new GpuTarget
                        {
                            position = target.Position,
                            mode = (float)target.Mode,
                            direction = target.Direction,
                            radius = Mathf.Max(0f, target.Radius)
                        });
            _targetCount = _targetData.Count;
            Grow(ref _targets, _targetCount, Marshal.SizeOf<GpuTarget>());
            if (_targetCount > 0)
                _targets.SetData(_targetData);
        }

        static void Grow(ref ComputeBuffer buffer, int count, int stride)
        {
            if (buffer.count >= Mathf.Max(1, count))
                return;
            buffer.Release();
            buffer = new ComputeBuffer(Mathf.NextPowerOfTwo(count), stride);
        }

        void RequestCommands()
        {
            if (_commandsRequested || _count == 0)
                return;
            _commandsRequested = true;
            int count = _count;
            var epochs = new int[count];
            Array.Copy(_epoch, epochs, count);
            AsyncGPUReadback.Request(_sleepCommands, count * sizeof(uint), 0, request =>
            {
                _commandsRequested = false;
                if (_disposed || request.hasError)
                    return;
                ApplyCommands(request.GetData<uint>(), epochs);
            });
        }

        /// <summary>
        ///   Turns the GPU's sleep commands into entity sleep changes, skipping bodies whose state was
        ///   reset since the commands were requested.
        /// </summary>
        internal void ApplyCommands(NativeArray<uint> commands, int[] epochs)
        {
            IReadOnlyList<ParticleEntity> entities = _emitter.Entities;
            int count = Mathf.Min(Mathf.Min(commands.Length, epochs.Length), Mathf.Min(_count, entities.Count));
            for (int i = 0; i < count; i++)
            {
                if (_epoch[i] != epochs[i])
                    continue;
                ParticleEntity entity = entities[i];
                if (!entity.IsValid || entity.IsParked)
                    continue;
                if (commands[i] == CommandSleep && !entity.IsSleeping)
                    entity.SetSleeping(true);
                else if (commands[i] == CommandWake && entity.IsSleeping)
                    entity.SetSleeping(false);
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _bodies.Release();
            _lifecycle.Release();
            _mediumState.Release();
            _sleepState.Release();
            _sleepCommands.Release();
            _bounds.Release();
            _targets.Release();
            if (Application.isPlaying)
                Object.Destroy(_shader);
            else
                Object.DestroyImmediate(_shader);
        }
    }
}

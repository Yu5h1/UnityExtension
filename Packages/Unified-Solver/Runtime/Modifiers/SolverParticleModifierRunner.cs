using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Yu5h1.UnifiedSolver
{
    [DefaultExecutionOrder(50)]
    // No RequireComponent back to the emitter.
    //
    // The emitter owns this component's whole lifecycle, and a
    // requirement pointing back at it would block removing the emitter
    // with a dialog naming a component the user cannot see, because this
    // one is hidden. Standalone use is handled by resolving the emitter
    // defensively instead.
    public sealed class SolverParticleModifierRunner :
        MonoBehaviour
    {
        const string ResourceName =
            "SolverParticleModifiers";
        const int ThreadsPerGroup = 64;

        [FormerlySerializedAs("computeShader")]
        // Must declare the same kernels and uniforms as the packaged one.
        [Tooltip("Leave empty to use the packaged compute.")]
        public ComputeShader overrideCompute;

        // Room for the largest topology, so any instance's pose fits one slot.
        const int SleepPoseStride = 12;

        SolverParticleEmitter _emitter;
        ComputeShader _runtimeCompute;
        int _oscillationKernel = -1;
        int _surfaceImpulseKernel = -1;
        int _rollDampingKernel = -1;
        int _sleepKernel = -1;
        int _speedLimitKernel = -1;
        int _mediumKernel = -1;
        int _locomotionKernel = -1;
        ComputeBuffer _volumeBuffer;
        SolverVolumeGPU[] _volumeData;
        int _volumeCount;
        int _mediumEffectCount;
        int _boundsEffectCount;
        int _boundsKernel = -1;
        ComputeBuffer _mediumStateBuffer;
        ComputeBuffer _targetBuffer;
        SolverMotionTargetGPU[] _targetData;
        int _targetCount;
        bool _reportedMissingMedium;
        ComputeBuffer _sleepState;
        ComputeBuffer _sleepPose;
        bool _reportedMissingCompute;
        bool _reportedModifiers;
        bool _reportedNeutralDensity;

        void Awake()
        {
            _emitter =
                GetComponent<SolverParticleEmitter>();
        }

        void FixedUpdate()
        {
            if (_emitter == null ||
                _emitter.profile == null ||
                _emitter.InstanceCount == 0 ||
                _emitter.InstanceBuffer == null ||
                _emitter.Solver == null ||
                _emitter.Solver.ParticleBuffer == null ||
                !EnsureCompute())
            {
                return;
            }

            SetSharedParameters();

            // First, so it acts on what the solver just produced rather than on
            // anything a modifier adds afterwards. A launch comes from a
            // depenetration or a moving collider, not from the drive.
            DispatchSpeedLimit();

            // Once per step, ahead of every kernel that reads a volume. The
            // buffer is shared: an effect added later consumes the same upload
            // rather than gathering the scene again.
            UploadVolumes();

            // Environment before performance: a body should be floating or
            // drifting before it decides how hard to swim against it.
            DispatchMedium();

            // Structural, not a modifier: an instance with no modifier attached
            // still must not corkscrew when something pushes it.
            DispatchRollDamping();

            SolverParticleModifierProfile[] modifiers =
                _emitter.profile.modifiers;
            if (modifiers == null ||
                modifiers.Length == 0)
            {
                DispatchBounds();
                DispatchSleep(false);
                ReportModifiersOnce(modifiers, 0);
                return;
            }

            int dispatched = 0;
            bool keepAwake = false;
            for (int i = 0; i < modifiers.Length; i++)
            {
                SolverParticleModifierProfile modifier =
                    modifiers[i];
                if (modifier == null ||
                    !modifier.enabled)
                {
                    continue;
                }

                if (modifier is SolverOscillationProfile
                    oscillation)
                {
                    DispatchOscillation(oscillation);
                    dispatched++;
                    keepAwake = true;
                }
                else if (modifier is
                    SolverSurfaceImpulseProfile
                    surface)
                {
                    DispatchSurfaceImpulse(surface);
                    dispatched++;
                    keepAwake = true;
                }
                else if (modifier is
                    SolverLocomotionProfile
                    locomotion)
                {
                    DispatchLocomotion(locomotion);
                    dispatched++;
                    keepAwake = true;
                }
            }

            // After the modifiers, so the teleport is the last positional word
            // of the step and nothing drags the body back out of bounds.
            DispatchBounds();

            // After the modifiers, so a body that was driven this step is
            // measured as moving rather than being caught mid-drive.
            DispatchSleep(keepAwake);

            ReportModifiersOnce(modifiers, dispatched);
        }

        // A modifier that is authored but never referenced by the profile is
        // indistinguishable in play from one that runs and does nothing, so say
        // outright what was found. Logged once, on the first step that runs.
        void ReportModifiersOnce(
            SolverParticleModifierProfile[] modifiers,
            int dispatched)
        {
            if (_reportedModifiers)
                return;

            _reportedModifiers = true;
            var names = new System.Text.StringBuilder();
            int count = modifiers == null
                ? 0
                : modifiers.Length;
            for (int i = 0; i < count; i++)
            {
                names.Append(i == 0 ? "" : ", ");
                if (modifiers[i] == null)
                {
                    names.Append("<null>");
                    continue;
                }

                names.Append(
                    modifiers[i].GetType().Name);
                if (!modifiers[i].enabled)
                    names.Append(" (disabled)");
            }
            if (count == 0)
                names.Append("EMPTY");

            Debug.Log(
                $"SolverParticleModifierRunner on " +
                $"'{name}': profile " +
                $"'{_emitter.profile.name}' lists " +
                $"{count} modifier(s) " +
                $"[{names}], {dispatched} dispatched.",
                this);
        }

        void SetSharedParameters()
        {
            _runtimeCompute.SetInt(
                "_InstanceCount",
                _emitter.InstanceCount);
            _runtimeCompute.SetFloat(
                "_Time",
                Time.fixedTime);
            _runtimeCompute.SetFloat(
                "_DeltaTime",
                Time.fixedDeltaTime);

            // The solver converts a positional correction into velocity by
            // dividing by the substep, so any budget expressed as a speed has
            // to be converted with the substep, not the frame.
            _runtimeCompute.SetFloat(
                "_SubDeltaTime",
                Time.fixedDeltaTime /
                Mathf.Max(1, _emitter.Solver.substeps));

            // Shared rather than owned by the oscillation modifier: the
            // structural unfold budgets its downward reach against this axis and
            // runs on instances that carry no modifier at all.
            SolverManager solver = _emitter.Solver;
            _runtimeCompute.SetVector(
                "_UpAxis",
                solver.gravity.sqrMagnitude > 1e-8f
                    ? -solver.gravity.normalized
                    : Vector3.up);
        }

        // Dispatched unconditionally. Roll damping still gates itself inside the
        // kernel on its own profile value, but the hairpin unfold ahead of it is
        // structural: a body folded onto itself has no axis for the frame to be
        // built from, and that is true whatever the profile asks for.
        void DispatchRollDamping()
        {
            SolverParticleProfile profile =
                _emitter.profile;
            float rollDamping =
                Mathf.Clamp01(profile.rollDamping);
            _runtimeCompute.SetFloat(
                "_RollDamping",
                rollDamping);
            BindBuffers(_rollDampingKernel);
            Dispatch(_rollDampingKernel);
        }

        // Gathers every registered volume, flattened one entry per effect.
        //
        // The list is global rather than per emitter, because a volume belongs
        // to the scene rather than to whoever is swimming in it. Re-uploaded
        // every step so a volume can be moved, resized or retuned at runtime.
        //
        // Nothing here knows what any effect does. The volume writes the
        // geometry and the effect writes its own payload, so a new kind of
        // volume effect adds a subclass and a kernel branch and does not touch
        // this method at all.
        void UploadVolumes()
        {
            IReadOnlyList<SolverVolume> volumes =
                SolverVolume.Registered;

            int required = 0;
            for (int i = 0; i < volumes.Count; i++)
            {
                SolverVolume volume = volumes[i];
                if (volume != null)
                    required += volume.EffectCount;
            }

            if (_volumeData == null ||
                _volumeData.Length < required)
            {
                _volumeData = new SolverVolumeGPU[
                    Mathf.Max(4, required)];
            }

            _volumeCount = 0;
            _mediumEffectCount = 0;
            _boundsEffectCount = 0;
            for (int i = 0; i < volumes.Count; i++)
            {
                SolverVolume volume = volumes[i];
                if (volume == null || !volume.IsUsable)
                    continue;

                int effects = volume.EffectCount;
                for (int e = 0; e < effects; e++)
                {
                    SolverVolumeEffectProfile effect =
                        volume.GetEffect(e);
                    if (effect == null || !effect.enabled)
                        continue;

                    var entry = new SolverVolumeGPU
                    {
                        center = volume.Center,
                        shape = (float)volume.shape,
                        halfExtents = volume.HalfExtents,
                        effectType =
                            (float)effect.EffectType,
                        axisX = volume.AxisX,
                        invert =
                            effect.actOutside ? 1f : 0f,
                        axisY = volume.AxisY,
                        axisZ = volume.AxisZ
                    };
                    effect.Write(volume, ref entry);
                    _volumeData[_volumeCount++] = entry;

                    if (effect.EffectType ==
                        SolverVolumeEffectType.Medium)
                    {
                        _mediumEffectCount++;
                    }
                    else if (effect.EffectType ==
                        SolverVolumeEffectType.Bounds)
                    {
                        _boundsEffectCount++;
                    }
                }
            }

            // Allocated even with nothing to upload, because the kernels read it
            // unconditionally and Unity requires every buffer a kernel declares
            // to be bound.
            if (_volumeBuffer == null ||
                _volumeBuffer.count <
                    Mathf.Max(1, _volumeCount))
            {
                _volumeBuffer?.Release();
                _volumeBuffer = new ComputeBuffer(
                    Mathf.Max(4, _volumeCount),
                    SolverVolumeGPU.Stride);
            }
            if (_volumeCount > 0)
            {
                _volumeBuffer.SetData(
                    _volumeData, 0, 0, _volumeCount);
            }
        }

        // Applies whatever the uploaded volumes declare, and writes the medium
        // state every instance's environment is read from.
        //
        // Dispatched even with no volumes at all, because this is what clears
        // that state. Skipping it would leave last step's value in place, and a
        // body that had left the water would still read as being in it.
        void DispatchMedium()
        {
            if (_mediumKernel < 0)
                return;
            if (!EnsureMediumStateBuffer())
                return;

            SolverManager solver = _emitter.Solver;
            float radius = Mathf.Max(
                1e-6f,
                solver.particleRadius);
            _runtimeCompute.SetBuffer(
                _mediumKernel,
                "_Volumes",
                _volumeBuffer);
            _runtimeCompute.SetBuffer(
                _mediumKernel,
                "_MediumState",
                _mediumStateBuffer);
            _runtimeCompute.SetInt(
                "_VolumeCount",
                _volumeCount);
            _runtimeCompute.SetVector(
                "_Gravity",
                solver.gravity);
            _runtimeCompute.SetFloat(
                "_ParticleVolume",
                4f / 3f * Mathf.PI *
                radius * radius * radius);
            if (_mediumEffectCount > 0)
                ReportNeutralDensityOnce(radius);
            BindBuffers(_mediumKernel);
            Dispatch(_mediumKernel);
        }

        // Recycles bodies that have left where they belong, and drives the fade
        // that covers it.
        //
        // Dispatched even with no boundary in the scene, and the kernel bails
        // after one buffer read when it has nothing to do. Gating the dispatch
        // instead would strand any body that happened to be mid-fade when the
        // last boundary was removed: invisible, forever, with nothing left
        // running to finish it.
        //
        // The spawn box is handed over as a centre and three pre-scaled axes, so
        // the kernel places a rebirth exactly where the emitter would have
        // without knowing anything about Transforms.
        void DispatchBounds()
        {
            if (_boundsKernel < 0)
                return;
            if (_emitter.LifecycleBuffer == null)
                return;

            Transform emitterTransform =
                _emitter.transform;
            Vector3 half = _emitter.spawnVolume * 0.5f;
            _runtimeCompute.SetBuffer(
                _boundsKernel,
                "_Volumes",
                _volumeBuffer);
            _runtimeCompute.SetBuffer(
                _boundsKernel,
                "_Lifecycle",
                _emitter.LifecycleBuffer);
            _runtimeCompute.SetInt(
                "_VolumeCount",
                _volumeCount);
            _runtimeCompute.SetInt(
                "_BoundsCount",
                _boundsEffectCount);
            _runtimeCompute.SetVector(
                "_SpawnCenter",
                emitterTransform.position);
            _runtimeCompute.SetVector(
                "_SpawnAxisX",
                emitterTransform.right * half.x);
            _runtimeCompute.SetVector(
                "_SpawnAxisY",
                emitterTransform.up * half.y);
            _runtimeCompute.SetVector(
                "_SpawnAxisZ",
                emitterTransform.forward * half.z);
            BindBuffers(_boundsKernel);
            Dispatch(_boundsKernel);
        }

        // Says what Density has to be set to for this profile to float.
        //
        // Buoyancy is a ratio of real densities, and the solver's masses are not
        // kilograms, so the value that means neutral is whatever the profile
        // mass and the global particle radius happen to imply -- typically in
        // the hundreds. Left undiscoverable, Density reads as a dead control:
        // every value a person would try first is a fraction of a percent of
        // gravity. Logged once, and only when a medium actually exists.
        void ReportNeutralDensityOnce(float radius)
        {
            if (_reportedNeutralDensity)
                return;

            _reportedNeutralDensity = true;
            SolverParticleProfile profile =
                _emitter.profile;
            int particles = Mathf.Max(
                1,
                profile.WorstCaseRequirements.particles);
            float particleVolume =
                4f / 3f * Mathf.PI *
                radius * radius * radius;
            float neutral =
                profile.mass /
                (particles * particleVolume);

            Debug.Log(
                $"A medium volume affects '{profile.name}': " +
                $"Density {neutral:0.#} is neutral buoyancy " +
                $"({profile.mass} mass over {particles} particles " +
                $"at radius {radius}). Below sinks, above lifts.",
                this);
        }

        // One slot per instance, so a slot always means one instance index.
        //
        // Explicitly zeroed: undefined contents would read as bodies already
        // submerged, adrift in a current that does not exist, and unable to
        // sleep because of it.
        bool EnsureMediumStateBuffer()
        {
            int instances = Mathf.Max(
                1,
                _emitter.maxInstances);
            if (_mediumStateBuffer != null &&
                _mediumStateBuffer.count >= instances)
            {
                return true;
            }

            _mediumStateBuffer?.Release();
            _mediumStateBuffer = new ComputeBuffer(
                instances,
                sizeof(float) * 4);
            _mediumStateBuffer.SetData(
                new Vector4[instances]);
            return true;
        }

        // Pushes bodies toward the nearest motion target that reaches them.
        //
        // Reads the submersion ApplyMedium wrote, so it can only act where there
        // is something to push against. Reported once when no medium exists at
        // all, because the modifier is then configured, dispatched and unable to
        // do anything, which looks identical to it being broken.
        void DispatchLocomotion(
            SolverLocomotionProfile profile)
        {
            if (_locomotionKernel < 0)
                return;
            if (!EnsureMediumStateBuffer())
                return;
            EnsureTargetBuffer();

            // Counts medium effects, not volumes. A scene can now hold volumes
            // that carry no medium at all, and a body has nothing to push
            // against in one of those, so the volume count would report a
            // reason to swim that does not exist.
            if (_mediumEffectCount == 0 &&
                !_reportedMissingMedium)
            {
                _reportedMissingMedium = true;
                Debug.LogWarning(
                    $"SolverLocomotionProfile '{profile.name}' " +
                    "cannot move anything: locomotion pushes " +
                    "against a medium, and no SolverVolume in the " +
                    "scene carries an enabled SolverMediumProfile.",
                    this);
            }

            _runtimeCompute.SetBuffer(
                _locomotionKernel,
                "_MediumState",
                _mediumStateBuffer);
            _runtimeCompute.SetBuffer(
                _locomotionKernel,
                "_MotionTargets",
                _targetBuffer);
            _runtimeCompute.SetInt(
                "_MotionTargetCount",
                _targetCount);
            _runtimeCompute.SetFloat(
                "_LocomotionSpeed",
                Mathf.Max(0f, profile.speed));
            _runtimeCompute.SetFloat(
                "_LocomotionFrequency",
                Mathf.Max(0f, profile.frequency));
            _runtimeCompute.SetFloat(
                "_LocomotionDuration",
                Mathf.Max(0.01f, profile.duration));
            _runtimeCompute.SetFloat(
                "_LocomotionRandomness",
                Mathf.Clamp01(profile.randomness));
            _runtimeCompute.SetFloat(
                "_LocomotionTurnRate",
                Mathf.Max(0f, profile.turnRate) *
                Mathf.Deg2Rad);
            _runtimeCompute.SetFloat(
                "_LocomotionUprightRate",
                Mathf.Max(0f, profile.uprightRate) *
                Mathf.Deg2Rad);
            _runtimeCompute.SetFloat(
                "_LocomotionHeadingSpread",
                Mathf.Clamp(profile.headingSpread, 0f, 89f) *
                Mathf.Deg2Rad);
            BindBuffers(_locomotionKernel);
            Dispatch(_locomotionKernel);
        }

        void EnsureTargetBuffer()
        {
            IReadOnlyList<SolverMotionTarget> targets =
                SolverMotionTarget.Registered;
            if (_targetData == null ||
                _targetData.Length < targets.Count)
            {
                _targetData = new SolverMotionTargetGPU[
                    Mathf.Max(4, targets.Count)];
            }

            _targetCount = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                SolverMotionTarget target = targets[i];
                if (target == null)
                    continue;

                _targetData[_targetCount++] =
                    new SolverMotionTargetGPU
                    {
                        position = target.Position,
                        mode = (float)target.mode,
                        direction = target.Direction,
                        radius = Mathf.Max(0f, target.radius)
                    };
            }

            if (_targetBuffer == null ||
                _targetBuffer.count <
                    Mathf.Max(1, _targetCount))
            {
                _targetBuffer?.Release();
                _targetBuffer = new ComputeBuffer(
                    Mathf.Max(4, _targetCount),
                    SolverMotionTargetGPU.Stride);
            }
            if (_targetCount > 0)
            {
                _targetBuffer.SetData(
                    _targetData, 0, 0, _targetCount);
            }
        }

        // Sheds travel speed above a threshold instead of clamping it, so the
        // ordering between a hard hit and a light one survives.
        void DispatchSpeedLimit()
        {
            SolverParticleProfile profile =
                _emitter.profile;
            float speedLimit =
                Mathf.Max(0f, profile.speedLimit);
            if (speedLimit <= 0f)
                return;
            if (_speedLimitKernel < 0)
                return;

            _runtimeCompute.SetFloat(
                "_SpeedLimit",
                speedLimit);
            _runtimeCompute.SetFloat(
                "_SpeedDecayRate",
                Mathf.Max(0.01f, profile.speedDecayRate));
            BindBuffers(_speedLimitKernel);
            Dispatch(_speedLimitKernel);
        }

        // Holds settled instances still by writing positions.
        //
        // Dispatched whether or not the profile lists modifiers, because a body
        // that will not stop moving is a fault of the body, not of its
        // performance. Gated only on sleepSpeed, which is what turns it off.
        void DispatchSleep(bool keepAwake)
        {
            SolverParticleProfile profile =
                _emitter.profile;
            float sleepSpeed =
                Mathf.Max(0f, profile.sleepSpeed);
            if (sleepSpeed <= 0f)
                return;
            if (!EnsureSleepBuffers())
                return;
            if (_emitter.LifecycleBuffer == null)
                return;
            // Bound whether or not a medium exists. The kernel reads it either
            // way, and a zeroed buffer is what "no medium is pushing on this"
            // looks like.
            if (!EnsureMediumStateBuffer())
                return;

            _runtimeCompute.SetFloat(
                "_SleepSpeed",
                sleepSpeed);
            _runtimeCompute.SetFloat(
                "_SleepDelay",
                Mathf.Max(0f, profile.sleepDelay));
            _runtimeCompute.SetFloat(
                "_WakeDistance",
                Mathf.Max(0.0001f, profile.wakeDistance));
            _runtimeCompute.SetFloat(
                "_KeepAwake",
                keepAwake ? 1f : 0f);
            _runtimeCompute.SetInt(
                "_SleepPoseStride",
                SleepPoseStride);
            _runtimeCompute.SetBuffer(
                _sleepKernel,
                "_SleepState",
                _sleepState);
            _runtimeCompute.SetBuffer(
                _sleepKernel,
                "_SleepPose",
                _sleepPose);
            _runtimeCompute.SetBuffer(
                _sleepKernel,
                "_MediumState",
                _mediumStateBuffer);
            // Read, not written, here: a body mid-recycle must not be held at a
            // pose recorded before it was moved.
            _runtimeCompute.SetBuffer(
                _sleepKernel,
                "_Lifecycle",
                _emitter.LifecycleBuffer);
            BindBuffers(_sleepKernel);
            Dispatch(_sleepKernel);
        }

        // Allocated against maxInstances rather than the current count, so the
        // state of an instance never moves slot as more are spawned. A slot's
        // meaning is its instance index and nothing else.
        bool EnsureSleepBuffers()
        {
            if (_sleepKernel < 0)
                return false;

            int instances = Mathf.Max(
                1,
                _emitter.maxInstances);
            if (_sleepState != null &&
                _sleepState.count >= instances)
            {
                return true;
            }

            _sleepState?.Release();
            _sleepPose?.Release();

            // Zeroed on creation, which is state 0 with a zero timer: awake.
            // ComputeBuffer contents are otherwise undefined, and undefined here
            // would read as instances that are already asleep at a pose made of
            // whatever was in memory.
            _sleepState = new ComputeBuffer(
                instances,
                sizeof(float) * 2);
            _sleepState.SetData(
                new Vector2[instances]);

            _sleepPose = new ComputeBuffer(
                instances * SleepPoseStride,
                sizeof(float) * 3);
            _sleepPose.SetData(
                new Vector3[
                    instances * SleepPoseStride]);
            return true;
        }

        void DispatchOscillation(
            SolverOscillationProfile profile)
        {
            _runtimeCompute.SetFloat(
                "_OscillationAcceleration",
                profile.acceleration);
            _runtimeCompute.SetFloat(
                "_OscillationFrequency",
                profile.frequency);
            _runtimeCompute.SetFloat(
                "_OscillationDuration",
                Mathf.Max(0f, profile.duration));
            _runtimeCompute.SetFloat(
                "_OscillationDurationRandomness",
                Mathf.Clamp01(profile.durationRandomness));
            _runtimeCompute.SetFloat(
                "_OscillationRandomness",
                profile.frequencyRandomness);
            _runtimeCompute.SetFloat(
                "_OscillationDirectionAngle",
                profile.directionAngle);
            _runtimeCompute.SetFloat(
                "_OscillationDirectionRandomness",
                profile.directionRandomness);
            _runtimeCompute.SetFloat(
                "_OscillationStiffness",
                Mathf.Clamp01(profile.stiffness));
            _runtimeCompute.SetFloat(
                "_OscillationMuscleTension",
                Mathf.Clamp01(profile.muscleTension));
            _runtimeCompute.SetFloat(
                "_OscillationTensionRandomness",
                profile.tensionRandomness);
            _runtimeCompute.SetFloat(
                "_OscillationTailBias",
                Mathf.Clamp01(profile.tailBias));

            // Vitality is the launch speed ceiling, enforced directly on the
            // body's velocity, so no substep conversion is needed: the kernel
            // reads the speed the solver already produced.
            _runtimeCompute.SetFloat(
                "_OscillationVitality",
                Mathf.Clamp01(profile.vitality));
            BindBuffers(_oscillationKernel);
            Dispatch(_oscillationKernel);
        }

        void DispatchSurfaceImpulse(
            SolverSurfaceImpulseProfile profile)
        {
            float spreadSeconds =
                SurfaceSpreadSeconds(profile);
            _runtimeCompute.SetFloat(
                "_SurfaceSpreadSeconds",
                spreadSeconds);
            _runtimeCompute.SetFloat(
                "_SurfaceImpulseSpeed",
                profile.impulseSpeed);
            _runtimeCompute.SetFloat(
                "_SurfaceContactSpeed",
                profile.fallSpeedLimit);
            _runtimeCompute.SetFloat(
                "_SurfaceFrequency",
                profile.frequency);
            _runtimeCompute.SetFloat(
                "_SurfaceRandomness",
                profile.frequencyRandomness);
            _runtimeCompute.SetFloat(
                "_SurfaceDebugTint",
                profile.debugTintOnHop ? 1f : 0f);
            BindBuffers(_surfaceImpulseKernel);
            Dispatch(_surfaceImpulseKernel);
        }

        // Impact Spread is authored in fixed steps because that is the unit the
        // lift is delivered in: the body rises impulseSpeed * fixedDeltaTime per
        // step for this many steps, so total lift is impulseSpeed * spread *
        // fixedDeltaTime before ballistic flight adds to it.
        //
        // Capped below the cycle period so a hop always finishes before the
        // next one starts. Without that, raising Impact Spread past the period
        // would leave the window permanently open and turn the hop back into
        // the continuous push this design replaced.
        static float SurfaceSpreadSeconds(
            SolverSurfaceImpulseProfile profile)
        {
            float step = Mathf.Max(
                1e-5f,
                Time.fixedDeltaTime);
            float spread =
                Mathf.Max(1f, profile.impactSpread) *
                step;
            float period =
                1f /
                Mathf.Max(1e-4f, profile.frequency);
            return Mathf.Max(
                step,
                Mathf.Min(spread, period * 0.9f));
        }

        void BindBuffers(int kernel)
        {
            _runtimeCompute.SetBuffer(
                kernel,
                "_Particles",
                _emitter.Solver.ParticleBuffer);
            _runtimeCompute.SetBuffer(
                kernel,
                "_Instances",
                _emitter.InstanceBuffer);
        }

        void Dispatch(int kernel)
        {
            _runtimeCompute.Dispatch(
                kernel,
                Mathf.CeilToInt(
                    _emitter.InstanceCount /
                    (float)ThreadsPerGroup),
                1,
                1);
        }

        bool EnsureCompute()
        {
            if (_runtimeCompute != null)
                return true;

            ComputeShader source = overrideCompute;
            if (source == null)
            {
                source =
                    Resources.Load<ComputeShader>(
                        ResourceName);
            }
            if (source == null)
            {
                if (!_reportedMissingCompute)
                {
                    Debug.LogError(
                        "SolverParticleModifierRunner: " +
                        "Modifier compute shader not found.",
                        this);
                    _reportedMissingCompute = true;
                }
                return false;
            }

            _runtimeCompute =
                Instantiate(source);
            _oscillationKernel =
                _runtimeCompute.FindKernel(
                    "ApplyOscillation");
            _surfaceImpulseKernel =
                _runtimeCompute.FindKernel(
                    "ApplySurfaceImpulse");
            _rollDampingKernel =
                _runtimeCompute.FindKernel(
                    "ApplyRollDamping");
            _sleepKernel =
                _runtimeCompute.FindKernel(
                    "ApplySleep");
            _speedLimitKernel =
                _runtimeCompute.FindKernel(
                    "ApplySpeedLimit");
            _mediumKernel =
                _runtimeCompute.FindKernel(
                    "ApplyMedium");
            _locomotionKernel =
                _runtimeCompute.FindKernel(
                    "ApplyLocomotion");
            _boundsKernel =
                _runtimeCompute.FindKernel(
                    "ApplyBounds");
            return true;
        }

        void OnDestroy()
        {
            if (_runtimeCompute != null)
                Destroy(_runtimeCompute);
            _runtimeCompute = null;
            _sleepState?.Release();
            _sleepState = null;
            _sleepPose?.Release();
            _sleepPose = null;
            _volumeBuffer?.Release();
            _volumeBuffer = null;
            _mediumStateBuffer?.Release();
            _mediumStateBuffer = null;
            _targetBuffer?.Release();
            _targetBuffer = null;
        }
    }
}

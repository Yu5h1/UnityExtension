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
        ComputeBuffer _mediumBuffer;
        SolverMediumGPU[] _mediumData;
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
            }

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

        // Uploads every registered medium and lets the kernel decide which
        // particles are inside which.
        //
        // The list is global rather than per emitter, because a medium belongs
        // to the scene rather than to whoever is swimming in it. Re-uploaded
        // every step so a volume can be moved, resized or retuned at runtime.
        void DispatchMedium()
        {
            if (_mediumKernel < 0)
                return;

            IReadOnlyList<SolverMediumVolume> volumes =
                SolverMediumVolume.Registered;
            int count = 0;
            if (_mediumData == null ||
                _mediumData.Length < volumes.Count)
            {
                _mediumData = new SolverMediumGPU[
                    Mathf.Max(4, volumes.Count)];
            }

            for (int i = 0; i < volumes.Count; i++)
            {
                SolverMediumVolume volume = volumes[i];
                if (volume == null ||
                    volume.profile == null ||
                    volume.Radius <= 0f)
                {
                    continue;
                }

                _mediumData[count++] = new SolverMediumGPU
                {
                    center = volume.Center,
                    radius = volume.Radius,
                    flow = volume.profile.flow,
                    density = Mathf.Max(
                        0f,
                        volume.profile.density),
                    viscosity = Mathf.Max(
                        0f,
                        volume.profile.viscosity)
                };
            }

            if (count == 0)
                return;

            if (_mediumBuffer == null ||
                _mediumBuffer.count < count)
            {
                _mediumBuffer?.Release();
                _mediumBuffer = new ComputeBuffer(
                    Mathf.Max(4, count),
                    SolverMediumGPU.Stride);
            }
            _mediumBuffer.SetData(_mediumData, 0, 0, count);

            SolverManager solver = _emitter.Solver;
            float radius = Mathf.Max(
                1e-6f,
                solver.particleRadius);
            _runtimeCompute.SetBuffer(
                _mediumKernel,
                "_Mediums",
                _mediumBuffer);
            _runtimeCompute.SetInt(
                "_MediumCount",
                count);
            _runtimeCompute.SetVector(
                "_Gravity",
                solver.gravity);
            _runtimeCompute.SetFloat(
                "_ParticleVolume",
                4f / 3f * Mathf.PI *
                radius * radius * radius);
            ReportNeutralDensityOnce(radius);
            BindBuffers(_mediumKernel);
            Dispatch(_mediumKernel);
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
                $"SolverMediumVolume affects '{profile.name}': " +
                $"Density {neutral:0.#} is neutral buoyancy " +
                $"({profile.mass} mass over {particles} particles " +
                $"at radius {radius}). Below floats nothing, " +
                $"above lifts.",
                this);
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
            _mediumBuffer?.Release();
            _mediumBuffer = null;
        }
    }
}

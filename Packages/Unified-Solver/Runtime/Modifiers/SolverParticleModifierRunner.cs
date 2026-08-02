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
        [Tooltip("Leave empty for normal use: the packaged Resources/SolverParticleModifiers.compute is loaded automatically. Assign one only to run a modified copy of that compute instead. It must declare the same kernels and uniforms.")]
        public ComputeShader overrideCompute;

        SolverParticleEmitter _emitter;
        ComputeShader _runtimeCompute;
        int _oscillationKernel = -1;
        int _surfaceImpulseKernel = -1;
        int _rollDampingKernel = -1;
        bool _reportedMissingCompute;
        bool _reportedModifiers;

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

            // Structural, not a modifier: an instance with no modifier attached
            // still must not corkscrew when something pushes it.
            DispatchRollDamping();

            SolverParticleModifierProfile[] modifiers =
                _emitter.profile.modifiers;
            if (modifiers == null ||
                modifiers.Length == 0)
            {
                ReportModifiersOnce(modifiers, 0);
                return;
            }

            int dispatched = 0;
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
                }
                else if (modifier is
                    SolverSurfaceImpulseProfile
                    surface)
                {
                    DispatchSurfaceImpulse(surface);
                    dispatched++;
                }
            }

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

        // Dispatched unconditionally. Roll damping and settle still gate
        // themselves inside the kernel on their own profile values, but the
        // hairpin unfold ahead of them is structural: a body folded onto itself
        // has no axis for the frame to be built from, and that is true whatever
        // the profile asks for.
        void DispatchRollDamping()
        {
            SolverParticleProfile profile =
                _emitter.profile;
            float rollDamping =
                Mathf.Clamp01(profile.rollDamping);
            float settleSpeed =
                Mathf.Max(0f, profile.settleSpeed);

            _runtimeCompute.SetFloat(
                "_RollDamping",
                rollDamping);
            _runtimeCompute.SetFloat(
                "_SettleSpeed",
                settleSpeed);
            BindBuffers(_rollDampingKernel);
            Dispatch(_rollDampingKernel);
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
            return true;
        }

        void OnDestroy()
        {
            if (_runtimeCompute != null)
                Destroy(_runtimeCompute);
            _runtimeCompute = null;
        }
    }
}

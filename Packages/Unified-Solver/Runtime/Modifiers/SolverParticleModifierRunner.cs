using UnityEngine;

namespace Yu5h1.UnifiedSolver
{
    [DefaultExecutionOrder(50)]
    [RequireComponent(typeof(SolverParticleEmitter))]
    public sealed class SolverParticleModifierRunner :
        MonoBehaviour
    {
        const string ResourceName =
            "SolverParticleModifiers";
        const int ThreadsPerGroup = 64;

        [Tooltip("Optional override. When empty, loads Resources/SolverParticleModifiers.compute.")]
        public ComputeShader computeShader;

        SolverParticleEmitter _emitter;
        ComputeShader _runtimeCompute;
        int _oscillationKernel = -1;
        int _surfaceImpulseKernel = -1;
        bool _reportedMissingCompute;

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

            SolverParticleModifierProfile[] modifiers =
                _emitter.profile.modifiers;
            if (modifiers == null ||
                modifiers.Length == 0)
            {
                return;
            }

            SetSharedParameters();
            for (int i = 0; i < modifiers.Length; i++)
            {
                SolverParticleModifierProfile modifier =
                    modifiers[i];
                if (modifier is SolverOscillationProfile
                    oscillation)
                {
                    DispatchOscillation(oscillation);
                }
                else if (modifier is
                    SolverSurfaceImpulseProfile
                    surface)
                {
                    DispatchSurfaceImpulse(surface);
                }
            }
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
                "_OscillationRandomness",
                profile.frequencyRandomness);
            _runtimeCompute.SetFloat(
                "_OscillationDirectionAngle",
                profile.directionAngle);
            _runtimeCompute.SetFloat(
                "_OscillationDirectionRandomness",
                profile.directionRandomness);
            _runtimeCompute.SetFloat(
                "_OscillationBendRatio",
                profile.bendRatio);
            _runtimeCompute.SetFloat(
                "_OscillationBendRandomness",
                profile.bendRandomness);
            BindBuffers(_oscillationKernel);
            Dispatch(_oscillationKernel);
        }

        void DispatchSurfaceImpulse(
            SolverSurfaceImpulseProfile profile)
        {
            _runtimeCompute.SetFloat(
                "_SurfaceAcceleration",
                profile.acceleration);
            _runtimeCompute.SetFloat(
                "_SurfaceY",
                profile.surfaceY);
            _runtimeCompute.SetFloat(
                "_SurfaceContactDistance",
                profile.contactDistance);
            _runtimeCompute.SetFloat(
                "_SurfaceFrequency",
                profile.frequency);
            _runtimeCompute.SetFloat(
                "_SurfaceRandomness",
                profile.frequencyRandomness);
            _runtimeCompute.SetFloat(
                "_SurfacePulseThreshold",
                profile.pulseThreshold);
            BindBuffers(_surfaceImpulseKernel);
            Dispatch(_surfaceImpulseKernel);
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

            ComputeShader source = computeShader;
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

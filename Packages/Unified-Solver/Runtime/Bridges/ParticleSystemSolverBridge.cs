using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1.UnifiedSolver
{
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class ParticleSystemSolverBridge :
        MonoBehaviour
    {
        public SolverParticleEmitter targetEmitter;

        [Header("Transfer")]
        public Vector3 worldPositionOffset;
        public float velocityScale = 1f;
        public bool inheritParticleAngularVelocity = true;
        public float angularVelocityScale = 1f;
        public Vector3 scaleMultiplier =
            Vector3.one;
        public Vector3 rotationOffsetEuler;
        public bool inheritParticleColor = true;
        public bool inheritParticleSize = true;

        readonly List<ParticleSystem.Particle>
            _enteredParticles =
                new List<ParticleSystem.Particle>(256);

        ParticleSystem _particleSystem;

        void Awake()
        {
            _particleSystem =
                GetComponent<ParticleSystem>();
        }

        void OnParticleTrigger()
        {
            if (_particleSystem == null ||
                targetEmitter == null)
            {
                return;
            }

            int count =
                _particleSystem.GetTriggerParticles(
                    ParticleSystemTriggerEventType.Enter,
                    _enteredParticles);
            if (count == 0)
                return;

            ParticleSystem.MainModule main =
                _particleSystem.main;
            Transform simulationTransform =
                ResolveSimulationTransform(main);
            Quaternion rotationOffset =
                Quaternion.Euler(
                    rotationOffsetEuler);
            bool changed = false;

            for (int i = 0; i < count; i++)
            {
                ParticleSystem.Particle particle =
                    _enteredParticles[i];
                Vector3 position =
                    ToWorldPosition(
                        particle.position,
                        main.simulationSpace,
                        simulationTransform) +
                    worldPositionOffset;
                Vector3 velocity =
                    ToWorldVector(
                        particle.velocity,
                        main.simulationSpace,
                        simulationTransform) *
                    velocityScale;
                Vector3 angularVelocity =
                    inheritParticleAngularVelocity
                        ? ToWorldVector(
                            particle.angularVelocity3D,
                            main.simulationSpace,
                            simulationTransform) *
                          angularVelocityScale
                        : Vector3.zero;
                Quaternion rotation =
                    ToWorldRotation(
                        Quaternion.Euler(
                            particle.rotation3D),
                        main.simulationSpace,
                        simulationTransform) *
                    rotationOffset;
                Vector3 scale = inheritParticleSize
                    ? Vector3.Scale(
                        particle.GetCurrentSize3D(
                            _particleSystem),
                        scaleMultiplier)
                    : scaleMultiplier;
                Color color = inheritParticleColor
                    ? particle.GetCurrentColor(
                        _particleSystem)
                    : targetEmitter.profile != null
                        ? targetEmitter.profile.baseColor
                        : Color.white;

                SolverParticleSpawnRequest request =
                    new SolverParticleSpawnRequest
                    {
                        position = position,
                        rotation = rotation,
                        velocity = velocity,
                        angularVelocity =
                            angularVelocity,
                        scale = scale,
                        color = color
                    };

                if (!targetEmitter.TryEnqueue(request))
                    continue;

                particle.remainingLifetime = 0f;
                _enteredParticles[i] = particle;
                changed = true;
            }

            if (changed)
            {
                _particleSystem.SetTriggerParticles(
                    ParticleSystemTriggerEventType.Enter,
                    _enteredParticles);
            }
        }

        Transform ResolveSimulationTransform(
            ParticleSystem.MainModule main)
        {
            if (main.simulationSpace ==
                ParticleSystemSimulationSpace.Custom)
            {
                return main.customSimulationSpace != null
                    ? main.customSimulationSpace
                    : _particleSystem.transform;
            }

            return _particleSystem.transform;
        }

        static Vector3 ToWorldPosition(
            Vector3 position,
            ParticleSystemSimulationSpace space,
            Transform simulationTransform)
        {
            return space ==
                   ParticleSystemSimulationSpace.World
                ? position
                : simulationTransform.TransformPoint(
                    position);
        }

        static Vector3 ToWorldVector(
            Vector3 vector,
            ParticleSystemSimulationSpace space,
            Transform simulationTransform)
        {
            return space ==
                   ParticleSystemSimulationSpace.World
                ? vector
                : simulationTransform.TransformVector(
                    vector);
        }

        static Quaternion ToWorldRotation(
            Quaternion rotation,
            ParticleSystemSimulationSpace space,
            Transform simulationTransform)
        {
            return space ==
                   ParticleSystemSimulationSpace.World
                ? rotation
                : simulationTransform.rotation *
                  rotation;
        }

        void OnValidate()
        {
            scaleMultiplier = new Vector3(
                Mathf.Max(
                    0.0001f,
                    Mathf.Abs(scaleMultiplier.x)),
                Mathf.Max(
                    0.0001f,
                    Mathf.Abs(scaleMultiplier.y)),
                Mathf.Max(
                    0.0001f,
                    Mathf.Abs(scaleMultiplier.z)));
        }
    }
}

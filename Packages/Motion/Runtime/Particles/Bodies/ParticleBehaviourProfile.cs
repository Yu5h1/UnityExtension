using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   A continuous behaviour an emitter's bodies run on themselves (ParticleExtensions EXT-12):
    ///   <see cref="ParticleOscillationProfile"/>, <see cref="ParticleSurfaceImpulseProfile"/> or
    ///   <see cref="ParticleLocomotionProfile"/>. While any enabled one is in the list, bodies never
    ///   sleep.
    /// </summary>
    public abstract class ParticleBehaviourModifier : ScriptableObject
    {
        [Tooltip("Off silences it without losing its slot or settings.")]
        [SerializeField]
        bool _enabled = true;

        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }
    }

    /// <summary>
    ///   What an emitter's bodies do besides falling and colliding (ParticleExtensions EXT-12): speed
    ///   limit, roll damping, sleep, and a list of behaviours. Names, units and defaults are
    ///   Unified-Solver's SolverParticleProfile.
    /// </summary>
    public sealed class ParticleBehaviourProfile : ScriptableObject
    {
        [Tooltip("m/s. 0 disables. A threshold, not a ceiling: the excess decays, so a body hit hard still ends up faster than one hit lightly.")]
        [Min(0f)]
        [SerializeField]
        float _speedLimit;
        [Tooltip("Higher approaches a hard clamp; lower leaves follow-through.")]
        [Min(0.01f)]
        [SerializeField]
        float _speedDecayRate = 10f;
        [Tooltip("Chain topologies only. No effect on Chain3.")]
        [Range(0f, 1f)]
        [SerializeField]
        float _rollDamping = 0.25f;
        [Tooltip("m/s. 0 disables sleeping.")]
        [Min(0f)]
        [SerializeField]
        float _sleepSpeed = 0.04f;
        [Tooltip("Seconds. Too short freezes bodies mid-tumble.")]
        [Min(0f)]
        [SerializeField]
        float _sleepDelay = 0.5f;
        [Tooltip("Meters. How far contacts may push a sleeping body before it wakes. Too small and contact noise wakes everything.")]
        [Min(0.0001f)]
        [SerializeField]
        float _wakeDistance = 0.005f;
        [Inline]
        [SerializeField]
        ParticleBehaviourModifier[] _modifiers = System.Array.Empty<ParticleBehaviourModifier>();

        static ParticleBehaviourProfile s_fallback;

        /// <summary>
        ///   The defaults an emitter without a profile runs with.
        /// </summary>
        internal static ParticleBehaviourProfile Fallback
        {
            get
            {
                if (s_fallback == null)
                {
                    s_fallback = CreateInstance<ParticleBehaviourProfile>();
                    s_fallback.hideFlags = HideFlags.HideAndDontSave;
                }
                return s_fallback;
            }
        }

        public float SpeedLimit
        {
            get => _speedLimit;
            set => _speedLimit = Mathf.Max(0f, value);
        }

        public float SpeedDecayRate
        {
            get => _speedDecayRate;
            set => _speedDecayRate = Mathf.Max(0.01f, value);
        }

        public float RollDamping
        {
            get => _rollDamping;
            set => _rollDamping = Mathf.Clamp01(value);
        }

        public float SleepSpeed
        {
            get => _sleepSpeed;
            set => _sleepSpeed = Mathf.Max(0f, value);
        }

        public float SleepDelay
        {
            get => _sleepDelay;
            set => _sleepDelay = Mathf.Max(0f, value);
        }

        public float WakeDistance
        {
            get => _wakeDistance;
            set => _wakeDistance = Mathf.Max(0.0001f, value);
        }

        public ParticleBehaviourModifier[] Modifiers
        {
            get => _modifiers;
            set => _modifiers = value ?? System.Array.Empty<ParticleBehaviourModifier>();
        }

        /// <summary>
        ///   True when an enabled oscillation, surface impulse or locomotion is in the list.
        /// </summary>
        public bool KeepsAwake
        {
            get
            {
                if (_modifiers == null)
                    return false;
                foreach (ParticleBehaviourModifier modifier in _modifiers)
                    if (modifier != null && modifier.Enabled)
                        return true;
                return false;
            }
        }
    }

    /// <summary>
    ///   Beats a chain body's two halves in a C bend, like a tail stroke (SolverOscillationProfile).
    /// </summary>
    public sealed class ParticleOscillationProfile : ParticleBehaviourModifier
    {
        [Tooltip("Caps the velocity channel only; the pose is always applied in full.")]
        [Min(0f)]
        public float acceleration = 120f;
        [Tooltip("0 stops all self-driven motion; contact still moves the body.")]
        [Range(0f, 1f)]
        public float vitality = 1f;
        [Range(-180f, 180f)]
        public float directionAngle;
        [Range(0f, 180f)]
        public float directionRandomness;
        [Tooltip("A rate, like a timescale in reverse. 1 freezes the current shape.")]
        [Range(0f, 1f)]
        public float stiffness;
        [Tooltip("0 swings fullest, 1 flattens to the topology's rest form.")]
        [Range(0f, 1f)]
        public float muscleTension = 0.3f;
        [Range(0f, 1f)]
        public float tensionRandomness = 0.15f;
        [Tooltip("0.5 sweeps head and tail alike; 1 holds the head and gives the beat to the tail.")]
        [Range(0f, 1f)]
        public float tailBias = 0.5f;
        [Tooltip("Runs per second. 0 means a run never begins.")]
        public float frequency = 0.8f;
        [Range(0f, 1f)]
        public float frequencyRandomness = 0.35f;
        [Tooltip("Seconds per run. Longer runs play back to back if Frequency leaves no gap.")]
        [Min(0f)]
        public float duration = 1f;
        [Range(0f, 1f)]
        public float durationRandomness = 0.25f;
    }

    /// <summary>
    ///   Hops a body off whatever supports it (SolverSurfaceImpulseProfile).
    /// </summary>
    public sealed class ParticleSurfaceImpulseProfile : ParticleBehaviourModifier
    {
        [Tooltip("m/s.")]
        [Min(0f)]
        public float impulseSpeed = 2.5f;
        [Tooltip("Hops per second.")]
        [Range(0f, 8f)]
        public float frequency = 1.2f;
        [Range(0f, 1f)]
        public float frequencyRandomness = 0.134f;
        [Tooltip("Fixed steps the launch speed is held for.")]
        [Min(1f)]
        public float impactSpread = 6f;
        [Tooltip("m/s of descent above which the body counts as airborne.")]
        [Min(0f)]
        public float fallSpeedLimit = 0.5f;
    }

    /// <summary>
    ///   How a body moves itself toward a <see cref="ParticleMotionTarget"/> by pushing against the
    ///   medium it is in (SolverLocomotionProfile). Does nothing outside a medium.
    /// </summary>
    public sealed class ParticleLocomotionProfile : ParticleBehaviourModifier
    {
        [Tooltip("m/s the body works toward while pushing.")]
        [Min(0f)]
        public float speed = 1.5f;
        [Tooltip("How much of the medium's density this stroke turns into travel. 0 means this body cannot swim.")]
        [Min(0f)]
        public float mediumThrust = 1f;
        [Tooltip("Pushes per second. 0 never pushes.")]
        [Min(0f)]
        public float frequency = 1f;
        [Tooltip("Seconds one push lasts. At or past 1/Frequency it is continuous.")]
        [Min(0.01f)]
        public float duration = 0.3f;
        [Tooltip("Spread of the rhythm across bodies, so a group does not pulse in unison.")]
        [Range(0f, 1f)]
        public float randomness = 0.3f;
        [Tooltip("Degrees the heading is scattered within, re-rolled each push. 0 makes the group an arrow.")]
        [Range(0f, 90f)]
        public float headingSpread = 25f;
        [Tooltip("Degrees per second the body swings its head onto the heading. 0 leaves it drifting sideways.")]
        [Min(0f)]
        public float turnRate = 180f;
        [Tooltip("Degrees per second the body rights itself. 0 lets it roll freely.")]
        [Min(0f)]
        public float uprightRate = 120f;
    }
}

using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   What a <see cref="ParticleVolume"/> does to the bodies and particles inside it
    ///   (ParticleExtensions EXT-14). The volume says where; an effect says what happens there, and
    ///   one volume can carry several.
    /// </summary>
    public abstract class ParticleVolumeEffect : ScriptableObject
    {
        [Tooltip("Off silences the effect without losing its slot or settings.")]
        [SerializeField]
        bool _enabled = true;

        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }
    }

    /// <summary>
    ///   Makes a volume a fluid: buoyancy from <see cref="Density"/> and convergence toward
    ///   <see cref="Flow"/> (ParticleSolver SOLVER-17). Density is in profile mass units, so a body
    ///   is neutral at its own mass over its volume, not at the kg/m³ of water.
    /// </summary>
    public sealed class ParticleMediumEffect : ParticleVolumeEffect
    {
        [Tooltip("Equals the body's own density for neutral buoyancy: body mass / (particles x 4/3 pi r^3).")]
        [Min(0f)]
        [SerializeField]
        float _density;
        [Tooltip("Meters per second the fluid itself moves at. Zero is still water.")]
        [SerializeField]
        Vector3 _flow;
        [Tooltip("On reads Flow in the volume's own axes, so rotating the volume aims the flow.")]
        [SerializeField]
        bool _flowIsLocal;
        [Tooltip("How strongly the fluid pulls a body toward its flow, multiplied by the density ratio.")]
        [Min(0f)]
        [SerializeField]
        float _dragCoefficient = 1f;

        public float Density
        {
            get => _density;
            set => _density = Mathf.Max(0f, value);
        }

        public Vector3 Flow
        {
            get => _flow;
            set => _flow = value;
        }

        public bool FlowIsLocal
        {
            get => _flowIsLocal;
            set => _flowIsLocal = value;
        }

        public float DragCoefficient
        {
            get => _dragCoefficient;
            set => _dragCoefficient = Mathf.Max(0f, value);
        }

        internal ParticleMedium ToMedium(ParticleVolume volume) => new ParticleMedium
        {
            center = volume.Center,
            halfExtents = volume.HalfExtents,
            rotation = volume.Rotation,
            density = Mathf.Max(0f, _density),
            flow = _flowIsLocal ? volume.Rotation * _flow : _flow,
            dragCoefficient = Mathf.Max(0f, _dragCoefficient)
        };
    }

    /// <summary>
    ///   Which side of a bounds volume bodies may live on.
    /// </summary>
    public enum ParticleBoundsMode
    {
        /// <summary>
        ///   Bodies may only live inside. Several Contain volumes are a union.
        /// </summary>
        Contain = 0,

        /// <summary>
        ///   A body that reaches inside is recycled, and this always wins over Contain.
        /// </summary>
        Exclude = 1
    }

    /// <summary>
    ///   Sends a body back to its emitter's spawn box when it is where it must not be, and hides
    ///   the trip behind a fade (ParticleExtensions EXT-14, EXT-12). The fade only shrinks what is
    ///   drawn; the body keeps colliding at full size.
    /// </summary>
    public sealed class ParticleBoundsEffect : ParticleVolumeEffect
    {
        [SerializeField]
        ParticleBoundsMode _mode = ParticleBoundsMode.Contain;
        [Tooltip("Seconds to shrink away before the body is moved. 0 moves it at once.")]
        [Min(0f)]
        [SerializeField]
        float _fadeOut = 0.5f;
        [Tooltip("Seconds to grow back once it has been moved.")]
        [Min(0f)]
        [SerializeField]
        float _fadeIn = 0.5f;

        public ParticleBoundsMode Mode
        {
            get => _mode;
            set => _mode = value;
        }

        public float FadeOut
        {
            get => _fadeOut;
            set => _fadeOut = Mathf.Max(0f, value);
        }

        public float FadeIn
        {
            get => _fadeIn;
            set => _fadeIn = Mathf.Max(0f, value);
        }
    }
}

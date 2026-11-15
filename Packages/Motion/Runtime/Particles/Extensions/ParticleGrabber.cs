using System;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   How many particles a grab took, and the world step it took them in
    ///   (ParticleExtensions EXT-07, ParticleWorld WORLD-14). A failed readback reports -1.
    /// </summary>
    public readonly struct ParticleGrabResult
    {
        public ParticleGrabResult(int count, int step)
        {
            Count = count;
            Step = step;
        }

        public int Count { get; }
        public int Step { get; }
    }

    /// <summary>
    ///   Picks up the particles of one entity of a <see cref="PhysicsParticle"/> that lie within
    ///   <see cref="Radius"/> of a hand and carries them with the hand until released
    ///   (ParticleExtensions EXT-07). <see cref="Grab"/> takes effect in the first substep of the
    ///   next step; particles already held by an anchor or another grabber are skipped. The count
    ///   arrives through <see cref="Grabbed"/> from an asynchronous readback. Released particles get
    ///   their inverse mass back and keep the hand's velocity at their point (EXT-05).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ParticleGrabber : ParticleHoldBehaviour
    {
        enum State
        {
            Idle,
            Pending,
            Holding
        }

        [Tooltip("The driver whose entity is grabbed.")]
        [SerializeField]
        PhysicsParticle _target;
        [Tooltip("Which of the driver's entities is grabbed.")]
        [Min(0)]
        [SerializeField]
        int _entityIndex;
        [Tooltip("The hand. Empty uses this Transform.")]
        [SerializeField]
        Transform _hand;
        [Tooltip("Particles within this distance of the hand, in meters, are grabbed.")]
        [Min(0f)]
        [SerializeField]
        float _radius = 0.1f;

        readonly Matrix4x4[] _previous = new Matrix4x4[1];
        readonly Matrix4x4[] _current = new Matrix4x4[1];
        ParticleHoldSet.Entry[] _entries = Array.Empty<ParticleHoldSet.Entry>();
        ParticleEntity _held;
        Matrix4x4 _lastHand;
        bool _hasLastHand;
        bool _simulated;
        State _state;

        /// <summary>
        ///   Raised once per grab with the number of particles taken.
        /// </summary>
        public event Action<ParticleGrabResult> Grabbed;

        public PhysicsParticle Target
        {
            get => _target;
            set
            {
                _target = value;
                SyncWorld();
            }
        }

        public int EntityIndex
        {
            get => _entityIndex;
            set => _entityIndex = Mathf.Max(0, value);
        }

        public Transform Hand
        {
            get => _hand != null ? _hand : transform;
            set => _hand = value;
        }

        public float Radius
        {
            get => _radius;
            set => _radius = Mathf.Max(0f, value);
        }

        /// <summary>
        ///   True from <see cref="Grab"/> until <see cref="Release"/>.
        /// </summary>
        public bool IsGrabbing => _state != State.Idle;

        protected override PhysicsParticle HeldDriver => _target;

        protected override bool HoldsLiveParticles => _state == State.Holding && _held.IsValid;

        /// <summary>
        ///   Grabs in the next step. Returns false when already grabbing; release first.
        /// </summary>
        public bool Grab()
        {
            if (_state != State.Idle)
                return false;
            _state = State.Pending;
            return true;
        }

        /// <summary>
        ///   Lets go of everything grabbed, immediately.
        /// </summary>
        public void Release()
        {
            if (_state == State.Holding)
                ReleaseHeld();
            _state = State.Idle;
        }

        protected override void OnRegistered() => _hasLastHand = false;

        protected override void OnReleased() => _state = State.Idle;

        public override void Prepare(ParticleExtensionContext context)
        {
            Matrix4x4 hand = Hand.localToWorldMatrix;
            _previous[0] = _hasLastHand ? _lastHand : hand;
            _current[0] = hand;
            _lastHand = hand;
            _hasLastHand = true;
            _simulated = false;
            if (_state == State.Idle)
                return;
            if (_state == State.Holding && !context.IsSimulated(_held))
            {
                if (!_held.IsValid)
                {
                    Holds.SetEntries(null, 0);
                    _state = State.Idle;
                }
                return;
            }
            if (_state == State.Pending && !TryPrepareCapture(context))
                return;
            Holds.SetHolders(_previous, _current, 1);
            _simulated = true;
        }

        public override void Execute(ParticleExtensionContext context)
        {
            if (!_simulated)
                return;
            if (_state == State.Pending && context.SubstepIndex == 0)
            {
                Holds.Capture(context, _radius);
                _state = State.Holding;
                int step = context.Step;
                Holds.RequestCount(count => Grabbed?.Invoke(new ParticleGrabResult(count, step)));
            }
            Holds.Apply(context);
        }

        bool TryPrepareCapture(ParticleExtensionContext context)
        {
            if (_target == null || _entityIndex >= _target.Entities.Count)
                return false;
            ParticleEntity entity = _target.Entities[_entityIndex];
            if (!context.IsSimulated(entity))
                return false;
            _held = entity;
            RangeInt particles = entity.Particles;
            if (_entries.Length < particles.length)
                _entries = new ParticleHoldSet.Entry[Mathf.NextPowerOfTwo(particles.length)];
            for (int i = 0; i < particles.length; i++)
                _entries[i] = new ParticleHoldSet.Entry { particle = (uint)(particles.start + i) };
            Holds.SetEntries(_entries, particles.length);
            return true;
        }
    }
}

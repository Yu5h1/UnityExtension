using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   Scene component that holds particles of a <see cref="PhysicsParticle"/> at Transforms
    ///   (ParticleExtensions EXT-05). It registers as a substep extension with the world its
    ///   driver is bound to, follows that world when it changes, and releases what it holds when
    ///   disabled or moved to another world.
    /// </summary>
    public abstract class ParticleHoldBehaviour : MonoBehaviour, IParticleExtension
    {
        ParticleWorld _registered;
        bool _active;
        string _lastError;

        /// <summary>
        ///   The world this component is registered with, or null.
        /// </summary>
        public ParticleWorld RegisteredWorld => _registered;

        public ParticleExtensionStage Stage => ParticleExtensionStage.Substep;
        public ParticleExtensionWrites Writes => ParticleExtensionWrites.Positions;

        /// <summary>
        ///   The driver whose particles are held.
        /// </summary>
        protected abstract PhysicsParticle HeldDriver { get; }

        internal ParticleHoldSet Holds { get; private set; }

        /// <summary>
        ///   False when the held entries may point at slots of a released entity, so releasing
        ///   must not write to them.
        /// </summary>
        protected virtual bool HoldsLiveParticles => true;

        protected virtual void OnEnable() => EnableHold();
        protected virtual void OnDisable() => DisableHold();
        protected virtual void FixedUpdate() => SyncWorld();

        protected virtual void OnDestroy() => DestroyHold();

        internal void EnableHold()
        {
            _active = true;
            SyncWorld();
        }

        internal void DisableHold()
        {
            _active = false;
            Unregister();
        }

        internal void DestroyHold()
        {
            DisableHold();
            Holds?.Dispose();
            Holds = null;
        }

        /// <summary>
        ///   Registers with the world the driver is bound to, following it when it changes.
        /// </summary>
        internal void SyncWorld()
        {
            PhysicsParticle driver = HeldDriver;
            ParticleWorld world = _active && driver != null ? driver.BoundWorld : null;
            if (world == _registered)
                return;
            Unregister();
            if (world == null)
                return;
            if (!ParticleHoldSet.TryLoadShader(out string error) || !world.RegisterExtension(this, out error))
            {
                Fail($"{GetType().Name} '{name}' is not active: {error}");
                return;
            }
            if (Holds == null)
                Holds = new ParticleHoldSet();
            _registered = world;
            _lastError = null;
            OnRegistered();
        }

        /// <summary>
        ///   Called after registering with a world, before its first step there.
        /// </summary>
        protected virtual void OnRegistered()
        {
        }

        /// <summary>
        ///   Called after releasing what was held, when unregistering.
        /// </summary>
        protected virtual void OnReleased()
        {
        }

        public abstract void Prepare(ParticleExtensionContext context);

        public virtual void Execute(ParticleExtensionContext context) => Holds.Apply(context);

        /// <summary>
        ///   Gives every held particle its recorded inverse mass back now.
        /// </summary>
        protected void ReleaseHeld()
        {
            if (_registered != null && HoldsLiveParticles)
                Holds?.Release(_registered);
            Holds?.SetEntries(null, 0);
        }

        void Unregister()
        {
            if (_registered == null)
                return;
            ReleaseHeld();
            _registered.UnregisterExtension(this);
            _registered = null;
            OnReleased();
        }

        protected void Fail(string message)
        {
            if (message != _lastError)
                Debug.LogError(message, this);
            _lastError = message;
        }
    }
}

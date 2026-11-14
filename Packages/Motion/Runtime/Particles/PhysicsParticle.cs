using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   Scene driver that turns a layout, an interaction and an appearance into
    ///   <see cref="Count"/> entities in a <see cref="ParticleWorld"/>. Enabling spawns its entities
    ///   (the first time by allocating them, afterwards by resetting them to their spawn poses);
    ///   disabling parks them so they cost nothing; destroying releases them (ParticleWorld
    ///   WORLD-08). It follows the world it resolves: when another world becomes the resolved one,
    ///   its entities move there (WORLD-02). Entity i spawns at the Transform's position plus
    ///   i × <see cref="BatchSpacing"/> in local space, with the Transform's rotation; scale is
    ///   ignored so physical sizes stay in meters. Particles take the GameObject's layer as their
    ///   category bit (WORLD-11).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PhysicsParticle : MonoBehaviour
    {
        [Tooltip("World to allocate in. Empty uses the only active ParticleWorld.")]
        [SerializeField]
        ParticleWorld _world;
        [Inline]
        [SerializeField]
        PhysicsParticleLayout _layout;
        [Inline]
        [SerializeField]
        PhysicsParticleInteraction _interaction;
        [Inline]
        [SerializeField]
        PhysicsParticleAppearance _appearance;
        [Min(0)]
        [SerializeField]
        int _count = 1;
        [SerializeField]
        Vector3 _batchSpacing = Vector3.right;

        readonly List<ParticleEntity> _entities = new List<ParticleEntity>();
        readonly List<Vector3> _positions = new List<Vector3>();
        ParticleWorld _bound;
        bool _active;
        string _lastError;

        public ParticleWorld World
        {
            get => _world;
            set
            {
                _world = value;
                if (_active)
                    OnWorldsChanged();
            }
        }

        public PhysicsParticleLayout Layout
        {
            get => _layout;
            set => _layout = value;
        }

        public PhysicsParticleInteraction Interaction
        {
            get => _interaction;
            set => _interaction = value;
        }

        public PhysicsParticleAppearance Appearance
        {
            get => _appearance;
            set => _appearance = value;
        }

        public int Count
        {
            get => _count;
            set => _count = Mathf.Max(0, value);
        }

        public Vector3 BatchSpacing
        {
            get => _batchSpacing;
            set => _batchSpacing = value;
        }

        public ParticleWorld BoundWorld => _bound;
        public IReadOnlyList<ParticleEntity> Entities => _entities;

        /// <summary>
        ///   The last allocation or resolution failure, or null.
        /// </summary>
        public string LastError => _lastError;

        void OnEnable() => EnableDriver();
        void OnDisable() => DisableDriver();
        void OnDestroy() => DestroyDriver();

        internal void EnableDriver()
        {
            _active = true;
            ParticleWorld.ActiveWorldsChanged -= OnWorldsChanged;
            ParticleWorld.ActiveWorldsChanged += OnWorldsChanged;
            Rebind();
            Spawn();
        }

        internal void DisableDriver()
        {
            _active = false;
            ParticleWorld.ActiveWorldsChanged -= OnWorldsChanged;
            foreach (ParticleEntity entity in _entities)
                if (entity.IsValid)
                    entity.SetParked(true);
        }

        internal void DestroyDriver()
        {
            DisableDriver();
            ReleaseAll();
            _bound = null;
        }

        /// <summary>
        ///   Pose of entity <paramref name="index"/> when it spawns or respawns.
        /// </summary>
        public Pose SpawnPose(int index) =>
            new Pose(transform.TransformPoint(_batchSpacing * index), transform.rotation);

        /// <summary>
        ///   Validates the three profiles against each other and their own rules.
        /// </summary>
        public void Validate(List<PhysicsParticleMessage> messages)
        {
            if (_layout == null)
                messages.Add(PhysicsParticleMessage.Error("No layout."));
            else
                _layout.Validate(messages);
            if (_interaction == null)
                messages.Add(PhysicsParticleMessage.Error("No interaction."));
            else
                _interaction.Validate(_layout, messages);
            if (_appearance == null)
                messages.Add(PhysicsParticleMessage.Info("No appearance; the entities simulate but are not drawn."));
            else
                _appearance.Validate(_layout, _interaction, messages);
            if (_count == 0)
                messages.Add(PhysicsParticleMessage.Info("Count is 0; nothing spawns."));
        }

        /// <summary>
        ///   Builds the request for one entity of <paramref name="variant"/> at
        ///   <paramref name="pose"/>: rest positions and roles from the layout, mass, radius,
        ///   grouping, constraints and rigid groups from the interaction.
        /// </summary>
        public static ParticleEntityRequest BuildRequest(
            PhysicsParticleLayout layout,
            PhysicsParticleInteraction interaction,
            int variant,
            Pose pose,
            uint category)
        {
            var request = new ParticleEntityRequest
            {
                pose = pose,
                category = category,
                groupMode = interaction.SelfCollision ? ParticleGroupMode.Shared : ParticleGroupMode.NewGroup
            };
            var positions = new List<Vector3>();
            layout.GetRestPositions(variant, positions);
            float invMass = interaction.ParticleMass > 0f ? 1f / interaction.ParticleMass : 0f;
            for (int i = 0; i < positions.Count; i++)
            {
                request.particles.Add(new ParticleSpawn
                {
                    position = positions[i],
                    invMass = invMass,
                    radius = interaction.ParticleRadius
                });
                request.roles.Add(layout.GetRole(variant, i));
            }
            interaction.Build(layout, variant, request);
            return request;
        }

        void OnWorldsChanged()
        {
            if (Rebind() && _active)
                Spawn();
        }

        /// <summary>
        ///   Resolves the world again. Returns true when the bound world changed; the previous
        ///   world's entities are then released. A failed resolution keeps the current binding, so
        ///   a disabled world stays paused with its entities.
        /// </summary>
        bool Rebind()
        {
            if (_bound == null || _bound.IsDestroyed)
            {
                _entities.Clear();
                _bound = null;
            }
            if (!ParticleWorld.TryResolve(_world, out ParticleWorld world, out string error))
            {
                if (_bound == null)
                    _lastError = error;
                return false;
            }
            if (world == _bound)
                return false;
            ReleaseAll();
            _bound = world;
            _lastError = null;
            return true;
        }

        void Spawn()
        {
            if (_bound == null)
                return;
            if (_entities.Count > 0)
            {
                for (int i = 0; i < _entities.Count; i++)
                {
                    if (!_entities[i].IsValid)
                        continue;
                    _entities[i].Reset(SpawnPose(i));
                    _entities[i].SetParked(false);
                }
                return;
            }
            if (_count == 0 || _layout == null || _interaction == null)
                return;
            var messages = new List<PhysicsParticleMessage>();
            Validate(messages);
            foreach (PhysicsParticleMessage message in messages)
            {
                if (message.severity != PhysicsParticleMessageSeverity.Error)
                    continue;
                Fail($"PhysicsParticle '{name}' did not spawn: {message.text}");
                return;
            }
            uint category = 1u << gameObject.layer;
            var requests = new List<ParticleEntityRequest>(_count);
            for (int i = 0; i < _count; i++)
                requests.Add(BuildRequest(_layout, _interaction, i % _layout.VariantCount, SpawnPose(i), category));
            if (_bound.Allocate(requests, _entities, ParticleAllocationMode.AllOrNothing, out string error) == 0)
                Fail($"PhysicsParticle '{name}' did not spawn: {error}");
            else
                _lastError = null;
        }

        void ReleaseAll()
        {
            foreach (ParticleEntity entity in _entities)
                entity.Release();
            _entities.Clear();
        }

        void Fail(string message)
        {
            if (message != _lastError)
                Debug.LogError(message, this);
            _lastError = message;
        }
    }
}

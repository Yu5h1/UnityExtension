using System;
using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   Creates, recycles and parks a batch of few-particle bodies such as fish or ice fragments in a
    ///   <see cref="ParticleWorld"/> (ParticleExtensions EXT-11). Its entities are the bodies of an
    ///   <see cref="IParticleBodySource"/>; a behaviour extension registered with the world runs their
    ///   kernels after every step (EXT-12), puts still bodies to sleep (EXT-13) and recycles bodies
    ///   that leave their <see cref="ParticleVolume"/> (EXT-14). The first enable spawns
    ///   <see cref="InitialCount"/> bodies at random points of the spawn box; disabling parks all of
    ///   them and enabling again moves each to a new random point (ParticleWorld WORLD-08). Scripts
    ///   queue bodies with <see cref="TryEnqueue"/>; the next fixed step creates them together.
    ///   Nothing here reads the GPU synchronously.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ParticleEmitter : MonoBehaviour, IParticleBodySource
    {
        [Tooltip("World to allocate in. Empty uses the only active ParticleWorld.")]
        [SerializeField]
        ParticleWorld _world;
        [Inline]
        [SerializeField]
        ParticleBodyLayout _layout;
        [Inline]
        [SerializeField]
        ParticleBodyInteraction _interaction;
        [Inline]
        [SerializeField]
        PhysicsParticleAppearance _appearance;
        [Inline]
        [SerializeField]
        ParticleBehaviourProfile _behaviour;
        [Min(1)]
        [SerializeField]
        int _maxInstances = 2048;
        [Tooltip("The same seed lays out the same pile every run. Needs a shape source on the layout.")]
        [SerializeField]
        int _shapeSeed = 12345;
        [Tooltip("Off means nothing appears until something calls TryEnqueue.")]
        [SerializeField]
        bool _playOnAwake = true;
        [Min(0)]
        [SerializeField]
        int _initialCount = 100;
        [SerializeField]
        Vector3 _spawnVolume = new Vector3(5f, 2f, 5f);
        [SerializeField]
        Vector3 _initialVelocity;
        [Min(0f)]
        [SerializeField]
        float _velocityVariation = 0.5f;
        [Tooltip("On gives every body a group of its own, so bodies collide with each other. Off puts the whole emitter in one group.")]
        [SerializeField]
        bool _collideWithSameProfile = true;
        [Range(0f, 1f)]
        [SerializeField]
        float _colorVariation = 0.15f;

        readonly List<ParticleEntity> _entities = new List<ParticleEntity>();
        readonly List<ParticleBodySpawn> _queued = new List<ParticleBodySpawn>();
        ParticleWorld _bound;
        ParticleBodyRunner _runner;
        int _sharedGroup;
        bool _active;
        bool _spawnedInitial;
        bool _wasDisabled;
        bool _reportedCapacity;
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

        public ParticleBodyLayout Layout
        {
            get => _layout;
            set => _layout = value;
        }

        public ParticleBodyInteraction Interaction
        {
            get => _interaction;
            set => _interaction = value;
        }

        public PhysicsParticleAppearance Appearance
        {
            get => _appearance;
            set
            {
                _appearance = value;
                if (_active && _appearance != null)
                    PhysicsParticleRenderer.Ensure(gameObject);
            }
        }

        public ParticleBehaviourProfile Behaviour
        {
            get => _behaviour;
            set => _behaviour = value;
        }

        public int MaxInstances
        {
            get => _maxInstances;
            set => _maxInstances = Mathf.Max(1, value);
        }

        public int ShapeSeed
        {
            get => _shapeSeed;
            set => _shapeSeed = value;
        }

        public bool PlayOnAwake
        {
            get => _playOnAwake;
            set => _playOnAwake = value;
        }

        public int InitialCount
        {
            get => _initialCount;
            set => _initialCount = Mathf.Max(0, value);
        }

        public Vector3 SpawnVolume
        {
            get => _spawnVolume;
            set => _spawnVolume = new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
        }

        public Vector3 InitialVelocity
        {
            get => _initialVelocity;
            set => _initialVelocity = value;
        }

        public float VelocityVariation
        {
            get => _velocityVariation;
            set => _velocityVariation = Mathf.Max(0f, value);
        }

        public bool CollideWithSameProfile
        {
            get => _collideWithSameProfile;
            set => _collideWithSameProfile = value;
        }

        public float ColorVariation
        {
            get => _colorVariation;
            set => _colorVariation = Mathf.Clamp01(value);
        }

        public ParticleWorld BoundWorld => _bound;
        public IReadOnlyList<ParticleEntity> Entities => _entities;
        public ComputeBuffer Bodies => _runner?.Bodies;
        public ComputeBuffer Lifecycle => _runner?.Lifecycle;
        public int Version => _runner != null ? _runner.Version + _entities.Count : _entities.Count;

        /// <summary>
        ///   The last allocation or resolution failure, or null.
        /// </summary>
        public string LastError => _lastError;

        /// <summary>
        ///   Requests waiting for the next fixed step.
        /// </summary>
        public int PendingCount => _queued.Count;

        internal ParticleBodyRunner Runner => _runner;

        internal ParticleBehaviourProfile EffectiveBehaviour => _behaviour != null ? _behaviour : ParticleBehaviourProfile.Fallback;

        void OnEnable() => EnableEmitter();
        void OnDisable() => DisableEmitter();
        void OnDestroy() => DestroyEmitter();
        void FixedUpdate() => Tick();

        internal void EnableEmitter()
        {
            _active = true;
            ParticleWorld.ActiveWorldsChanged -= OnWorldsChanged;
            ParticleWorld.ActiveWorldsChanged += OnWorldsChanged;
            bool rebound = Rebind();
            if (!rebound && _wasDisabled)
                Respawn();
            else if (_bound != null)
                SpawnInitial();
            _wasDisabled = false;
            if (_appearance != null)
                PhysicsParticleRenderer.Ensure(gameObject);
        }

        internal void DisableEmitter()
        {
            _active = false;
            _wasDisabled = true;
            ParticleWorld.ActiveWorldsChanged -= OnWorldsChanged;
            Park();
        }

        internal void DestroyEmitter()
        {
            DisableEmitter();
            ReleaseAll();
            DisposeRunner();
            _bound = null;
        }

        /// <summary>
        ///   One fixed step of the emitter's own work: creates the queued bodies together, and wakes
        ///   sleeping bodies while sleep is off or a continuous behaviour is in the list
        ///   (ParticleExtensions EXT-13). Called from FixedUpdate.
        /// </summary>
        internal void Tick()
        {
            if (_bound == null)
                return;
            FlushQueued();
            ParticleBehaviourProfile behaviour = EffectiveBehaviour;
            if (behaviour.KeepsAwake || behaviour.SleepSpeed <= 0f)
                foreach (ParticleEntity entity in _entities)
                    if (entity.IsValid && entity.IsSleeping)
                        entity.SetSleeping(false);
        }

        /// <summary>
        ///   Moves every body to a new random point of the spawn box at rest, with its lifecycle and
        ///   sleep state cleared, and brings parked bodies back (ParticleWorld WORLD-07, WORLD-08).
        /// </summary>
        public void Respawn()
        {
            foreach (ParticleEntity entity in _entities)
            {
                if (!entity.IsValid)
                    continue;
                var pose = new Pose(RandomSpawnPoint(), entity.Pose.rotation);
                entity.Reset(pose);
                entity.Teleport(pose, Vector3.zero);
                entity.SetParked(false);
            }
            _runner?.ResetState();
        }

        /// <summary>
        ///   Takes every body out of play: not simulated, not colliding, not drawn.
        /// </summary>
        public void Park()
        {
            foreach (ParticleEntity entity in _entities)
                if (entity.IsValid)
                    entity.SetParked(true);
        }

        /// <summary>
        ///   Queues one body; it is created, with everything else queued, at the next fixed step.
        ///   Returns false when the emitter's instance limit would be exceeded.
        /// </summary>
        public bool TryEnqueue(ParticleBodySpawn spawn)
        {
            if (_entities.Count + _queued.Count >= _maxInstances)
            {
                ReportCapacity($"ParticleEmitter '{name}' holds {_maxInstances} bodies already; the request was rejected.");
                return false;
            }
            spawn.rotation = NormalizeRotation(spawn.rotation);
            spawn.scale = SanitizeScale(spawn.scale);
            _queued.Add(spawn);
            return true;
        }

        public bool TryEnqueue(Vector3 position, Vector3 velocity) =>
            TryEnqueue(ParticleBodySpawn.Create(position, velocity, BaseColor));

        /// <summary>
        ///   Creates the queued bodies in one allocation. As many as fit are created and a shortfall is
        ///   reported once (ParticleWorld WORLD-04). Returns how many were created.
        /// </summary>
        public int FlushQueued()
        {
            if (_queued.Count == 0 || _bound == null)
                return 0;
            int created = Allocate(_queued);
            _queued.Clear();
            return created;
        }

        public void ClearPending() => _queued.Clear();

        /// <summary>
        ///   Where entity <paramref name="index"/>'s variant comes from the shape source, seeded by
        ///   <see cref="ShapeSeed"/> so the same seed lays out the same pile every run.
        /// </summary>
        public int TemplateFor(int instanceIndex, int templateCount)
        {
            if (templateCount <= 1)
                return 0;
            uint value = unchecked((uint)(_shapeSeed * 73856093 ^ (instanceIndex + 1) * 19349663));
            value ^= value >> 16;
            value *= 0x7feb352du;
            value ^= value >> 15;
            value *= 0x846ca68bu;
            value ^= value >> 16;
            return (int)(value % (uint)templateCount);
        }

        Color BaseColor => _appearance != null ? _appearance.Color : Color.white;

        Vector3 RandomSpawnPoint()
        {
            Vector3 half = _spawnVolume * 0.5f;
            return transform.TransformPoint(new Vector3(
                UnityEngine.Random.Range(-half.x, half.x),
                UnityEngine.Random.Range(-half.y, half.y),
                UnityEngine.Random.Range(-half.z, half.z)));
        }

        void SpawnInitial()
        {
            if (_spawnedInitial || !_playOnAwake || _initialCount <= 0 || _layout == null || _interaction == null)
                return;
            _spawnedInitial = true;
            var requests = new List<ParticleBodySpawn>(_initialCount);
            for (int i = 0; i < _initialCount; i++)
            {
                ParticleBodySpawn spawn = ParticleBodySpawn.Create(
                    RandomSpawnPoint(),
                    transform.TransformDirection(_initialVelocity + UnityEngine.Random.insideUnitSphere * _velocityVariation),
                    BaseColor);
                spawn.rotation = transform.rotation;
                requests.Add(spawn);
            }
            Allocate(requests);
        }

        int Allocate(List<ParticleBodySpawn> spawns)
        {
            if (_bound == null)
                return 0;
            if (!Validate(out string problem))
            {
                Fail($"ParticleEmitter '{name}' did not spawn: {problem}");
                return 0;
            }
            if (!EnsureRunner())
                return 0;
            int room = Mathf.Max(0, _maxInstances - _entities.Count);
            int wanted = Mathf.Min(spawns.Count, room);
            if (wanted < spawns.Count)
                ReportCapacity($"ParticleEmitter '{name}' can hold {_maxInstances} bodies; {spawns.Count - wanted} requests were dropped.");
            if (wanted == 0)
                return 0;

            if (!_collideWithSameProfile && _sharedGroup == 0)
                _sharedGroup = _bound.AllocateGroup();
            uint category = 1u << gameObject.layer;
            var requests = new List<ParticleEntityRequest>(wanted);
            var colors = new List<Color32>(wanted);
            var variants = new List<int>(wanted);
            for (int i = 0; i < wanted; i++)
            {
                int index = _entities.Count + i;
                int variant = TemplateFor(index, _layout.VariantCount);
                ParticleBodySpawn spawn = spawns[i];
                ParticleEntityRequest request = _interaction.BuildBody(_layout, variant, spawn, category);
                if (!_collideWithSameProfile)
                    request.sharedGroup = _sharedGroup;
                requests.Add(request);
                colors.Add(ResolveColor(spawn.color));
                variants.Add(variant);
            }

            int first = _entities.Count;
            int created = _bound.Allocate(requests, _entities, ParticleAllocationMode.BestEffort, out string error);
            if (created < wanted)
                Fail($"ParticleEmitter '{name}' created {created} of {wanted} bodies: {error}");
            else
                _lastError = null;

            for (int i = 0; i < created; i++)
            {
                ParticleEntity entity = _entities[first + i];
                ParticleBodySpawn spawn = spawns[i];
                _runner.SetBody(first + i, MakeBody(entity, variants[i], spawn), PackColor(colors[i]));
            }
            return created;
        }

        static uint PackColor(Color32 color) => color.r | ((uint)color.g << 8) | ((uint)color.b << 16) | ((uint)color.a << 24);

        ParticleBodyData MakeBody(ParticleEntity entity, int variant, in ParticleBodySpawn spawn)
        {
            Vector3 dimensions = _layout.BaseDimensions;
            Quaternion rotation = spawn.rotation;
            return new ParticleBodyData
            {
                particleStart = (uint)entity.Particles.start,
                particleCount = (uint)entity.Particles.length,
                topology = (uint)_layout.TopologyOf(variant),
                flags = 1u,
                scale = new Vector4(spawn.scale.x, spawn.scale.y, spawn.scale.z, 1f),
                spawnRotation = new Vector4(rotation.x, rotation.y, rotation.z, rotation.w),
                dimensions = new Vector4(dimensions.x, dimensions.y, dimensions.z, 0f)
            };
        }

        Color32 ResolveColor(Color requested)
        {
            Color baseColor = requested.a > 0f ? requested : BaseColor;
            if (_colorVariation <= 0f)
                return baseColor;
            Color.RGBToHSV(baseColor, out float h, out float s, out float v);
            h = Mathf.Repeat(h + UnityEngine.Random.Range(-_colorVariation * 0.5f, _colorVariation * 0.5f), 1f);
            s = Mathf.Clamp01(s + UnityEngine.Random.Range(-_colorVariation * 0.2f, _colorVariation * 0.2f));
            v = Mathf.Clamp01(v + UnityEngine.Random.Range(-_colorVariation * 0.2f, _colorVariation * 0.2f));
            Color result = Color.HSVToRGB(h, s, v);
            result.a = baseColor.a;
            return result;
        }

        bool Validate(out string problem)
        {
            problem = null;
            if (_layout == null)
                problem = "no layout.";
            else if (_interaction == null)
                problem = "no interaction.";
            else
            {
                var messages = new List<PhysicsParticleMessage>();
                _layout.Validate(messages);
                _interaction.Validate(_layout, messages);
                foreach (PhysicsParticleMessage message in messages)
                    if (message.severity == PhysicsParticleMessageSeverity.Error)
                    {
                        problem = message.text;
                        break;
                    }
            }
            return problem == null;
        }

        bool EnsureRunner()
        {
            if (_runner != null)
                return true;
            if (!ParticleBodyRunner.TryLoadShader(out ComputeShader shader, out string error))
            {
                Fail($"ParticleEmitter '{name}' cannot run behaviours: {error}");
                return false;
            }
            _runner = new ParticleBodyRunner(this, shader, _maxInstances);
            if (!_bound.RegisterExtension(_runner, out error))
            {
                Fail($"ParticleEmitter '{name}' cannot run behaviours: {error}");
                DisposeRunner();
                return false;
            }
            return true;
        }

        void DisposeRunner()
        {
            if (_runner == null)
                return;
            if (_bound != null && !_bound.IsDestroyed)
                _bound.UnregisterExtension(_runner);
            _runner.Dispose();
            _runner = null;
        }

        void OnWorldsChanged()
        {
            if (Rebind() && _active)
                SpawnInitial();
        }

        /// <summary>
        ///   Resolves the world again. Returns true when the bound world changed; the previous world's
        ///   bodies are then released and the initial bodies spawn in the new one. A failed resolution
        ///   keeps the current binding.
        /// </summary>
        bool Rebind()
        {
            if (_bound != null && _bound.IsDestroyed)
            {
                _entities.Clear();
                DisposeRunner();
                _bound = null;
                _sharedGroup = 0;
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
            DisposeRunner();
            _bound = world;
            _spawnedInitial = false;
            _lastError = null;
            return true;
        }

        void ReleaseAll()
        {
            foreach (ParticleEntity entity in _entities)
                entity.Release();
            _entities.Clear();
            if (_sharedGroup != 0 && _bound != null && !_bound.IsDestroyed)
                _bound.ReleaseGroup(_sharedGroup);
            _sharedGroup = 0;
        }

        void Fail(string message)
        {
            if (message != _lastError)
                Debug.LogError(message, this);
            _lastError = message;
        }

        void ReportCapacity(string message)
        {
            if (_reportedCapacity)
                return;
            _reportedCapacity = true;
            Debug.LogError(message, this);
        }

        static Quaternion NormalizeRotation(Quaternion rotation)
        {
            float length = Mathf.Sqrt(rotation.x * rotation.x + rotation.y * rotation.y + rotation.z * rotation.z + rotation.w * rotation.w);
            if (length < 0.0001f)
                return Quaternion.identity;
            float inverse = 1f / length;
            return new Quaternion(rotation.x * inverse, rotation.y * inverse, rotation.z * inverse, rotation.w * inverse);
        }

        static Vector3 SanitizeScale(Vector3 scale)
        {
            if (scale.sqrMagnitude < 0.000001f)
                return Vector3.one;
            return new Vector3(Mathf.Max(0.0001f, Mathf.Abs(scale.x)), Mathf.Max(0.0001f, Mathf.Abs(scale.y)), Mathf.Max(0.0001f, Mathf.Abs(scale.z)));
        }

        void OnValidate()
        {
            _maxInstances = Mathf.Max(1, _maxInstances);
            _initialCount = Mathf.Max(0, _initialCount);
            _spawnVolume = new Vector3(Mathf.Abs(_spawnVolume.x), Mathf.Abs(_spawnVolume.y), Mathf.Abs(_spawnVolume.z));
            _velocityVariation = Mathf.Max(0f, _velocityVariation);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.color = new Color(0.2f, 0.75f, 1f, 0.25f);
            Gizmos.DrawCube(Vector3.zero, _spawnVolume);
            Gizmos.color = new Color(0.2f, 0.75f, 1f, 1f);
            Gizmos.DrawWireCube(Vector3.zero, _spawnVolume);
        }
    }
}

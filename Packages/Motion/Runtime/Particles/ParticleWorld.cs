using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   Whether a batch allocation creates every entity or none (<see cref="AllOrNothing"/>), or as
    ///   many complete entities as fit (<see cref="BestEffort"/>).
    /// </summary>
    public enum ParticleAllocationMode
    {
        AllOrNothing,
        BestEffort
    }

    /// <summary>
    ///   One independent particle simulation in a scene: it owns capacity, entities, colliders and
    ///   the step, and lets particle sources allocate, release and reset entities at any time
    ///   (ParticleWorld 1.0.0, Packages/Motion/Documentation/ParticlePhysics/ParticleWorld.md).
    ///   There is no singleton: sources and colliders name a world or resolve the only active one
    ///   (<see cref="TryResolve"/>). Changes made between steps reach the solver together at the
    ///   start of the next step. A disabled world pauses with its state kept; a destroyed world
    ///   releases everything.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ParticleWorld : MonoBehaviour
    {
        internal sealed class EntityRecord
        {
            public int generation;
            public bool alive;
            public RangeInt particles;
            public RangeInt constraints;
            public RangeInt rigidGroups;
            public int group;
            public bool groupOwned;
            public bool parked;
            public bool sleeping;
            public Pose pose;
            public ParticleEntityRequest rest;
        }

        static readonly List<ParticleWorld> s_active = new List<ParticleWorld>();

        /// <summary>
        ///   Raised when a world is enabled, disabled or destroyed, so sources and colliders can
        ///   resolve their world again (ParticleWorld WORLD-02).
        /// </summary>
        public static event Action ActiveWorldsChanged;

        /// <summary>
        ///   Creates the solver backend for a world when it is first enabled. Null uses the GPU
        ///   solver; a world whose backend cannot be created keeps its bookkeeping and simulates
        ///   nothing.
        /// </summary>
        internal static Func<ParticleWorld, IParticleBackend> BackendFactory;

        [SerializeField]
        ParticleSolverSettings _settings = new ParticleSolverSettings();
        [SerializeField]
        ParticleWorldCapacity _capacity = new ParticleWorldCapacity();
        [Tooltip("Log registration and allocation. Off by default (ParticleWorld WORLD-02).")]
        [SerializeField]
        bool _verboseLogging;

        readonly List<EntityRecord> _entities = new List<EntityRecord>();
        readonly Stack<int> _freeIds = new Stack<int>();
        readonly List<ParticleWorldChange> _pending = new List<ParticleWorldChange>();
        readonly List<ParticleCollider>[] _colliders =
        {
            new List<ParticleCollider>(),
            new List<ParticleCollider>(),
            new List<ParticleCollider>()
        };
        readonly List<ParticleColliderState> _colliderStates = new List<ParticleColliderState>();
        readonly List<ParticleMedium> _media = new List<ParticleMedium>();
        readonly List<ParticleMedium> _stepMedia = new List<ParticleMedium>();
        readonly List<ParticleVolume> _volumes = new List<ParticleVolume>();
        readonly List<ParticleMotionTarget> _motionTargets = new List<ParticleMotionTarget>();
        readonly ParticleGroupAllocator _groups = new ParticleGroupAllocator();
        readonly List<(IParticleExtension extension, bool add)> _extensionChanges =
            new List<(IParticleExtension extension, bool add)>();
        ParticleExtensionRunner _extensions;
        IParticleBackend _backend;
        ParticleRangeAllocator _particleSlots;
        ParticleRangeAllocator _constraintSlots;
        ParticleRangeAllocator _rigidGroupSlots;
        string _lastSettingsError;
        bool _destroyed;

        public static IReadOnlyList<ParticleWorld> ActiveWorlds => s_active;

        public ParticleSolverSettings Settings => _settings;
        public ParticleWorldCapacity Capacity => _capacity;

        public bool VerboseLogging
        {
            get => _verboseLogging;
            set => _verboseLogging = value;
        }

        public bool IsDestroyed => _destroyed;
        public int UsedParticles => Slots(ref _particleSlots, _capacity.particles).Used;
        public int UsedConstraints => Slots(ref _constraintSlots, _capacity.constraints).Used;
        public int UsedRigidGroups => Slots(ref _rigidGroupSlots, _capacity.rigidGroups).Used;

        /// <summary>
        ///   Particles of entities that are allocated, not parked and not asleep; the solver's
        ///   workload (ParticleWorld WORLD-10).
        /// </summary>
        public int ActiveParticleCount { get; private set; }

        /// <summary>
        ///   Particles of sleeping entities: not processed by any solver kernel, only present in
        ///   the neighbour structure as immovable contact partners (ParticleExtensions EXT-13).
        /// </summary>
        public int SleepingParticleCount { get; private set; }

        /// <summary>
        ///   Steps advanced with a valid time step since the world was created.
        /// </summary>
        public int StepCount { get; private set; }

        /// <summary>
        ///   Fluid regions a script adds, applied after every step in list order after the media of
        ///   the registered <see cref="ParticleVolume"/>s (ParticleSolver SOLVER-17, ParticleExtensions
        ///   EXT-14).
        /// </summary>
        public List<ParticleMedium> Media => _media;

        /// <summary>
        ///   Volumes registered with this world, in registration order.
        /// </summary>
        public IReadOnlyList<ParticleVolume> Volumes => _volumes;

        /// <summary>
        ///   Motion targets registered with this world, in registration order.
        /// </summary>
        public IReadOnlyList<ParticleMotionTarget> MotionTargets => _motionTargets;

        /// <summary>
        ///   The media in effect for the current step: the volumes' first, then
        ///   <see cref="Media"/>. Refreshed at the start of every step.
        /// </summary>
        public IReadOnlyList<ParticleMedium> StepMedia => _stepMedia;

        /// <summary>
        ///   The solver side of this world. Replacing it disposes the previous backend.
        /// </summary>
        internal IParticleBackend Backend
        {
            get => _backend;
            set
            {
                if (ReferenceEquals(_backend, value))
                    return;
                _backend?.Dispose();
                _backend = value;
            }
        }
        /// <summary>
        ///   Extensions in effect for the current step, in registration order (ParticleExtensions
        ///   EXT-01). Registrations made since the last step are not included yet.
        /// </summary>
        public IReadOnlyList<IParticleExtension> Extensions => Runner.Registered;

        ParticleExtensionRunner Runner => _extensions ?? (_extensions = new ParticleExtensionRunner(this));

        internal IReadOnlyList<ParticleWorldChange> PendingChanges => _pending;
        internal IReadOnlyList<ParticleColliderState> ColliderStates => _colliderStates;

        /// <summary>
        ///   Picks the world a source or collider uses: <paramref name="preferred"/> when it is
        ///   active, otherwise the only active world. Returns false with the reason when the
        ///   preferred world is inactive, or when no world or more than one is active. An inactive
        ///   world is never chosen (ParticleWorld WORLD-01).
        /// </summary>
        public static bool TryResolve(ParticleWorld preferred, out ParticleWorld world, out string error)
        {
            world = null;
            error = null;
            if (preferred != null)
            {
                if (s_active.Contains(preferred))
                {
                    world = preferred;
                    return true;
                }
                error = $"The assigned ParticleWorld '{preferred.name}' is not active.";
                return false;
            }
            if (s_active.Count == 1)
            {
                world = s_active[0];
                return true;
            }
            error = s_active.Count == 0
                ? "No active ParticleWorld in the loaded scenes."
                : "More than one active ParticleWorld; assign one explicitly: "
                  + string.Join(", ", s_active.Select(w => w.name));
            return false;
        }

        void OnEnable() => EnableWorld();
        void OnDisable() => DisableWorld();
        void OnDestroy() => DestroyWorld();
        void FixedUpdate() => Step(Time.fixedDeltaTime);

        internal void EnableWorld()
        {
            if (_destroyed || s_active.Contains(this))
                return;
            if (Backend == null)
                Backend = (BackendFactory ?? GpuParticleBackend.TryCreate)(this);
            s_active.Add(this);
            Log("enabled");
            ActiveWorldsChanged?.Invoke();
        }

        internal void DisableWorld()
        {
            if (!s_active.Remove(this))
                return;
            Log("disabled; paused with state kept");
            ActiveWorldsChanged?.Invoke();
        }

        internal void DestroyWorld()
        {
            if (_destroyed)
                return;
            s_active.Remove(this);
            _destroyed = true;
            foreach (EntityRecord record in _entities)
                record.alive = false;
            _entities.Clear();
            _freeIds.Clear();
            _pending.Clear();
            foreach (List<ParticleCollider> list in _colliders)
                list.Clear();
            _volumes.Clear();
            _motionTargets.Clear();
            _extensionChanges.Clear();
            Runner.Clear();
            ActiveParticleCount = 0;
            SleepingParticleCount = 0;
            Backend = null;
            ActiveWorldsChanged?.Invoke();
        }

        /// <summary>
        ///   Advances the world by one fixed step: queued changes and extension registrations take
        ///   effect first, collider poses and surface velocities are captured, and the solver and
        ///   extensions run only when some particle is active and the settings are valid
        ///   (ParticleSolver SOLVER-01, SOLVER-18; ParticleExtensions EXT-01, EXT-02).
        /// </summary>
        internal void Step(float deltaTime)
        {
            if (_destroyed || !(deltaTime > 0f) || float.IsInfinity(deltaTime))
                return;
            StepCount++;
            if (_pending.Count > 0)
            {
                Backend?.Apply(_pending);
                _pending.Clear();
            }
            ApplyExtensionChanges();
            CaptureColliders(deltaTime);
            if (ActiveParticleCount == 0 || Backend == null)
                return;
            if (!_settings.TryValidate(out string error))
            {
                if (error != _lastSettingsError)
                    Debug.LogError($"ParticleWorld '{name}' skipped its step: {error}", this);
                _lastSettingsError = error;
                return;
            }
            _lastSettingsError = null;
            CaptureMedia();
            ParticleExtensionRunner runner = Runner;
            runner.Prepare(Backend as IParticleDataBinder, StepCount, deltaTime, _settings);
            Backend.Step(deltaTime, _settings, _colliderStates, _stepMedia, ActiveParticleCount, runner);
        }

        /// <summary>
        ///   Registers <paramref name="extension"/> from the next step on, after the extensions
        ///   already registered in its stage. Registering it again keeps its place. Fails with the
        ///   reason for a null extension, a destroyed world, or a substep extension that declares
        ///   velocity writes, which the velocity update would overwrite (ParticleExtensions EXT-01,
        ///   EXT-04).
        /// </summary>
        public bool RegisterExtension(IParticleExtension extension, out string error)
        {
            error = null;
            if (extension == null)
                error = "The extension is null.";
            else if (_destroyed)
                error = $"ParticleWorld '{name}' is destroyed.";
            else if (extension.Stage == ParticleExtensionStage.Substep
                     && (extension.Writes & ParticleExtensionWrites.Velocities) != 0)
                error = $"Extension {extension.GetType().Name} runs every substep but declares velocity writes; "
                    + "the velocity update rebuilds velocities from positions in the same substep.";
            if (error != null)
                return false;
            _extensionChanges.Add((extension, true));
            return true;
        }

        /// <summary>
        ///   Stops calling <paramref name="extension"/> from the next step on.
        /// </summary>
        public void UnregisterExtension(IParticleExtension extension)
        {
            if (extension != null && !_destroyed)
                _extensionChanges.Add((extension, false));
        }

        /// <summary>
        ///   Binds this world's particle buffers to <paramref name="kernel"/> for work outside a
        ///   step, such as releasing held particles. Returns false when the world has no GPU data
        ///   (ParticleExtensions EXT-03).
        /// </summary>
        public bool TryBind(ComputeShader shader, int kernel)
        {
            if (_destroyed || shader == null || !(Backend is IParticleDataBinder binder))
                return false;
            binder.Bind(shader, kernel);
            return true;
        }

        /// <summary>
        ///   Binds this world's particle and rigid-group buffers to <paramref name="block"/> for a
        ///   shader that draws straight from the simulation, including rigid fragments posed on the
        ///   GPU. Returns false when the world has no GPU data (ParticleExtensions EXT-08).
        /// </summary>
        public bool TryBind(MaterialPropertyBlock block)
        {
            if (_destroyed || block == null || !(Backend is IParticleDataBinder binder))
                return false;
            binder.Bind(block);
            return true;
        }

        void ApplyExtensionChanges()
        {
            if (_extensionChanges.Count == 0)
                return;
            ParticleExtensionRunner runner = Runner;
            foreach ((IParticleExtension extension, bool add) in _extensionChanges)
            {
                if (add)
                    runner.Add(extension);
                else
                    runner.Remove(extension);
            }
            _extensionChanges.Clear();
        }

        /// <summary>
        ///   Requests the entity's simulated state without stalling the CPU (ParticleWorld
        ///   WORLD-14). <paramref name="onComplete"/> runs once the data arrives, or immediately with
        ///   <see cref="ParticleReadback.Succeeded"/> false when the world cannot read it.
        /// </summary>
        /// <summary>
        ///   Reads the positions of a run of particle slots in one asynchronous request (ParticleWorld
        ///   WORLD-14), for callers that watch many entities at once, such as draw bounds. The array
        ///   holds xyz and the inverse mass in w, indexed from <paramref name="particles"/>.start;
        ///   slots of other or released entities inside the run are included as they are. The step
        ///   number is the one the request was made after. The array is null when the world cannot
        ///   read it.
        /// </summary>
        public void RequestPositions(RangeInt particles, Action<Vector4[], int> onComplete)
        {
            int step = StepCount;
            if (_destroyed || Backend == null || particles.start < 0 || particles.length <= 0
                || particles.end > _capacity.particles)
            {
                onComplete?.Invoke(null, step);
                return;
            }
            Backend.RequestPositions(particles, positions => onComplete?.Invoke(positions, step));
        }

        public void RequestReadback(ParticleEntity entity, Action<ParticleReadback> onComplete)
        {
            var readback = new ParticleReadback { Entity = entity, Step = StepCount };
            if (!IsAlive(entity))
            {
                readback.Error = "The ParticleEntity was released or its world was destroyed.";
                onComplete?.Invoke(readback);
                return;
            }
            if (Backend == null)
            {
                readback.Error = $"ParticleWorld '{name}' has no solver backend.";
                onComplete?.Invoke(readback);
                return;
            }
            EntityRecord record = _entities[entity.Id];
            Backend.RequestReadback(record.particles, record.constraints, record.rigidGroups, readback, onComplete);
        }

        /// <summary>
        ///   Reserves a group number for <see cref="ParticleEntityRequest.sharedGroup"/>: entities in
        ///   it skip one another (ParticleSolver SOLVER-08). Release it with
        ///   <see cref="ReleaseGroup"/> once no entity uses it.
        /// </summary>
        public int AllocateGroup() => _groups.Allocate();

        public void ReleaseGroup(int group) => _groups.Release(group);

        public bool TryAllocate(ParticleEntityRequest request, out ParticleEntity entity, out string error)
        {
            var results = new List<ParticleEntity>(1);
            bool created = Allocate(new[] { request }, results, ParticleAllocationMode.AllOrNothing, out error) == 1;
            entity = created ? results[0] : default;
            return created;
        }

        /// <summary>
        ///   Allocates complete entities. Every request is validated first; an invalid one rejects
        ///   the whole call. Capacity is checked in call order: with
        ///   <see cref="ParticleAllocationMode.AllOrNothing"/> a shortfall creates nothing, with
        ///   <see cref="ParticleAllocationMode.BestEffort"/> it creates as many complete entities as
        ///   fit. The error names what was needed and what remained (ParticleWorld WORLD-04).
        ///   Returns the number of entities created, appended to <paramref name="results"/>.
        /// </summary>
        public int Allocate(
            IReadOnlyList<ParticleEntityRequest> requests,
            List<ParticleEntity> results,
            ParticleAllocationMode mode,
            out string error)
        {
            error = null;
            if (_destroyed)
            {
                error = $"ParticleWorld '{name}' is destroyed.";
                return 0;
            }
            if (requests == null || requests.Count == 0)
                return 0;
            for (int i = 0; i < requests.Count; i++)
            {
                if (requests[i] == null)
                {
                    error = $"Request {i} is null.";
                    return 0;
                }
                if (!requests[i].TryValidate(out string reason))
                {
                    error = $"Request {i} rejected: {reason}";
                    return 0;
                }
            }

            ParticleRangeAllocator particleSlots = Slots(ref _particleSlots, _capacity.particles);
            ParticleRangeAllocator constraintSlots = Slots(ref _constraintSlots, _capacity.constraints);
            ParticleRangeAllocator rigidGroupSlots = Slots(ref _rigidGroupSlots, _capacity.rigidGroups);
            var created = new List<EntityRecord>(requests.Count);
            for (int i = 0; i < requests.Count; i++)
            {
                ParticleEntityRequest request = requests[i];
                if (!TryReserve(request, particleSlots, constraintSlots, rigidGroupSlots, out EntityRecord record))
                {
                    int firstUnplaced = i;
                    if (mode == ParticleAllocationMode.AllOrNothing)
                    {
                        foreach (EntityRecord made in created)
                            Unreserve(made, particleSlots, constraintSlots, rigidGroupSlots);
                        created.Clear();
                        firstUnplaced = 0;
                    }
                    error = ShortfallMessage(requests, firstUnplaced, i, particleSlots, constraintSlots, rigidGroupSlots);
                    break;
                }
                created.Add(record);
            }

            foreach (EntityRecord record in created)
            {
                int id = _freeIds.Count > 0 ? _freeIds.Pop() : _entities.Count;
                if (id == _entities.Count)
                    _entities.Add(record);
                else
                {
                    record.generation = _entities[id].generation + 1;
                    _entities[id] = record;
                }
                ActiveParticleCount += record.particles.length;
                Enqueue(ParticleWorldChangeKind.Upload, id, record);
                results.Add(new ParticleEntity(this, id, record.generation));
            }
            if (created.Count > 0)
                Log($"allocated {created.Count} entities; {UsedParticles}/{_capacity.particles} particles in use");
            return created.Count;
        }

        internal bool IsAlive(ParticleEntity entity)
        {
            return !_destroyed
                && entity.Id >= 0
                && entity.Id < _entities.Count
                && _entities[entity.Id].alive
                && _entities[entity.Id].generation == entity.Generation;
        }

        internal EntityRecord RecordOf(ParticleEntity entity)
        {
            if (!IsAlive(entity))
                throw new InvalidOperationException("The ParticleEntity was released or its world was destroyed.");
            return _entities[entity.Id];
        }

        internal void Release(ParticleEntity entity)
        {
            if (!IsAlive(entity))
                return;
            EntityRecord record = _entities[entity.Id];
            Enqueue(ParticleWorldChangeKind.Release, entity.Id, record);
            Unreserve(record, _particleSlots, _constraintSlots, _rigidGroupSlots);
            if (record.sleeping)
                SleepingParticleCount -= record.particles.length;
            else if (!record.parked)
                ActiveParticleCount -= record.particles.length;
            record.alive = false;
            _freeIds.Push(entity.Id);
        }

        internal void Teleport(ParticleEntity entity, Pose pose, Vector3 velocity)
        {
            EntityRecord record = RecordOf(entity);
            Wake(entity.Id, record);
            ParticleWorldChange change = Enqueue(ParticleWorldChangeKind.Teleport, entity.Id, record);
            change.transformDelta =
                Matrix4x4.TRS(pose.position, pose.rotation, Vector3.one)
                * Matrix4x4.TRS(record.pose.position, record.pose.rotation, Vector3.one).inverse;
            change.velocity = velocity;
            change.pose = pose;
            record.pose = pose;
        }

        internal void Reset(ParticleEntity entity, Pose pose)
        {
            EntityRecord record = RecordOf(entity);
            Wake(entity.Id, record);
            record.pose = pose;
            Enqueue(ParticleWorldChangeKind.Reset, entity.Id, record).pose = pose;
        }

        internal void SetParked(ParticleEntity entity, bool parked)
        {
            EntityRecord record = RecordOf(entity);
            if (record.parked == parked)
                return;
            if (parked)
                Wake(entity.Id, record);
            record.parked = parked;
            ActiveParticleCount += parked ? -record.particles.length : record.particles.length;
            Enqueue(ParticleWorldChangeKind.SetParked, entity.Id, record).parked = parked;
        }

        internal void SetSleeping(ParticleEntity entity, bool sleeping)
        {
            EntityRecord record = RecordOf(entity);
            if (record.sleeping == sleeping || (sleeping && record.parked))
                return;
            record.sleeping = sleeping;
            int length = record.particles.length;
            ActiveParticleCount += sleeping ? -length : length;
            SleepingParticleCount += sleeping ? length : -length;
            Enqueue(ParticleWorldChangeKind.SetSleeping, entity.Id, record).sleeping = sleeping;
        }

        /// <summary>
        ///   Clears the entity's sleep right away, before the change that called for it is queued
        ///   (ParticleExtensions EXT-13).
        /// </summary>
        void Wake(int id, EntityRecord record)
        {
            if (!record.sleeping)
                return;
            record.sleeping = false;
            ActiveParticleCount += record.particles.length;
            SleepingParticleCount -= record.particles.length;
            Enqueue(ParticleWorldChangeKind.SetSleeping, id, record).sleeping = false;
        }

        internal void SetCategory(ParticleEntity entity, uint category)
        {
            EntityRecord record = RecordOf(entity);
            record.rest.category = category;
            Enqueue(ParticleWorldChangeKind.SetCategory, entity.Id, record).category = category;
        }

        internal void SetRadius(ParticleEntity entity, float radius)
        {
            if (!(radius > 0f) || float.IsInfinity(radius))
                throw new ArgumentOutOfRangeException(nameof(radius), radius, "A particle radius must be a finite value above 0.");
            EntityRecord record = RecordOf(entity);
            for (int i = 0; i < record.rest.particles.Count; i++)
            {
                ParticleSpawn spawn = record.rest.particles[i];
                spawn.radius = radius;
                record.rest.particles[i] = spawn;
            }
            Enqueue(ParticleWorldChangeKind.SetRadius, entity.Id, record).radius = radius;
        }

        internal bool SetConstraintParameters(
            ParticleEntity entity,
            IReadOnlyList<DistanceConstraintSpec> constraints,
            out string error)
        {
            EntityRecord record = RecordOf(entity);
            List<DistanceConstraintSpec> current = record.rest.constraints;
            error = null;
            if (constraints == null || constraints.Count != current.Count)
                error = $"The entity has {current.Count} constraints; {constraints?.Count ?? 0} were given.";
            else
                for (int i = 0; i < current.Count && error == null; i++)
                {
                    DistanceConstraintSpec spec = constraints[i];
                    if (spec.a != current[i].a || spec.b != current[i].b)
                        error = $"Constraint {i} joins {spec.a} and {spec.b}; the entity's joins {current[i].a} and {current[i].b}.";
                    else if (spec.compliance < 0f || spec.damping < 0f || spec.breakForce < 0f
                             || float.IsInfinity(spec.compliance) || float.IsInfinity(spec.damping) || float.IsInfinity(spec.breakForce)
                             || (!float.IsNaN(spec.restLength) && (spec.restLength < 0f || float.IsInfinity(spec.restLength))))
                        error = $"Constraint {i} has a negative or non-finite parameter.";
                }
            if (error != null)
                return false;
            for (int i = 0; i < current.Count; i++)
            {
                DistanceConstraintSpec spec = constraints[i];
                if (float.IsNaN(spec.restLength))
                    spec.restLength = current[i].restLength;
                current[i] = spec;
            }
            Enqueue(ParticleWorldChangeKind.SetConstraints, entity.Id, record);
            return true;
        }

        internal void SetGroupMode(ParticleEntity entity, ParticleGroupMode mode)
        {
            EntityRecord record = RecordOf(entity);
            if (record.rest.groupMode == mode && record.rest.sharedGroup <= 0)
                return;
            if (record.groupOwned)
                _groups.Release(record.group);
            record.rest.sharedGroup = 0;
            record.groupOwned = mode == ParticleGroupMode.NewGroup;
            record.group = record.groupOwned ? _groups.Allocate() : 0;
            record.rest.groupMode = mode;
            Enqueue(ParticleWorldChangeKind.SetGroup, entity.Id, record);
        }

        /// <summary>
        ///   Adds a collider in registration order. Fails with an error when its shape's capacity
        ///   is full instead of dropping it (ParticleWorld WORLD-04, WORLD-15).
        /// </summary>
        internal bool RegisterCollider(ParticleCollider collider, out string error)
        {
            error = null;
            if (_destroyed)
            {
                error = $"ParticleWorld '{name}' is destroyed.";
                return false;
            }
            List<ParticleCollider> list = _colliders[(int)collider.Shape];
            if (list.Contains(collider))
                return true;
            int limit = _capacity.ColliderLimit(collider.Shape);
            if (list.Count >= limit)
            {
                error = $"ParticleWorld '{name}' already holds {limit} {collider.Shape} colliders; '{collider.name}' was not registered.";
                return false;
            }
            list.Add(collider);
            Log($"registered {collider.Shape} collider '{collider.name}'");
            return true;
        }

        internal void UnregisterCollider(ParticleCollider collider)
        {
            if (_colliders[(int)collider.Shape].Remove(collider))
                Log($"unregistered {collider.Shape} collider '{collider.name}'");
        }

        internal int ColliderCount(ParticleColliderShape shape) => _colliders[(int)shape].Count;

        /// <summary>
        ///   Adds a volume in registration order; registering it again keeps its place
        ///   (ParticleExtensions EXT-14, ParticleWorld WORLD-02).
        /// </summary>
        internal bool RegisterVolume(ParticleVolume volume, out string error)
        {
            error = null;
            if (_destroyed)
            {
                error = $"ParticleWorld '{name}' is destroyed.";
                return false;
            }
            if (!_volumes.Contains(volume))
            {
                _volumes.Add(volume);
                Log($"registered volume '{volume.name}'");
            }
            return true;
        }

        internal void UnregisterVolume(ParticleVolume volume)
        {
            if (_volumes.Remove(volume))
                Log($"unregistered volume '{volume.name}'");
        }

        /// <summary>
        ///   Adds a motion target in registration order (ParticleExtensions EXT-12).
        /// </summary>
        internal bool RegisterMotionTarget(ParticleMotionTarget target, out string error)
        {
            error = null;
            if (_destroyed)
            {
                error = $"ParticleWorld '{name}' is destroyed.";
                return false;
            }
            if (!_motionTargets.Contains(target))
            {
                _motionTargets.Add(target);
                Log($"registered motion target '{target.name}'");
            }
            return true;
        }

        internal void UnregisterMotionTarget(ParticleMotionTarget target)
        {
            if (_motionTargets.Remove(target))
                Log($"unregistered motion target '{target.name}'");
        }

        void CaptureMedia()
        {
            _stepMedia.Clear();
            foreach (ParticleVolume volume in _volumes)
                if (volume != null)
                    volume.AppendMedia(_stepMedia);
            _stepMedia.AddRange(_media);
        }

        void CaptureColliders(float deltaTime)
        {
            _colliderStates.Clear();
            foreach (List<ParticleCollider> list in _colliders)
                foreach (ParticleCollider collider in list)
                    if (collider != null)
                        _colliderStates.Add(collider.Capture(deltaTime));
        }

        bool TryReserve(
            ParticleEntityRequest request,
            ParticleRangeAllocator particleSlots,
            ParticleRangeAllocator constraintSlots,
            ParticleRangeAllocator rigidGroupSlots,
            out EntityRecord record)
        {
            record = null;
            if (!particleSlots.TryAllocate(request.particles.Count, out RangeInt particles))
                return false;
            if (!constraintSlots.TryAllocate(request.constraints.Count, out RangeInt constraints))
            {
                particleSlots.Free(particles);
                return false;
            }
            if (!rigidGroupSlots.TryAllocate(request.rigidGroups.Count, out RangeInt rigidGroups))
            {
                particleSlots.Free(particles);
                constraintSlots.Free(constraints);
                return false;
            }
            ParticleEntityRequest rest = Snapshot(request);
            bool owned = rest.sharedGroup <= 0 && rest.groupMode == ParticleGroupMode.NewGroup;
            record = new EntityRecord
            {
                alive = true,
                particles = particles,
                constraints = constraints,
                rigidGroups = rigidGroups,
                group = rest.sharedGroup > 0 ? rest.sharedGroup : owned ? _groups.Allocate() : 0,
                groupOwned = owned,
                pose = rest.pose,
                rest = rest
            };
            return true;
        }

        void Unreserve(
            EntityRecord record,
            ParticleRangeAllocator particleSlots,
            ParticleRangeAllocator constraintSlots,
            ParticleRangeAllocator rigidGroupSlots)
        {
            particleSlots.Free(record.particles);
            constraintSlots.Free(record.constraints);
            rigidGroupSlots.Free(record.rigidGroups);
            if (record.groupOwned)
                _groups.Release(record.group);
        }

        /// <summary>
        ///   Copies a request so later edits by its owner cannot change the entity, and fixes every
        ///   unspecified rest length to the distance between its two particles at rest.
        /// </summary>
        static ParticleEntityRequest Snapshot(ParticleEntityRequest request)
        {
            var copy = new ParticleEntityRequest
            {
                groupMode = request.groupMode,
                sharedGroup = request.sharedGroup,
                category = request.category,
                pose = request.pose
            };
            copy.particles.AddRange(request.particles);
            copy.roles.AddRange(request.roles);
            foreach (int[] members in request.rigidGroups)
                copy.rigidGroups.Add((int[])members.Clone());
            foreach (DistanceConstraintSpec spec in request.constraints)
            {
                DistanceConstraintSpec fixedSpec = spec;
                if (float.IsNaN(fixedSpec.restLength))
                    fixedSpec.restLength = Vector3.Distance(
                        request.particles[spec.a].position,
                        request.particles[spec.b].position);
                copy.constraints.Add(fixedSpec);
            }
            return copy;
        }

        ParticleWorldChange Enqueue(ParticleWorldChangeKind kind, int id, EntityRecord record)
        {
            var change = new ParticleWorldChange
            {
                kind = kind,
                entityId = id,
                particles = record.particles,
                constraints = record.constraints,
                rigidGroups = record.rigidGroups,
                group = record.group,
                request = record.rest,
                pose = record.pose,
                parked = record.parked,
                sleeping = record.sleeping,
                category = record.rest.category
            };
            _pending.Add(change);
            return change;
        }

        /// <summary>
        ///   Describes a capacity shortfall: what the entities from <paramref name="firstUnplaced"/>
        ///   on need, and what remains after the call. Remaining space can be fragmented, so a need
        ///   below the remaining total can still fail.
        /// </summary>
        string ShortfallMessage(
            IReadOnlyList<ParticleEntityRequest> requests,
            int firstUnplaced,
            int failedIndex,
            ParticleRangeAllocator particleSlots,
            ParticleRangeAllocator constraintSlots,
            ParticleRangeAllocator rigidGroupSlots)
        {
            int particles = 0, constraints = 0, rigidGroups = 0;
            for (int i = firstUnplaced; i < requests.Count; i++)
            {
                particles += requests[i].particles.Count;
                constraints += requests[i].constraints.Count;
                rigidGroups += requests[i].rigidGroups.Count;
            }
            return $"ParticleWorld '{name}' could not place entity {failedIndex} of {requests.Count}; "
                + $"entities {firstUnplaced} to {requests.Count - 1} needs {particles} particles, {constraints} constraints "
                + $"and {rigidGroups} rigid groups; {particleSlots.Remaining} particles, {constraintSlots.Remaining} constraints "
                + $"and {rigidGroupSlots.Remaining} rigid groups remain.";
        }

        static ParticleRangeAllocator Slots(ref ParticleRangeAllocator slots, int capacity)
        {
            if (slots == null)
                slots = new ParticleRangeAllocator(capacity);
            return slots;
        }

        void Log(string message)
        {
            if (_verboseLogging)
                Debug.Log($"ParticleWorld '{name}': {message}", this);
        }
    }
}

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
            public bool parked;
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
        readonly ParticleGroupAllocator _groups = new ParticleGroupAllocator();
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
        ///   Particles of entities that are allocated and not parked; the solver's workload
        ///   (ParticleWorld WORLD-10).
        /// </summary>
        public int ActiveParticleCount { get; private set; }

        /// <summary>
        ///   Steps advanced with a valid time step since the world was created.
        /// </summary>
        public int StepCount { get; private set; }

        /// <summary>
        ///   Fluid regions applied after every step in list order (ParticleSolver SOLVER-17).
        /// </summary>
        public List<ParticleMedium> Media => _media;

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
            ActiveParticleCount = 0;
            Backend = null;
            ActiveWorldsChanged?.Invoke();
        }

        /// <summary>
        ///   Advances the world by one fixed step: queued changes reach the backend first, collider
        ///   poses and surface velocities are captured, and the solver runs only when some particle
        ///   is active and the settings are valid (ParticleSolver SOLVER-01, SOLVER-18).
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
            Backend.Step(deltaTime, _settings, _colliderStates, _media, ActiveParticleCount);
        }

        /// <summary>
        ///   Requests the entity's simulated state without stalling the CPU (ParticleWorld
        ///   WORLD-14). <paramref name="onComplete"/> runs once the data arrives, or immediately with
        ///   <see cref="ParticleReadback.Succeeded"/> false when the world cannot read it.
        /// </summary>
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
            if (!record.parked)
                ActiveParticleCount -= record.particles.length;
            record.alive = false;
            _freeIds.Push(entity.Id);
        }

        internal void Teleport(ParticleEntity entity, Pose pose, Vector3 velocity)
        {
            EntityRecord record = RecordOf(entity);
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
            record.pose = pose;
            Enqueue(ParticleWorldChangeKind.Reset, entity.Id, record).pose = pose;
        }

        internal void SetParked(ParticleEntity entity, bool parked)
        {
            EntityRecord record = RecordOf(entity);
            if (record.parked == parked)
                return;
            record.parked = parked;
            ActiveParticleCount += parked ? -record.particles.length : record.particles.length;
            Enqueue(ParticleWorldChangeKind.SetParked, entity.Id, record).parked = parked;
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

        internal void SetGroupMode(ParticleEntity entity, ParticleGroupMode mode)
        {
            EntityRecord record = RecordOf(entity);
            if (record.rest.groupMode == mode)
                return;
            _groups.Release(record.group);
            record.group = mode == ParticleGroupMode.NewGroup ? _groups.Allocate() : 0;
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
            record = new EntityRecord
            {
                alive = true,
                particles = particles,
                constraints = constraints,
                rigidGroups = rigidGroups,
                group = rest.groupMode == ParticleGroupMode.NewGroup ? _groups.Allocate() : 0,
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

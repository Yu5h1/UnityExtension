using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace Yu5h1Lib
{
    /// <summary>
    ///   GPU solver behind a <see cref="ParticleWorld"/>, running its own copy of the compute
    ///   shader Resources/Yu5h1LibParticleSolver (which implements ParticleSolver 1.0.1) so worlds
    ///   never share uniform state. Storage is sized
    ///   from the world's capacity when the world is first enabled. Entity changes are written to
    ///   their own slots only, so other particles are never disturbed (ParticleWorld WORLD-06);
    ///   every kernel walks compact lists of the active particles, constraints and rigid groups,
    ///   rebuilt whenever entities are added, released, parked or resized (WORLD-10). State leaves
    ///   the GPU only through asynchronous readback (WORLD-14).
    /// </summary>
    internal sealed class GpuParticleBackend : IParticleBackend
    {
        const string ShaderName = "Yu5h1LibParticleSolver";
        const int Threads = 64;

        [StructLayout(LayoutKind.Sequential)]
        struct GpuMeta
        {
            public uint group;
            public uint category;
            public uint flags;
            public float radius;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct GpuConstraint
        {
            public int a;
            public int b;
            public float restLength;
            public float compliance;
            public float damping;
            public float breakForce;
            public uint broken;
            public uint padding;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct GpuGroup
        {
            public uint memberStart;
            public uint memberCount;
            public uint padding0;
            public uint padding1;
            public Vector4 rotation;
            public Vector4 centroid;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct GpuCollider
        {
            public uint shape;
            public uint mask;
            public uint padding0;
            public uint padding1;
            public Vector4 center;
            public Vector4 pointA;
            public Vector4 pointB;
            public Vector4 halfExtents;
            public Vector4 axisX;
            public Vector4 axisY;
            public Vector4 axisZ;
            public Vector4 linearVelocity;
            public Vector4 angularVelocity;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct GpuMedium
        {
            public Vector4 center;
            public Vector4 halfExtents;
            public Vector4 axisX;
            public Vector4 axisY;
            public Vector4 axisZ;
            public Vector4 flow;
            public Vector4 parameters;
        }

        sealed class Entity
        {
            public RangeInt particles;
            public RangeInt constraints;
            public RangeInt rigidGroups;
            public RangeInt members;
            public int group;
            public uint category;
            public bool parked;
            public ParticleEntityRequest rest;
        }

        static readonly string[] KernelNames =
        {
            "Predict", "ClearAccumulators", "SolveDistance", "SolveContacts", "ApplyAccumulated",
            "CollideShapes", "MatchRigidGroups", "UpdateVelocities", "ClearCells", "InsertCells",
            "ApplyMedia", "TransformRange", "TransformGroups"
        };

        readonly ComputeShader _shader;
        readonly int[] _kernels = new int[KernelNames.Length];
        readonly Dictionary<int, Entity> _entities = new Dictionary<int, Entity>();
        readonly ParticleRangeAllocator _memberSlots;
        readonly List<uint> _activeParticles = new List<uint>();
        readonly List<uint> _activeConstraints = new List<uint>();
        readonly List<uint> _activeGroups = new List<uint>();
        readonly List<ComputeBuffer> _buffers = new List<ComputeBuffer>();
        readonly ComputeBuffer _position;
        readonly ComputeBuffer _previous;
        readonly ComputeBuffer _velocity;
        readonly ComputeBuffer _meta;
        readonly ComputeBuffer _accumulated;
        readonly ComputeBuffer _accumulatedCount;
        readonly ComputeBuffer _activeParticleBuffer;
        readonly ComputeBuffer _activeConstraintBuffer;
        readonly ComputeBuffer _activeGroupBuffer;
        readonly ComputeBuffer _constraints;
        readonly ComputeBuffer _groups;
        readonly ComputeBuffer _groupMembers;
        readonly ComputeBuffer _groupOffsets;
        readonly ComputeBuffer _cellHead;
        readonly ComputeBuffer _cellNext;
        readonly ComputeBuffer _particleCell;
        readonly int _cellCount;
        ComputeBuffer _colliders;
        ComputeBuffer _media;
        GpuCollider[] _colliderData = Array.Empty<GpuCollider>();
        GpuMedium[] _mediumData = Array.Empty<GpuMedium>();
        float _maxRadius;
        bool _listsDirty;
        bool _disposed;

        /// <summary>
        ///   Creates the backend, or returns null with a logged reason when the platform has no
        ///   compute shaders or the shader asset is missing.
        /// </summary>
        public static IParticleBackend TryCreate(ParticleWorld world)
        {
            if (!SystemInfo.supportsComputeShaders)
            {
                Debug.LogWarning($"ParticleWorld '{world.name}': this platform has no compute shaders; the world will not simulate.", world);
                return null;
            }
            var shader = Resources.Load<ComputeShader>(ShaderName);
            if (shader == null)
            {
                Debug.LogError($"ParticleWorld '{world.name}': compute shader Resources/{ShaderName} is missing.", world);
                return null;
            }
            return new GpuParticleBackend(shader, world.Capacity);
        }

        GpuParticleBackend(ComputeShader shader, ParticleWorldCapacity capacity)
        {
            _shader = UnityEngine.Object.Instantiate(shader);
            _shader.hideFlags = HideFlags.HideAndDontSave;
            for (int k = 0; k < KernelNames.Length; k++)
                _kernels[k] = shader.FindKernel(KernelNames[k]);
            int particles = Mathf.Max(1, capacity.particles);
            int constraints = Mathf.Max(1, capacity.constraints);
            int groups = Mathf.Max(1, capacity.rigidGroups);
            _cellCount = Mathf.NextPowerOfTwo(Mathf.Max(1024, particles * 2));
            _memberSlots = new ParticleRangeAllocator(particles);
            _position = Create(particles, 16);
            _previous = Create(particles, 16);
            _velocity = Create(particles, 16);
            _meta = Create(particles, Marshal.SizeOf<GpuMeta>());
            _accumulated = Create(particles * 3, 4);
            _accumulatedCount = Create(particles, 4);
            _activeParticleBuffer = Create(particles, 4);
            _activeConstraintBuffer = Create(constraints, 4);
            _activeGroupBuffer = Create(groups, 4);
            _constraints = Create(constraints, Marshal.SizeOf<GpuConstraint>());
            _groups = Create(groups, Marshal.SizeOf<GpuGroup>());
            _groupMembers = Create(particles, 4);
            _groupOffsets = Create(particles, 16);
            _cellHead = Create(_cellCount, 4);
            _cellNext = Create(particles, 4);
            _particleCell = Create(particles, 16);
            _colliders = Create(1, Marshal.SizeOf<GpuCollider>());
            _media = Create(1, Marshal.SizeOf<GpuMedium>());
            BindAll();
        }

        public void Apply(IReadOnlyList<ParticleWorldChange> changes)
        {
            foreach (ParticleWorldChange change in changes)
            {
                switch (change.kind)
                {
                    case ParticleWorldChangeKind.Upload:
                        Upload(change);
                        break;
                    case ParticleWorldChangeKind.Release:
                        if (_entities.TryGetValue(change.entityId, out Entity released))
                        {
                            _memberSlots.Free(released.members);
                            _entities.Remove(change.entityId);
                        }
                        _listsDirty = true;
                        break;
                    case ParticleWorldChangeKind.Teleport:
                        Teleport(change);
                        break;
                    case ParticleWorldChangeKind.Reset:
                        if (_entities.TryGetValue(change.entityId, out Entity reset))
                            WriteRest(reset, change.pose, writeMembers: false);
                        break;
                    case ParticleWorldChangeKind.SetParked:
                        if (_entities.TryGetValue(change.entityId, out Entity parked))
                            parked.parked = change.parked;
                        _listsDirty = true;
                        break;
                    case ParticleWorldChangeKind.SetCategory:
                    case ParticleWorldChangeKind.SetRadius:
                    case ParticleWorldChangeKind.SetGroup:
                        if (_entities.TryGetValue(change.entityId, out Entity entity))
                        {
                            entity.group = change.group;
                            entity.category = change.category;
                            WriteMeta(entity);
                        }
                        _listsDirty = true;
                        break;
                }
            }
            if (_listsDirty)
                RebuildActiveLists();
        }

        public void Step(
            float deltaTime,
            ParticleSolverSettings settings,
            IReadOnlyList<ParticleColliderState> colliders,
            IReadOnlyList<ParticleMedium> media,
            int activeParticleCount)
        {
            if (_disposed || _activeParticles.Count == 0)
                return;
            UploadColliders(colliders);
            UploadMedia(media);
            float substep = deltaTime / settings.substeps;
            _shader.SetFloat("_Substep", substep);
            _shader.SetFloat("_StepTime", deltaTime);
            _shader.SetVector("_Gravity", settings.gravity);
            _shader.SetFloat("_Damping", settings.damping);
            _shader.SetFloat("_Sor", settings.sor);
            _shader.SetFloat("_FrictionStatic", settings.frictionStatic);
            _shader.SetFloat("_FrictionKinetic", settings.frictionKinetic);
            _shader.SetFloat("_MaxDepenetrationSpeed", settings.maxDepenetrationSpeed);
            _shader.SetFloat("_GroundY", settings.groundY);
            _shader.SetInt("_GroundMask", unchecked((int)settings.groundMask));
            _shader.SetFloat("_CellSize", Mathf.Max(2f * _maxRadius, 1e-4f));
            _shader.SetInt("_CellCount", _cellCount);

            int particles = _activeParticles.Count;
            for (int s = 0; s < settings.substeps; s++)
            {
                Dispatch(0, particles);
                Dispatch(8, _cellCount);
                Dispatch(9, particles);
                Dispatch(1, particles);
                Dispatch(2, _activeConstraints.Count);
                Dispatch(3, particles);
                Dispatch(4, particles);
                Dispatch(5, particles);
                Dispatch(6, _activeGroups.Count);
                Dispatch(7, particles);
            }
            if (_mediumData.Length > 0 && media.Count > 0)
                Dispatch(10, particles);
        }

        public void RequestReadback(
            RangeInt particles,
            RangeInt constraints,
            RangeInt rigidGroups,
            ParticleReadback readback,
            Action<ParticleReadback> onComplete)
        {
            if (_disposed)
            {
                readback.Error = "The solver backend was disposed.";
                onComplete?.Invoke(readback);
                return;
            }
            int pending = 2 + (constraints.length > 0 ? 1 : 0) + (rigidGroups.length > 0 ? 1 : 0);
            bool failed = false;
            void Finish(AsyncGPUReadbackRequest request, Action<AsyncGPUReadbackRequest> fill)
            {
                if (request.hasError)
                    failed = true;
                else
                    fill(request);
                if (--pending > 0)
                    return;
                readback.Succeeded = !failed;
                if (failed)
                {
                    readback.Error = "GPU readback failed.";
                    readback.Positions = Array.Empty<Vector3>();
                    readback.Velocities = Array.Empty<Vector3>();
                    readback.BrokenConstraints = Array.Empty<bool>();
                    readback.RigidRotations = Array.Empty<Quaternion>();
                }
                onComplete?.Invoke(readback);
            }

            AsyncGPUReadback.Request(_position, particles.length * 16, particles.start * 16, r => Finish(r, done =>
                readback.Positions = ToVectors(done.GetData<Vector4>())));
            AsyncGPUReadback.Request(_velocity, particles.length * 16, particles.start * 16, r => Finish(r, done =>
                readback.Velocities = ToVectors(done.GetData<Vector4>())));
            if (constraints.length > 0)
            {
                int stride = Marshal.SizeOf<GpuConstraint>();
                AsyncGPUReadback.Request(_constraints, constraints.length * stride, constraints.start * stride, r => Finish(r, done =>
                {
                    var data = done.GetData<GpuConstraint>();
                    var broken = new bool[data.Length];
                    for (int i = 0; i < data.Length; i++)
                        broken[i] = data[i].broken != 0u;
                    readback.BrokenConstraints = broken;
                }));
            }
            if (rigidGroups.length > 0)
            {
                int stride = Marshal.SizeOf<GpuGroup>();
                AsyncGPUReadback.Request(_groups, rigidGroups.length * stride, rigidGroups.start * stride, r => Finish(r, done =>
                {
                    var data = done.GetData<GpuGroup>();
                    var rotations = new Quaternion[data.Length];
                    for (int i = 0; i < data.Length; i++)
                        rotations[i] = new Quaternion(data[i].rotation.x, data[i].rotation.y, data[i].rotation.z, data[i].rotation.w);
                    readback.RigidRotations = rotations;
                }));
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            foreach (ComputeBuffer buffer in _buffers)
                buffer?.Release();
            _buffers.Clear();
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(_shader);
            else
                UnityEngine.Object.DestroyImmediate(_shader);
        }

        void Upload(ParticleWorldChange change)
        {
            ParticleEntityRequest rest = change.request;
            var entity = new Entity
            {
                particles = change.particles,
                constraints = change.constraints,
                rigidGroups = change.rigidGroups,
                group = change.group,
                category = change.category,
                rest = rest
            };
            int memberCount = 0;
            foreach (int[] members in rest.rigidGroups)
                memberCount += members.Length;
            if (!_memberSlots.TryAllocate(memberCount, out entity.members))
            {
                Debug.LogError($"Particle solver: no room for {memberCount} rigid group members; the entity's rigid groups will not hold their shape.");
                entity.members = new RangeInt(0, 0);
            }
            _entities[change.entityId] = entity;
            WriteRest(entity, change.pose, writeMembers: true);
            _listsDirty = true;
        }

        /// <summary>
        ///   Places the entity's rest layout at <paramref name="pose"/>: positions and previous
        ///   positions, velocities, metadata, unbroken constraints and rigid groups at the pose
        ///   rotation. Member lists and rest offsets are written only on upload.
        /// </summary>
        void WriteRest(Entity entity, Pose pose, bool writeMembers)
        {
            ParticleEntityRequest rest = entity.rest;
            int count = entity.particles.length;
            var positions = new Vector4[count];
            var velocities = new Vector4[count];
            for (int i = 0; i < count; i++)
            {
                ParticleSpawn spawn = rest.particles[i];
                Vector3 world = pose.position + pose.rotation * spawn.position;
                positions[i] = new Vector4(world.x, world.y, world.z, spawn.invMass);
                Vector3 velocity = pose.rotation * spawn.velocity;
                velocities[i] = new Vector4(velocity.x, velocity.y, velocity.z, 0f);
            }
            _position.SetData(positions, 0, entity.particles.start, count);
            _previous.SetData(positions, 0, entity.particles.start, count);
            _velocity.SetData(velocities, 0, entity.particles.start, count);
            WriteMeta(entity);

            if (entity.constraints.length > 0)
            {
                var constraints = new GpuConstraint[entity.constraints.length];
                for (int c = 0; c < constraints.Length; c++)
                {
                    DistanceConstraintSpec spec = rest.constraints[c];
                    constraints[c] = new GpuConstraint
                    {
                        a = entity.particles.start + spec.a,
                        b = entity.particles.start + spec.b,
                        restLength = spec.restLength,
                        compliance = spec.compliance,
                        damping = spec.damping,
                        breakForce = spec.breakForce
                    };
                }
                _constraints.SetData(constraints, 0, entity.constraints.start, constraints.Length);
            }

            if (entity.rigidGroups.length == 0)
                return;
            var groups = new GpuGroup[entity.rigidGroups.length];
            var members = writeMembers ? new uint[entity.members.length] : null;
            var offsets = writeMembers ? new Vector4[entity.members.length] : null;
            int memberCursor = 0;
            bool hasMembers = entity.members.length > 0;
            for (int g = 0; g < groups.Length; g++)
            {
                int[] indices = rest.rigidGroups[g];
                float totalMass = 0f;
                Vector3 centroid = Vector3.zero;
                foreach (int index in indices)
                {
                    float invMass = rest.particles[index].invMass;
                    float mass = invMass > 0f ? 1f / invMass : 0f;
                    totalMass += mass;
                    centroid += mass * rest.particles[index].position;
                }
                if (totalMass > 0f)
                    centroid /= totalMass;
                if (writeMembers && hasMembers)
                    for (int m = 0; m < indices.Length; m++)
                    {
                        members[memberCursor + m] = (uint)(entity.particles.start + indices[m]);
                        Vector3 offset = rest.particles[indices[m]].position - centroid;
                        offsets[memberCursor + m] = new Vector4(offset.x, offset.y, offset.z, 0f);
                    }
                Vector3 worldCentroid = pose.position + pose.rotation * centroid;
                groups[g] = new GpuGroup
                {
                    memberStart = hasMembers ? (uint)(entity.members.start + memberCursor) : 0u,
                    memberCount = hasMembers ? (uint)indices.Length : 0u,
                    rotation = new Vector4(pose.rotation.x, pose.rotation.y, pose.rotation.z, pose.rotation.w),
                    centroid = new Vector4(worldCentroid.x, worldCentroid.y, worldCentroid.z, 0f)
                };
                memberCursor += indices.Length;
            }
            if (writeMembers && hasMembers)
            {
                _groupMembers.SetData(members, 0, entity.members.start, members.Length);
                _groupOffsets.SetData(offsets, 0, entity.members.start, offsets.Length);
            }
            _groups.SetData(groups, 0, entity.rigidGroups.start, groups.Length);
        }

        void WriteMeta(Entity entity)
        {
            int count = entity.particles.length;
            var meta = new GpuMeta[count];
            for (int i = 0; i < count; i++)
                meta[i] = new GpuMeta
                {
                    group = (uint)entity.group,
                    category = entity.category,
                    flags = 1u,
                    radius = entity.rest.particles[i].radius
                };
            _meta.SetData(meta, 0, entity.particles.start, count);
        }

        void Teleport(ParticleWorldChange change)
        {
            if (!_entities.TryGetValue(change.entityId, out Entity entity))
                return;
            _shader.SetMatrix("_RangeTransform", change.transformDelta);
            Quaternion rotation = change.transformDelta.rotation;
            _shader.SetVector("_RangeRotation", new Vector4(rotation.x, rotation.y, rotation.z, rotation.w));
            _shader.SetVector("_RangeVelocity", change.velocity);
            _shader.SetInt("_RangeStart", entity.particles.start);
            _shader.SetInt("_RangeCount", entity.particles.length);
            Dispatch(11, entity.particles.length);
            if (entity.rigidGroups.length == 0)
                return;
            _shader.SetInt("_RangeStart", entity.rigidGroups.start);
            _shader.SetInt("_RangeCount", entity.rigidGroups.length);
            Dispatch(12, entity.rigidGroups.length);
        }

        void RebuildActiveLists()
        {
            _listsDirty = false;
            _activeParticles.Clear();
            _activeConstraints.Clear();
            _activeGroups.Clear();
            _maxRadius = 0f;
            foreach (Entity entity in _entities.Values)
            {
                if (entity.parked)
                    continue;
                for (int i = 0; i < entity.particles.length; i++)
                {
                    _activeParticles.Add((uint)(entity.particles.start + i));
                    _maxRadius = Mathf.Max(_maxRadius, entity.rest.particles[i].radius);
                }
                for (int c = 0; c < entity.constraints.length; c++)
                    _activeConstraints.Add((uint)(entity.constraints.start + c));
                if (entity.members.length > 0)
                    for (int g = 0; g < entity.rigidGroups.length; g++)
                        _activeGroups.Add((uint)(entity.rigidGroups.start + g));
            }
            _activeParticles.Sort();
            if (_activeParticles.Count > 0)
                _activeParticleBuffer.SetData(_activeParticles);
            if (_activeConstraints.Count > 0)
                _activeConstraintBuffer.SetData(_activeConstraints);
            if (_activeGroups.Count > 0)
                _activeGroupBuffer.SetData(_activeGroups);
            _shader.SetInt("_ActiveParticleCount", _activeParticles.Count);
            _shader.SetInt("_ActiveConstraintCount", _activeConstraints.Count);
            _shader.SetInt("_ActiveGroupCount", _activeGroups.Count);
        }

        void UploadColliders(IReadOnlyList<ParticleColliderState> colliders)
        {
            int count = colliders?.Count ?? 0;
            if (_colliderData.Length != count)
                _colliderData = new GpuCollider[count];
            for (int k = 0; k < count; k++)
            {
                ParticleColliderState c = colliders[k];
                _colliderData[k] = new GpuCollider
                {
                    shape = (uint)c.shape,
                    mask = c.mask,
                    center = new Vector4(c.center.x, c.center.y, c.center.z, c.radius),
                    pointA = c.pointA,
                    pointB = c.pointB,
                    halfExtents = c.halfExtents,
                    axisX = c.axisX,
                    axisY = c.axisY,
                    axisZ = c.axisZ,
                    linearVelocity = c.linearVelocity,
                    angularVelocity = c.angularVelocity
                };
            }
            if (count > _colliders.count)
            {
                Replace(ref _colliders, count, Marshal.SizeOf<GpuCollider>());
                BindAll();
            }
            if (count > 0)
                _colliders.SetData(_colliderData);
            _shader.SetInt("_ColliderCount", count);
        }

        void UploadMedia(IReadOnlyList<ParticleMedium> media)
        {
            int count = media?.Count ?? 0;
            if (_mediumData.Length != count)
                _mediumData = new GpuMedium[count];
            for (int k = 0; k < count; k++)
            {
                ParticleMedium m = media[k];
                Quaternion rotation = m.rotation.x == 0f && m.rotation.y == 0f && m.rotation.z == 0f && m.rotation.w == 0f
                    ? Quaternion.identity
                    : m.rotation;
                _mediumData[k] = new GpuMedium
                {
                    center = m.center,
                    halfExtents = m.halfExtents,
                    axisX = rotation * Vector3.right,
                    axisY = rotation * Vector3.up,
                    axisZ = rotation * Vector3.forward,
                    flow = new Vector4(m.flow.x, m.flow.y, m.flow.z, m.density),
                    parameters = new Vector4(m.dragCoefficient, 0f, 0f, 0f)
                };
            }
            if (count > _media.count)
            {
                Replace(ref _media, count, Marshal.SizeOf<GpuMedium>());
                BindAll();
            }
            if (count > 0)
                _media.SetData(_mediumData);
            _shader.SetInt("_MediumCount", count);
        }

        void Dispatch(int kernel, int threads)
        {
            if (threads <= 0)
                return;
            _shader.Dispatch(_kernels[kernel], (threads + Threads - 1) / Threads, 1, 1);
        }

        ComputeBuffer Create(int count, int stride)
        {
            var buffer = new ComputeBuffer(Mathf.Max(1, count), stride);
            _buffers.Add(buffer);
            return buffer;
        }

        void Replace(ref ComputeBuffer buffer, int count, int stride)
        {
            _buffers.Remove(buffer);
            buffer.Release();
            buffer = Create(count, stride);
        }

        void BindAll()
        {
            foreach (int kernel in _kernels)
            {
                _shader.SetBuffer(kernel, "_Position", _position);
                _shader.SetBuffer(kernel, "_Previous", _previous);
                _shader.SetBuffer(kernel, "_Velocity", _velocity);
                _shader.SetBuffer(kernel, "_Meta", _meta);
                _shader.SetBuffer(kernel, "_Accumulated", _accumulated);
                _shader.SetBuffer(kernel, "_AccumulatedCount", _accumulatedCount);
                _shader.SetBuffer(kernel, "_ActiveParticles", _activeParticleBuffer);
                _shader.SetBuffer(kernel, "_ActiveConstraints", _activeConstraintBuffer);
                _shader.SetBuffer(kernel, "_ActiveGroups", _activeGroupBuffer);
                _shader.SetBuffer(kernel, "_Constraints", _constraints);
                _shader.SetBuffer(kernel, "_Groups", _groups);
                _shader.SetBuffer(kernel, "_GroupMembers", _groupMembers);
                _shader.SetBuffer(kernel, "_GroupOffsets", _groupOffsets);
                _shader.SetBuffer(kernel, "_Colliders", _colliders);
                _shader.SetBuffer(kernel, "_Media", _media);
                _shader.SetBuffer(kernel, "_CellHead", _cellHead);
                _shader.SetBuffer(kernel, "_CellNext", _cellNext);
                _shader.SetBuffer(kernel, "_ParticleCell", _particleCell);
            }
        }

        static Vector3[] ToVectors(Unity.Collections.NativeArray<Vector4> data)
        {
            var vectors = new Vector3[data.Length];
            for (int i = 0; i < data.Length; i++)
                vectors[i] = data[i];
            return vectors;
        }
    }
}

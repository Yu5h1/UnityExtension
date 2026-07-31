using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1.UnifiedSolver
{
    [DefaultExecutionOrder(-100)]
    public sealed class SolverParticleEmitter : MonoBehaviour
    {
        [Header("Definition")]
        public SolverParticleProfile profile;

        [Header("Capacity")]
        [Min(1)]
        public int maxInstances = 2048;

        [Header("Initial Spawn")]
        public bool spawnOnStart;
        [Min(0)]
        public int initialCount;
        public Vector3 spawnVolume =
            new Vector3(5f, 2f, 5f);
        public Vector3 initialVelocity;
        [Min(0f)]
        public float velocityVariation = 0.5f;

        readonly List<SolverParticleSpawnRequest>
            _pending =
                new List<SolverParticleSpawnRequest>();
        readonly List<SolverParticleInstance>
            _instances =
                new List<SolverParticleInstance>();
        readonly Vector3[] _shapeScratch =
            new Vector3[12];
        readonly int[] _indexScratch =
            new int[12];
        readonly int[] _rigidIndexScratch =
            new int[4];

        SolverManager _solver;
        ComputeBuffer _instanceBuffer;
        int _sharedPhase;
        bool _reportedCapacity;

        public SolverManager Solver => _solver;
        public ComputeBuffer InstanceBuffer =>
            _instanceBuffer;
        public int InstanceCount => _instances.Count;
        public int PendingCount => _pending.Count;
        public IReadOnlyList<SolverParticleInstance>
            Instances => _instances;
        public bool IsReady =>
            _solver != null &&
            profile != null &&
            _instanceBuffer != null;
        public int LastFlushCount { get; private set; }

        void Awake()
        {
            CreateInstanceBuffer();
            EnsureModifierRunner();
        }

        // Modifiers and roll damping are dispatched by
        // SolverParticleModifierRunner, and nothing forces that component to
        // exist: RequireComponent on the runner pulls in an emitter, never the
        // other way round. An emitter with a fully configured profile and no
        // runner is therefore silently inert, which is indistinguishable from a
        // modifier that runs and has no visible effect. Add it rather than warn,
        // because a populated profile has already stated the intent.
        void EnsureModifierRunner()
        {
            if (profile == null)
                return;

            bool wantsModifiers =
                profile.modifiers != null &&
                profile.modifiers.Length > 0;
            bool wantsRollDamping =
                profile.rollDamping > 0f;
            if (!wantsModifiers && !wantsRollDamping)
                return;

            if (GetComponent<
                    SolverParticleModifierRunner>() !=
                null)
            {
                return;
            }

            gameObject.AddComponent<
                SolverParticleModifierRunner>();
            Debug.LogWarning(
                "SolverParticleEmitter: profile " +
                $"'{profile.name}' needs a " +
                "SolverParticleModifierRunner to " +
                "dispatch its modifiers and roll " +
                "damping, so one was added. Add it " +
                "in the scene to silence this.",
                this);
        }

        void Start()
        {
            _solver = SolverManager.Instance;
            if (_solver == null)
            {
                Debug.LogError(
                    "SolverParticleEmitter: No SolverManager found.",
                    this);
                return;
            }

            if (profile == null)
            {
                Debug.LogError(
                    "SolverParticleEmitter: Assign a profile.",
                    this);
                return;
            }

            if (spawnOnStart && initialCount > 0)
            {
                QueueInitialRequests();
                FlushQueued();
            }
        }

        void FixedUpdate()
        {
            FlushQueued();
        }

        public void SpawnOne()
        {
            TryEnqueueSpawn(
                transform.position,
                initialVelocity);
        }

        public bool TryEnqueueSpawn(
            Vector3 worldPosition,
            Vector3 worldVelocity)
        {
            Color color = profile != null
                ? profile.baseColor
                : Color.white;
            return TryEnqueue(
                SolverParticleSpawnRequest.Create(
                    worldPosition,
                    worldVelocity,
                    color));
        }

        public bool TryEnqueue(
            SolverParticleSpawnRequest request)
        {
            ResolveDependencies();
            if (!IsReady || !CanReserve(1))
                return false;

            request.rotation =
                NormalizeRotation(request.rotation);
            request.scale =
                SanitizeScale(request.scale);
            _pending.Add(request);
            return true;
        }

        public int EnqueueBatch(
            IReadOnlyList<SolverParticleSpawnRequest>
                requests)
        {
            if (requests == null)
                return 0;

            int accepted = 0;
            for (int i = 0; i < requests.Count; i++)
            {
                if (!TryEnqueue(requests[i]))
                    break;
                accepted++;
            }
            return accepted;
        }

        public int FlushQueued()
        {
            LastFlushCount = 0;
            ResolveDependencies();
            if (!IsReady || _pending.Count == 0)
                return 0;

            for (int i = 0; i < _pending.Count; i++)
            {
                if (!TrySpawnImmediate(_pending[i]))
                    break;
                LastFlushCount++;
            }

            if (LastFlushCount > 0)
            {
                _pending.RemoveRange(
                    0,
                    LastFlushCount);
                UploadInstances();
            }

            return LastFlushCount;
        }

        public void ClearPending()
        {
            _pending.Clear();
        }

        bool TrySpawnImmediate(
            SolverParticleSpawnRequest request)
        {
            if (!HasImmediateCapacity())
            {
                ReportCapacity();
                return false;
            }

            SolverParticleRequirements requirements =
                profile.Requirements;
            Vector3 dimensions = Vector3.Scale(
                profile.baseDimensions,
                request.scale);
            int shapeCount =
                BuildLocalShape(
                    profile.topology,
                    dimensions,
                    _shapeScratch);
            if (shapeCount !=
                requirements.particles)
            {
                Debug.LogError(
                    "SolverParticleEmitter: Topology shape " +
                    "does not match its requirements.",
                    this);
                return false;
            }

            int particleOffset = _solver.ActiveCount;
            int constraintOffset =
                _solver.ConstraintCount;
            int rigidBodyOffset =
                _solver.RigidBodyCount;
            int phase = ResolvePhase();
            float particleMass =
                profile.mass /
                Mathf.Max(1, requirements.particles);
            Color color = ResolveColor(request.color);
            for (int i = 0; i < shapeCount; i++)
            {
                Vector3 worldOffset =
                    request.rotation *
                    _shapeScratch[i];
                Vector3 velocity =
                    request.velocity +
                    Vector3.Cross(
                        request.angularVelocity,
                        worldOffset);
                _indexScratch[i] =
                    _solver.AddParticle(
                    request.position + worldOffset,
                    velocity,
                    particleMass,
                    color,
                    phase,
                    profile.showCollisionParticles);
                if (_indexScratch[i] < 0)
                    return false;
            }

            AddTopologyData(
                profile.topology,
                _indexScratch,
                request.position,
                request.rotation);

            _instances.Add(
                new SolverParticleInstance
                {
                    particleOffset = particleOffset,
                    particleCount =
                        requirements.particles,
                    constraintOffset =
                        constraintOffset,
                    constraintCount =
                        requirements.constraints,
                    rigidBodyOffset =
                        rigidBodyOffset,
                    rigidBodyCount =
                        requirements.rigidBodies,
                    topology =
                        (int)profile.topology,
                    profileId =
                        profile.GetInstanceID(),
                    scale = request.scale,
                    _padding = 0f,
                    spawnRotation =
                        request.rotation
                });
            _reportedCapacity = false;
            return true;
        }

        void AddTopologyData(
            SolverParticleTopology topology,
            int[] p,
            Vector3 origin,
            Quaternion rotation)
        {
            switch (topology)
            {
                case SolverParticleTopology.Chain3:
                    AddJoint(p, 0, 1);
                    AddJoint(p, 1, 2);
                    AddBend(p, 0, 2);
                    break;

                case SolverParticleTopology.GuideChain4:
                    AddJoint(p, 0, 1);
                    AddJoint(p, 1, 2);
                    AddJoint(p, 1, 3);
                    AddBend(p, 0, 3);
                    AddBend(p, 2, 3);
                    AddBend(p, 0, 2);
                    break;

                case SolverParticleTopology.DualRail6:
                    AddJoint(p, 0, 2);
                    AddJoint(p, 2, 4);
                    AddJoint(p, 1, 3);
                    AddJoint(p, 3, 5);
                    AddJoint(p, 0, 1);
                    AddJoint(p, 2, 3);
                    AddJoint(p, 4, 5);
                    AddBend(p, 0, 3);
                    AddBend(p, 1, 2);
                    AddBend(p, 2, 5);
                    AddBend(p, 3, 4);
                    AddBend(p, 0, 4);
                    AddBend(p, 1, 5);
                    break;

                case SolverParticleTopology.RigidCluster4:
                    AddRigidGroup(
                        p,
                        0,
                        origin,
                        rotation);
                    break;

                case SolverParticleTopology.ArticulatedCluster12:
                    for (int segment = 0;
                         segment < 3;
                         segment++)
                    {
                        int start = segment * 4;
                        AddRigidGroup(
                            p,
                            start,
                            origin,
                            rotation);
                    }
                    AddArticulatedJoint(p, 0, 1);
                    AddArticulatedJoint(p, 1, 2);
                    break;
            }
        }

        void AddRigidGroup(
            int[] indices,
            int start,
            Vector3 origin,
            Quaternion rotation)
        {
            for (int i = 0; i < 4; i++)
            {
                _rigidIndexScratch[i] =
                    indices[start + i];
            }
            _solver.AddRigidBody(
                _rigidIndexScratch,
                origin,
                rotation);
        }

        void AddJoint(int[] p, int a, int b)
        {
            _solver.AddDistanceConstraint(
                p[a],
                p[b],
                profile.jointCompliance,
                0f,
                profile.jointDamping);
        }

        void AddBend(int[] p, int a, int b)
        {
            _solver.AddDistanceConstraint(
                p[a],
                p[b],
                profile.bendCompliance,
                0f,
                profile.jointDamping);
        }

        void AddArticulatedJoint(
            int[] p,
            int frontSegment,
            int rearSegment)
        {
            int front = frontSegment * 4;
            int rear = rearSegment * 4;
            AddJoint(p, front + 2, rear);
            AddJoint(p, front + 3, rear + 1);
            AddBend(p, front + 2, rear + 1);
            AddBend(p, front + 3, rear);
            AddBend(p, front, rear + 2);
            AddBend(p, front + 1, rear + 3);
        }

        int BuildLocalShape(
            SolverParticleTopology topology,
            Vector3 dimensions,
            Vector3[] result)
        {
            float hx = dimensions.x * 0.5f;
            float hy = dimensions.y * 0.5f;
            float hz = dimensions.z * 0.5f;

            switch (topology)
            {
                case SolverParticleTopology.Single:
                    result[0] = Vector3.zero;
                    return 1;

                case SolverParticleTopology.Chain3:
                    result[0] = Vector3.up * hy;
                    result[1] = Vector3.zero;
                    result[2] = Vector3.down * hy;
                    return 3;

                case SolverParticleTopology.GuideChain4:
                    result[0] = Vector3.up * hy;
                    result[1] = Vector3.zero;
                    result[2] = Vector3.down * hy;
                    result[3] = Vector3.right * hx;
                    return 4;

                case SolverParticleTopology.DualRail6:
                    result[0] =
                        new Vector3(hx, hy, 0f);
                    result[1] =
                        new Vector3(-hx, hy, 0f);
                    result[2] =
                        new Vector3(hx, 0f, 0f);
                    result[3] =
                        new Vector3(-hx, 0f, 0f);
                    result[4] =
                        new Vector3(hx, -hy, 0f);
                    result[5] =
                        new Vector3(-hx, -hy, 0f);
                    return 6;

                case SolverParticleTopology.RigidCluster4:
                    result[0] =
                        new Vector3(hx, hy, hz);
                    result[1] =
                        new Vector3(-hx, hy, -hz);
                    result[2] =
                        new Vector3(-hx, -hy, hz);
                    result[3] =
                        new Vector3(hx, -hy, -hz);
                    return 4;

                case SolverParticleTopology.ArticulatedCluster12:
                    BuildArticulatedShape(
                        dimensions,
                        result);
                    return 12;

                default:
                    return 0;
            }
        }

        void BuildArticulatedShape(
            Vector3 dimensions,
            Vector3[] result)
        {
            float centerSpacing =
                dimensions.y / 3f;
            float segmentHalfLength =
                dimensions.y / 6f;
            float hx = dimensions.x * 0.5f;
            float hz = dimensions.z * 0.5f;
            for (int segment = 0;
                 segment < 3;
                 segment++)
            {
                float center =
                    (1 - segment) *
                    centerSpacing;
                int start = segment * 4;
                Vector3 centerOffset =
                    Vector3.up * center;
                result[start] =
                    centerOffset +
                    new Vector3(
                        hx,
                        segmentHalfLength,
                        hz);
                result[start + 1] =
                    centerOffset +
                    new Vector3(
                        -hx,
                        segmentHalfLength,
                        -hz);
                result[start + 2] =
                    centerOffset +
                    new Vector3(
                        -hx,
                        -segmentHalfLength,
                        hz);
                result[start + 3] =
                    centerOffset +
                    new Vector3(
                        hx,
                        -segmentHalfLength,
                        -hz);
            }
        }

        int ResolvePhase()
        {
            if (profile.collideWithSameProfile)
                return PhaseManager.AllocatePhase();

            if (_sharedPhase == 0)
                _sharedPhase =
                    PhaseManager.AllocatePhase();
            return _sharedPhase;
        }

        bool CanReserve(int additionalRequests)
        {
            int requested =
                _pending.Count +
                additionalRequests;
            if (_instances.Count + requested >
                maxInstances)
            {
                ReportCapacity();
                return false;
            }

            SolverParticleRequirements r =
                profile.Requirements;
            bool available =
                _solver.ActiveCount +
                    requested * r.particles <=
                    _solver.maxParticles &&
                _solver.ConstraintCount +
                    requested * r.constraints <=
                    _solver.maxConstraints &&
                _solver.RigidBodyCount +
                    requested * r.rigidBodies <=
                    _solver.maxRigidBodies &&
                HasRigidParticleRefCapacity(
                    requested,
                    r.rigidParticleRefs);

            if (!available)
                ReportCapacity();
            return available;
        }

        bool HasImmediateCapacity()
        {
            if (_instances.Count >= maxInstances)
                return false;

            SolverParticleRequirements r =
                profile.Requirements;
            return
                _solver.ActiveCount + r.particles <=
                    _solver.maxParticles &&
                _solver.ConstraintCount + r.constraints <=
                    _solver.maxConstraints &&
                _solver.RigidBodyCount + r.rigidBodies <=
                    _solver.maxRigidBodies &&
                HasRigidParticleRefCapacity(
                    1,
                    r.rigidParticleRefs);
        }

        bool HasRigidParticleRefCapacity(
            int requestCount,
            int refsPerRequest)
        {
            if (requestCount <= 0 ||
                refsPerRequest <= 0)
            {
                return true;
            }

            if (!SolverManagerAccess
                    .TryGetRigidParticleRefCount(
                        _solver,
                        out int currentCount))
            {
                return false;
            }

            long required =
                (long)requestCount *
                refsPerRequest;
            return currentCount + required <=
                _solver.maxRigidParticleRefs;
        }

        void QueueInitialRequests()
        {
            Vector3 halfVolume =
                spawnVolume * 0.5f;
            for (int i = 0; i < initialCount; i++)
            {
                Vector3 localPosition =
                    new Vector3(
                        Random.Range(
                            -halfVolume.x,
                            halfVolume.x),
                        Random.Range(
                            -halfVolume.y,
                            halfVolume.y),
                        Random.Range(
                            -halfVolume.z,
                            halfVolume.z));
                SolverParticleSpawnRequest request =
                    SolverParticleSpawnRequest.Create(
                        transform.TransformPoint(
                            localPosition),
                        transform.TransformDirection(
                            initialVelocity +
                            Random.insideUnitSphere *
                            velocityVariation),
                        profile.baseColor);
                request.rotation =
                    transform.rotation;
                if (!TryEnqueue(request))
                    break;
            }
        }

        Color ResolveColor(Color requested)
        {
            Color baseColor =
                requested.a > 0f
                    ? requested
                    : profile.baseColor;
            if (profile.colorVariation <= 0f)
                return baseColor;

            Color.RGBToHSV(
                baseColor,
                out float h,
                out float s,
                out float v);
            float variation =
                profile.colorVariation;
            h = Mathf.Repeat(
                h +
                Random.Range(
                    -variation * 0.5f,
                    variation * 0.5f),
                1f);
            s = Mathf.Clamp01(
                s +
                Random.Range(
                    -variation * 0.2f,
                    variation * 0.2f));
            v = Mathf.Clamp01(
                v +
                Random.Range(
                    -variation * 0.2f,
                    variation * 0.2f));
            Color result =
                Color.HSVToRGB(h, s, v);
            result.a = baseColor.a;
            return result;
        }

        void ResolveDependencies()
        {
            if (_solver == null)
                _solver = SolverManager.Instance;
            if (_instanceBuffer == null)
                CreateInstanceBuffer();
        }

        void CreateInstanceBuffer()
        {
            if (_instanceBuffer != null)
                return;
            int capacity = Mathf.Max(1, maxInstances);
            _instanceBuffer = new ComputeBuffer(
                capacity,
                SolverParticleInstance.Stride,
                ComputeBufferType.Structured);
        }

        void UploadInstances()
        {
            if (_instances.Count == 0 ||
                _instanceBuffer == null)
            {
                return;
            }

            _instanceBuffer.SetData(
                _instances,
                0,
                0,
                _instances.Count);
        }

        void ReportCapacity()
        {
            if (_reportedCapacity)
                return;
            _reportedCapacity = true;
            Debug.LogWarning(
                "SolverParticleEmitter: Instance or Solver " +
                "capacity reached. The request was rejected.",
                this);
        }

        static Quaternion NormalizeRotation(
            Quaternion rotation)
        {
            float length = Mathf.Sqrt(
                rotation.x * rotation.x +
                rotation.y * rotation.y +
                rotation.z * rotation.z +
                rotation.w * rotation.w);
            if (length < 0.0001f)
                return Quaternion.identity;
            float inverse = 1f / length;
            return new Quaternion(
                rotation.x * inverse,
                rotation.y * inverse,
                rotation.z * inverse,
                rotation.w * inverse);
        }

        static Vector3 SanitizeScale(Vector3 scale)
        {
            if (scale.sqrMagnitude < 0.000001f)
                return Vector3.one;
            return new Vector3(
                Mathf.Max(
                    0.0001f,
                    Mathf.Abs(scale.x)),
                Mathf.Max(
                    0.0001f,
                    Mathf.Abs(scale.y)),
                Mathf.Max(
                    0.0001f,
                    Mathf.Abs(scale.z)));
        }

        void OnValidate()
        {
            maxInstances =
                Mathf.Max(1, maxInstances);
            initialCount =
                Mathf.Max(0, initialCount);
            spawnVolume = new Vector3(
                Mathf.Abs(spawnVolume.x),
                Mathf.Abs(spawnVolume.y),
                Mathf.Abs(spawnVolume.z));
            velocityVariation =
                Mathf.Max(0f, velocityVariation);
        }

        void OnDestroy()
        {
            _instanceBuffer?.Release();
            _instanceBuffer = null;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.matrix = Matrix4x4.TRS(
                transform.position,
                transform.rotation,
                Vector3.one);
            Gizmos.color =
                new Color(0.2f, 0.75f, 1f, 0.25f);
            Gizmos.DrawCube(
                Vector3.zero,
                spawnVolume);
            Gizmos.color =
                new Color(0.2f, 0.75f, 1f, 1f);
            Gizmos.DrawWireCube(
                Vector3.zero,
                spawnVolume);
        }
    }
}

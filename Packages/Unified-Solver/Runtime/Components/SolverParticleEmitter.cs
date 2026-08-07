using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Yu5h1Lib;

namespace Yu5h1.UnifiedSolver
{
    // The emitter owns its renderer and modifier runner outright.
    //
    // They stay separate classes because they run in different phases and have
    // different concerns, but they are not the user's to assemble. Unity's own
    // ParticleSystem works this way: ParticleSystemRenderer is a real second
    // component, and you never see it, because the ParticleSystem inspector
    // draws it as a module. `[RequireComponent]` was the wrong tool here — it
    // only converted "forgot to add it" into "forced to look at it", which is
    // still manual work the code could have done.
    //
    // Companions are created here, hidden here, and removed here. Nothing about
    // them is a decision.
    [DefaultExecutionOrder(-100)]
    public sealed class SolverParticleEmitter : MonoBehaviour
    {
        [Inline]
        public SolverParticleProfile profile;

        [Space]
        [Min(1)]
        public int maxInstances = 2048;

        [Tooltip("The same seed lays out the same pile every run. Needs a Shape Source.")]
        public int shapeSeed = 12345;

        [Space]
        // Named for the Unity convention, not for the callback it happens to
        // use. AudioSource and ParticleSystem both call this Play On Awake, and
        // a component that spawns nothing by default looks broken rather than
        // idle, so it defaults on with a count that is visible.
        [FormerlySerializedAs("spawnOnStart")]
        [Tooltip("Off means nothing appears until something calls TryEnqueue or SpawnOne.")]
        public bool playOnAwake = true;
        [Min(0)]
        public int initialCount = 100;
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
        // One per variant, not one shared array. AddRigidBody already reads
        // particleIndices.Length, so the solver has always supported 6 and 8
        // particle groups; these arrays were the only thing pinning the
        // extension to 4.
        readonly int[] _rigidScratch4 = new int[4];
        readonly int[] _rigidScratch6 = new int[6];
        readonly int[] _rigidScratch8 = new int[8];

        // Rigid body ids grouped by the template their instance was built from,
        // so the renderer can draw everything sharing a template in one
        // instanced call. Instances are append-only and a template is fixed at
        // spawn, so these only ever grow.
        readonly List<List<int>> _templateBodies =
            new List<List<int>>();

        SolverManager _solver;
        ComputeBuffer _instanceBuffer;
        ComputeBuffer _lifecycleBuffer;
        int _sharedPhase;
        bool _reportedCapacity;

        public SolverManager Solver => _solver;
        public ComputeBuffer InstanceBuffer =>
            _instanceBuffer;

        // Where each instance is in the fade-out / respawn / fade-in cycle:
        // float4(state, phase seconds, hidden, respawn count).
        //
        // Owned here rather than by either companion because both need it and
        // neither owns the other: the modifier runner writes it and the mesh
        // renderer reads it, exactly as they already share InstanceBuffer.
        //
        // Zero means alive, unfaded and never respawned, which is why the third
        // slot stores *hidden* rather than visible. A scene with no bounds
        // effect therefore never writes this buffer at all and the renderer
        // still reads a correct answer out of it.
        public ComputeBuffer LifecycleBuffer =>
            _lifecycleBuffer;
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
            EnsureCompanions();
            CreateInstanceBuffer();
        }

        // Called when the component is first added in the editor, and when Reset
        // is chosen. A safe context for AddComponent, unlike OnValidate.
        void Reset()
        {
            EnsureCompanions();
        }

        // Adds the renderer and the modifier runner if they are missing, and
        // keeps them out of the inspector.
        //
        // Both are added unconditionally rather than only when the profile looks
        // like it needs them. Gating on the profile meant that changing the
        // profile later left the object one component short, silently: a
        // populated modifier list with no runner is indistinguishable from a
        // modifier that runs and does nothing, and an emitter with no renderer
        // is indistinguishable from every other reason nothing is on screen.
        // Both cost nothing when there is nothing to do.
        //
        // Public so the editor can call it on objects created before the
        // companions were owned here.
        public void EnsureCompanions()
        {
            Hide(
                GetComponent<SolverMeshRenderer>() ??
                gameObject.AddComponent<
                    SolverMeshRenderer>());
            Hide(
                GetComponent<
                    SolverParticleModifierRunner>() ??
                gameObject.AddComponent<
                    SolverParticleModifierRunner>());
        }

        static void Hide(Component companion)
        {
            // HideInInspector only. DontSave would stop them serializing, and
            // their settings have to survive a scene reload like any other.
            if (companion != null)
            {
                companion.hideFlags =
                    HideFlags.HideInInspector;
            }
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

            if (playOnAwake && initialCount > 0)
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

            Vector3 dimensions = Vector3.Scale(
                profile.baseDimensions,
                request.scale);

            // The shape source, when there is one, both builds the rest
            // positions and decides which topology this instance realized. That
            // is why the variant is read back out rather than passed in: a pile
            // of ice is a mix of sizes, and a profile that could only spawn one
            // of them would need three profiles and three emitters to make one.
            SolverParticleTopology topology;
            int shapeCount;
            int templateIndex = -1;
            if (profile.shapeSource != null)
            {
                templateIndex = TemplateFor(
                    _instances.Count,
                    profile.shapeSource.TemplateCount);
                topology =
                    profile.shapeSource.BuildTemplate(
                        templateIndex,
                        dimensions,
                        _shapeScratch,
                        out shapeCount);
            }
            else
            {
                topology = profile.topology;
                shapeCount = BuildLocalShape(
                    topology,
                    dimensions,
                    _shapeScratch);
            }

            SolverParticleRequirements requirements =
                SolverParticleProfile.RequirementsFor(
                    topology);
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
                topology,
                _indexScratch,
                request.position,
                request.rotation);

            // Rigid bodies are appended in order, so the id of the one just
            // created is the count captured before creating it.
            if (requirements.rigidBodies > 0)
                TrackTemplate(templateIndex, rigidBodyOffset);

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
                    topology = (int)topology,
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
                case SolverParticleTopology.RigidCluster6:
                case SolverParticleTopology.RigidCluster8:
                    AddRigidGroup(
                        p,
                        0,
                        SolverTopologyInfo
                            .RigidClusterParticles(
                                topology),
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
                            4,
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
            int count,
            Vector3 origin,
            Quaternion rotation)
        {
            // AddRigidBody sizes the group from the array it is handed, so the
            // scratch has to be exactly as long as the group. A fixed length 8
            // array with four entries filled would silently create an 8 particle
            // body referencing whatever the last spawn left in slots 4 to 7.
            // Hence one preallocated array per variant rather than one shared
            // one: spawning is a hot path and must not allocate.
            int[] group = ResolveRigidScratch(count);
            if (group == null)
            {
                Debug.LogError(
                    "SolverParticleEmitter: Unsupported " +
                    $"rigid group size {count}.",
                    this);
                return;
            }

            for (int i = 0; i < count; i++)
                group[i] = indices[start + i];
            _solver.AddRigidBody(
                group,
                origin,
                rotation);
        }

        int[] ResolveRigidScratch(int count)
        {
            switch (count)
            {
                case 4:
                    return _rigidScratch4;
                case 6:
                    return _rigidScratch6;
                case 8:
                    return _rigidScratch8;
                default:
                    return null;
            }
        }

        // Which template an instance is built from. Spread across the library by
        // the emitter's seed, so the same seed lays out the same pile every run.
        int TemplateFor(int instanceIndex, int templateCount)
        {
            if (templateCount <= 1)
                return 0;

            uint value =
                (uint)(shapeSeed * 73856093 ^
                       (instanceIndex + 1) * 19349663);
            value ^= value >> 16;
            value *= 0x7feb352du;
            value ^= value >> 15;
            value *= 0x846ca68bu;
            value ^= value >> 16;
            return (int)(value % (uint)templateCount);
        }

        void TrackTemplate(
            int templateIndex,
            int rigidBodyId)
        {
            if (templateIndex < 0 || rigidBodyId < 0)
                return;

            while (_templateBodies.Count <= templateIndex)
                _templateBodies.Add(new List<int>());
            _templateBodies[templateIndex].Add(rigidBodyId);
        }

        // Rigid body ids sharing one template.
        //
        // The renderer turns each id into a matrix through the solver's own
        // TryGetRigidBodyMeshPose and draws the whole list in a single instanced
        // call, so the list is what a draw call is made of.
        public IReadOnlyList<int> TemplateBodies(
            int templateIndex)
        {
            return
                templateIndex >= 0 &&
                templateIndex < _templateBodies.Count
                    ? _templateBodies[templateIndex]
                    : null;
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

                // The same base polyhedra the hull renderer draws, unjittered.
                // A profile can therefore use a rigid cluster on its own, with
                // no shape source, and still get a shape the renderer can close
                // into a surface. Varying them is the shape source's job.
                case SolverParticleTopology.RigidCluster4:
                case SolverParticleTopology.RigidCluster6:
                case SolverParticleTopology.RigidCluster8:
                {
                    Vector3[] baseVertices =
                        SolverHullShapes.BaseVertices(
                            topology);
                    Vector3 halfExtents =
                        new Vector3(hx, hy, hz);
                    for (int i = 0;
                         i < baseVertices.Length;
                         i++)
                    {
                        result[i] = Vector3.Scale(
                            baseVertices[i],
                            halfExtents);
                    }
                    return baseVertices.Length;
                }

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
                profile.WorstCaseRequirements;
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
                profile.WorstCaseRequirements;
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

            // Explicitly zeroed. Undefined contents would read as instances
            // part-way through a fade they never started, at a hidden fraction
            // made of whatever was in memory.
            _lifecycleBuffer = new ComputeBuffer(
                capacity,
                sizeof(float) * 4,
                ComputeBufferType.Structured);
            _lifecycleBuffer.SetData(
                new Vector4[capacity]);
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
            _lifecycleBuffer?.Release();
            _lifecycleBuffer = null;
#if UNITY_EDITOR
            RemoveCompanionsIfOrphaned();
#endif
        }

#if UNITY_EDITOR
        // Take the hidden companions with it when the emitter is removed.
        //
        // Without this they survive as components that cannot be seen and
        // therefore cannot be deleted. Deferred, because destroying components
        // from inside OnDestroy is not allowed, and guarded on the GameObject
        // still being alive so that closing a scene or leaving play mode does
        // not trip it.
        void RemoveCompanionsIfOrphaned()
        {
            if (Application.isPlaying)
                return;

            GameObject owner = gameObject;
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (owner == null)
                    return;
                if (owner.GetComponent<
                        SolverParticleEmitter>() != null)
                {
                    return;
                }

                DestroyCompanion(
                    owner.GetComponent<
                        SolverMeshRenderer>());
                DestroyCompanion(
                    owner.GetComponent<
                        SolverParticleModifierRunner>());
            };
        }

        static void DestroyCompanion(Component companion)
        {
            if (companion != null)
                DestroyImmediate(companion);
        }
#endif

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

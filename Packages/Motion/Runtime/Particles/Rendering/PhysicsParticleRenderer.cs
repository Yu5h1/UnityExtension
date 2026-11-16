using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Yu5h1Lib
{
    /// <summary>
    ///   Draws the entities of the <see cref="IParticleBodySource"/> on the same GameObject from the
    ///   simulation data on the GPU, every frame, by the source's appearance profile (ParticleRendering
    ///   REND-01): grid surfaces, chain strips, articulated meshes and rigid fragments. A source with
    ///   an appearance makes sure this component exists with <see cref="Ensure"/>. Nothing is read
    ///   back synchronously and no matrix is built on the CPU: the world binds its particle and rigid
    ///   group buffers to the property block of each draw (ParticleWorld.TryBind), and the shaders
    ///   read them. Parked and released entities are left out of the draws; sleeping ones are drawn.
    ///   <para>
    ///   The draw bounds start as the layout's rest extent at the spawn poses, then follow the
    ///   entities' particles through asynchronous readbacks, at most four times per second, plus the
    ///   particle radius and <see cref="BoundsMargin"/>; Unity culls and fits shadow cascades with
    ///   them. The draw layer is the GameObject's layer, and shadows follow the appearance.
    ///   </para>
    /// </summary>
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    public sealed class PhysicsParticleRenderer : MonoBehaviour
    {
        enum DrawKind
        {
            None,
            Grid,
            Chain,
            Articulated,
            Rigid
        }

        struct RigidInstance
        {
            public uint group;
            public uint body;
        }

        sealed class Batch : IDisposable
        {
            public Mesh mesh;
            public bool ownsMesh;
            public bool failed;
            public int count;
            public int columns;
            public int rows;
            public Vector3 restCentre;
            public uint[] ids = new uint[16];
            public RigidInstance[] rigid;
            public ComputeBuffer buffer;

            public void AddId(uint id)
            {
                if (count == ids.Length)
                    Array.Resize(ref ids, ids.Length * 2);
                ids[count++] = id;
            }

            public void AddRigid(uint group, uint body)
            {
                rigid = rigid ?? new RigidInstance[16];
                if (count == rigid.Length)
                    Array.Resize(ref rigid, rigid.Length * 2);
                rigid[count++] = new RigidInstance { group = group, body = body };
            }

            public void Upload(bool isRigid)
            {
                if (count == 0)
                    return;
                int stride = isRigid ? 8 : 4;
                if (buffer == null || buffer.count < count || buffer.stride != stride)
                {
                    buffer?.Release();
                    buffer = new ComputeBuffer(Mathf.Max(count, 16), stride);
                }
                if (isRigid)
                    buffer.SetData(rigid, 0, 0, count);
                else
                    buffer.SetData(ids, 0, 0, count);
            }

            public void Dispose()
            {
                buffer?.Release();
                buffer = null;
                if (ownsMesh && mesh != null)
                    DestroyUnityObject(mesh);
                mesh = null;
            }
        }

        static readonly int EntityStartsId = Shader.PropertyToID("_EntityStarts");
        static readonly int GridColumnsId = Shader.PropertyToID("_GridColumns");
        static readonly int GridRowsId = Shader.PropertyToID("_GridRows");
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int NetMapId = Shader.PropertyToID("_NetMap");
        static readonly int NetEnabledId = Shader.PropertyToID("_NetEnabled");
        static readonly int UseNetTextureId = Shader.PropertyToID("_UseNetTexture");
        static readonly int NetPatternId = Shader.PropertyToID("_NetPattern");
        static readonly int NetTilingId = Shader.PropertyToID("_NetTiling");
        static readonly int ThreadWidthId = Shader.PropertyToID("_ThreadWidth");
        static readonly int CutoffId = Shader.PropertyToID("_Cutoff");
        static readonly int BorderColorId = Shader.PropertyToID("_BorderColor");
        static readonly int BorderWidthId = Shader.PropertyToID("_BorderWidth");
        static readonly int MetallicId = Shader.PropertyToID("_Metallic");
        static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        static readonly int ChainCountId = Shader.PropertyToID("_ChainCount");
        static readonly int StripWidthId = Shader.PropertyToID("_StripWidth");
        static readonly int BodiesId = Shader.PropertyToID("_Bodies");
        static readonly int BodyIndicesId = Shader.PropertyToID("_BodyIndices");
        static readonly int LifecycleId = Shader.PropertyToID("_Lifecycle");
        static readonly int HasLifecycleId = Shader.PropertyToID("_HasLifecycle");
        static readonly int MeshCenterId = Shader.PropertyToID("_MeshCenter");
        static readonly int MeshSizeId = Shader.PropertyToID("_MeshSize");
        static readonly int VisualScaleId = Shader.PropertyToID("_VisualScale");
        static readonly int FitToDimensionsId = Shader.PropertyToID("_FitToDimensions");
        static readonly int MeshForwardAxisId = Shader.PropertyToID("_MeshForwardAxis");
        static readonly int MeshAxisMinId = Shader.PropertyToID("_MeshAxisMin");
        static readonly int MeshAxisLengthId = Shader.PropertyToID("_MeshAxisLength");
        static readonly int MeshFlipForwardId = Shader.PropertyToID("_MeshFlipForward");
        static readonly int RigidInstancesId = Shader.PropertyToID("_RigidInstances");
        static readonly int RigidRestCentreId = Shader.PropertyToID("_RigidRestCentre");
        static readonly int RigidHasLifecycleId = Shader.PropertyToID("_RigidHasLifecycle");

        const string ErrorShaderName = "Hidden/InternalErrorShader";
        const float BoundsRequestTimeout = 2f;

        [Tooltip("Added around the entities' particles when the draw bounds follow them, in meters.")]
        [Min(0f)]
        [SerializeField]
        float _boundsMargin = 1f;

        MaterialPropertyBlock _block;
        readonly Dictionary<long, Batch> _batches = new Dictionary<long, Batch>();
        readonly List<Batch> _drawList = new List<Batch>();
        readonly List<int> _scratch = new List<int>();
        readonly List<int> _drawn = new List<int>();
        readonly List<float> _drawnRadii = new List<float>();
        readonly List<Vector3> _points = new List<Vector3>();
        readonly List<float> _radii = new List<float>();

        IParticleBodySource _source;
        ParticleWorld _world;
        PhysicsParticleAppearance _appearance;
        int _signature;
        int _version = -1;
        Material _runtimeMaterial;
        Material _runtimeSource;
        ComputeBuffer _dummyLifecycle;
        string _reported;

        Bounds _bounds = new Bounds(Vector3.zero, Vector3.one);
        int _boundsToken;
        int _boundsPending;
        float _nextBoundsRequest;
        float _boundsRequestTime;
        bool _boundsAny;
        Bounds _boundsAccumulator;

        /// <summary>
        ///   Distance added around the entities' particles when the draw bounds follow them.
        /// </summary>
        public float BoundsMargin
        {
            get => _boundsMargin;
            set => _boundsMargin = Mathf.Max(0f, value);
        }

        /// <summary>
        ///   Seconds between two bounds readbacks; 0.25 gives the four per second REND-01 allows.
        /// </summary>
        internal float BoundsInterval = 0.25f;

        /// <summary>
        ///   Draws only to this camera when set; null draws to every camera.
        /// </summary>
        internal Camera TargetCamera;

        internal Bounds DrawBounds => _bounds;
        internal int DrawnInstanceCount { get; private set; }
        internal int BatchCount => _drawList.Count;

        internal IEnumerable<Mesh> DrawnMeshes
        {
            get
            {
                foreach (Batch batch in _drawList)
                    yield return batch.mesh;
            }
        }

        internal int ReadbackRequestCount { get; private set; }
        internal bool BoundsFollowReadback { get; private set; }
        internal Material RuntimeMaterial => _runtimeMaterial;

        /// <summary>
        ///   Adds this component to <paramref name="target"/> when it has none and returns it. A
        ///   source calls it once it has an appearance, so users never add the component themselves.
        /// </summary>
        public static PhysicsParticleRenderer Ensure(GameObject target)
        {
            if (target == null)
                return null;
            if (!target.TryGetComponent(out PhysicsParticleRenderer renderer))
                renderer = target.AddComponent<PhysicsParticleRenderer>();
            return renderer;
        }

        void LateUpdate() => Draw(TargetCamera);

        void OnDisable() => _boundsToken++;

        void OnDestroy()
        {
            _boundsToken++;
            DisposeBatches();
            _dummyLifecycle?.Release();
            _dummyLifecycle = null;
            DestroyRuntimeMaterial();
        }

        /// <summary>
        ///   Brings the draw state up to date with the source and issues this frame's draws. Returns
        ///   true when something was submitted.
        /// </summary>
        internal bool Draw(Camera camera)
        {
            if (!SystemInfo.supportsComputeShaders)
                return false;
            if (_source == null)
                _source = GetComponent<IParticleBodySource>();
            _block = _block ?? new MaterialPropertyBlock();
            DrawnInstanceCount = 0;
            _drawList.Clear();
            if (_source == null)
                return false;
            ParticleWorld world = _source.BoundWorld;
            PhysicsParticleAppearance appearance = _source.Appearance;
            if (world == null || world.IsDestroyed || appearance == null)
            {
                DropState();
                return false;
            }
            DrawKind kind = KindOf(appearance);
            if (kind == DrawKind.None)
            {
                ReportOnce($"appearance '{appearance.name}' ({appearance.GetType().Name}) has no drawing");
                return false;
            }
            Material material = PrepareMaterial(appearance);
            if (material == null)
                return false;

            Refresh(world, appearance, kind);
            if (_drawn.Count == 0)
                return false;
            UpdateBounds(world);

            ShadowCastingMode shadows = appearance.CastShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            bool submitted = false;
            foreach (Batch batch in _batches.Values)
            {
                if (batch.count == 0 || batch.failed || batch.mesh == null)
                    continue;
                _block.Clear();
                if (!world.TryBind(_block))
                    return submitted;
                if (!BindBatch(kind, appearance, batch))
                    continue;
                Graphics.DrawMeshInstancedProcedural(
                    batch.mesh,
                    0,
                    material,
                    _bounds,
                    batch.count,
                    _block,
                    shadows,
                    appearance.ReceiveShadows,
                    gameObject.layer,
                    camera);
                _drawList.Add(batch);
                DrawnInstanceCount += batch.count;
                submitted = true;
            }
            return submitted;
        }

        static DrawKind KindOf(PhysicsParticleAppearance appearance)
        {
            if (appearance is GridSurfaceAppearance)
                return DrawKind.Grid;
            if (appearance is ChainStripAppearance)
                return DrawKind.Chain;
            if (appearance is ArticulatedMeshAppearance)
                return DrawKind.Articulated;
            if (appearance is ParticleHullAppearance || appearance is AuthoredMeshAppearance)
                return DrawKind.Rigid;
            return DrawKind.None;
        }

        static int SignatureOf(PhysicsParticleAppearance appearance)
        {
            var hash = new HashCode();
            hash.Add(appearance.GetType());
            switch (appearance)
            {
                case GridSurfaceAppearance grid:
                    hash.Add(grid.DoubleSided);
                    break;
                case ArticulatedMeshAppearance articulated:
                    hash.Add(articulated.Mesh);
                    break;
                case AuthoredMeshAppearance authored:
                    hash.Add(authored.Mesh);
                    hash.Add(authored.ForwardAxis);
                    hash.Add(authored.FlipForward);
                    hash.Add(authored.FitToDimensions);
                    hash.Add(authored.VisualScale);
                    break;
            }
            return hash.ToHashCode();
        }

        Material PrepareMaterial(PhysicsParticleAppearance appearance)
        {
            Material source = appearance.Material;
            if (source == null)
            {
                ReportOnce($"appearance '{appearance.name}' has no material");
                return null;
            }
            if (source.shader == null || source.shader.name == ErrorShaderName)
            {
                ReportOnce($"material '{source.name}' has no working shader; the one it was authored against is missing");
                return null;
            }
            if (_runtimeMaterial == null || _runtimeSource != source)
            {
                DestroyRuntimeMaterial();
                _runtimeMaterial = new Material(source)
                {
                    name = $"{source.name} (Runtime)",
                    hideFlags = HideFlags.HideAndDontSave,
                    enableInstancing = true
                };
                _runtimeSource = source;
            }
#if UNITY_EDITOR
            else
            {
                _runtimeMaterial.CopyPropertiesFromMaterial(source);
                _runtimeMaterial.enableInstancing = true;
            }
#endif
            return _runtimeMaterial;
        }

        void Refresh(ParticleWorld world, PhysicsParticleAppearance appearance, DrawKind kind)
        {
            IReadOnlyList<ParticleEntity> entities = _source.Entities;
            _scratch.Clear();
            for (int i = 0; i < entities.Count; i++)
            {
                ParticleEntity entity = entities[i];
                if (entity.IsValid && !entity.IsParked)
                    _scratch.Add(i);
            }

            int signature = SignatureOf(appearance);
            bool reset = world != _world || appearance != _appearance || signature != _signature;
            bool changed = reset || _source.Version != _version || !SameList(_scratch, _drawn);
            if (!changed)
                return;
            if (reset)
                DisposeBatches();

            _world = world;
            _appearance = appearance;
            _signature = signature;
            _version = _source.Version;
            _drawn.Clear();
            _drawn.AddRange(_scratch);
            foreach (Batch batch in _batches.Values)
                batch.count = 0;

            _drawnRadii.Clear();
            foreach (int index in _drawn)
            {
                ParticleEntity entity = entities[index];
                float maxRadius = 0f;
                IReadOnlyList<ParticleSpawn> rest = entity.RestParticles;
                for (int k = 0; k < rest.Count; k++)
                    maxRadius = Mathf.Max(maxRadius, rest[k].radius);
                _drawnRadii.Add(maxRadius);
                switch (kind)
                {
                    case DrawKind.Grid:
                        AddGrid((GridSurfaceAppearance)appearance, entity);
                        break;
                    case DrawKind.Chain:
                        AddChain(entity);
                        break;
                    case DrawKind.Articulated:
                        AddArticulated((ArticulatedMeshAppearance)appearance, index);
                        break;
                    case DrawKind.Rigid:
                        AddRigid(appearance, entity, index);
                        break;
                }
            }
            foreach (Batch batch in _batches.Values)
                batch.Upload(kind == DrawKind.Rigid);
            ResetBounds(entities);
        }

        static bool SameList(List<int> a, List<int> b)
        {
            if (a.Count != b.Count)
                return false;
            for (int i = 0; i < a.Count; i++)
                if (a[i] != b[i])
                    return false;
            return true;
        }

        Batch Obtain(long key, Func<Mesh> build, bool ownsMesh)
        {
            if (_batches.TryGetValue(key, out Batch batch))
                return batch;
            Mesh mesh = build();
            batch = new Batch { mesh = mesh, ownsMesh = ownsMesh, failed = mesh == null };
            _batches.Add(key, batch);
            return batch;
        }

        void AddGrid(GridSurfaceAppearance appearance, ParticleEntity entity)
        {
            IReadOnlyList<Vector3Int> roles = entity.Roles;
            if (roles.Count < 4)
                return;
            int columns = 0;
            for (int i = 0; i < roles.Count && roles[i].y == roles[0].y; i++)
                columns++;
            if (columns < 2 || roles.Count % columns != 0 || roles.Count / columns < 2)
            {
                ReportOnce($"an entity of {roles.Count} particles is not a grid of at least 2 x 2 in row order");
                return;
            }
            int rows = roles.Count / columns;
            Batch batch = Obtain(((long)columns << 24) | (long)rows, () => ParticleGridMesh.Build(columns, rows, appearance.DoubleSided), true);
            batch.columns = columns;
            batch.rows = rows;
            batch.AddId((uint)entity.Particles.start);
        }

        void AddChain(ParticleEntity entity)
        {
            int count = entity.Particles.length;
            if (count < 2)
                return;
            Batch batch = Obtain(count, () => ParticleStripMesh.Build(count), true);
            batch.columns = count;
            batch.AddId((uint)entity.Particles.start);
        }

        void AddArticulated(ArticulatedMeshAppearance appearance, int bodyIndex)
        {
            ComputeBuffer bodies = _source.Bodies;
            if (bodies == null || bodyIndex >= bodies.count)
                return;
            if (appearance.Mesh == null)
            {
                ReportOnce($"appearance '{appearance.name}' has no mesh");
                return;
            }
            Batch batch = Obtain(0, () => appearance.Mesh, false);
            batch.AddId((uint)bodyIndex);
        }

        void AddRigid(PhysicsParticleAppearance appearance, ParticleEntity entity, int bodyIndex)
        {
            if (entity.RigidGroups.length == 0)
            {
                ReportOnce($"an entity of appearance '{appearance.name}' has no rigid group to follow");
                return;
            }
            IReadOnlyList<ParticleSpawn> rest = entity.RestParticles;
            var key = new HashCode();
            key.Add(rest.Count);
            for (int k = 0; k < rest.Count; k++)
            {
                key.Add(Mathf.RoundToInt(rest[k].position.x * 10000f));
                key.Add(Mathf.RoundToInt(rest[k].position.y * 10000f));
                key.Add(Mathf.RoundToInt(rest[k].position.z * 10000f));
                key.Add(Mathf.RoundToInt(rest[k].radius * 10000f));
            }
            long id = (long)key.ToHashCode() << 8 ^ rest.Count;
            Batch batch = Obtain(id, () => BuildRigidMesh(appearance, rest), true);
            batch.restCentre = entity.RigidGroupRestCentroid(0);
            batch.AddRigid((uint)entity.RigidGroups.start, (uint)bodyIndex);
        }

        Mesh BuildRigidMesh(PhysicsParticleAppearance appearance, IReadOnlyList<ParticleSpawn> rest)
        {
            _points.Clear();
            _radii.Clear();
            for (int k = 0; k < rest.Count; k++)
            {
                _points.Add(rest[k].position);
                _radii.Add(rest[k].radius);
            }
            if (appearance is AuthoredMeshAppearance authored)
            {
                if (authored.Mesh == null)
                {
                    ReportOnce($"appearance '{authored.name}' has no mesh");
                    return null;
                }
                if (!authored.Mesh.isReadable)
                {
                    ReportOnce($"mesh '{authored.Mesh.name}' is not readable; enable Read/Write on its import settings");
                    return null;
                }
                Bounds extent = new Bounds(_points[0], Vector3.zero);
                float radius = 0f;
                for (int k = 0; k < _points.Count; k++)
                {
                    extent.Encapsulate(_points[k]);
                    radius = Mathf.Max(radius, _radii[k]);
                }
                return ParticleAuthoredMesh.Bake(
                    authored.Mesh,
                    authored.ForwardAxis,
                    authored.FlipForward,
                    authored.VisualScale,
                    authored.FitToDimensions,
                    extent.size + Vector3.one * (radius * 2f),
                    extent.center);
            }
            Mesh hull = ParticleHullMesh.Build(_points, _radii);
            if (hull == null)
                ReportOnce($"appearance '{appearance.name}': the rest particles have no convex hull");
            return hull;
        }

        bool BindBatch(DrawKind kind, PhysicsParticleAppearance appearance, Batch batch)
        {
            if (batch.buffer == null)
                return false;
            _block.SetColor(ColorId, appearance.Color);
            switch (kind)
            {
                case DrawKind.Grid:
                    BindGrid((GridSurfaceAppearance)appearance, batch);
                    return true;
                case DrawKind.Chain:
                    _block.SetBuffer(EntityStartsId, batch.buffer);
                    _block.SetInt(ChainCountId, batch.columns);
                    _block.SetFloat(StripWidthId, ((ChainStripAppearance)appearance).Width);
                    return true;
                case DrawKind.Articulated:
                    return BindArticulated((ArticulatedMeshAppearance)appearance, batch);
                default:
                    _block.SetBuffer(RigidInstancesId, batch.buffer);
                    _block.SetVector(RigidRestCentreId, batch.restCentre);
                    BindLifecycle(LifecycleId, RigidHasLifecycleId);
                    return true;
            }
        }

        void BindGrid(GridSurfaceAppearance grid, Batch batch)
        {
            _block.SetBuffer(EntityStartsId, batch.buffer);
            _block.SetInt(GridColumnsId, batch.columns);
            _block.SetInt(GridRowsId, batch.rows);
            _block.SetFloat(NetEnabledId, grid.UseNet ? 1f : 0f);
            if (grid.NetMap != null)
                _block.SetTexture(NetMapId, grid.NetMap);
            _block.SetFloat(UseNetTextureId, grid.UseNetTexture ? 1f : 0f);
            _block.SetFloat(NetPatternId, (float)grid.NetStyle);
            _block.SetVector(NetTilingId, new Vector4(grid.NetTiling.x, grid.NetTiling.y, 0f, 0f));
            _block.SetFloat(ThreadWidthId, grid.ThreadWidth);
            _block.SetFloat(CutoffId, grid.Cutoff);
            _block.SetColor(BorderColorId, grid.BorderColor);
            _block.SetFloat(BorderWidthId, grid.BorderWidth);
            _block.SetFloat(MetallicId, grid.Metallic);
            _block.SetFloat(SmoothnessId, grid.Smoothness);
        }

        bool BindArticulated(ArticulatedMeshAppearance appearance, Batch batch)
        {
            ComputeBuffer bodies = _source.Bodies;
            if (bodies == null)
                return false;
            Mesh mesh = batch.mesh;
            int axis = ParticleMeshAxis.Resolve(appearance.ForwardAxis, appearance.FlipForward, out bool flipped);
            Bounds bounds = mesh.bounds;
            _block.SetBuffer(BodiesId, bodies);
            _block.SetBuffer(BodyIndicesId, batch.buffer);
            BindLifecycle(LifecycleId, HasLifecycleId);
            _block.SetVector(MeshCenterId, bounds.center);
            _block.SetVector(MeshSizeId, bounds.size);
            _block.SetVector(VisualScaleId, appearance.VisualScale);
            _block.SetFloat(FitToDimensionsId, appearance.FitMeshToDimensions ? 1f : 0f);
            _block.SetInt(MeshForwardAxisId, axis);
            _block.SetFloat(MeshAxisMinId, bounds.min[axis]);
            _block.SetFloat(MeshAxisLengthId, Mathf.Max(0.000001f, bounds.size[axis]));
            _block.SetFloat(MeshFlipForwardId, flipped ? 1f : 0f);
            return true;
        }

        void BindLifecycle(int bufferId, int flagId)
        {
            ComputeBuffer lifecycle = _source.Lifecycle;
            if (lifecycle == null)
            {
                if (_dummyLifecycle == null)
                {
                    _dummyLifecycle = new ComputeBuffer(1, 16);
                    _dummyLifecycle.SetData(new[] { Vector4.zero });
                }
                _block.SetBuffer(bufferId, _dummyLifecycle);
                _block.SetInt(flagId, 0);
                return;
            }
            _block.SetBuffer(bufferId, lifecycle);
            _block.SetInt(flagId, 1);
        }

        void ResetBounds(IReadOnlyList<ParticleEntity> entities)
        {
            _boundsToken++;
            _boundsPending = 0;
            BoundsFollowReadback = false;
            bool any = false;
            Bounds bounds = default;
            foreach (int index in _drawn)
            {
                ParticleEntity entity = entities[index];
                Pose pose = entity.Pose;
                IReadOnlyList<ParticleSpawn> rest = entity.RestParticles;
                for (int k = 0; k < rest.Count; k++)
                {
                    Vector3 position = pose.position + pose.rotation * rest[k].position;
                    Vector3 extent = Vector3.one * rest[k].radius;
                    if (!any)
                    {
                        bounds = new Bounds(position, Vector3.zero);
                        any = true;
                    }
                    bounds.Encapsulate(new Bounds(position, extent * 2f));
                }
            }
            if (!any)
                bounds = new Bounds(transform.position, Vector3.zero);
            bounds.Expand(_boundsMargin * 2f);
            _bounds = bounds;
            _nextBoundsRequest = 0f;
        }

        void UpdateBounds(ParticleWorld world)
        {
            float now = Time.realtimeSinceStartup;
            if (_boundsPending > 0 && now - _boundsRequestTime > BoundsRequestTimeout)
            {
                _boundsToken++;
                _boundsPending = 0;
            }
            if (_boundsPending > 0 || now < _nextBoundsRequest)
                return;

            IReadOnlyList<ParticleEntity> entities = _source.Entities;
            int token = ++_boundsToken;
            _boundsAny = false;
            _boundsRequestTime = now;
            _nextBoundsRequest = now + BoundsInterval;
            _boundsPending = _drawn.Count;
            for (int i = 0; i < _drawn.Count; i++)
            {
                float radius = _drawnRadii[i];
                ReadbackRequestCount++;
                world.RequestReadback(entities[_drawn[i]], readback => OnBoundsReadback(token, radius, readback));
            }
        }

        void OnBoundsReadback(int token, float radius, ParticleReadback readback)
        {
            if (token != _boundsToken)
                return;
            if (readback.Succeeded)
            {
                foreach (Vector3 position in readback.Positions)
                {
                    Bounds particle = new Bounds(position, Vector3.one * (radius * 2f));
                    if (_boundsAny)
                        _boundsAccumulator.Encapsulate(particle);
                    else
                        _boundsAccumulator = particle;
                    _boundsAny = true;
                }
            }
            if (--_boundsPending > 0)
                return;
            if (!_boundsAny)
                return;
            Bounds bounds = _boundsAccumulator;
            bounds.Expand(_boundsMargin * 2f);
            _bounds = bounds;
            BoundsFollowReadback = true;
        }

        void DropState()
        {
            if (_world == null)
                return;
            DisposeBatches();
            _world = null;
            _appearance = null;
            _drawn.Clear();
            _version = -1;
            _boundsToken++;
            _boundsPending = 0;
        }

        void DisposeBatches()
        {
            foreach (Batch batch in _batches.Values)
                batch.Dispose();
            _batches.Clear();
            _drawList.Clear();
        }

        void DestroyRuntimeMaterial()
        {
            if (_runtimeMaterial != null)
                DestroyUnityObject(_runtimeMaterial);
            _runtimeMaterial = null;
            _runtimeSource = null;
        }

        void ReportOnce(string reason)
        {
            if (_reported == reason)
                return;
            _reported = reason;
            Debug.LogWarning($"PhysicsParticleRenderer on '{name}': nothing can be drawn because {reason}.", this);
        }

        static void DestroyUnityObject(UnityEngine.Object target)
        {
            if (Application.isPlaying)
                Destroy(target);
            else
                DestroyImmediate(target);
        }
    }
}

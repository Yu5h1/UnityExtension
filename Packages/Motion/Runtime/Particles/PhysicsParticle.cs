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
    ///   category bit (WORLD-11). It is a body source (ParticleExtensions EXT-09): one
    ///   <see cref="ParticleBodyData"/> per entity, with a chain layout's first three particles as
    ///   head, middle and tail, so an articulated appearance can bend a mesh along it. With an
    ///   appearance it makes sure a <see cref="PhysicsParticleRenderer"/> draws its entities.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PhysicsParticle : MonoBehaviour, IParticleBodySource
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
        int _appliedInteractionVersion;
        bool _active;
        string _lastError;
        ComputeBuffer _bodies;
        ParticleBodyData[] _bodyData = new ParticleBodyData[0];
        int _bodiesVersion;

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
            set
            {
                _appearance = value;
                if (_active && _appearance != null)
                    PhysicsParticleRenderer.Ensure(gameObject);
            }
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
        ///   One <see cref="ParticleBodyData"/> per entity in entity order, or null before anything
        ///   is allocated (<see cref="IParticleBodySource"/>).
        /// </summary>
        public ComputeBuffer Bodies => _entities.Count > 0 ? _bodies : null;

        /// <summary>
        ///   A driver has no lifecycle, so every body is fully visible.
        /// </summary>
        public ComputeBuffer Lifecycle => null;

        /// <summary>
        ///   Changes whenever the entity list or the body buffer content changes.
        /// </summary>
        public int Version => _bodiesVersion;

        /// <summary>
        ///   The last allocation or resolution failure, or null.
        /// </summary>
        public string LastError => _lastError;

        void OnEnable() => EnableDriver();
        void OnDisable() => DisableDriver();
        void OnDestroy() => DestroyDriver();

        void LateUpdate() => RefreshBodyFlags();

        void FixedUpdate()
        {
            if (_interaction != null && _interaction.Version != _appliedInteractionVersion)
                ApplyInteraction();
        }

        internal void EnableDriver()
        {
            _active = true;
            ParticleWorld.ActiveWorldsChanged -= OnWorldsChanged;
            ParticleWorld.ActiveWorldsChanged += OnWorldsChanged;
            Rebind();
            Spawn();
            if (_appearance != null)
                PhysicsParticleRenderer.Ensure(gameObject);
        }

        internal void DisableDriver()
        {
            _active = false;
            ParticleWorld.ActiveWorldsChanged -= OnWorldsChanged;
            foreach (ParticleEntity entity in _entities)
                if (entity.IsValid)
                    entity.SetParked(true);
            RefreshBodyFlags();
        }

        internal void DestroyDriver()
        {
            DisableDriver();
            ReleaseAll();
            _bound = null;
            _bodies?.Release();
            _bodies = null;
        }

        /// <summary>
        ///   Pose of entity <paramref name="index"/> when it spawns or respawns.
        /// </summary>
        public Pose SpawnPose(int index) =>
            new Pose(transform.TransformPoint(_batchSpacing * index), transform.rotation);

        /// <summary>
        ///   Hands the interaction's current constraint parameters to every entity without
        ///   resetting it (ParticleWorld WORLD-16). Called automatically after the profile changes;
        ///   an interaction that now builds a different set of constraints is reported and needs a
        ///   respawn instead.
        /// </summary>
        public void ApplyInteraction()
        {
            if (_interaction == null || _layout == null)
                return;
            _appliedInteractionVersion = _interaction.Version;
            for (int i = 0; i < _entities.Count; i++)
            {
                if (!_entities[i].IsValid)
                    continue;
                ParticleEntityRequest request = BuildRequest(_layout, _interaction, i % _layout.VariantCount, SpawnPose(i), 1u << gameObject.layer);
                if (!_entities[i].SetConstraintParameters(request.constraints, out string error))
                    Fail($"PhysicsParticle '{name}' could not apply its interaction: {error}");
            }
        }

        /// <summary>
        ///   Total particles of all <see cref="Count"/> entities as the layout lays them out, or 0
        ///   without a layout.
        /// </summary>
        public int FlatParticleCount
        {
            get
            {
                if (_layout == null || _layout.VariantCount < 1)
                    return 0;
                int total = 0;
                for (int i = 0; i < _count; i++)
                    total += _layout.GetParticleCount(i % _layout.VariantCount);
                return total;
            }
        }

        /// <summary>
        ///   Splits a flat particle number, which joins the entities' particles in entity order,
        ///   into the entity number and the particle index inside it (ParticleExtensions EXT-06).
        /// </summary>
        public bool TryLocate(int flatIndex, out int entityIndex, out int particleIndex)
        {
            entityIndex = -1;
            particleIndex = -1;
            if (flatIndex < 0 || _layout == null || _layout.VariantCount < 1)
                return false;
            int start = 0;
            for (int i = 0; i < _count; i++)
            {
                int count = _layout.GetParticleCount(i % _layout.VariantCount);
                if (flatIndex < start + count)
                {
                    entityIndex = i;
                    particleIndex = flatIndex - start;
                    return true;
                }
                start += count;
            }
            return false;
        }

        /// <summary>
        ///   World position of a flat particle number at its entity's spawn pose, from the layout
        ///   alone, so it works in the editor before anything is allocated.
        /// </summary>
        public bool TryGetRestPosition(int flatIndex, out Vector3 position)
        {
            position = default;
            if (!TryLocate(flatIndex, out int entityIndex, out int particleIndex))
                return false;
            _layout.GetRestPositions(entityIndex % _layout.VariantCount, _positions);
            Pose pose = SpawnPose(entityIndex);
            position = pose.position + pose.rotation * _positions[particleIndex];
            return true;
        }

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
                RefreshBodyFlags();
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
            _appliedInteractionVersion = _interaction.Version;
            var requests = new List<ParticleEntityRequest>(_count);
            for (int i = 0; i < _count; i++)
                requests.Add(BuildRequest(_layout, _interaction, i % _layout.VariantCount, SpawnPose(i), category));
            if (_bound.Allocate(requests, _entities, ParticleAllocationMode.AllOrNothing, out string error) == 0)
                Fail($"PhysicsParticle '{name}' did not spawn: {error}");
            else
            {
                _lastError = null;
                RebuildBodies();
            }
        }

        void ReleaseAll()
        {
            foreach (ParticleEntity entity in _entities)
                entity.Release();
            _entities.Clear();
            _bodiesVersion++;
        }

        /// <summary>
        ///   The topology a chain layout variant is drawn with as a body: four particles are
        ///   GuideChain4, anything else Chain3 over its first three particles; other layouts are
        ///   single bodies. A chain of more particles has no head, middle and tail of its own.
        /// </summary>
        internal static ParticleBodyTopology ResolveBodyTopology(PhysicsParticleLayout layout, int variant)
        {
            if (layout is ParticleBodyLayout body)
                return body.TopologyOf(variant);
            if (layout.Kind != ParticleLayoutKind.Chain)
                return ParticleBodyTopology.Single;
            return layout.GetParticleCount(variant) == 4 ? ParticleBodyTopology.GuideChain4 : ParticleBodyTopology.Chain3;
        }

        void RebuildBodies()
        {
            if (_layout == null || _layout.VariantCount < 1 || !SystemInfo.supportsComputeShaders)
                return;
            if (_bodyData.Length != _entities.Count)
                _bodyData = new ParticleBodyData[_entities.Count];
            for (int i = 0; i < _entities.Count; i++)
                _bodyData[i] = BuildBody(i);
            if (_bodies == null || _bodies.count < _bodyData.Length)
            {
                _bodies?.Release();
                _bodies = new ComputeBuffer(Mathf.Max(1, _bodyData.Length), ParticleBodyData.Stride);
            }
            _bodies.SetData(_bodyData);
            _bodiesVersion++;
        }

        internal void RefreshBodyFlags()
        {
            if (_bodies == null || _bodyData.Length != _entities.Count)
                return;
            bool changed = false;
            for (int i = 0; i < _entities.Count; i++)
            {
                uint flags = BodyFlags(_entities[i]);
                if (_bodyData[i].flags == flags)
                    continue;
                _bodyData[i].flags = flags;
                changed = true;
            }
            if (!changed)
                return;
            _bodies.SetData(_bodyData);
            _bodiesVersion++;
        }

        static uint BodyFlags(ParticleEntity entity) =>
            entity.IsValid && !entity.IsParked && !entity.IsSleeping ? 1u : 0u;

        ParticleBodyData BuildBody(int index)
        {
            ParticleEntity entity = _entities[index];
            int variant = index % _layout.VariantCount;
            _layout.GetRestPositions(variant, _positions);
            float radius = _interaction != null ? _interaction.ParticleRadius : 0.1f;
            Vector3 dimensions;
            Quaternion spin = Quaternion.identity;
            if (_layout is ParticleBodyLayout bodyLayout)
                dimensions = bodyLayout.BaseDimensions;
            else if (_layout.Kind == ParticleLayoutKind.Chain && _positions.Count >= 3)
            {
                Vector3 headToTail = _positions[2] - _positions[0];
                dimensions = new Vector3(radius * 2f, headToTail.magnitude, radius * 2f);
                if (headToTail.sqrMagnitude > 0f)
                    spin = Quaternion.FromToRotation(Vector3.up, -headToTail.normalized);
            }
            else
            {
                Bounds extent = new Bounds(_positions.Count > 0 ? _positions[0] : Vector3.zero, Vector3.zero);
                for (int i = 1; i < _positions.Count; i++)
                    extent.Encapsulate(_positions[i]);
                dimensions = extent.size + Vector3.one * (radius * 2f);
            }
            Quaternion rotation = SpawnPose(index).rotation * spin;
            const uint White = 0xFFFFFFFFu;
            return new ParticleBodyData
            {
                particleStart = (uint)entity.Particles.start,
                particleCount = (uint)entity.Particles.length,
                topology = (uint)ResolveBodyTopology(_layout, variant),
                flags = BodyFlags(entity),
                scale = new Vector4(1f, 1f, 1f, 1f),
                spawnRotation = new Vector4(rotation.x, rotation.y, rotation.z, rotation.w),
                dimensions = dimensions,
                color = White
            };
        }

        void Fail(string message)
        {
            if (message != _lastError)
                Debug.LogError(message, this);
            _lastError = message;
        }
    }
}

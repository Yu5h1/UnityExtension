using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Yu5h1Lib.Tests
{
    /// <summary>
    ///   Creates worlds, drivers, colliders and profiles for one test and tears all of them down,
    ///   driving their lifecycle through the internal entry points because EditMode does not call
    ///   OnEnable for these components.
    /// </summary>
    internal sealed class ParticleTestScope : IDisposable
    {
        readonly List<GameObject> _objects = new List<GameObject>();
        readonly List<ScriptableObject> _assets = new List<ScriptableObject>();
        readonly List<ParticleWorld> _worlds = new List<ParticleWorld>();

        public ParticleWorld World(string name, int particles = 1024, bool enable = true)
        {
            var go = new GameObject(name);
            _objects.Add(go);
            var world = go.AddComponent<ParticleWorld>();
            world.Capacity.particles = particles;
            _worlds.Add(world);
            if (enable)
                world.EnableWorld();
            return world;
        }

        public T Collider<T>(string name, ParticleWorld world = null) where T : ParticleCollider
        {
            var go = new GameObject(name);
            _objects.Add(go);
            var collider = go.AddComponent<T>();
            collider.World = world;
            return collider;
        }

        public PhysicsParticle Driver(string name, PhysicsParticleLayout layout, PhysicsParticleInteraction interaction, int count)
        {
            var go = new GameObject(name);
            _objects.Add(go);
            var driver = go.AddComponent<PhysicsParticle>();
            driver.Layout = layout;
            driver.Interaction = interaction;
            driver.Count = count;
            return driver;
        }

        public T Asset<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            asset.name = typeof(T).Name;
            _assets.Add(asset);
            return asset;
        }

        public GridParticleLayout Grid(int columns, int rows, float spacing = 0.1f)
        {
            var grid = Asset<GridParticleLayout>();
            grid.Columns = columns;
            grid.Rows = rows;
            grid.Spacing = spacing;
            return grid;
        }

        public static ParticleEntityRequest Request(int particles, int constraints = 0)
        {
            var request = new ParticleEntityRequest();
            for (int i = 0; i < particles; i++)
                request.particles.Add(new ParticleSpawn { position = new Vector3(i, 0f, 0f), invMass = 1f, radius = 0.1f });
            for (int i = 0; i < constraints; i++)
                request.constraints.Add(new DistanceConstraintSpec { a = i % particles, b = (i + 1) % particles, restLength = float.NaN });
            return request;
        }

        public static List<ParticleEntityRequest> Requests(int entities, int particles) =>
            Enumerable.Range(0, entities).Select(_ => Request(particles)).ToList();

        public void Dispose()
        {
            foreach (GameObject go in _objects)
            {
                if (go == null)
                    continue;
                foreach (PhysicsParticle driver in go.GetComponents<PhysicsParticle>())
                    driver.DisableDriver();
                foreach (ParticleCollider collider in go.GetComponents<ParticleCollider>())
                    collider.DisableCollider();
            }
            foreach (ParticleWorld world in _worlds)
                if (world != null)
                    world.DestroyWorld();
            foreach (GameObject go in _objects)
                if (go != null)
                    UnityEngine.Object.DestroyImmediate(go);
            foreach (ScriptableObject asset in _assets)
                if (asset != null)
                    UnityEngine.Object.DestroyImmediate(asset);
        }
    }

    /// <summary>
    ///   Backend stand-in that records what the world hands it.
    /// </summary>
    internal sealed class RecordingBackend : IParticleBackend
    {
        public readonly List<ParticleWorldChangeKind> applied = new List<ParticleWorldChangeKind>();
        public readonly List<int> steppedActiveCounts = new List<int>();
        public int colliderCountAtLastStep;
        public bool disposed;

        public void Apply(IReadOnlyList<ParticleWorldChange> changes)
        {
            foreach (ParticleWorldChange change in changes)
                applied.Add(change.kind);
        }

        public void Step(float deltaTime, ParticleSolverSettings settings, IReadOnlyList<ParticleColliderState> colliders, IReadOnlyList<ParticleMedium> media, int activeParticleCount, ParticleExtensionRunner extensions)
        {
            steppedActiveCounts.Add(activeParticleCount);
            colliderCountAtLastStep = colliders.Count;
        }

        public void RequestPositions(RangeInt particles, Action<Vector4[]> onComplete) => onComplete?.Invoke(null);

        public void RequestReadback(RangeInt particles, RangeInt constraints, RangeInt rigidGroups, ParticleReadback readback, Action<ParticleReadback> onComplete)
        {
            readback.Error = "Recording backend holds no state.";
            onComplete?.Invoke(readback);
        }

        public void Dispose() => disposed = true;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Yu5h1Lib.Tests
{
    /// <summary>
    ///   Builds worlds, emitters, volumes and targets for the body, behaviour, sleep and volume
    ///   checks (ParticleExtensions PEXT-CHECK-10 to 14) and tears them down. EditMode does not call
    ///   OnEnable for these components, so each is enabled through its internal entry point.
    /// </summary>
    internal sealed class ParticleBodyTestKit : IDisposable
    {
        public const float Dt = 0.02f;
        const string SetupPath = "Packages/com.yu5h1.motion/Tests/Editor/Particles/ParticleBodyTestSetup.compute";

        public readonly ParticleTestScope scope = new ParticleTestScope();
        readonly List<GameObject> _objects = new List<GameObject>();
        ComputeShader _setup;

        public ParticleWorld World(int substeps = 3, int particles = 4096, Vector3? gravity = null)
        {
            ParticleWorld world = scope.World("World", particles);
            world.Capacity.constraints = 8192;
            world.Capacity.rigidGroups = 512;
            world.Settings.substeps = substeps;
            world.Settings.gravity = gravity ?? Vector3.zero;
            world.Settings.damping = 0f;
            world.Settings.groundY = -1000f;
            return world;
        }

        public ParticleBodyLayout Layout(ParticleBodyTopology topology, Vector3? dimensions = null, ParticleBodyShapeSource source = null)
        {
            var layout = scope.Asset<ParticleBodyLayout>();
            layout.Topology = topology;
            if (dimensions.HasValue)
                layout.BaseDimensions = dimensions.Value;
            layout.ShapeSource = source;
            return layout;
        }

        public ParticleBodyInteraction Interaction(float bodyMass = 1f, float radius = 0.02f, float jointCompliance = 0.00002f, float bendCompliance = 0.0005f, float jointDamping = 0.15f)
        {
            var interaction = scope.Asset<ParticleBodyInteraction>();
            interaction.BodyMass = bodyMass;
            interaction.ParticleRadius = radius;
            interaction.JointCompliance = jointCompliance;
            interaction.BendCompliance = bendCompliance;
            interaction.JointDamping = jointDamping;
            return interaction;
        }

        public ParticleBehaviourProfile Behaviour(float sleepSpeed = 0f, float rollDamping = 0.25f, float speedLimit = 0f, params ParticleBehaviourModifier[] modifiers)
        {
            var behaviour = scope.Asset<ParticleBehaviourProfile>();
            behaviour.SleepSpeed = sleepSpeed;
            behaviour.RollDamping = rollDamping;
            behaviour.SpeedLimit = speedLimit;
            behaviour.Modifiers = modifiers;
            return behaviour;
        }

        public ParticleEmitter Emitter(
            ParticleWorld world,
            ParticleBodyLayout layout,
            ParticleBodyInteraction interaction,
            ParticleBehaviourProfile behaviour,
            int initialCount = 0,
            Vector3? spawnVolume = null,
            bool enable = true)
        {
            var go = new GameObject("Emitter");
            _objects.Add(go);
            var emitter = go.AddComponent<ParticleEmitter>();
            emitter.World = world;
            emitter.Layout = layout;
            emitter.Interaction = interaction;
            emitter.Behaviour = behaviour;
            emitter.InitialCount = initialCount;
            emitter.PlayOnAwake = initialCount > 0;
            emitter.ColorVariation = 0f;
            emitter.SpawnVolume = spawnVolume ?? new Vector3(1f, 1f, 1f);
            emitter.VelocityVariation = 0f;
            if (enable)
                emitter.EnableEmitter();
            return emitter;
        }

        public ParticleVolume Volume(string name, Vector3 position, Vector3 size, Quaternion? rotation = null, ParticleWorld world = null, bool enable = true, params ParticleVolumeEffect[] effects)
        {
            var go = new GameObject(name);
            _objects.Add(go);
            go.transform.SetPositionAndRotation(position, rotation ?? Quaternion.identity);
            go.transform.localScale = size;
            var volume = go.AddComponent<ParticleVolume>();
            volume.World = world;
            volume.Effects = effects;
            if (enable)
                volume.EnableVolume();
            return volume;
        }

        public ParticleMediumEffect Medium(float density, Vector3 flow = default, bool flowIsLocal = false, float drag = 1f)
        {
            var medium = scope.Asset<ParticleMediumEffect>();
            medium.Density = density;
            medium.Flow = flow;
            medium.FlowIsLocal = flowIsLocal;
            medium.DragCoefficient = drag;
            return medium;
        }

        public ParticleBoundsEffect Bounds(ParticleBoundsMode mode, float fadeOut = 0.5f, float fadeIn = 0.5f)
        {
            var bounds = scope.Asset<ParticleBoundsEffect>();
            bounds.Mode = mode;
            bounds.FadeOut = fadeOut;
            bounds.FadeIn = fadeIn;
            return bounds;
        }

        public ParticleMotionTarget Target(Vector3 position, Quaternion rotation, ParticleMotionTargetMode mode, float radius = 0f, ParticleWorld world = null)
        {
            var go = new GameObject("Target");
            _objects.Add(go);
            go.transform.SetPositionAndRotation(position, rotation);
            var target = go.AddComponent<ParticleMotionTarget>();
            target.World = world;
            target.Mode = mode;
            target.Radius = radius;
            target.EnableTarget();
            return target;
        }

        public T Profile<T>() where T : ScriptableObject => scope.Asset<T>();

        /// <summary>
        ///   Creates one body per spawn and returns the entities, using the emitter's own queue.
        /// </summary>
        public List<ParticleEntity> Spawn(ParticleEmitter emitter, params ParticleBodySpawn[] spawns)
        {
            int first = emitter.Entities.Count;
            foreach (ParticleBodySpawn spawn in spawns)
                Assert.IsTrue(emitter.TryEnqueue(spawn));
            Assert.AreEqual(spawns.Length, emitter.FlushQueued());
            return emitter.Entities.Skip(first).ToList();
        }

        public static ParticleBodySpawn At(Vector3 position, Vector3 velocity = default, Quaternion? rotation = null, Vector3? scale = null)
        {
            ParticleBodySpawn spawn = ParticleBodySpawn.Create(position, velocity, Color.white);
            spawn.rotation = rotation ?? Quaternion.identity;
            spawn.scale = scale ?? Vector3.one;
            return spawn;
        }

        /// <summary>
        ///   Steps the world and completes every asynchronous readback, so sleep commands reach the
        ///   entities the way they would a few frames later.
        /// </summary>
        public void Frame(ParticleWorld world, ParticleEmitter emitter = null, int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                emitter?.Tick();
                world.Step(Dt);
                AsyncGPUReadback.WaitAllRequests();
            }
        }

        public static ParticleReadback Read(ParticleWorld world, ParticleEntity entity)
        {
            ParticleReadback readback = null;
            world.RequestReadback(entity, result => readback = result);
            AsyncGPUReadback.WaitAllRequests();
            Assert.IsNotNull(readback, "The readback did not complete.");
            Assert.IsTrue(readback.Succeeded, readback.Error);
            return readback;
        }

        public static T[] Read<T>(ComputeBuffer buffer, int count) where T : struct
        {
            T[] result = null;
            AsyncGPUReadback.Request(buffer, count * System.Runtime.InteropServices.Marshal.SizeOf<T>(), 0, r => result = r.GetData<T>().ToArray());
            AsyncGPUReadback.WaitAllRequests();
            Assert.IsNotNull(result, "The buffer readback did not complete.");
            return result;
        }

        /// <summary>
        ///   Places the entity's particles and gives them velocities, replacing their position and
        ///   previous position, keeping their inverse mass.
        /// </summary>
        public void Place(ParticleWorld world, ParticleEntity entity, Vector3[] positions, Vector3[] velocities = null)
        {
            if (_setup == null)
                _setup = AssetDatabase.LoadAssetAtPath<ComputeShader>(SetupPath);
            Assert.IsNotNull(_setup, SetupPath);
            int count = positions.Length;
            var indices = new uint[count];
            var positionData = new Vector4[count];
            var velocityData = new Vector4[count];
            for (int i = 0; i < count; i++)
            {
                indices[i] = (uint)(entity.Particles.start + i);
                positionData[i] = positions[i];
                velocityData[i] = velocities != null ? (Vector4)velocities[i] : Vector4.zero;
            }
            var indexBuffer = new ComputeBuffer(count, 4);
            var positionBuffer = new ComputeBuffer(count, 16);
            var velocityBuffer = new ComputeBuffer(count, 16);
            try
            {
                indexBuffer.SetData(indices);
                positionBuffer.SetData(positionData);
                velocityBuffer.SetData(velocityData);
                int kernel = _setup.FindKernel("SetParticles");
                Assert.IsTrue(world.TryBind(_setup, kernel));
                _setup.SetBuffer(kernel, "_TestIndices", indexBuffer);
                _setup.SetBuffer(kernel, "_TestPositions", positionBuffer);
                _setup.SetBuffer(kernel, "_TestVelocities", velocityBuffer);
                _setup.SetInt("_TestCount", count);
                ParticleExtensionContext.Dispatch(_setup, kernel, count);
            }
            finally
            {
                indexBuffer.Release();
                positionBuffer.Release();
                velocityBuffer.Release();
            }
        }

        /// <summary>
        ///   The body colour of body <paramref name="index"/> as the raw RGBA8 bits in the GPU buffer.
        /// </summary>
        public static uint RawColor(ParticleEmitter emitter, int index) =>
            Read<uint>(emitter.Bodies, (index + 1) * 16)[index * 16 + 15];

        public static Vector3 Mean(IEnumerable<Vector3> values)
        {
            Vector3 sum = Vector3.zero;
            int count = 0;
            foreach (Vector3 value in values)
            {
                sum += value;
                count++;
            }
            return sum / Mathf.Max(1, count);
        }

        public static void AreClose(Vector3 expected, Vector3 actual, float tolerance, string what) =>
            Assert.That(Vector3.Distance(expected, actual), Is.LessThanOrEqualTo(tolerance),
                $"{what}: expected {expected:F5}, got {actual:F5}");

        public void Dispose()
        {
            foreach (GameObject go in _objects)
            {
                if (go == null)
                    continue;
                foreach (ParticleEmitter emitter in go.GetComponents<ParticleEmitter>())
                    emitter.DestroyEmitter();
                foreach (ParticleVolume volume in go.GetComponents<ParticleVolume>())
                    volume.DisableVolume();
                foreach (ParticleMotionTarget target in go.GetComponents<ParticleMotionTarget>())
                    target.DisableTarget();
            }
            scope.Dispose();
            foreach (GameObject go in _objects)
                if (go != null)
                    UnityEngine.Object.DestroyImmediate(go);
        }
    }
}

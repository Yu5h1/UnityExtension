using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Yu5h1Lib.Tests
{
    /// <summary>
    ///   Platform check PEXT-CHECK-11 of ParticleExtensions 1.2.0: the emitter spawns its initial
    ///   bodies in the spawn box, creates what fits when capacity is short and says so, parks and
    ///   respawns on disable and enable, and creates queued requests together at the next step.
    /// </summary>
    public sealed class ParticleEmitterTests
    {
        ParticleBodyTestKit _kit;

        [SetUp]
        public void SetUp()
        {
            if (!SystemInfo.supportsComputeShaders)
                Assert.Ignore("Compute shaders are not supported on this device.");
            _kit = new ParticleBodyTestKit();
        }

        [TearDown]
        public void TearDown() => _kit?.Dispose();

        static bool InsideSpawnBox(ParticleEmitter emitter, Vector3 world)
        {
            Vector3 local = emitter.transform.InverseTransformPoint(world);
            Vector3 half = emitter.SpawnVolume * 0.5f;
            return Mathf.Abs(local.x) <= half.x + 1e-4f && Mathf.Abs(local.y) <= half.y + 1e-4f && Mathf.Abs(local.z) <= half.z + 1e-4f;
        }

        ParticleEmitter Make(ParticleWorld world, int initialCount, Vector3 spawnVolume, bool enable = true)
        {
            ParticleEmitter emitter = _kit.Emitter(world, _kit.Layout(ParticleBodyTopology.Chain3), _kit.Interaction(), _kit.Behaviour(), initialCount, spawnVolume, enable: false);
            emitter.transform.SetPositionAndRotation(new Vector3(5f, 1f, 2f), Quaternion.Euler(0f, 45f, 0f));
            if (enable)
                emitter.EnableEmitter();
            return emitter;
        }

        [Test]
        public void EnablingSpawnsTheInitialBodiesInsideTheSpawnBox()
        {
            ParticleWorld world = _kit.World();
            var appearance = _kit.Profile<ParticleHullAppearance>();
            appearance.Color = new Color(1f, 0.5f, 0f, 1f);
            ParticleEmitter emitter = Make(world, 30, new Vector3(4f, 2f, 6f), enable: false);
            emitter.Appearance = appearance;
            emitter.ColorVariation = 0.15f;
            emitter.InitialVelocity = new Vector3(0f, 0f, 1f);
            emitter.VelocityVariation = 0.5f;
            emitter.EnableEmitter();

            Assert.AreEqual(30, emitter.Entities.Count);
            Assert.AreEqual(90, world.UsedParticles);
            foreach (ParticleEntity entity in emitter.Entities)
            {
                Assert.IsTrue(InsideSpawnBox(emitter, entity.Pose.position), $"{entity.Pose.position} is outside the spawn box");
                Assert.That(Quaternion.Angle(emitter.transform.rotation, entity.Pose.rotation), Is.LessThan(1e-3f));
                Vector3 velocity = entity.RestParticles[0].velocity;
                Assert.That(Vector3.Distance(new Vector3(0f, 0f, 1f), velocity), Is.LessThanOrEqualTo(0.5f + 1e-4f), "initial velocity plus a variation of at most 0.5 m/s");
            }

            _kit.Frame(world);
            ParticleBodyData[] bodies = ParticleBodyTestKit.Read<ParticleBodyData>(emitter.Bodies, 30);
            Color.RGBToHSV(appearance.Color, out float h0, out float s0, out float v0);
            bool anyDiffers = false;
            foreach (ParticleBodyData body in bodies)
            {
                uint packed = ParticleBodyTestKit.RawColor(emitter, System.Array.IndexOf(bodies, body));
                var color = new Color32((byte)(packed & 0xff), (byte)((packed >> 8) & 0xff), (byte)((packed >> 16) & 0xff), (byte)(packed >> 24));
                Color.RGBToHSV(color, out float h, out float s, out float v);
                float dh = Mathf.Abs(Mathf.DeltaAngle(h * 360f, h0 * 360f)) / 360f;
                Assert.That(dh, Is.LessThanOrEqualTo(0.075f + 0.02f), "hue stays within half the colour variation");
                Assert.That(Mathf.Abs(s - s0), Is.LessThanOrEqualTo(0.03f + 0.02f));
                Assert.That(Mathf.Abs(v - v0), Is.LessThanOrEqualTo(0.03f + 0.02f));
                anyDiffers |= dh > 0.005f;
            }
            Assert.IsTrue(anyDiffers, "colours are jittered");
        }

        [Test]
        public void PlayOnAwakeOffSpawnsNothingUntilQueued()
        {
            ParticleWorld world = _kit.World();
            ParticleEmitter emitter = Make(world, 30, Vector3.one, enable: false);
            emitter.PlayOnAwake = false;
            emitter.EnableEmitter();
            Assert.AreEqual(0, emitter.Entities.Count);
            Assert.AreEqual(0, world.UsedParticles);
        }

        [Test]
        public void ShortCapacityCreatesWhatFitsAndReportsOnce()
        {
            ParticleWorld world = _kit.World(particles: 50);
            ParticleEmitter emitter = Make(world, 20, Vector3.one, enable: false);
            LogAssert.Expect(LogType.Error, new Regex("created 16 of 20 bodies"));
            emitter.EnableEmitter();

            Assert.AreEqual(16, emitter.Entities.Count, "the sixteen bodies that fit are complete");
            Assert.AreEqual(48, world.UsedParticles);
            foreach (ParticleEntity entity in emitter.Entities)
            {
                Assert.AreEqual(3, entity.Particles.length);
                Assert.AreEqual(3, entity.Constraints.length);
            }
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void DisablingParksEveryBodyAndEnablingRespawnsThemInsideTheBox()
        {
            ParticleWorld world = _kit.World();
            ParticleEmitter emitter = Make(world, 20, new Vector3(4f, 2f, 6f));
            emitter.InitialVelocity = Vector3.zero;
            _kit.Frame(world);
            Vector3[] before = emitter.Entities.Select(e => e.Pose.position).ToArray();
            ParticleEntity first = emitter.Entities[0];

            emitter.DisableEmitter();
            Assert.IsTrue(emitter.Entities.All(e => e.IsParked), "every body is parked");
            Assert.AreEqual(0, world.ActiveParticleCount, "parked bodies cost nothing");
            Assert.AreEqual(20, emitter.Entities.Count, "parking keeps capacity");

            emitter.EnableEmitter();
            Assert.AreEqual(20, emitter.Entities.Count, "no body is added or lost");
            Assert.IsTrue(emitter.Entities.All(e => !e.IsParked));
            Assert.AreEqual(60, world.ActiveParticleCount);
            int moved = 0;
            for (int i = 0; i < 20; i++)
            {
                Vector3 position = emitter.Entities[i].Pose.position;
                Assert.IsTrue(InsideSpawnBox(emitter, position), "respawned inside the spawn box");
                if (Vector3.Distance(position, before[i]) > 1e-3f)
                    moved++;
            }
            Assert.Greater(moved, 15, "each body gets a new random point");

            _kit.Frame(world);
            ParticleReadback readback = ParticleBodyTestKit.Read(world, first);
            foreach (Vector3 velocity in readback.Velocities)
                Assert.That(velocity.magnitude, Is.LessThan(1e-4f), "respawned bodies are at rest");
            Assert.AreEqual(60, world.UsedParticles, "respawning adds no particle");
        }

        [Test]
        public void QueuedRequestsAreCreatedTogetherAtTheNextStep()
        {
            ParticleWorld world = _kit.World();
            ParticleEmitter emitter = Make(world, 0, Vector3.one);
            var color = new Color(0.2f, 0.4f, 0.6f, 1f);
            Quaternion rotation = Quaternion.Euler(0f, 90f, 0f);
            for (int i = 0; i < 5; i++)
            {
                ParticleBodySpawn spawn = ParticleBodySpawn.Create(new Vector3(i, 0f, 0f), new Vector3(0f, 1f, 0f), color);
                spawn.rotation = rotation;
                spawn.scale = new Vector3(1f, 2f, 1f);
                Assert.IsTrue(emitter.TryEnqueue(spawn));
            }
            Assert.AreEqual(5, emitter.PendingCount);
            Assert.AreEqual(0, emitter.Entities.Count, "nothing is created before the step");

            world.Step(ParticleBodyTestKit.Dt);
            Assert.AreEqual(0, emitter.Entities.Count, "a world step alone does not flush the queue");
            emitter.Tick();
            Assert.AreEqual(0, emitter.PendingCount);
            Assert.AreEqual(5, emitter.Entities.Count);
            Assert.AreEqual(15, world.UsedParticles, "one allocation for the whole batch");
            _kit.Frame(world);

            ParticleBodyData[] bodies = ParticleBodyTestKit.Read<ParticleBodyData>(emitter.Bodies, 5);
            for (int i = 0; i < 5; i++)
            {
                ParticleEntity entity = emitter.Entities[i];
                ParticleBodyTestKit.AreClose(new Vector3(i, 0f, 0f), entity.Pose.position, 1e-5f, "position");
                Assert.That(Quaternion.Angle(rotation, entity.Pose.rotation), Is.LessThan(1e-3f));
                ParticleBodyTestKit.AreClose(new Vector3(1f, 2f, 1f), bodies[i].scale, 1e-6f, "scale");
                ParticleBodyTestKit.AreClose(new Vector3(0f, 1f, 0f), rotation * entity.RestParticles[0].velocity, 1e-4f, "velocity");
                uint packed = ParticleBodyTestKit.RawColor(emitter, i);
                Assert.AreEqual((uint)Mathf.RoundToInt(0.2f * 255f), packed & 0xffu, "red of the requested colour");
                Assert.AreEqual((uint)Mathf.RoundToInt(0.6f * 255f), (packed >> 16) & 0xffu, "blue of the requested colour");
            }
        }

        [Test]
        public void RequestsBeyondTheInstanceLimitAreRejected()
        {
            ParticleWorld world = _kit.World();
            ParticleEmitter emitter = Make(world, 0, Vector3.one);
            emitter.MaxInstances = 3;
            LogAssert.Expect(LogType.Error, new Regex("holds 3 bodies already"));
            Assert.IsTrue(emitter.TryEnqueue(Vector3.zero, Vector3.zero));
            Assert.IsTrue(emitter.TryEnqueue(Vector3.right, Vector3.zero));
            Assert.IsTrue(emitter.TryEnqueue(Vector3.up, Vector3.zero));
            Assert.IsFalse(emitter.TryEnqueue(Vector3.forward, Vector3.zero));
            Assert.AreEqual(3, emitter.PendingCount);
        }

        [Test]
        public void SameProfileCollisionDecidesTheGroups()
        {
            ParticleWorld world = _kit.World();
            ParticleEmitter colliding = Make(world, 4, Vector3.one);
            Assert.AreEqual(4, colliding.Entities.Select(e => e.Group).Distinct().Count(), "every body has a group of its own");
            Assert.IsTrue(colliding.Entities.All(e => e.Group > 0));

            ParticleEmitter apart = Make(world, 4, Vector3.one, enable: false);
            apart.CollideWithSameProfile = false;
            apart.EnableEmitter();
            Assert.AreEqual(1, apart.Entities.Select(e => e.Group).Distinct().Count(), "the whole emitter shares one group");
            Assert.Greater(apart.Entities[0].Group, 0);
            Assert.IsFalse(colliding.Entities.Any(e => e.Group == apart.Entities[0].Group));
        }

        [Test]
        public void SharedGroupBodiesPassThroughEachOtherAndOtherBodiesDoNot()
        {
            ParticleWorld world = _kit.World();
            ParticleEmitter apart = Make(world, 0, Vector3.one);
            apart.CollideWithSameProfile = false;
            var pair = _kit.Spawn(apart,
                ParticleBodyTestKit.At(new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 0f), scale: new Vector3(1f, 0.1f, 1f)),
                ParticleBodyTestKit.At(new Vector3(0.2f, 0f, 0f), new Vector3(-1f, 0f, 0f), scale: new Vector3(1f, 0.1f, 1f)));
            _kit.Frame(world, apart, 30);
            float a = ParticleBodyTestKit.Read(world, pair[0]).Positions[1].x;
            float b = ParticleBodyTestKit.Read(world, pair[1]).Positions[1].x;
            Assert.Greater(a, b, "bodies of one group pass through each other");

            ParticleEmitter separate = Make(world, 0, Vector3.one);
            var other = _kit.Spawn(separate, ParticleBodyTestKit.At(new Vector3(0f, 5f, 0f), new Vector3(1f, 0f, 0f), scale: new Vector3(1f, 0.1f, 1f)),
                ParticleBodyTestKit.At(new Vector3(0.2f, 5f, 0f), new Vector3(-1f, 0f, 0f), scale: new Vector3(1f, 0.1f, 1f)));
            _kit.Frame(world, separate, 30);
            float c = ParticleBodyTestKit.Read(world, other[0]).Positions[1].x;
            float d = ParticleBodyTestKit.Read(world, other[1]).Positions[1].x;
            Assert.Less(c, d, "bodies with groups of their own collide");
        }

        [Test]
        public void DestroyingTheEmitterReleasesItsBodies()
        {
            ParticleWorld world = _kit.World();
            ParticleEmitter emitter = Make(world, 10, Vector3.one);
            Assert.AreEqual(30, world.UsedParticles);
            emitter.DestroyEmitter();
            _kit.Frame(world);
            Assert.AreEqual(0, world.UsedParticles);
            Assert.AreEqual(0, world.UsedConstraints);
        }
    }
}

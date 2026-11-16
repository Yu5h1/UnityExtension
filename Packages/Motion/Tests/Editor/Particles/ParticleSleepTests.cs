using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Yu5h1Lib.Tests
{
    /// <summary>
    ///   Platform check PEXT-CHECK-13 of ParticleExtensions 1.3.0 and the sleep part of PWORLD-CHECK-12
    ///   (ParticleWorld 1.3.0 WORLD-10): still bodies fall asleep after the delay and leave the solver's
    ///   kernel ranges, a particle hitting a sleeper at speed is stopped by it and wakes it, and
    ///   teleporting, resetting, parking and enabling a behaviour wake at once.
    /// </summary>
    public sealed class ParticleSleepTests
    {
        const float Dt = ParticleBodyTestKit.Dt;
        const float Radius = 0.05f;

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

        ParticleEmitter Emitter(ParticleWorld world, ParticleBehaviourProfile behaviour, ParticleBodyTopology topology = ParticleBodyTopology.Single)
        {
            ParticleBodyLayout layout = _kit.Layout(topology);
            ParticleBodyInteraction interaction = _kit.Interaction(1f, Radius, 1f, 1f, 0f);
            return _kit.Emitter(world, layout, interaction, behaviour);
        }

        ParticleEntity Plain(ParticleWorld world, Vector3 position, Vector3 velocity = default, float invMass = 1f, float radius = Radius)
        {
            var request = new ParticleEntityRequest { groupMode = ParticleGroupMode.NewGroup, pose = new Pose(position, Quaternion.identity) };
            request.particles.Add(new ParticleSpawn { position = Vector3.zero, velocity = velocity, invMass = invMass, radius = radius });
            Assert.IsTrue(world.TryAllocate(request, out ParticleEntity entity, out string error), error);
            return entity;
        }

        static GpuParticleBackend Backend(ParticleWorld world) => (GpuParticleBackend)world.Backend;

        int FramesUntilAllSleep(ParticleWorld world, ParticleEmitter emitter, int limit)
        {
            for (int frame = 1; frame <= limit; frame++)
            {
                _kit.Frame(world, emitter);
                if (emitter.Entities.All(e => e.IsSleeping))
                    return frame;
            }
            return -1;
        }

        [Test]
        public void StillBodiesSleepAfterTheDelayAndLeaveTheSolversRange()
        {
            ParticleWorld world = _kit.World();
            ParticleEmitter emitter = Emitter(world, _kit.Behaviour(sleepSpeed: 0.04f));
            _kit.Spawn(emitter, Enumerable.Range(0, 4).Select(i => ParticleBodyTestKit.At(new Vector3(i * 2f, 0f, 0f))).ToArray());
            var awake = Enumerable.Range(0, 4).Select(i => Plain(world, new Vector3(i * 2f, 20f, 0f))).ToList();

            for (int frame = 1; frame <= 22; frame++)
            {
                _kit.Frame(world, emitter);
                Assert.IsFalse(emitter.Entities.Any(e => e.IsSleeping), $"frame {frame}: still within the 0.5 s delay");
            }
            int slept = FramesUntilAllSleep(world, emitter, 20);
            Assert.That(slept, Is.InRange(1, 8), "asleep within a few frames after the delay: the decision is made on the GPU and arrives by readback");

            _kit.Frame(world, emitter);
            Assert.AreEqual(4, world.SleepingParticleCount);
            Assert.AreEqual(4, world.ActiveParticleCount);
            Assert.AreEqual(4, Backend(world).ActiveParticleListCount, "the solver's kernels walk only the awake particles");
            Assert.AreEqual(4, Backend(world).SleepingParticleListCount, "sleepers only join the neighbour structure");

            Vector3[] frozen = emitter.Entities.Select(e => ParticleBodyTestKit.Read(world, e).Positions[0]).ToArray();
            world.Settings.gravity = new Vector3(0f, -9.81f, 0f);
            _kit.Frame(world, emitter, 10);
            for (int i = 0; i < 4; i++)
            {
                ParticleReadback sleeper = ParticleBodyTestKit.Read(world, emitter.Entities[i]);
                Assert.AreEqual(frozen[i], sleeper.Positions[0], $"sleeper {i} does not move");
                Assert.AreEqual(Vector3.zero, sleeper.Velocities[0], $"sleeper {i} has no velocity");
                Assert.Less(ParticleBodyTestKit.Read(world, awake[i]).Positions[0].y, 19.9f, $"awake particle {i} falls");
            }
            Assert.IsTrue(emitter.Entities.All(e => e.IsSleeping), "gravity does not wake a sleeper");
        }

        [Test]
        public void ASpeedingParticleIsStoppedBySleepersAndWakesThem()
        {
            ParticleWorld world = _kit.World();
            ParticleEmitter emitter = Emitter(world, _kit.Behaviour(sleepSpeed: 0.04f));
            ParticleEntity sleeper = _kit.Spawn(emitter, ParticleBodyTestKit.At(Vector3.zero))[0];
            ParticleEntity projectile = Plain(world, new Vector3(50f, 0f, 0f));
            Assert.Greater(FramesUntilAllSleep(world, emitter, 40), 0, "the body falls asleep first");
            _kit.Frame(world, emitter);
            Assert.IsTrue(sleeper.IsSleeping);

            float h = Dt / 3f;
            float reach = 2f * Radius;
            float overlap = 0.008f;
            float x0 = reach - overlap + 2f * h;
            _kit.Place(world, projectile, new[] { new Vector3(x0, 0f, 0f) }, new[] { new Vector3(-2f, 0f, 0f) });
            Assert.IsTrue(sleeper.IsSleeping, "nothing woke it before the hit");

            _kit.Frame(world, emitter);
            Assert.IsFalse(sleeper.IsSleeping, "a 2 m/s hit wakes it within the frame the GPU measured it");
            float x = ParticleBodyTestKit.Read(world, projectile).Positions[0].x;
            Assert.That(x, Is.GreaterThanOrEqualTo(reach - 1e-3f), "the sleeper is a wall for the first step: the particle is pushed out, not through");
            ParticleBodyTestKit.AreClose(Vector3.zero, ParticleBodyTestKit.Read(world, sleeper).Positions[0], 1e-6f, "the sleeper did not move while asleep");

            float nearest = x;
            for (int i = 0; i < 20; i++)
            {
                _kit.Frame(world, emitter);
                nearest = Mathf.Min(nearest, ParticleBodyTestKit.Read(world, projectile).Positions[0].x);
            }
            Assert.That(nearest, Is.GreaterThan(0f), "the particle never passes through the body it hit");
        }

        [Test]
        public void ARestingLoadDoesNotWakeASleeperAndIsHeldUpByIt()
        {
            ParticleWorld world = _kit.World(substeps: 30);
            ParticleEmitter emitter = Emitter(world, _kit.Behaviour(sleepSpeed: 0.04f));
            ParticleEntity sleeper = _kit.Spawn(emitter, ParticleBodyTestKit.At(Vector3.zero))[0];
            ParticleEntity load = Plain(world, new Vector3(50f, 0f, 0f));
            Assert.Greater(FramesUntilAllSleep(world, emitter, 40), 0);
            _kit.Frame(world, emitter);

            world.Settings.gravity = new Vector3(0f, -9.81f, 0f);
            _kit.Place(world, load, new[] { new Vector3(0f, 2f * Radius + 0.0002f, 0f) });
            _kit.Frame(world, emitter, 45);

            Assert.IsTrue(sleeper.IsSleeping, "a particle resting on the sleeper presses it by far less than the wake distance");
            float y = ParticleBodyTestKit.Read(world, load).Positions[0].y;
            Assert.That(y, Is.InRange(2f * Radius - 2e-3f, 2f * Radius + 0.02f), "the particle came to rest on top of the sleeper");
            Assert.AreEqual(Vector3.zero, ParticleBodyTestKit.Read(world, sleeper).Positions[0], "the sleeper stays where it slept");
        }

        ParticleBoxCollider Box(ParticleWorld world, float topY)
        {
            var box = _kit.scope.Collider<ParticleBoxCollider>("Box", world);
            box.transform.localScale = new Vector3(1f, 0.2f, 1f);
            box.transform.position = new Vector3(0f, topY - 0.1f, 0f);
            box.EnableCollider();
            return box;
        }

        [Test]
        public void AStillColliderKeepsASleeperAsleep_AndAMovingOneWakesIt()
        {
            ParticleWorld world = _kit.World();
            ParticleEmitter emitter = Emitter(world, _kit.Behaviour(sleepSpeed: 0.04f));
            ParticleEntity sleeper = _kit.Spawn(emitter, ParticleBodyTestKit.At(Vector3.zero))[0];
            Plain(world, new Vector3(0f, 30f, 0f));
            ParticleBoxCollider box = Box(world, -Radius);
            Assert.Greater(FramesUntilAllSleep(world, emitter, 40), 0, "a body resting on a still box falls asleep");
            _kit.Frame(world, emitter, 10);
            Assert.IsTrue(sleeper.IsSleeping, "a still box touching the sleeper does not press into it");

            for (int frame = 0; frame < 6 && sleeper.IsSleeping; frame++)
            {
                box.transform.position += new Vector3(0f, 0.01f, 0f);
                _kit.Frame(world, emitter);
            }
            Assert.IsFalse(sleeper.IsSleeping, "a box rising 1 cm per step into the sleeper wakes it");
        }

        [Test]
        public void AMovingColliderWakesSleepers_WhenNothingElseIsAwake()
        {
            ParticleWorld world = _kit.World();
            ParticleEmitter emitter = Emitter(world, _kit.Behaviour(sleepSpeed: 0.04f));
            ParticleEntity sleeper = _kit.Spawn(emitter, ParticleBodyTestKit.At(Vector3.zero))[0];
            ParticleBoxCollider box = Box(world, -1f);
            Assert.Greater(FramesUntilAllSleep(world, emitter, 40), 0);
            _kit.Frame(world, emitter);
            Assert.AreEqual(0, world.ActiveParticleCount, "only the sleeper is left");

            box.transform.position = new Vector3(0f, -Radius - 0.1f + 0.01f, 0f);
            for (int frame = 0; frame < 4 && sleeper.IsSleeping; frame++)
                _kit.Frame(world, emitter);
            Assert.IsFalse(sleeper.IsSleeping, "the collider check still runs with only sleepers in the world");
        }

        [Test]
        public void TeleportResetParkAndRespawnWakeAtOnce()
        {
            ParticleWorld world = _kit.World();
            ParticleEmitter emitter = Emitter(world, _kit.Behaviour(sleepSpeed: 0.04f));
            _kit.Spawn(emitter, Enumerable.Range(0, 4).Select(i => ParticleBodyTestKit.At(new Vector3(i * 2f, 0f, 0f))).ToArray());
            Plain(world, new Vector3(0f, 30f, 0f));
            Assert.Greater(FramesUntilAllSleep(world, emitter, 40), 0);
            _kit.Frame(world, emitter);
            Assert.IsTrue(emitter.Entities.All(e => e.IsSleeping));

            emitter.Entities[0].Teleport(new Pose(new Vector3(0f, 3f, 0f), Quaternion.identity));
            Assert.IsFalse(emitter.Entities[0].IsSleeping, "teleporting wakes at once");
            emitter.Entities[1].Reset(new Pose(new Vector3(2f, 3f, 0f), Quaternion.identity));
            Assert.IsFalse(emitter.Entities[1].IsSleeping, "resetting wakes at once");
            emitter.Entities[2].SetParked(true);
            Assert.IsFalse(emitter.Entities[2].IsSleeping, "parking clears the sleep");
            Assert.IsTrue(emitter.Entities[2].IsParked);
            emitter.Entities[2].SetSleeping(true);
            Assert.IsFalse(emitter.Entities[2].IsSleeping, "a parked body cannot sleep");
            Assert.IsTrue(emitter.Entities[3].IsSleeping, "the others keep sleeping");
            Assert.AreEqual(1, world.SleepingParticleCount);

            emitter.Respawn();
            Assert.IsFalse(emitter.Entities[3].IsSleeping, "respawning wakes at once");
            Assert.AreEqual(0, world.SleepingParticleCount);
            _kit.Frame(world, emitter);
            Assert.AreEqual(0, world.SleepingParticleCount);
        }

        [Test]
        public void AWokenBodyStartsItsDelayAgain()
        {
            ParticleWorld world = _kit.World();
            ParticleEmitter emitter = Emitter(world, _kit.Behaviour(sleepSpeed: 0.04f));
            _kit.Spawn(emitter, ParticleBodyTestKit.At(Vector3.zero));
            Plain(world, new Vector3(0f, 30f, 0f));
            Assert.Greater(FramesUntilAllSleep(world, emitter, 40), 0);
            _kit.Frame(world, emitter);

            emitter.Entities[0].Teleport(new Pose(new Vector3(0f, 1f, 0f), Quaternion.identity));
            for (int frame = 1; frame <= 20; frame++)
            {
                _kit.Frame(world, emitter);
                Assert.IsFalse(emitter.Entities[0].IsSleeping, $"frame {frame} after waking: the 0.5 s delay runs again from zero");
            }
            Assert.Greater(FramesUntilAllSleep(world, emitter, 20), 0, "and it falls asleep again once it has been still that long");
        }

        [Test]
        public void EnablingAContinuousBehaviourWakesEverySleeper()
        {
            ParticleWorld world = _kit.World();
            ParticleBehaviourProfile behaviour = _kit.Behaviour(sleepSpeed: 0.04f);
            ParticleEmitter emitter = Emitter(world, behaviour, ParticleBodyTopology.Chain3);
            _kit.Spawn(emitter, Enumerable.Range(0, 3).Select(i => ParticleBodyTestKit.At(new Vector3(i * 2f, 0f, 0f))).ToArray());
            Plain(world, new Vector3(0f, 30f, 0f));
            Assert.Greater(FramesUntilAllSleep(world, emitter, 40), 0);
            _kit.Frame(world, emitter);

            var beat = _kit.Profile<ParticleOscillationProfile>();
            beat.vitality = 0f;
            behaviour.Modifiers = new ParticleBehaviourModifier[] { beat };
            emitter.Tick();
            Assert.IsTrue(emitter.Entities.All(e => !e.IsSleeping), "a behaviour that runs every step leaves nothing asleep");
            _kit.Frame(world, emitter, 60);
            Assert.IsFalse(emitter.Entities.Any(e => e.IsSleeping), "and nothing sleeps while it is on, however still the bodies are");

            beat.Enabled = false;
            behaviour.Modifiers = new ParticleBehaviourModifier[] { beat };
            Assert.Greater(FramesUntilAllSleep(world, emitter, 60), 0, "a disabled behaviour does not keep them up");
        }

        [Test]
        public void SleepOffWakesSleepersAndKeepsEveryoneAwake()
        {
            ParticleWorld world = _kit.World();
            ParticleBehaviourProfile behaviour = _kit.Behaviour(sleepSpeed: 0.04f);
            ParticleEmitter emitter = Emitter(world, behaviour);
            _kit.Spawn(emitter, ParticleBodyTestKit.At(Vector3.zero));
            Plain(world, new Vector3(0f, 30f, 0f));
            Assert.Greater(FramesUntilAllSleep(world, emitter, 40), 0);

            behaviour.SleepSpeed = 0f;
            emitter.Tick();
            Assert.IsFalse(emitter.Entities[0].IsSleeping);
            _kit.Frame(world, emitter, 60);
            Assert.IsFalse(emitter.Entities[0].IsSleeping, "sleep speed 0 disables sleeping");
        }

        [Test]
        public void ACurrentWakesASleeperAndNothingSleepsInIt()
        {
            ParticleWorld world = _kit.World();
            ParticleEmitter emitter = Emitter(world, _kit.Behaviour(sleepSpeed: 0.04f));
            _kit.Spawn(emitter, ParticleBodyTestKit.At(Vector3.zero), ParticleBodyTestKit.At(new Vector3(100f, 0f, 0f)));
            Plain(world, new Vector3(0f, 30f, 0f));
            Assert.Greater(FramesUntilAllSleep(world, emitter, 40), 0);
            _kit.Frame(world, emitter);

            _kit.Volume("Current", new Vector3(0f, 0f, 0f), new Vector3(10f, 10f, 10f), null, world, true, _kit.Medium(0f, new Vector3(1f, 0f, 0f)));
            _kit.Frame(world, emitter, 4);
            Assert.IsFalse(emitter.Entities[0].IsSleeping, "a current faster than the sleep speed wakes the sleeper inside it");
            Assert.IsTrue(emitter.Entities[1].IsSleeping, "a sleeper outside the current stays asleep");
            _kit.Frame(world, emitter, 60);
            Assert.IsFalse(emitter.Entities[0].IsSleeping, "and nothing sleeps while the water moves against it");
        }

        [Test]
        public void NeutralStillWaterLetsABodySleepAndBuoyantWaterDoesNot()
        {
            ParticleWorld world = _kit.World(gravity: new Vector3(0f, -9.81f, 0f));
            float neutral = 1f / (4f / 3f * Mathf.PI * Radius * Radius * Radius);
            _kit.Volume("Neutral", Vector3.zero, new Vector3(10f, 10f, 10f), null, world, true, _kit.Medium(neutral));
            _kit.Volume("Buoyant", new Vector3(100f, 0f, 0f), new Vector3(10f, 10f, 10f), null, world, true, _kit.Medium(neutral * 2f));
            ParticleEmitter emitter = Emitter(world, _kit.Behaviour(sleepSpeed: 0.04f));
            _kit.Spawn(emitter, ParticleBodyTestKit.At(Vector3.zero), ParticleBodyTestKit.At(new Vector3(100f, 0f, 0f)));
            _kit.Frame(world, emitter, 60);
            Assert.IsTrue(emitter.Entities[0].IsSleeping, "a body floating still in neutral water sleeps");
            Assert.IsFalse(emitter.Entities[1].IsSleeping, "a body the water lifts does not");
        }

        [Test]
        public void WorldCountsFollowSleepingAndReleasing()
        {
            ParticleWorld world = _kit.World();
            ParticleEntity a = Plain(world, Vector3.zero);
            ParticleEntity b = Plain(world, new Vector3(5f, 0f, 0f));
            Assert.AreEqual(2, world.ActiveParticleCount);
            a.SetSleeping(true);
            Assert.IsTrue(a.IsSleeping);
            Assert.AreEqual(1, world.ActiveParticleCount);
            Assert.AreEqual(1, world.SleepingParticleCount);
            world.Step(Dt);
            Assert.AreEqual(1, Backend(world).ActiveParticleListCount);
            Assert.AreEqual(1, Backend(world).SleepingParticleListCount);

            a.SetSleeping(false);
            Assert.AreEqual(2, world.ActiveParticleCount);
            Assert.AreEqual(0, world.SleepingParticleCount);
            b.SetSleeping(true);
            b.SetParked(true);
            Assert.IsFalse(b.IsSleeping);
            Assert.AreEqual(1, world.ActiveParticleCount);
            Assert.AreEqual(0, world.SleepingParticleCount);
            b.SetParked(false);
            a.SetSleeping(true);
            b.Release();
            a.Release();
            Assert.AreEqual(0, world.ActiveParticleCount);
            Assert.AreEqual(0, world.SleepingParticleCount);
            Assert.AreEqual(0, world.UsedParticles);
            world.Step(Dt);
            Assert.AreEqual(0, Backend(world).ActiveParticleListCount);
            Assert.AreEqual(0, Backend(world).SleepingParticleListCount);
        }

        [Test]
        public void ASleepingEntityResumesFromRestWhereItStopped()
        {
            ParticleWorld world = _kit.World(gravity: new Vector3(0f, -9.81f, 0f));
            ParticleEntity falling = Plain(world, new Vector3(0f, 10f, 0f), new Vector3(1f, 0f, 0f));
            ParticleEntity other = Plain(world, new Vector3(20f, 10f, 0f));
            _kit.Frame(world, null, 5);
            falling.SetSleeping(true);
            _kit.Frame(world, null, 1);
            Vector3 stopped = ParticleBodyTestKit.Read(world, falling).Positions[0];
            _kit.Frame(world, null, 10);
            ParticleReadback asleep = ParticleBodyTestKit.Read(world, falling);
            Assert.AreEqual(stopped, asleep.Positions[0], "a sleeper is not moved");
            Assert.AreEqual(Vector3.zero, asleep.Velocities[0], "and has no velocity");
            Assert.Less(ParticleBodyTestKit.Read(world, other).Positions[0].y, 9.6f, "while an awake particle falls");

            falling.SetSleeping(false);
            _kit.Frame(world, null, 1);
            Vector3 resumed = ParticleBodyTestKit.Read(world, falling).Positions[0];
            Assert.That(Vector3.Distance(stopped, resumed), Is.LessThan(0.005f), "it resumes from where it stopped, from rest");
            _kit.Frame(world, null, 10);
            Assert.Less(ParticleBodyTestKit.Read(world, falling).Positions[0].y, stopped.y - 0.1f, "and falls again");
        }
    }
}

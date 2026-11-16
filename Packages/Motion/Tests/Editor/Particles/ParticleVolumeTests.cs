using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Yu5h1Lib.Tests
{
    /// <summary>
    ///   Platform check PEXT-CHECK-14 of ParticleExtensions 1.2.0: medium volumes act like the world's
    ///   medium list (ParticleSolver SOLVER-17) and come before the media a script adds, a volume's
    ///   local flow turns with it, Contain and Exclude recycle bodies with a fade and bring them back
    ///   to the spawn box, and a spawn box outside every Contain (or inside an Exclude) skips that
    ///   volume with one warning. Expected medium velocities are the SOLVER-17 formulas applied step
    ///   by step to a free particle.
    /// </summary>
    public sealed class ParticleVolumeTests
    {
        const float Dt = ParticleBodyTestKit.Dt;
        const float Radius = 0.1f;
        static readonly Vector3 Gravity = new Vector3(0f, -9.81f, 0f);
        static readonly float ParticleVolumeM3 = 4f / 3f * Mathf.PI * Radius * Radius * Radius;

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

        ParticleEntity Plain(ParticleWorld world, Vector3 position)
        {
            var request = new ParticleEntityRequest { groupMode = ParticleGroupMode.NewGroup, pose = new Pose(position, Quaternion.identity) };
            request.particles.Add(new ParticleSpawn { position = Vector3.zero, invMass = 1f, radius = Radius });
            Assert.IsTrue(world.TryAllocate(request, out ParticleEntity entity, out string error), error);
            return entity;
        }

        static Vector3 Medium(Vector3 velocity, float density, Vector3 flow, float drag, Vector3 gravity)
        {
            float ratio = density * ParticleVolumeM3;
            velocity -= gravity * ratio * Dt;
            float blend = 1f - Mathf.Exp(-ratio * drag * Dt);
            return velocity + (flow - velocity) * blend;
        }

        static ParticleMedium Region(Vector3 center, float size, float density, Vector3 flow, float drag) => new ParticleMedium
        {
            center = center,
            halfExtents = Vector3.one * size * 0.5f,
            density = density,
            flow = flow,
            dragCoefficient = drag
        };

        [Test]
        public void MediumVolumeActsLikeTheWorldsMediumList()
        {
            ParticleWorld world = _kit.World(gravity: Gravity);
            float density = 2f / ParticleVolumeM3;
            Vector3 flow = new Vector3(0.5f, 0f, 0f);
            world.Media.Add(Region(new Vector3(-10f, 0f, 0f), 10f, density, flow, 1.5f));
            _kit.Volume("Water", new Vector3(10f, 0f, 0f), new Vector3(10f, 10f, 10f), null, world, true, _kit.Medium(density, flow, false, 1.5f));
            ParticleEntity inList = Plain(world, new Vector3(-10f, 0f, 0f));
            ParticleEntity inVolume = Plain(world, new Vector3(10f, 0f, 0f));

            Vector3 expected = Vector3.zero;
            for (int step = 1; step <= 5; step++)
            {
                _kit.Frame(world);
                expected = Medium(expected + Gravity * Dt, density, flow, 1.5f, Gravity);
                Vector3 a = ParticleBodyTestKit.Read(world, inList).Velocities[0];
                Vector3 b = ParticleBodyTestKit.Read(world, inVolume).Velocities[0];
                ParticleBodyTestKit.AreClose(expected, a, 2e-4f, $"script medium, step {step}");
                ParticleBodyTestKit.AreClose(expected, b, 2e-4f, $"volume medium, step {step}");
            }
            Assert.Greater(expected.y, 0.3f, "twice as dense as the particle, it is lifted");
        }

        [Test]
        public void VolumeMediaActBeforeTheMediaAScriptAdds()
        {
            ParticleWorld world = _kit.World(gravity: Gravity);
            float d1 = 10f / ParticleVolumeM3;
            float d2 = 10f / ParticleVolumeM3;
            Vector3 f1 = new Vector3(5f, 0f, 0f);
            Vector3 f2 = new Vector3(0f, 0f, -5f);
            world.Media.Add(Region(Vector3.zero, 100f, d2, f2, 5f));
            _kit.Volume("Water", Vector3.zero, new Vector3(100f, 100f, 100f), null, world, true, _kit.Medium(d1, f1, false, 5f));
            ParticleEntity particle = Plain(world, Vector3.zero);

            Vector3 volumeFirst = Medium(Medium(Gravity * Dt, d1, f1, 5f, Gravity), d2, f2, 5f, Gravity);
            Vector3 scriptFirst = Medium(Medium(Gravity * Dt, d2, f2, 5f, Gravity), d1, f1, 5f, Gravity);
            Assert.Greater((volumeFirst - scriptFirst).magnitude, 0.5f, "the two orders differ, so the case can tell them apart");

            _kit.Frame(world);
            Vector3 actual = ParticleBodyTestKit.Read(world, particle).Velocities[0];
            ParticleBodyTestKit.AreClose(volumeFirst, actual, 2e-4f, "volume medium first, script medium after");
        }

        [Test]
        public void VolumesAreTakenInRegistrationOrder()
        {
            ParticleWorld world = _kit.World(gravity: Gravity);
            float d1 = 0.5f / ParticleVolumeM3;
            float d2 = 3f / ParticleVolumeM3;
            Vector3 f1 = new Vector3(2f, 0f, 0f);
            Vector3 f2 = new Vector3(0f, 0f, -1f);
            _kit.Volume("First", Vector3.zero, new Vector3(100f, 100f, 100f), null, world, true, _kit.Medium(d1, f1, false, 1f));
            _kit.Volume("Second", Vector3.zero, new Vector3(100f, 100f, 100f), null, world, true, _kit.Medium(d2, f2, false, 2f));
            ParticleEntity particle = Plain(world, Vector3.zero);
            _kit.Frame(world);
            Vector3 expected = Medium(Medium(Gravity * Dt, d1, f1, 1f, Gravity), d2, f2, 2f, Gravity);
            ParticleBodyTestKit.AreClose(expected, ParticleBodyTestKit.Read(world, particle).Velocities[0], 2e-4f, "first registered acts first");
        }

        [Test]
        public void ALocalFlowTurnsWithTheVolumeAndTheBoxIsOriented()
        {
            ParticleWorld world = _kit.World();
            float density = 5f / ParticleVolumeM3;
            Quaternion turned = Quaternion.Euler(0f, 90f, 0f);
            _kit.Volume("Pipe", Vector3.zero, new Vector3(4f, 1f, 1f), turned, world, true, _kit.Medium(density, new Vector3(1f, 0f, 0f), flowIsLocal: true));
            _kit.Volume("Wind", new Vector3(100f, 0f, 0f), new Vector3(4f, 1f, 1f), null, world, true, _kit.Medium(density, new Vector3(1f, 0f, 0f), flowIsLocal: false));
            ParticleEntity along = Plain(world, new Vector3(0f, 0f, 1.5f));
            ParticleEntity across = Plain(world, new Vector3(1.5f, 0f, 0f));
            ParticleEntity wind = Plain(world, new Vector3(100f, 0f, 0f));
            _kit.Frame(world);

            float blend = 1f - Mathf.Exp(-5f * Dt);
            Vector3 localFlow = turned * Vector3.right;
            ParticleBodyTestKit.AreClose(localFlow * blend, ParticleBodyTestKit.Read(world, along).Velocities[0], 1e-4f, "the flow follows the volume's axes");
            Assert.That(localFlow.z, Is.EqualTo(-1f).Within(1e-5f));
            ParticleBodyTestKit.AreClose(Vector3.zero, ParticleBodyTestKit.Read(world, across).Velocities[0], 1e-6f, "the turned box is 1 m wide along x: 1.5 m is outside");
            ParticleBodyTestKit.AreClose(new Vector3(blend, 0f, 0f), ParticleBodyTestKit.Read(world, wind).Velocities[0], 1e-4f, "a world flow ignores the rotation");
        }

        [Test]
        public void ADisabledEffectOrVolumeDoesNothing()
        {
            ParticleWorld world = _kit.World();
            float density = 5f / ParticleVolumeM3;
            ParticleMediumEffect off = _kit.Medium(density, new Vector3(1f, 0f, 0f));
            off.Enabled = false;
            _kit.Volume("Off", Vector3.zero, new Vector3(10f, 10f, 10f), null, world, true, off);
            ParticleVolume disabled = _kit.Volume("Disabled", new Vector3(100f, 0f, 0f), new Vector3(10f, 10f, 10f), null, world, true, _kit.Medium(density, new Vector3(1f, 0f, 0f)));
            disabled.DisableVolume();
            ParticleEntity a = Plain(world, Vector3.zero);
            ParticleEntity b = Plain(world, new Vector3(100f, 0f, 0f));
            _kit.Frame(world, null, 3);
            Assert.AreEqual(Vector3.zero, ParticleBodyTestKit.Read(world, a).Velocities[0]);
            Assert.AreEqual(Vector3.zero, ParticleBodyTestKit.Read(world, b).Velocities[0]);
        }

        [Test]
        public void VolumesRegisterWhateverTheEnableOrder()
        {
            ParticleVolume early = _kit.Volume("Early", Vector3.zero, Vector3.one, null, null, true);
            Assert.IsNull(early.BoundWorld, "no world yet");
            ParticleWorld world = _kit.World();
            Assert.AreSame(world, early.BoundWorld, "the volume registers when a world appears");
            ParticleVolume late = _kit.Volume("Late", Vector3.zero, Vector3.one, null, null, true);
            Assert.AreSame(world, late.BoundWorld);
            Assert.AreEqual(2, world.Volumes.Count);
            CollectionAssert.AreEqual(new[] { early, late }, world.Volumes, "registration order");

            early.DisableVolume();
            Assert.AreEqual(1, world.Volumes.Count, "a disabled volume always unregisters");
            early.EnableVolume();
            Assert.AreEqual(2, world.Volumes.Count);
            Assert.AreSame(late, world.Volumes[0], "enabling again goes to the end");
        }

        [Test]
        public void AVolumeFollowsTheWorldItResolves()
        {
            ParticleVolume volume = _kit.Volume("Volume", Vector3.zero, Vector3.one, null, null, true);
            ParticleWorld first = _kit.World();
            Assert.AreSame(first, volume.BoundWorld);
            first.DisableWorld();
            ParticleWorld second = _kit.scope.World("Second");
            Assert.AreSame(second, volume.BoundWorld, "the volume moves to the only active world");
            Assert.AreEqual(0, first.Volumes.Count);
            Assert.AreEqual(1, second.Volumes.Count);
        }

        ParticleEmitter RecyclingEmitter(ParticleWorld world, Vector3 position, ParticleBehaviourProfile behaviour = null)
        {
            ParticleBodyLayout layout = _kit.Layout(ParticleBodyTopology.Single);
            ParticleBodyInteraction interaction = _kit.Interaction(1f, 0.01f);
            ParticleEmitter emitter = _kit.Emitter(world, layout, interaction, behaviour ?? _kit.Behaviour(), 0, new Vector3(1f, 1f, 1f), enable: false);
            emitter.transform.position = position;
            emitter.EnableEmitter();
            return emitter;
        }

        static Vector4 Life(ParticleEmitter emitter, int index) => ParticleBodyTestKit.Read<Vector4>(emitter.Lifecycle, index + 1)[index];

        [Test]
        public void ContainRecyclesABodyWithAFadeAndBringsItBackToTheSpawnBox()
        {
            ParticleWorld world = _kit.World();
            _kit.Volume("Room", Vector3.zero, new Vector3(6f, 6f, 6f), null, world, true, _kit.Bounds(ParticleBoundsMode.Contain, 0.5f, 0.5f));
            ParticleEmitter emitter = RecyclingEmitter(world, Vector3.zero);
            var bodies = _kit.Spawn(emitter, ParticleBodyTestKit.At(new Vector3(10f, 0f, 0f)), ParticleBodyTestKit.At(new Vector3(0.5f, 0f, 0f)));

            int frame = 0;
            while (frame < 10)
            {
                _kit.Frame(world, emitter);
                frame++;
            }
            Vector4 life = Life(emitter, 0);
            Assert.AreEqual(1f, life.x, "fading out");
            Assert.AreEqual(0.04f * frame, life.z, 0.002f, "hidden fraction grows by dt / fadeOut per step");
            Assert.AreEqual(0f, Life(emitter, 1).x, "the body inside the room is left alone");

            while (Life(emitter, 0).x == 1f && frame < 40)
            {
                _kit.Frame(world, emitter);
                frame++;
            }
            life = Life(emitter, 0);
            Assert.AreEqual(2f, life.x, "faded out, moved, and fading in");
            Assert.That(frame, Is.InRange(24, 28), "0.5 s of fade at 0.02 s per step");
            Assert.AreEqual(1f, life.z, 1e-5f, "fully hidden at the moment it is moved");
            Vector3 position = ParticleBodyTestKit.Mean(ParticleBodyTestKit.Read(world, bodies[0]).Positions);
            Assert.That(Mathf.Abs(position.x), Is.LessThanOrEqualTo(0.5f + 1e-4f), "back inside the spawn box");
            Assert.That(Mathf.Abs(position.y), Is.LessThanOrEqualTo(0.5f + 1e-4f));
            Assert.That(Mathf.Abs(position.z), Is.LessThanOrEqualTo(0.5f + 1e-4f));
            Assert.AreEqual(Vector3.zero, ParticleBodyTestKit.Read(world, bodies[0]).Velocities[0], "and at rest");

            _kit.Frame(world, emitter, 10);
            life = Life(emitter, 0);
            Assert.AreEqual(2f, life.x);
            Assert.AreEqual(1f - 0.04f * 10f, life.z, 0.03f, "fading in");
            _kit.Frame(world, emitter, 20);
            life = Life(emitter, 0);
            Assert.AreEqual(0f, life.x, "alive again");
            Assert.AreEqual(0f, life.z, "fully visible");
            _kit.Frame(world, emitter, 10);
            Assert.AreEqual(0f, Life(emitter, 0).x, "inside the room it is not recycled again");
        }

        [Test]
        public void NoFadeRecyclesOutright()
        {
            ParticleWorld world = _kit.World();
            _kit.Volume("Room", Vector3.zero, new Vector3(6f, 6f, 6f), null, world, true, _kit.Bounds(ParticleBoundsMode.Contain, 0f, 0f));
            ParticleEmitter emitter = RecyclingEmitter(world, Vector3.zero);
            ParticleEntity body = _kit.Spawn(emitter, ParticleBodyTestKit.At(new Vector3(10f, 0f, 0f)))[0];
            _kit.Frame(world, emitter);
            Assert.AreEqual(2f, Life(emitter, 0).x, "moved in the first step");
            Assert.That(ParticleBodyTestKit.Read(world, body).Positions[0].magnitude, Is.LessThan(1f));
            _kit.Frame(world, emitter);
            Assert.AreEqual(0f, Life(emitter, 0).x, "back the step after");
        }

        [Test]
        public void ExcludeWinsOverContain()
        {
            ParticleWorld world = _kit.World();
            _kit.Volume("Room", Vector3.zero, new Vector3(20f, 20f, 20f), null, world, true, _kit.Bounds(ParticleBoundsMode.Contain, 0f, 0f));
            _kit.Volume("Pit", new Vector3(3f, 0f, 0f), new Vector3(2f, 2f, 2f), null, world, true, _kit.Bounds(ParticleBoundsMode.Exclude, 0.5f, 0.5f));
            ParticleEmitter emitter = RecyclingEmitter(world, Vector3.zero);
            _kit.Spawn(emitter, ParticleBodyTestKit.At(new Vector3(3f, 0f, 0f)), ParticleBodyTestKit.At(new Vector3(-3f, 0f, 0f)));
            _kit.Frame(world, emitter, 3);
            Assert.AreEqual(1f, Life(emitter, 0).x, "inside the pit, which is inside the room, it is still recycled");
            Assert.AreEqual(0f, Life(emitter, 1).x, "outside the pit and inside the room it lives");
        }

        [Test]
        public void ContainVolumesAreAUnion()
        {
            ParticleWorld world = _kit.World();
            ParticleBoundsEffect bounds = _kit.Bounds(ParticleBoundsMode.Contain, 0f, 0f);
            _kit.Volume("A", Vector3.zero, new Vector3(4f, 4f, 4f), null, world, true, bounds);
            _kit.Volume("B", new Vector3(10f, 0f, 0f), new Vector3(4f, 4f, 4f), null, world, true, bounds);
            ParticleEmitter emitter = RecyclingEmitter(world, Vector3.zero);
            var bodies = _kit.Spawn(emitter, ParticleBodyTestKit.At(new Vector3(10f, 0f, 0f)), ParticleBodyTestKit.At(new Vector3(5f, 0f, 0f)));
            _kit.Frame(world, emitter);
            Assert.AreEqual(0f, Life(emitter, 0).x, "inside the second volume is inside the union");
            Assert.AreEqual(2f, Life(emitter, 1).x, "between them it is outside both, so it was sent home");
            Assert.That(ParticleBodyTestKit.Read(world, bodies[1]).Positions[0].magnitude, Is.LessThan(1f));
            Assert.That(ParticleBodyTestKit.Read(world, bodies[0]).Positions[0].x, Is.EqualTo(10f).Within(1e-4f), "the other stayed");
        }

        [Test]
        public void ASpawnBoxOutsideEveryContainSkipsThemWithOneWarning()
        {
            ParticleWorld world = _kit.World();
            _kit.Volume("Room", Vector3.zero, new Vector3(4f, 4f, 4f), null, world, true, _kit.Bounds(ParticleBoundsMode.Contain, 0f, 0f));
            ParticleEmitter emitter = RecyclingEmitter(world, new Vector3(20f, 0f, 0f));
            _kit.Spawn(emitter, ParticleBodyTestKit.At(new Vector3(20f, 0f, 0f)));
            LogAssert.Expect(LogType.Warning, new Regex("outside all 1 Contain bounds volume"));
            _kit.Frame(world, emitter, 5);
            Assert.AreEqual(0f, Life(emitter, 0).x, "Contain is not applied to this emitter's bodies");
            _kit.Frame(world, emitter, 5);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void ASpawnBoxInsideAnExcludeSkipsThatVolumeWithOneWarning()
        {
            ParticleWorld world = _kit.World();
            _kit.Volume("Home", Vector3.zero, new Vector3(4f, 4f, 4f), null, world, true, _kit.Bounds(ParticleBoundsMode.Exclude, 0f, 0f));
            _kit.Volume("Pit", new Vector3(10f, 0f, 0f), new Vector3(2f, 2f, 2f), null, world, true, _kit.Bounds(ParticleBoundsMode.Exclude, 0.5f, 0.5f));
            ParticleEmitter emitter = RecyclingEmitter(world, Vector3.zero);
            _kit.Spawn(emitter, ParticleBodyTestKit.At(Vector3.zero), ParticleBodyTestKit.At(new Vector3(10f, 0f, 0f)));
            LogAssert.Expect(LogType.Warning, new Regex("inside the Exclude bounds volume 'Home'"));
            _kit.Frame(world, emitter, 5);
            Assert.AreEqual(0f, Life(emitter, 0).x, "the volume the spawn box is in is skipped");
            Assert.AreEqual(1f, Life(emitter, 1).x, "another Exclude still applies");
            _kit.Frame(world, emitter, 5);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void ABodyBeingRecycledNeverSleeps()
        {
            ParticleWorld world = _kit.World();
            _kit.Volume("Room", Vector3.zero, new Vector3(6f, 6f, 6f), null, world, true, _kit.Bounds(ParticleBoundsMode.Contain, 5f, 5f));
            ParticleEmitter emitter = RecyclingEmitter(world, Vector3.zero, _kit.Behaviour(sleepSpeed: 0.04f));
            var bodies = _kit.Spawn(emitter, ParticleBodyTestKit.At(new Vector3(10f, 0f, 0f)), ParticleBodyTestKit.At(new Vector3(0f, 0f, 0f)));
            _kit.Frame(world, emitter, 60);
            Assert.IsFalse(bodies[0].IsSleeping, "a body mid-recycle stays awake");
            Assert.IsTrue(bodies[1].IsSleeping, "a still body inside the room sleeps");
        }
    }
}

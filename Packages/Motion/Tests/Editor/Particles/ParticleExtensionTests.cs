using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace Yu5h1Lib.Tests
{
    /// <summary>
    ///   Platform checks PEXT-CHECK-01 to 07 of ParticleExtensions 1.0.0 on the GPU backend.
    /// </summary>
    public sealed class ParticleExtensionTests
    {
        const float Dt = 0.02f;
        const string PlatePath = "Packages/com.yu5h1.motion/Tests/Editor/Particles/ParticleExtensionTestPlate.compute";

        sealed class RecordingExtension : IParticleExtension
        {
            readonly string _name;
            readonly List<string> _log;

            public RecordingExtension(string name, ParticleExtensionStage stage, List<string> log,
                ParticleExtensionWrites writes = ParticleExtensionWrites.Positions)
            {
                _name = name;
                _log = log;
                Stage = stage;
                Writes = writes;
            }

            public ParticleExtensionStage Stage { get; }
            public ParticleExtensionWrites Writes { get; }
            public Action<ParticleExtensionContext> onExecute;

            public void Prepare(ParticleExtensionContext context) => _log.Add($"prepare {_name}");

            public void Execute(ParticleExtensionContext context)
            {
                _log.Add(Stage == ParticleExtensionStage.Substep ? $"{_name} {context.SubstepIndex}" : _name);
                onExecute?.Invoke(context);
            }
        }

        sealed class PlateExtension : IParticleExtension
        {
            readonly ComputeShader _shader;
            readonly int _kernel;
            public int prepared;

            public PlateExtension(ComputeShader shader)
            {
                _shader = shader;
                _kernel = shader.FindKernel("PushOutOfPlate");
            }

            public ParticleExtensionStage Stage => ParticleExtensionStage.Substep;
            public ParticleExtensionWrites Writes => ParticleExtensionWrites.Positions;

            public void Prepare(ParticleExtensionContext context)
            {
                prepared++;
                _shader.SetFloat("_PlateY", 0f);
                _shader.SetFloat("_PlateHalfThickness", 0.01f);
            }

            public void Execute(ParticleExtensionContext context)
            {
                context.Bind(_shader, _kernel);
                ParticleExtensionContext.Dispatch(_shader, _kernel, context.World.ActiveParticleCount);
            }
        }

        readonly List<GameObject> _cleanup = new List<GameObject>();
        ParticleTestScope _scope;

        [SetUp]
        public void SetUp()
        {
            if (!SystemInfo.supportsComputeShaders)
                Assert.Ignore("Compute shaders are not supported on this device.");
            _scope = new ParticleTestScope();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _cleanup)
            {
                if (go == null)
                    continue;
                foreach (ParticleHoldBehaviour hold in go.GetComponents<ParticleHoldBehaviour>())
                    hold.DestroyHold();
                UnityEngine.Object.DestroyImmediate(go);
            }
            _cleanup.Clear();
            _scope?.Dispose();
        }

        ParticleWorld World(int substeps = 3, Vector3? gravity = null)
        {
            ParticleWorld world = _scope.World("World");
            world.Settings.substeps = substeps;
            world.Settings.gravity = gravity ?? Vector3.zero;
            world.Settings.damping = 0f;
            world.Settings.groundY = -1000f;
            return world;
        }

        static ParticleReadback Read(ParticleWorld world, ParticleEntity entity)
        {
            ParticleReadback readback = null;
            world.RequestReadback(entity, result => readback = result);
            AsyncGPUReadback.WaitAllRequests();
            Assert.IsNotNull(readback, "The readback did not complete.");
            Assert.IsTrue(readback.Succeeded, readback.Error);
            return readback;
        }

        static void AreClose(Vector3 expected, Vector3 actual, float tolerance, string what) =>
            Assert.That(Vector3.Distance(expected, actual), Is.LessThanOrEqualTo(tolerance),
                $"{what}: expected {expected:F5}, got {actual:F5}");

        PhysicsParticle Net(ParticleWorld world, int columns, int rows, float spacing, Vector3 position)
        {
            var cloth = _scope.Asset<ClothInteraction>();
            cloth.ParticleRadius = 0.02f;
            PhysicsParticle driver = _scope.Driver("Net", _scope.Grid(columns, rows, spacing), cloth, 1);
            driver.World = world;
            driver.transform.position = position;
            driver.EnableDriver();
            return driver;
        }

        T Hold<T>(string name) where T : ParticleHoldBehaviour
        {
            var go = new GameObject(name);
            var hold = go.AddComponent<T>();
            hold.enabled = false;
            _cleanup.Add(go);
            return hold;
        }


        [Test]
        public void Check01_CallOrder_PrepareSubstepsThenPostStep_AndLateRegistrationWaits()
        {
            ParticleWorld world = World();
            Assert.IsTrue(world.TryAllocate(ParticleTestScope.Request(2), out _, out string error), error);
            var log = new List<string>();
            var substep = new RecordingExtension("S", ParticleExtensionStage.Substep, log);
            var postA = new RecordingExtension("A", ParticleExtensionStage.PostStep, log, ParticleExtensionWrites.Velocities);
            var postB = new RecordingExtension("B", ParticleExtensionStage.PostStep, log, ParticleExtensionWrites.Velocities);
            var late = new RecordingExtension("L", ParticleExtensionStage.PostStep, log);
            Assert.IsTrue(world.RegisterExtension(substep, out error), error);
            Assert.IsTrue(world.RegisterExtension(postA, out error), error);
            Assert.IsTrue(world.RegisterExtension(postB, out error), error);
            substep.onExecute = context =>
            {
                if (context.SubstepIndex == 1)
                    world.RegisterExtension(late, out _);
            };

            world.Step(Dt);
            CollectionAssert.AreEqual(
                new[] { "prepare S", "prepare A", "prepare B", "S 0", "S 1", "S 2", "A", "B" }, log);

            substep.onExecute = null;
            log.Clear();
            world.Step(Dt);
            CollectionAssert.AreEqual(
                new[] { "prepare S", "prepare A", "prepare B", "prepare L", "S 0", "S 1", "S 2", "A", "B", "L" }, log);
        }

        [Test]
        public void Check02_SubstepVelocityWritesAreRejected_AndReregisteringKeepsOrder()
        {
            ParticleWorld world = World();
            Assert.IsTrue(world.TryAllocate(ParticleTestScope.Request(2), out _, out string error), error);
            var log = new List<string>();
            var bad = new RecordingExtension("V", ParticleExtensionStage.Substep, log, ParticleExtensionWrites.Velocities);
            Assert.IsFalse(world.RegisterExtension(bad, out error));
            StringAssert.Contains("velocity", error);

            var a = new RecordingExtension("A", ParticleExtensionStage.PostStep, log);
            var b = new RecordingExtension("B", ParticleExtensionStage.PostStep, log);
            world.RegisterExtension(a, out _);
            world.RegisterExtension(b, out _);
            world.RegisterExtension(a, out _);
            world.Step(Dt);
            CollectionAssert.AreEqual(new IParticleExtension[] { a, b }, world.Extensions.ToArray());
            CollectionAssert.AreEqual(new[] { "prepare A", "prepare B", "A", "B" }, log);
        }

        [Test]
        public void Check03_SubstepPushOutHoldsAThinPlate_AndParkedWorldSkipsExtensions()
        {
            var shader = AssetDatabase.LoadAssetAtPath<ComputeShader>(PlatePath);
            Assert.IsNotNull(shader, PlatePath);
            ParticleWorld world = World(substeps: 4);
            var request = new ParticleEntityRequest();
            request.particles.Add(new ParticleSpawn { position = new Vector3(0f, 0.1f, 0f), velocity = new Vector3(0f, -3f, 0f), invMass = 1f, radius = 0.005f });
            Assert.IsTrue(world.TryAllocate(request, out ParticleEntity entity, out string error), error);
            var plate = new PlateExtension(shader);
            Assert.IsTrue(world.RegisterExtension(plate, out error), error);

            for (int step = 0; step < 10; step++)
                world.Step(Dt);
            ParticleReadback readback = Read(world, entity);
            Assert.That(readback.Positions[0].y, Is.GreaterThanOrEqualTo(0.015f - 1e-4f), "The particle passed through the plate.");

            int prepared = plate.prepared;
            entity.SetParked(true);
            world.Step(Dt);
            Assert.AreEqual(prepared, plate.prepared, "An extension ran while no particle was simulated.");
        }

        [Test]
        public void Check04_AnchorFollowsAMovingTransform_AndReleasesOnDisable()
        {
            ParticleWorld world = World(gravity: new Vector3(0f, -9.81f, 0f));
            PhysicsParticle net = Net(world, 5, 1, 0.1f, Vector3.zero);
            Assert.IsTrue(net.TryGetRestPosition(0, out Vector3 end));
            var holder = new GameObject("Holder").transform;
            _cleanup.Add(holder.gameObject);
            holder.position = end;
            var anchor = Hold<ParticleAnchor>("Anchor");
            anchor.Source = net;
            anchor.Bindings.Add(new ParticleAnchor.Binding { target = holder, points = { new ParticleAnchor.Point { index = 0 } } });
            anchor.EnableHold();

            for (int step = 1; step <= 3; step++)
            {
                holder.position = end + new Vector3(0.02f * step, 0f, 0f);
                world.Step(Dt);
            }
            ParticleReadback readback = Read(world, net.Entities[0]);
            AreClose(holder.position, readback.Positions[0], 1e-4f, "anchored end");
            AreClose(new Vector3(1f, 0f, 0f), readback.Velocities[0], 1e-2f, "anchored end velocity");

            anchor.DisableHold();
            float before = readback.Positions[0].y;
            for (int step = 0; step < 5; step++)
                world.Step(Dt);
            readback = Read(world, net.Entities[0]);
            Assert.That(readback.Positions[0].y, Is.LessThan(before - 1e-3f), "The released end did not start falling.");
        }

        [Test]
        public void Check05_GrabCarriesParticlesWithTheHand_AndReleasesAtHandVelocity()
        {
            ParticleWorld world = World();
            PhysicsParticle net = Net(world, 9, 9, 0.1f, Vector3.zero);
            ParticleEntity entity = net.Entities[0];
            world.Step(Dt);
            Vector3[] rest = Read(world, entity).Positions;

            var grabber = Hold<ParticleGrabber>("Hand");
            grabber.Target = net;
            grabber.Radius = 0.15f;
            Transform hand = grabber.transform;
            hand.position = new Vector3(0.03f, 0.02f, 0f);
            grabber.EnableHold();
            world.Step(Dt);
            Matrix4x4 grabPose = hand.worldToLocalMatrix;
            ParticleGrabResult? result = null;
            grabber.Grabbed += r => result = r;
            Assert.IsTrue(grabber.Grab());
            Assert.IsFalse(grabber.Grab(), "A second grab before release was accepted.");
            world.Step(Dt);
            AsyncGPUReadback.WaitAllRequests();

            int[] grabbed = Enumerable.Range(0, rest.Length)
                .Where(i => Vector3.Distance(rest[i], hand.position) <= grabber.Radius).ToArray();
            Assert.IsNotNull(result, "The grab count did not arrive.");
            Assert.AreEqual(grabbed.Length, result.Value.Count);
            Assert.AreEqual(world.StepCount, result.Value.Step);

            Vector3 start = hand.position;
            Quaternion startRotation = hand.rotation;
            for (int step = 1; step <= 10; step++)
            {
                hand.SetPositionAndRotation(
                    start + new Vector3(0.05f * step, 0f, 0f),
                    Quaternion.AngleAxis(9f * step, Vector3.up) * startRotation);
                world.Step(Dt);
            }
            Vector3[] carried = Read(world, entity).Positions;
            foreach (int i in grabbed)
                AreClose(hand.TransformPoint(grabPose.MultiplyPoint3x4(rest[i])), carried[i], 2e-4f, $"grabbed particle {i}");

            hand.position += new Vector3(0.02f, 0f, 0f);
            world.Step(Dt);
            ParticleReadback last = Read(world, entity);
            foreach (int i in grabbed)
                AreClose(new Vector3(1f, 0f, 0f), last.Velocities[i], 1e-2f, $"velocity of particle {i} at release");
            grabber.Release();
            Assert.IsFalse(grabber.IsGrabbing);
            hand.position += new Vector3(1f, 0f, 0f);
            world.Step(Dt);
            Vector3[] after = Read(world, entity).Positions;
            foreach (int i in grabbed)
                Assert.That(Vector3.Distance(hand.TransformPoint(grabPose.MultiplyPoint3x4(rest[i])), after[i]), Is.GreaterThan(0.5f),
                    $"Particle {i} still follows the hand after release.");
        }

        [Test]
        public void Check06_OneHolderKeepsThreePointsApart_SkipsParkedSource_AndRepinsAfterReset()
        {
            ParticleWorld world = World(gravity: new Vector3(0f, -9.81f, 0f));
            PhysicsParticle net = Net(world, 4, 4, 0.1f, Vector3.zero);
            PhysicsParticle other = Net(world, 2, 1, 0.1f, new Vector3(5f, 0f, 0f));
            var holder = new GameObject("Holder").transform;
            _cleanup.Add(holder.gameObject);
            holder.SetPositionAndRotation(new Vector3(0.3f, 0.2f, 0f), Quaternion.AngleAxis(30f, Vector3.forward));
            var anchor = Hold<ParticleAnchor>("Anchor");
            anchor.Source = net;
            var binding = new ParticleAnchor.Binding { target = holder };
            foreach (int index in new[] { 12, 13, 15 })
            {
                Assert.IsTrue(net.TryGetRestPosition(index, out Vector3 rest));
                binding.points.Add(new ParticleAnchor.Point { index = index, localOffset = holder.InverseTransformPoint(rest) });
            }
            anchor.Bindings.Add(binding);
            anchor.EnableHold();

            holder.position += new Vector3(0f, 0.5f, 0f);
            world.Step(Dt);
            world.Step(Dt);
            ParticleReadback readback = Read(world, net.Entities[0]);
            foreach (ParticleAnchor.Point point in binding.points)
                AreClose(holder.TransformPoint(point.localOffset), readback.Positions[point.index], 1e-4f, $"point {point.index}");

            net.DisableDriver();
            Vector3[] parked = Read(world, net.Entities[0]).Positions;
            holder.position += new Vector3(0f, 0.5f, 0f);
            world.Step(Dt);
            world.Step(Dt);
            Vector3[] stillParked = Read(world, net.Entities[0]).Positions;
            for (int i = 0; i < parked.Length; i++)
                AreClose(parked[i], stillParked[i], 0f, $"parked particle {i}");

            net.EnableDriver();
            world.Step(Dt);
            world.Step(Dt);
            readback = Read(world, net.Entities[0]);
            foreach (ParticleAnchor.Point point in binding.points)
                AreClose(holder.TransformPoint(point.localOffset), readback.Positions[point.index], 1e-4f, $"point {point.index} after reset");
            Assert.IsTrue(other.Entities[0].IsValid);
        }

        [Test]
        public void Check07_GrabCountArrivesAsynchronously_AndSkipsAnchoredParticles()
        {
            ParticleWorld world = World();
            PhysicsParticle net = Net(world, 5, 5, 0.1f, Vector3.zero);
            Assert.IsTrue(net.TryGetRestPosition(12, out Vector3 center));
            var anchor = Hold<ParticleAnchor>("Anchor");
            anchor.Source = net;
            anchor.Bindings.Add(new ParticleAnchor.Binding
            {
                target = anchor.transform,
                points = { new ParticleAnchor.Point { index = 12 } }
            });
            anchor.transform.position = center;
            anchor.EnableHold();
            world.Step(Dt);

            var grabber = Hold<ParticleGrabber>("Hand");
            grabber.Target = net;
            grabber.Radius = 0.11f;
            grabber.transform.position = center;
            grabber.EnableHold();
            var results = new List<ParticleGrabResult>();
            grabber.Grabbed += results.Add;
            world.Step(Dt);
            grabber.Grab();
            world.Step(Dt);
            int grabStep = world.StepCount;
            AsyncGPUReadback.WaitAllRequests();

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(4, results[0].Count, "Expected the four neighbours within 0.11 m, without the anchored centre.");
            Assert.AreEqual(grabStep, results[0].Step);
        }
    }
}

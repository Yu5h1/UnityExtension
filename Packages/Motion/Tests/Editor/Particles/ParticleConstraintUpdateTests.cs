using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace Yu5h1Lib.Tests
{
    /// <summary>
    ///   Platform check PWORLD-CHECK-18 of ParticleWorld 1.1.0 (WORLD-16) on the GPU backend. The
    ///   expected stretch of a hanging particle at the end of a substep is the single-iteration
    ///   XPBD equilibrium compliance × m × g: the correction cancels the substep's gravity drop
    ///   g × h², which needs a predicted stretch of g × h² × (w + α̃) / w (ParticleSolver SOLVER-02,
    ///   SOLVER-03, SOLVER-16).
    ///   Two substeps keep the per-substep correction (about 1e-3 m) far above the 1e-5 m
    ///   accumulation resolution (SOLVER-05), which otherwise shifts this equilibrium.
    /// </summary>
    public sealed class ParticleConstraintUpdateTests
    {
        const float Dt = 0.02f;
        const int Substeps = 2;
        const float Gravity = 9.81f;

        ParticleTestScope _scope;

        [SetUp]
        public void SetUp()
        {
            if (!SystemInfo.supportsComputeShaders)
                Assert.Ignore("Compute shaders are not supported on this device.");
            _scope = new ParticleTestScope();
        }

        [TearDown]
        public void TearDown() => _scope?.Dispose();

        ParticleWorld World()
        {
            ParticleWorld world = _scope.World("World");
            world.Settings.substeps = Substeps;
            world.Settings.gravity = new Vector3(0f, -Gravity, 0f);
            world.Settings.damping = 5f;
            world.Settings.groundY = -1000f;
            return world;
        }

        static ParticleEntityRequest Pendulum(float compliance, float breakForce)
        {
            var request = new ParticleEntityRequest { groupMode = ParticleGroupMode.NewGroup };
            request.particles.Add(new ParticleSpawn { position = Vector3.zero, invMass = 0f, radius = 0.01f });
            request.particles.Add(new ParticleSpawn { position = new Vector3(0.5f, 0f, 0f), invMass = 1f, radius = 0.01f });
            request.constraints.Add(new DistanceConstraintSpec { a = 0, b = 1, restLength = 0.5f, compliance = compliance, breakForce = breakForce });
            return request;
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

        static float Stretch(ParticleReadback readback) => readback.Positions[1].magnitude - 0.5f;

        static float ExpectedStretch(float compliance) => compliance * Gravity;

        static void Run(ParticleWorld world, int steps)
        {
            for (int i = 0; i < steps; i++)
                world.Step(Dt);
        }

        [Test]
        public void Check18_DoublingComplianceDoublesTheStretch_WithoutResetting()
        {
            ParticleWorld world = World();
            Assert.IsTrue(world.TryAllocate(Pendulum(1e-3f, 0f), out ParticleEntity entity, out string error), error);
            Run(world, 250);
            ParticleReadback settled = Read(world, entity);
            Assert.That(Stretch(settled), Is.EqualTo(ExpectedStretch(1e-3f)).Within(5).Percent);

            var doubled = new[] { new DistanceConstraintSpec { a = 0, b = 1, restLength = float.NaN, compliance = 2e-3f } };
            Assert.IsTrue(entity.SetConstraintParameters(doubled, out error), error);
            world.Step(Dt);
            ParticleReadback next = Read(world, entity);
            Assert.That(Vector3.Distance(settled.Positions[1], next.Positions[1]), Is.LessThan(0.01f),
                "The entity was reset instead of keeping its state.");

            Run(world, 250);
            Assert.That(Stretch(Read(world, entity)), Is.EqualTo(ExpectedStretch(2e-3f)).Within(5).Percent);
        }

        [Test]
        public void Check18_BrokenConstraintStaysBroken_AndMismatchedUpdatesAreRejected()
        {
            ParticleWorld world = World();
            Assert.IsTrue(world.TryAllocate(Pendulum(0f, 1e-6f), out ParticleEntity entity, out string error), error);
            world.Step(Dt);
            Assert.IsTrue(Read(world, entity).BrokenConstraints[0], "The weak constraint did not break.");

            var unbreakable = new[] { new DistanceConstraintSpec { a = 0, b = 1, restLength = float.NaN } };
            Assert.IsTrue(entity.SetConstraintParameters(unbreakable, out error), error);
            Run(world, 10);
            ParticleReadback readback = Read(world, entity);
            Assert.IsTrue(readback.BrokenConstraints[0], "Updating the parameters restored a broken constraint.");
            Assert.That(readback.Positions[1].y, Is.LessThan(-0.05f), "The freed particle did not fall.");

            Assert.IsFalse(entity.SetConstraintParameters(new DistanceConstraintSpec[0], out error));
            StringAssert.Contains("constraints", error);
            var swapped = new[] { new DistanceConstraintSpec { a = 1, b = 0, restLength = float.NaN } };
            Assert.IsFalse(entity.SetConstraintParameters(swapped, out error));
        }

        [Test]
        public void Check18_DriverHandsChangedInteractionToItsEntities()
        {
            ParticleWorld world = World();
            var chain = _scope.Asset<ChainParticleLayout>();
            chain.Count = 4;
            var rope = _scope.Asset<ChainInteraction>();
            rope.Material.compliance = 1e-4f;
            PhysicsParticle driver = _scope.Driver("Rope", chain, rope, 1);
            driver.World = world;
            driver.EnableDriver();

            rope.Material.compliance = 3e-4f;
            rope.MarkChanged();
            driver.ApplyInteraction();
            Assert.AreEqual(3e-4f, world.RecordOf(driver.Entities[0]).rest.constraints[0].compliance);
            Assert.AreEqual(1, world.PendingChanges.Count(c => c.kind == ParticleWorldChangeKind.SetConstraints));
        }
    }
}

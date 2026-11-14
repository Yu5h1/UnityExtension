using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Yu5h1Lib.Tests
{
    /// <summary>
    ///   PhysicsParticle driver, profile recipes and collider registration (PWORLD-CHECK-02, 03, 11,
    ///   13, 14 and 17 in ParticleWorld.md, and the profile compatibility rules).
    /// </summary>
    public sealed class PhysicsParticleTests
    {
        ParticleTestScope _scope;

        [SetUp]
        public void SetUp() => _scope = new ParticleTestScope();

        [TearDown]
        public void TearDown() => _scope.Dispose();

        ClothInteraction Cloth(bool shear = true, bool bend = false)
        {
            var cloth = _scope.Asset<ClothInteraction>();
            cloth.Shear = shear;
            cloth.Bend = bend;
            return cloth;
        }

        [Test]
        public void GridLayout_IsCenteredInXY_WithColumnRowRoles()
        {
            GridParticleLayout grid = _scope.Grid(3, 2, 0.5f);
            var positions = new List<Vector3>();

            grid.GetRestPositions(0, positions);

            Assert.AreEqual(6, positions.Count);
            Assert.AreEqual(new Vector3(-0.5f, -0.25f, 0f), positions[0]);
            Assert.AreEqual(new Vector3(0.5f, 0.25f, 0f), positions[5]);
            Assert.AreEqual(new Vector3Int(2, 1, 0), grid.GetRole(0, 5));
        }

        [Test]
        public void ClothRecipe_BuildsStructuralShearAndBendLinks()
        {
            GridParticleLayout grid = _scope.Grid(4, 3);

            ParticleEntityRequest structuralOnly = PhysicsParticle.BuildRequest(grid, Cloth(shear: false), 0, Pose.identity, 1u);
            ParticleEntityRequest withShear = PhysicsParticle.BuildRequest(grid, Cloth(), 0, Pose.identity, 1u);
            ParticleEntityRequest withBend = PhysicsParticle.BuildRequest(grid, Cloth(shear: false, bend: true), 0, Pose.identity, 1u);

            Assert.AreEqual(3 * 3 + 4 * 2, structuralOnly.constraints.Count);
            Assert.AreEqual(17 + 2 * 3 * 2, withShear.constraints.Count);
            Assert.AreEqual(17 + 2 * 3 + 4 * 1, withBend.constraints.Count);
            Assert.IsTrue(withShear.TryValidate(out _));
        }

        [Test]
        public void ChainRecipe_LinksNeighboursAndBendsEveryOther()
        {
            var chain = _scope.Asset<ChainParticleLayout>();
            chain.Count = 5;
            var rope = _scope.Asset<ChainInteraction>();

            ParticleEntityRequest request = PhysicsParticle.BuildRequest(chain, rope, 0, Pose.identity, 1u);

            Assert.AreEqual(4 + 3, request.constraints.Count);
            Assert.AreEqual(new Vector3(0f, -0.4f, 0f), request.particles[4].position);
        }

        [Test]
        public void RigidRecipe_PutsEveryParticleInOneGroup_PerVariant()
        {
            var points = _scope.Asset<PointParticleLayout>();
            points.Variants = new[]
            {
                new PointParticleLayout.Variant { positions = new Vector3[4] },
                new PointParticleLayout.Variant { positions = new Vector3[6] }
            };
            var rigid = _scope.Asset<RigidGroupInteraction>();

            ParticleEntityRequest six = PhysicsParticle.BuildRequest(points, rigid, 1, Pose.identity, 1u);

            Assert.AreEqual(6, six.particles.Count);
            CollectionAssert.AreEqual(Enumerable.Range(0, 6), six.rigidGroups.Single());
            Assert.AreEqual(ParticleGroupMode.NewGroup, six.groupMode);
        }

        [Test]
        public void Validation_ReportsIncompatibleProfilesAsErrors()
        {
            var chain = _scope.Asset<ChainParticleLayout>();
            chain.Count = 2;
            PhysicsParticle driver = _scope.Driver("Driver", chain, Cloth(), 1);
            var articulated = _scope.Asset<ArticulatedMeshAppearance>();
            driver.Appearance = articulated;
            var messages = new List<PhysicsParticleMessage>();

            driver.Validate(messages);

            string[] errors = messages.Where(m => m.severity == PhysicsParticleMessageSeverity.Error).Select(m => m.text).ToArray();
            Assert.IsTrue(errors.Any(e => e.Contains("does not accept a Chain layout")));
            Assert.IsTrue(errors.Any(e => e.Contains("at least 3 control particles")));
            Assert.IsTrue(errors.Any(e => e.Contains("no material")));
        }

        [Test]
        public void Driver_DoesNotSpawn_WhenProfilesAreIncompatible()
        {
            ParticleWorld world = _scope.World("World");
            PhysicsParticle driver = _scope.Driver("Driver", _scope.Asset<ChainParticleLayout>(), Cloth(), 2);

            LogAssert.Expect(LogType.Error, new Regex("did not spawn"));
            driver.EnableDriver();

            Assert.IsEmpty(driver.Entities);
            Assert.AreEqual(0, world.UsedParticles);
        }

        [Test]
        public void Driver_SpawnsParksRespawnsAndReleases()
        {
            ParticleWorld world = _scope.World("World");
            PhysicsParticle driver = _scope.Driver("Driver", _scope.Grid(3, 2), Cloth(), 2);
            driver.transform.position = new Vector3(0f, 5f, 0f);

            driver.EnableDriver();
            Assert.AreEqual(2, driver.Entities.Count);
            Assert.AreEqual(12, world.UsedParticles);
            Assert.AreEqual(1u, driver.Entities[0].Category);
            Assert.AreEqual(new Vector3(1f, 5f, 0f), driver.Entities[1].Pose.position);
            Assert.AreEqual(new Vector3Int(2, 1, 0), driver.Entities[0].Roles[5]);
            Assert.AreEqual(11, driver.Entities[0].Constraints.length);

            driver.DisableDriver();
            Assert.IsTrue(driver.Entities.All(e => e.IsParked));
            Assert.AreEqual(0, world.ActiveParticleCount);

            world.Step(0.02f);
            driver.EnableDriver();
            Assert.AreEqual(12, world.UsedParticles);
            Assert.IsTrue(driver.Entities.All(e => !e.IsParked));
            CollectionAssert.AreEqual(
                new[] { ParticleWorldChangeKind.Reset, ParticleWorldChangeKind.SetParked, ParticleWorldChangeKind.Reset, ParticleWorldChangeKind.SetParked },
                world.PendingChanges.Select(c => c.kind));

            driver.DestroyDriver();
            Assert.AreEqual(0, world.UsedParticles);
        }

        [Test]
        public void Driver_RejectsABatchThatDoesNotFit_WithoutPartialEntities()
        {
            ParticleWorld world = _scope.World("World", particles: 10);
            PhysicsParticle driver = _scope.Driver("Driver", _scope.Grid(3, 2), Cloth(), 2);

            LogAssert.Expect(LogType.Error, new Regex("needs 12 particles"));
            driver.EnableDriver();

            Assert.IsEmpty(driver.Entities);
            Assert.AreEqual(0, world.UsedParticles);
        }

        [Test]
        public void Driver_UsesItsLayerAsCategory()
        {
            _scope.World("World");
            PhysicsParticle driver = _scope.Driver("Driver", _scope.Grid(2, 2), Cloth(), 1);
            driver.gameObject.layer = 5;

            driver.EnableDriver();

            Assert.AreEqual(1u << 5, driver.Entities[0].Category);
        }

        [Test]
        public void Driver_MovesToTheNewWorld_WhenTheResolvedWorldChanges()
        {
            ParticleWorld first = _scope.World("First");
            PhysicsParticle driver = _scope.Driver("Driver", _scope.Grid(2, 2), Cloth(), 1);
            driver.EnableDriver();

            first.DestroyWorld();
            ParticleWorld second = _scope.World("Second");

            Assert.AreSame(second, driver.BoundWorld);
            Assert.AreEqual(4, second.UsedParticles);
            Assert.AreSame(second, driver.Entities.Single().World);
        }

        [Test]
        public void Driver_StaysWithADisabledWorld_WhenNoOtherWorldIsActive()
        {
            ParticleWorld world = _scope.World("World");
            PhysicsParticle driver = _scope.Driver("Driver", _scope.Grid(2, 2), Cloth(), 1);
            driver.EnableDriver();

            world.DisableWorld();

            Assert.AreSame(world, driver.BoundWorld);
            Assert.IsTrue(driver.Entities.Single().IsValid);
        }

        [Test]
        public void Driver_SpawnsWhenTheWorldIsEnabledAfterIt()
        {
            ParticleWorld world = _scope.World("World", enable: false);
            PhysicsParticle driver = _scope.Driver("Driver", _scope.Grid(2, 2), Cloth(), 1);

            driver.EnableDriver();
            Assert.IsEmpty(driver.Entities);

            world.EnableWorld();
            Assert.AreEqual(1, driver.Entities.Count);
        }

        [Test]
        public void Collider_RegistersInEitherEnableOrder_AndAlwaysUnregistersWhenDisabled()
        {
            ParticleWorld worldFirst = _scope.World("WorldFirst");
            var early = _scope.Collider<ParticleBoxCollider>("Early");
            early.EnableCollider();
            Assert.AreSame(worldFirst, early.BoundWorld);
            early.DisableCollider();
            Assert.AreEqual(0, worldFirst.ColliderCount(ParticleColliderShape.Box));
            worldFirst.DestroyWorld();

            var late = _scope.Collider<ParticleSphereCollider>("Late");
            late.EnableCollider();
            Assert.IsNull(late.BoundWorld);
            ParticleWorld worldSecond = _scope.World("WorldSecond");
            Assert.AreSame(worldSecond, late.BoundWorld);
            Assert.AreEqual(1, worldSecond.ColliderCount(ParticleColliderShape.Sphere));
        }

        [Test]
        public void Collider_MovesToTheNewWorld_AndRegistrationIsSilentByDefault()
        {
            ParticleWorld first = _scope.World("First");
            var collider = _scope.Collider<ParticleCapsuleCollider>("Capsule");
            collider.EnableCollider();

            first.DestroyWorld();
            ParticleWorld second = _scope.World("Second");

            Assert.AreSame(second, collider.BoundWorld);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Collider_BeyondCapacity_IsRejectedWithAnError()
        {
            ParticleWorld world = _scope.World("World");
            world.Capacity.boxColliders = 1;
            _scope.Collider<ParticleBoxCollider>("BoxA").EnableCollider();
            var second = _scope.Collider<ParticleBoxCollider>("BoxB");

            LogAssert.Expect(LogType.Error, new Regex("already holds 1 Box colliders"));
            second.EnableCollider();

            Assert.IsNull(second.BoundWorld);
            Assert.AreEqual(1, world.ColliderCount(ParticleColliderShape.Box));
        }

        [Test]
        public void ColliderShapes_FollowTheTransform()
        {
            var sphere = _scope.Collider<ParticleSphereCollider>("Sphere");
            sphere.transform.localScale = new Vector3(2f, 4f, 1f);
            var capsule = _scope.Collider<ParticleCapsuleCollider>("Capsule");
            capsule.transform.position = new Vector3(0f, 1f, 0f);
            var box = _scope.Collider<ParticleBoxCollider>("Box");
            box.transform.localScale = new Vector3(2f, -4f, 6f);
            box.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            ParticleColliderState s = sphere.Capture(0.02f);
            ParticleColliderState c = capsule.Capture(0.02f);
            ParticleColliderState b = box.Capture(0.02f);

            Assert.AreEqual(2f, s.radius, 1e-6f);
            Assert.AreEqual(0.5f, c.radius, 1e-6f);
            Assert.AreEqual(new Vector3(0f, 0.5f, 0f), c.pointA);
            Assert.AreEqual(new Vector3(0f, 1.5f, 0f), c.pointB);
            Assert.That(Vector3.Distance(b.halfExtents, new Vector3(1f, 2f, 3f)), Is.LessThan(1e-5f));
            Assert.That(Vector3.Distance(b.axisX, new Vector3(0f, 0f, -1f)), Is.LessThan(1e-5f));
            Assert.AreEqual(uint.MaxValue, b.mask);
        }

        [Test]
        public void ColliderSurfaceVelocity_ComesFromThePoseChange_AndIsZeroAfterATeleport()
        {
            var box = _scope.Collider<ParticleBoxCollider>("Box");

            Assert.AreEqual(Vector3.zero, box.Capture(0.02f).linearVelocity);
            box.transform.position = new Vector3(0.02f, 0f, 0f);
            Assert.That(Vector3.Distance(box.Capture(0.02f).linearVelocity, new Vector3(1f, 0f, 0f)), Is.LessThan(1e-4f));
            box.transform.rotation = Quaternion.Euler(0f, 0f, 1.8f);
            Assert.That(Vector3.Distance(box.Capture(0.02f).angularVelocity, new Vector3(0f, 0f, Mathf.PI / 2f)), Is.LessThan(1e-3f));
            box.transform.position = new Vector3(10f, 0f, 0f);
            box.MarkTeleported();
            Assert.AreEqual(Vector3.zero, box.Capture(0.02f).linearVelocity);
        }

        [Test]
        public void World_PassesCollidersToTheBackend_ByShapeThenRegistrationOrder()
        {
            ParticleWorld world = _scope.World("World");
            var backend = new RecordingBackend();
            world.Backend = backend;
            _scope.Collider<ParticleBoxCollider>("Box").EnableCollider();
            _scope.Collider<ParticleSphereCollider>("Sphere").EnableCollider();
            world.TryAllocate(ParticleTestScope.Request(1), out _, out _);

            world.Step(0.02f);

            Assert.AreEqual(2, backend.colliderCountAtLastStep);
            CollectionAssert.AreEqual(
                new[] { ParticleColliderShape.Sphere, ParticleColliderShape.Box },
                world.ColliderStates.Select(s => s.shape));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Yu5h1Lib.Tests
{
    /// <summary>
    ///   ParticleWorld 1.0.0 checks that need no solver: world resolution, allocation and capacity,
    ///   release and reuse, step-boundary changes, teleport, idle cost and grouping
    ///   (PWORLD-CHECK-01, 04, 06, 07, 08, 10 and 12 in ParticleWorld.md).
    /// </summary>
    public sealed class ParticleWorldTests
    {
        ParticleTestScope _scope;

        [SetUp]
        public void SetUp() => _scope = new ParticleTestScope();

        [TearDown]
        public void TearDown() => _scope.Dispose();

        [Test]
        public void Resolve_PicksTheOnlyActiveWorld_AndNeverAnInactiveOne()
        {
            _scope.World("Inactive", enable: false);
            ParticleWorld active = _scope.World("Active");

            Assert.IsTrue(ParticleWorld.TryResolve(null, out ParticleWorld resolved, out _));
            Assert.AreSame(active, resolved);
        }

        [Test]
        public void Resolve_FailsAndNamesCandidates_WhenTwoWorldsAreActive()
        {
            _scope.World("WorldA");
            _scope.World("WorldB");

            Assert.IsFalse(ParticleWorld.TryResolve(null, out _, out string error));
            StringAssert.Contains("WorldA", error);
            StringAssert.Contains("WorldB", error);
        }

        [Test]
        public void Resolve_Fails_WhenNoWorldIsActive_OrThePreferredOneIsInactive()
        {
            ParticleWorld inactive = _scope.World("Inactive", enable: false);

            Assert.IsFalse(ParticleWorld.TryResolve(null, out _, out _));
            Assert.IsFalse(ParticleWorld.TryResolve(inactive, out _, out string error));
            StringAssert.Contains("Inactive", error);
        }

        [Test]
        public void AllOrNothing_RejectsTheWholeBatch_WhenCapacityIsShort()
        {
            ParticleWorld world = _scope.World("World", particles: 10);
            var results = new List<ParticleEntity>();

            int created = world.Allocate(ParticleTestScope.Requests(3, 4), results, ParticleAllocationMode.AllOrNothing, out string error);

            Assert.AreEqual(0, created);
            Assert.IsEmpty(results);
            Assert.AreEqual(0, world.UsedParticles);
            Assert.IsEmpty(world.PendingChanges);
            StringAssert.Contains("needs", error);
            StringAssert.Contains("remain", error);
        }

        [Test]
        public void BestEffort_CreatesOnlyCompleteEntities()
        {
            ParticleWorld world = _scope.World("World", particles: 10);
            var results = new List<ParticleEntity>();

            int created = world.Allocate(ParticleTestScope.Requests(3, 4), results, ParticleAllocationMode.BestEffort, out _);

            Assert.AreEqual(2, created);
            Assert.AreEqual(8, world.UsedParticles);
            Assert.IsTrue(results.All(e => e.Particles.length == 4));
        }

        [Test]
        public void InvalidRequest_RejectsTheWholeCall()
        {
            ParticleWorld world = _scope.World("World");
            ParticleEntityRequest bad = ParticleTestScope.Request(2);
            bad.constraints.Add(new DistanceConstraintSpec { a = 0, b = 5, restLength = float.NaN });

            int created = world.Allocate(new[] { ParticleTestScope.Request(2), bad }, new List<ParticleEntity>(), ParticleAllocationMode.BestEffort, out string error);

            Assert.AreEqual(0, created);
            StringAssert.Contains("Request 1", error);
        }

        [Test]
        public void ZeroRadius_IsRejected()
        {
            ParticleWorld world = _scope.World("World");
            ParticleEntityRequest request = ParticleTestScope.Request(1);
            request.particles[0] = new ParticleSpawn { invMass = 1f, radius = 0f };

            Assert.IsFalse(world.TryAllocate(request, out _, out string error));
            StringAssert.Contains("radius", error);
        }

        [Test]
        public void ReleaseAndReuse_KeepsUsageStable_Over50Cycles()
        {
            ParticleWorld world = _scope.World("World", particles: 64);
            Assert.IsTrue(world.TryAllocate(ParticleTestScope.Request(4, 3), out ParticleEntity keep, out _));
            int baselineParticles = world.UsedParticles;
            int baselineConstraints = world.UsedConstraints;

            for (int cycle = 0; cycle < 50; cycle++)
            {
                Assert.IsTrue(world.TryAllocate(ParticleTestScope.Request(6, 5), out ParticleEntity a, out _));
                Assert.IsTrue(world.TryAllocate(ParticleTestScope.Request(3, 2), out ParticleEntity b, out _));
                a.Release();
                a.Release();
                b.Release();
                Assert.AreEqual(baselineParticles, world.UsedParticles);
                Assert.AreEqual(baselineConstraints, world.UsedConstraints);
                Assert.IsTrue(keep.IsValid);
            }
        }

        [Test]
        public void ReleasedHandle_IsInvalid_AndAccessThrows()
        {
            ParticleWorld world = _scope.World("World");
            world.TryAllocate(ParticleTestScope.Request(2), out ParticleEntity entity, out _);

            entity.Release();

            Assert.IsFalse(entity.IsValid);
            Assert.Throws<InvalidOperationException>(() => { RangeInt unused = entity.Particles; });
            Assert.Throws<InvalidOperationException>(() => entity.SetParked(true));
        }

        [Test]
        public void ReusedSlot_DoesNotRevalidateAnOldHandle()
        {
            ParticleWorld world = _scope.World("World");
            world.TryAllocate(ParticleTestScope.Request(2), out ParticleEntity first, out _);
            first.Release();

            world.TryAllocate(ParticleTestScope.Request(2), out ParticleEntity second, out _);

            Assert.IsFalse(first.IsValid);
            Assert.IsTrue(second.IsValid);
            Assert.AreNotEqual(first, second);
        }

        [Test]
        public void Changes_ReachTheBackendInCallOrder_OnlyAtTheNextStep()
        {
            ParticleWorld world = _scope.World("World");
            var backend = new RecordingBackend();
            world.Backend = backend;

            world.TryAllocate(ParticleTestScope.Request(2), out ParticleEntity a, out _);
            a.Release();
            world.TryAllocate(ParticleTestScope.Request(2), out ParticleEntity b, out _);
            b.SetParked(true);

            Assert.IsEmpty(backend.applied);
            world.Step(0.02f);
            CollectionAssert.AreEqual(
                new[] { ParticleWorldChangeKind.Upload, ParticleWorldChangeKind.Release, ParticleWorldChangeKind.Upload, ParticleWorldChangeKind.SetParked },
                backend.applied);
            Assert.IsEmpty(world.PendingChanges);
        }

        [Test]
        public void Step_RunsTheSolverOnlyWhileSomeParticleIsActive()
        {
            ParticleWorld world = _scope.World("World");
            var backend = new RecordingBackend();
            world.Backend = backend;

            world.Step(0.02f);
            world.TryAllocate(ParticleTestScope.Request(3), out ParticleEntity entity, out _);
            entity.SetParked(true);
            world.Step(0.02f);
            Assert.IsEmpty(backend.steppedActiveCounts);

            entity.SetParked(false);
            world.Step(0.02f);
            CollectionAssert.AreEqual(new[] { 3 }, backend.steppedActiveCounts);
        }

        [Test]
        public void Step_DoesNothing_ForInvalidDeltaTime_OrSettings()
        {
            ParticleWorld world = _scope.World("World");
            var backend = new RecordingBackend();
            world.Backend = backend;
            world.TryAllocate(ParticleTestScope.Request(1), out _, out _);

            world.Step(0f);
            world.Step(float.NaN);
            Assert.IsEmpty(backend.applied);

            world.Settings.substeps = 0;
            UnityEngine.TestTools.LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("substeps"));
            world.Step(0.02f);
            Assert.IsEmpty(backend.steppedActiveCounts);
        }

        [Test]
        public void Teleport_CarriesTheRigidMoveBetweenPoses_AndTheVelocity()
        {
            ParticleWorld world = _scope.World("World");
            ParticleEntityRequest request = ParticleTestScope.Request(1);
            request.pose = new Pose(new Vector3(1f, 0f, 0f), Quaternion.identity);
            world.TryAllocate(request, out ParticleEntity entity, out _);
            world.Step(0.02f);
            var target = new Pose(new Vector3(11f, 2f, 0f), Quaternion.Euler(0f, 90f, 0f));

            entity.Teleport(target, new Vector3(0f, 3f, 0f));

            ParticleWorldChange change = world.PendingChanges.Single();
            Assert.AreEqual(ParticleWorldChangeKind.Teleport, change.kind);
            Vector3 moved = change.transformDelta.MultiplyPoint3x4(new Vector3(1f, 0f, 0f) + new Vector3(0f, 0f, 1f));
            Assert.That(Vector3.Distance(moved, new Vector3(12f, 2f, 0f)), Is.LessThan(1e-5f));
            Assert.AreEqual(new Vector3(0f, 3f, 0f), change.velocity);
            Assert.AreEqual(target.position, entity.Pose.position);
        }

        [Test]
        public void UnspecifiedRestLength_IsTheDistanceAtRest()
        {
            ParticleWorld world = _scope.World("World");
            ParticleEntityRequest request = ParticleTestScope.Request(2, 1);
            request.particles[1] = new ParticleSpawn { position = new Vector3(0f, 3f, 4f), invMass = 1f, radius = 0.1f };
            request.particles[0] = new ParticleSpawn { position = Vector3.zero, invMass = 1f, radius = 0.1f };

            world.TryAllocate(request, out _, out _);

            Assert.AreEqual(5f, world.PendingChanges[0].request.constraints[0].restLength, 1e-6f);
            Assert.IsTrue(float.IsNaN(request.constraints[0].restLength));
        }

        [Test]
        public void NewGroups_AreDistinctAndNonZero_AndReusedAfterRelease()
        {
            ParticleWorld world = _scope.World("World");
            ParticleEntityRequest grouped = ParticleTestScope.Request(1);
            grouped.groupMode = ParticleGroupMode.NewGroup;

            world.TryAllocate(grouped, out ParticleEntity a, out _);
            world.TryAllocate(grouped, out ParticleEntity b, out _);
            world.TryAllocate(ParticleTestScope.Request(1), out ParticleEntity shared, out _);

            Assert.AreNotEqual(0, a.Group);
            Assert.AreNotEqual(a.Group, b.Group);
            Assert.AreEqual(0, shared.Group);
            int released = a.Group;
            a.Release();
            world.TryAllocate(grouped, out ParticleEntity c, out _);
            Assert.AreEqual(released, c.Group);
        }

        [Test]
        public void DestroyedWorld_InvalidatesItsEntities_AndDisposesTheBackend()
        {
            ParticleWorld world = _scope.World("World");
            var backend = new RecordingBackend();
            world.Backend = backend;
            world.TryAllocate(ParticleTestScope.Request(2), out ParticleEntity entity, out _);

            world.DestroyWorld();

            Assert.IsFalse(entity.IsValid);
            Assert.IsTrue(backend.disposed);
            Assert.IsFalse(ParticleWorld.ActiveWorlds.Contains(world));
        }
    }
}

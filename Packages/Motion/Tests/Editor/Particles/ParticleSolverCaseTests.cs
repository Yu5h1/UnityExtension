using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace Yu5h1Lib.Tests
{
    /// <summary>
    ///   Replays ParticleSolverCases.json through the public ParticleWorld entry points on the GPU
    ///   backend and compares the asynchronously read state with each case's expected values. The
    ///   adapter only translates case data into worlds, entities, colliders and media; it computes
    ///   no expected value. A moving collider is captured once at its pose one step earlier, so its
    ///   surface velocity already equals the case velocity on the first simulated step.
    /// </summary>
    public sealed class ParticleSolverCaseTests
    {
        const string CasePath = "Packages/com.yu5h1.motion/Documentation/ParticlePhysics/ParticleSolverCases.json";

        [Serializable] sealed class CaseFile { public string version; public CaseDefinition[] cases; }
        [Serializable] sealed class CaseDefinition
        {
            public string id;
            public WorldDefinition world;
            public ParticleDefinition[] particles;
            public ConstraintDefinition[] constraints;
            public ColliderDefinition[] colliders;
            public RigidDefinition[] rigidGroups;
            public MediumDefinition[] media;
            public PoseDefinition pose;
            public int steps;
            public ExpectedDefinition expected;
            public ToleranceDefinition tolerance;
        }
        [Serializable] sealed class WorldDefinition
        {
            public float fixedDeltaTime, damping, sor, contactWeight, frictionStatic, frictionKinetic, maxDepenetrationSpeed, groundY;
            public int substeps;
            public float[] gravity;
        }
        [Serializable] sealed class ParticleDefinition { public float[] position, velocity; public float invMass, radius; public int group; public uint category; }
        [Serializable] sealed class ConstraintDefinition { public int a, b; public float restLength, compliance, damping, breakForce; }
        [Serializable] sealed class ColliderDefinition
        {
            public string type;
            public float[] center, halfExtents, axisX, axisY, axisZ, linearVelocity;
            public float radius;
            public uint mask;
        }
        [Serializable] sealed class RigidDefinition { public int[] particles; }
        [Serializable] sealed class MediumDefinition { public float[] center, halfExtents, flow; public float density, dragCoefficient; }
        [Serializable] sealed class PoseDefinition { public float[] position, rotation; }
        [Serializable] sealed class ExpectedDefinition
        {
            public ExpectedParticle[] particles;
            public ExpectedConstraint[] constraints;
            public ExpectedGroup[] rigidGroups;
        }
        [Serializable] sealed class ExpectedParticle { public int index; public float[] position, velocity; }
        [Serializable] sealed class ExpectedConstraint { public int index; public bool broken; }
        [Serializable] sealed class ExpectedGroup { public int index; public float[] rotation; }
        [Serializable] sealed class ToleranceDefinition { public float position, velocity, rotation; }

        static CaseFile s_file;

        static CaseFile File
        {
            get
            {
                if (s_file == null)
                    s_file = JsonUtility.FromJson<CaseFile>(System.IO.File.ReadAllText(Path.GetFullPath(CasePath)));
                return s_file;
            }
        }

        static IEnumerable<string> CaseIds() => File.cases.Select(c => c.id);

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

        [TestCaseSource(nameof(CaseIds))]
        public void Case(string id)
        {
            CaseDefinition definition = File.cases.Single(c => c.id == id);
            ParticleWorld world = _scope.World(id);
            Assert.IsNotNull(world.Backend, "The GPU backend was not created.");
            ApplySettings(world.Settings, definition.world);
            foreach (MediumDefinition medium in definition.media ?? Array.Empty<MediumDefinition>())
                world.Media.Add(new ParticleMedium
                {
                    center = V(medium.center),
                    halfExtents = V(medium.halfExtents),
                    rotation = Quaternion.identity,
                    density = medium.density,
                    flow = V(medium.flow),
                    dragCoefficient = medium.dragCoefficient
                });

            float dt = definition.world.fixedDeltaTime;
            var moving = new List<(Transform transform, Vector3 start, Vector3 velocity)>();
            foreach (ColliderDefinition collider in definition.colliders ?? Array.Empty<ColliderDefinition>())
                moving.Add(CreateCollider(collider, dt));
            world.Step(dt);
            foreach (var (transform, start, _) in moving)
                transform.position = start;

            Assert.IsTrue(world.TryAllocate(BuildRequest(definition), out ParticleEntity entity, out string error), error);
            for (int step = 0; step < definition.steps; step++)
            {
                foreach (var (transform, start, velocity) in moving)
                    transform.position = start + velocity * (dt * step);
                world.Step(dt);
            }

            ParticleReadback readback = null;
            world.RequestReadback(entity, result => readback = result);
            AsyncGPUReadback.WaitAllRequests();
            Assert.IsNotNull(readback, "The readback did not complete.");
            Assert.IsTrue(readback.Succeeded, readback.Error);
            Compare(definition, readback);
        }

        static void ApplySettings(ParticleSolverSettings settings, WorldDefinition world)
        {
            settings.substeps = world.substeps;
            settings.gravity = V(world.gravity);
            settings.damping = world.damping;
            settings.sor = world.sor;
            settings.contactWeight = world.contactWeight;
            settings.frictionStatic = world.frictionStatic;
            settings.frictionKinetic = world.frictionKinetic;
            settings.maxDepenetrationSpeed = world.maxDepenetrationSpeed;
            settings.groundY = world.groundY;
        }

        (Transform, Vector3, Vector3) CreateCollider(ColliderDefinition definition, float dt)
        {
            ParticleCollider collider;
            Vector3 center = V(definition.center);
            if (definition.type == "sphere")
            {
                collider = _scope.Collider<ParticleSphereCollider>("Sphere");
                collider.transform.localScale = Vector3.one * (2f * definition.radius);
            }
            else
            {
                collider = _scope.Collider<ParticleBoxCollider>("Box");
                collider.transform.rotation = Quaternion.LookRotation(V(definition.axisZ), V(definition.axisY));
                collider.transform.localScale = V(definition.halfExtents) * 2f;
            }
            collider.CollidesWith = unchecked((int)definition.mask);
            Vector3 velocity = definition.linearVelocity != null && definition.linearVelocity.Length == 3 ? V(definition.linearVelocity) : Vector3.zero;
            collider.transform.position = center - velocity * dt;
            collider.EnableCollider();
            return (collider.transform, center, velocity);
        }

        static ParticleEntityRequest BuildRequest(CaseDefinition definition)
        {
            var request = new ParticleEntityRequest();
            foreach (ParticleDefinition p in definition.particles)
                request.particles.Add(new ParticleSpawn
                {
                    position = V(p.position),
                    velocity = V(p.velocity),
                    invMass = p.invMass,
                    radius = p.radius
                });
            int[] groups = definition.particles.Select(p => p.group).Distinct().ToArray();
            Assert.AreEqual(1, groups.Length, "The adapter expects one group value per case.");
            request.groupMode = groups[0] == 0 ? ParticleGroupMode.Shared : ParticleGroupMode.NewGroup;
            request.category = definition.particles[0].category;
            foreach (ConstraintDefinition c in definition.constraints ?? Array.Empty<ConstraintDefinition>())
                request.constraints.Add(new DistanceConstraintSpec
                {
                    a = c.a,
                    b = c.b,
                    restLength = c.restLength,
                    compliance = c.compliance,
                    damping = c.damping,
                    breakForce = c.breakForce
                });
            foreach (RigidDefinition g in definition.rigidGroups ?? Array.Empty<RigidDefinition>())
                request.rigidGroups.Add(g.particles);
            if (definition.pose != null && definition.pose.position != null && definition.pose.position.Length == 3)
            {
                float[] r = definition.pose.rotation;
                request.pose = new Pose(V(definition.pose.position), new Quaternion(r[1], r[2], r[3], r[0]));
            }
            return request;
        }

        static void Compare(CaseDefinition definition, ParticleReadback readback)
        {
            ToleranceDefinition tolerance = definition.tolerance;
            foreach (ExpectedParticle expected in definition.expected.particles ?? Array.Empty<ExpectedParticle>())
            {
                if (expected.position != null && expected.position.Length == 3)
                    AssertNear(V(expected.position), readback.Positions[expected.index], tolerance.position, $"{definition.id} particle {expected.index} position");
                if (expected.velocity != null && expected.velocity.Length == 3)
                    AssertNear(V(expected.velocity), readback.Velocities[expected.index], tolerance.velocity, $"{definition.id} particle {expected.index} velocity");
            }
            foreach (ExpectedConstraint expected in definition.expected.constraints ?? Array.Empty<ExpectedConstraint>())
                Assert.AreEqual(expected.broken, readback.BrokenConstraints[expected.index], $"{definition.id} constraint {expected.index} broken");
            foreach (ExpectedGroup expected in definition.expected.rigidGroups ?? Array.Empty<ExpectedGroup>())
            {
                float[] r = expected.rotation;
                var wanted = new Quaternion(r[1], r[2], r[3], r[0]);
                float alignment = Mathf.Abs(Quaternion.Dot(wanted, readback.RigidRotations[expected.index]));
                Assert.That(1f - alignment, Is.LessThan(tolerance.rotation), $"{definition.id} rigid group {expected.index} rotation {readback.RigidRotations[expected.index]}");
            }
        }

        static void AssertNear(Vector3 expected, Vector3 actual, float tolerance, string label)
        {
            float error = Mathf.Max(Mathf.Abs(expected.x - actual.x), Mathf.Max(Mathf.Abs(expected.y - actual.y), Mathf.Abs(expected.z - actual.z)));
            Assert.That(error, Is.LessThanOrEqualTo(tolerance), $"{label}: expected {expected.ToString("F7")}, got {actual.ToString("F7")}");
        }

        static Vector3 V(float[] values) =>
            values != null && values.Length == 3 ? new Vector3(values[0], values[1], values[2]) : Vector3.zero;
    }
}

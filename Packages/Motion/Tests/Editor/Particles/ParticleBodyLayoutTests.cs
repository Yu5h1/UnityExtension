using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Yu5h1Lib.Tests
{
    /// <summary>
    ///   Platform check PEXT-CHECK-10 of ParticleExtensions 1.2.0: every body layout gives the particle
    ///   positions, constraint pairs and rigid groups of Unified-Solver's SolverParticleEmitter
    ///   (BuildLocalShape and AddTopologyData), the body buffer agrees with the entities, and the body
    ///   frame on the GPU gives the expected tangent and side. Expected values are written out from
    ///   those two methods, not read from the code under test.
    /// </summary>
    public sealed class ParticleBodyLayoutTests
    {
        const float JointCompliance = 0.001f;
        const float BendCompliance = 0.01f;
        const float JointDamping = 0.3f;
        const string FramePath = "Packages/com.yu5h1.motion/Tests/Editor/Particles/ParticleBodyFrameTest.compute";
        static readonly Vector3 Dimensions = new Vector3(0.12f, 0.4f, 0.08f);

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

        static (int a, int b, bool bend) J(int a, int b) => (a, b, false);
        static (int a, int b, bool bend) B(int a, int b) => (a, b, true);

        static object[] Case(ParticleBodyTopology topology, Vector3[] positions, (int a, int b, bool bend)[] constraints, int[][] groups) =>
            new object[] { topology, positions, constraints, groups };

        static object[][] Cases()
        {
            float hx = 0.06f, hy = 0.2f, hz = 0.04f;
            float spacing = 0.4f / 3f;
            float segment = 0.4f / 6f;
            Vector3[] articulated = new Vector3[12];
            for (int s = 0; s < 3; s++)
            {
                float center = (1 - s) * spacing;
                articulated[s * 4] = new Vector3(hx, center + segment, hz);
                articulated[s * 4 + 1] = new Vector3(-hx, center + segment, -hz);
                articulated[s * 4 + 2] = new Vector3(-hx, center - segment, hz);
                articulated[s * 4 + 3] = new Vector3(hx, center - segment, -hz);
            }
            Vector3[] tetra = { new Vector3(1, 1, 1), new Vector3(1, -1, -1), new Vector3(-1, 1, -1), new Vector3(-1, -1, 1) };
            Vector3[] octa = { new Vector3(1, 0, 0), new Vector3(-1, 0, 0), new Vector3(0, 1, 0), new Vector3(0, -1, 0), new Vector3(0, 0, 1), new Vector3(0, 0, -1) };
            Vector3[] box = new Vector3[8];
            for (int i = 0; i < 8; i++)
                box[i] = new Vector3((i & 1) != 0 ? 1 : -1, (i & 2) != 0 ? 1 : -1, (i & 4) != 0 ? 1 : -1);
            Vector3 half = new Vector3(hx, hy, hz);
            return new[]
            {
                Case(ParticleBodyTopology.Single, new[] { Vector3.zero }, new (int, int, bool)[0], new int[0][]),
                Case(ParticleBodyTopology.Chain3,
                    new[] { new Vector3(0, hy, 0), Vector3.zero, new Vector3(0, -hy, 0) },
                    new[] { J(0, 1), J(1, 2), B(0, 2) }, new int[0][]),
                Case(ParticleBodyTopology.GuideChain4,
                    new[] { new Vector3(0, hy, 0), Vector3.zero, new Vector3(0, -hy, 0), new Vector3(hx, 0, 0) },
                    new[] { J(0, 1), J(1, 2), J(1, 3), B(0, 3), B(2, 3), B(0, 2) }, new int[0][]),
                Case(ParticleBodyTopology.DualRail6,
                    new[]
                    {
                        new Vector3(hx, hy, 0), new Vector3(-hx, hy, 0), new Vector3(hx, 0, 0),
                        new Vector3(-hx, 0, 0), new Vector3(hx, -hy, 0), new Vector3(-hx, -hy, 0)
                    },
                    new[]
                    {
                        J(0, 2), J(2, 4), J(1, 3), J(3, 5), J(0, 1), J(2, 3), J(4, 5),
                        B(0, 3), B(1, 2), B(2, 5), B(3, 4), B(0, 4), B(1, 5)
                    }, new int[0][]),
                Case(ParticleBodyTopology.ArticulatedCluster12, articulated,
                    new[]
                    {
                        J(2, 4), J(3, 5), B(2, 5), B(3, 4), B(0, 6), B(1, 7),
                        J(6, 8), J(7, 9), B(6, 9), B(7, 8), B(4, 10), B(5, 11)
                    },
                    new[] { new[] { 0, 1, 2, 3 }, new[] { 4, 5, 6, 7 }, new[] { 8, 9, 10, 11 } }),
                Case(ParticleBodyTopology.RigidCluster4, tetra.Select(v => Vector3.Scale(v, half)).ToArray(),
                    new (int, int, bool)[0], new[] { new[] { 0, 1, 2, 3 } }),
                Case(ParticleBodyTopology.RigidCluster6, octa.Select(v => Vector3.Scale(v, half)).ToArray(),
                    new (int, int, bool)[0], new[] { new[] { 0, 1, 2, 3, 4, 5 } }),
                Case(ParticleBodyTopology.RigidCluster8, box.Select(v => Vector3.Scale(v, half)).ToArray(),
                    new (int, int, bool)[0], new[] { new[] { 0, 1, 2, 3, 4, 5, 6, 7 } })
            };
        }

        [TestCaseSource(nameof(Cases))]
        public void EveryLayoutMatchesUnifiedSolver(object[] data)
        {
            var topology = (ParticleBodyTopology)data[0];
            var positions = (Vector3[])data[1];
            var constraints = ((int a, int b, bool bend)[])data[2];
            var groups = (int[][])data[3];

            ParticleBodyLayout layout = _kit.Layout(topology, Dimensions);
            ParticleBodyInteraction interaction = _kit.Interaction(bodyMass: 3f, radius: 0.02f, JointCompliance, BendCompliance, JointDamping);
            ParticleEntityRequest request = interaction.BuildBody(layout, 0, ParticleBodyTestKit.At(Vector3.zero), 1u);

            Assert.AreEqual(positions.Length, request.particles.Count, "particle count");
            for (int i = 0; i < positions.Length; i++)
            {
                ParticleBodyTestKit.AreClose(positions[i], request.particles[i].position, 1e-6f, $"{topology} particle {i}");
                Assert.AreEqual(positions.Length / 3f, request.particles[i].invMass, 1e-5f, "mass is the body mass over its particles");
            }
            Assert.AreEqual(constraints.Length, request.constraints.Count, "constraint count");
            for (int c = 0; c < constraints.Length; c++)
            {
                DistanceConstraintSpec spec = request.constraints[c];
                Assert.AreEqual((constraints[c].a, constraints[c].b), (spec.a, spec.b), $"{topology} constraint {c}");
                Assert.AreEqual(constraints[c].bend ? BendCompliance : JointCompliance, spec.compliance, 1e-9f, $"constraint {c} compliance");
                Assert.AreEqual(JointDamping, spec.damping, 1e-9f, $"constraint {c} damping");
                Assert.AreEqual(0f, spec.breakForce, "no constraint breaks");
            }
            Assert.AreEqual(groups.Length, request.rigidGroups.Count, "rigid group count");
            for (int g = 0; g < groups.Length; g++)
                CollectionAssert.AreEqual(groups[g], request.rigidGroups[g], $"rigid group {g}");
            Assert.IsTrue(request.TryValidate(out string error), error);
        }

        [Test]
        public void SpawnScaleAndRotationShapeTheBody()
        {
            ParticleBodyLayout layout = _kit.Layout(ParticleBodyTopology.GuideChain4, Dimensions);
            ParticleBodyInteraction interaction = _kit.Interaction();
            Quaternion rotation = Quaternion.Euler(10f, 50f, 30f);
            var spawn = ParticleBodyTestKit.At(new Vector3(1f, 2f, 3f), new Vector3(0f, 0f, 2f), rotation, new Vector3(2f, 3f, 1f));
            spawn.angularVelocity = new Vector3(0f, 1f, 0f);
            ParticleEntityRequest request = interaction.BuildBody(layout, 0, spawn, 1u);

            float hx = 0.12f * 2f * 0.5f;
            float hy = 0.4f * 3f * 0.5f;
            ParticleBodyTestKit.AreClose(new Vector3(0, hy, 0), request.particles[0].position, 1e-6f, "head");
            ParticleBodyTestKit.AreClose(new Vector3(hx, 0, 0), request.particles[3].position, 1e-6f, "guide");
            Assert.AreEqual(spawn.position, request.pose.position);
            Assert.That(Quaternion.Angle(rotation, request.pose.rotation), Is.LessThan(1e-3f));
            Vector3 worldOffset = rotation * request.particles[0].position;
            Vector3 expectedVelocity = spawn.velocity + Vector3.Cross(spawn.angularVelocity, worldOffset);
            ParticleBodyTestKit.AreClose(expectedVelocity, rotation * request.particles[0].velocity, 1e-5f, "head velocity");
        }

        [Test]
        public void BodyBufferAgreesWithEntities()
        {
            ParticleWorld world = _kit.World();
            var source = _kit.Profile<ParticleIceFragmentShapeSource>();
            ParticleBodyLayout layout = _kit.Layout(ParticleBodyTopology.Chain3, Dimensions, source);
            ParticleBodyInteraction interaction = _kit.Interaction();
            ParticleEmitter emitter = _kit.Emitter(world, layout, interaction, _kit.Behaviour());
            var color = new Color32(255, 128, 0, 64);
            Quaternion rotation = Quaternion.Euler(0f, 30f, 0f);
            var spawns = Enumerable.Range(0, 12)
                .Select(i => ParticleBodyTestKit.At(new Vector3(i * 0.5f, 0f, 0f), default, rotation, new Vector3(1f, 1f, 2f)))
                .ToArray();
            for (int i = 0; i < spawns.Length; i++)
                spawns[i].color = color;
            var entities = _kit.Spawn(emitter, spawns);
            _kit.Frame(world);
            Assert.AreEqual(12, entities.Count);

            ParticleBodyData[] bodies = ParticleBodyTestKit.Read<ParticleBodyData>(emitter.Bodies, 12);
            uint packed = color.r | ((uint)color.g << 8) | ((uint)color.b << 16) | ((uint)color.a << 24);
            for (int i = 0; i < 12; i++)
            {
                ParticleEntity entity = entities[i];
                Assert.AreEqual((uint)entity.Particles.start, bodies[i].particleStart, $"body {i} start");
                Assert.AreEqual((uint)entity.Particles.length, bodies[i].particleCount, $"body {i} count");
                Assert.AreEqual((uint)layout.TopologyOf(emitter.TemplateFor(i, layout.VariantCount)), bodies[i].topology, $"body {i} topology");
                Assert.AreEqual(1u, bodies[i].flags, $"body {i} flags");
                ParticleBodyTestKit.AreClose(new Vector3(1f, 1f, 2f), bodies[i].scale, 1e-6f, "scale");
                Assert.AreEqual(1f, bodies[i].scale.w, "drag coefficient");
                Assert.That(Quaternion.Angle(rotation, new Quaternion(bodies[i].spawnRotation.x, bodies[i].spawnRotation.y, bodies[i].spawnRotation.z, bodies[i].spawnRotation.w)), Is.LessThan(1e-3f));
                ParticleBodyTestKit.AreClose(Dimensions, bodies[i].dimensions, 1e-6f, "dimensions");
                Assert.AreEqual(packed, ParticleBodyTestKit.RawColor(emitter, i), "colour");
            }

            entities[3].SetParked(true);
            _kit.Frame(world);
            bodies = ParticleBodyTestKit.Read<ParticleBodyData>(emitter.Bodies, 12);
            Assert.AreEqual(0u, bodies[3].flags, "a parked body has no simulated flag");
            Assert.AreEqual(1u, bodies[4].flags);
            entities[3].SetParked(false);
            entities[5].SetSleeping(true);
            _kit.Frame(world);
            bodies = ParticleBodyTestKit.Read<ParticleBodyData>(emitter.Bodies, 12);
            Assert.AreEqual(1u, bodies[3].flags, "an unparked body is simulated again");
            Assert.AreEqual(2u, bodies[5].flags, "a sleeping body has no simulated flag, only the sleeping one");
        }

        [Test]
        public void ReleasedBodyHasNoFlag()
        {
            ParticleWorld world = _kit.World();
            ParticleEmitter emitter = _kit.Emitter(world, _kit.Layout(ParticleBodyTopology.Chain3), _kit.Interaction(), _kit.Behaviour());
            var entities = _kit.Spawn(emitter, ParticleBodyTestKit.At(Vector3.zero), ParticleBodyTestKit.At(Vector3.right));
            entities[0].Release();
            _kit.Frame(world);
            ParticleBodyData[] bodies = ParticleBodyTestKit.Read<ParticleBodyData>(emitter.Bodies, 2);
            Assert.AreEqual(0u, bodies[0].flags);
            Assert.AreEqual(1u, bodies[1].flags);
        }

        [Test]
        public void GpuFrameOfAStraightBodyHasTheExpectedTangentAndSide()
        {
            var shader = AssetDatabase.LoadAssetAtPath<ComputeShader>(FramePath);
            Assert.IsNotNull(shader, FramePath);
            ParticleWorld world = _kit.World();
            ParticleEmitter emitter = _kit.Emitter(world, _kit.Layout(ParticleBodyTopology.Chain3, Dimensions), _kit.Interaction(), _kit.Behaviour());
            Quaternion rotation = Quaternion.Euler(20f, 40f, 90f);
            Vector3 position = new Vector3(1f, 2f, 3f);
            _kit.Spawn(emitter, ParticleBodyTestKit.At(position, default, rotation));
            _kit.Frame(world);

            var frames = new ComputeBuffer(4, 16);
            try
            {
                int kernel = shader.FindKernel("EvaluateFrames");
                Assert.IsTrue(world.TryBind(shader, kernel));
                shader.SetBuffer(kernel, "_TestBodies", emitter.Bodies);
                shader.SetBuffer(kernel, "_TestFrames", frames);
                shader.SetInt("_TestCount", 1);
                ParticleExtensionContext.Dispatch(shader, kernel, 1);
                Vector4[] result = ParticleBodyTestKit.Read<Vector4>(frames, 4);
                ParticleBodyTestKit.AreClose(position, result[0], 1e-4f, "centre");
                ParticleBodyTestKit.AreClose(rotation * Vector3.up, result[1], 1e-4f, "tangent");
                ParticleBodyTestKit.AreClose(rotation * Vector3.right, result[2], 1e-4f, "side");
            }
            finally
            {
                frames.Release();
            }
        }

        static float Random01(int key, uint salt)
        {
            unchecked
            {
                uint value = (uint)key * 747796405u + salt * 2891336453u + 1u;
                value ^= value >> 16;
                value *= 0x7feb352du;
                value ^= value >> 15;
                value *= 0x846ca68bu;
                value ^= value >> 16;
                return (value & 0x00ffffffu) / 16777215f;
            }
        }

        [Test]
        public void IceFragmentTemplatesFollowTheSeed()
        {
            var source = _kit.Profile<ParticleIceFragmentShapeSource>();
            source.Jitter = 0f;
            source.Stretch = 0f;
            source.Seed = 7;
            var result = new Vector3[12];
            var again = new Vector3[12];
            for (int template = 0; template < source.TemplateCount; template++)
            {
                int key = unchecked(source.Seed * 73856093 ^ (template + 1) * 19349663);
                float pick = Random01(key, 0u) * 3f;
                ParticleBodyTopology expected = pick < 1f ? ParticleBodyTopology.RigidCluster4 : pick < 2f ? ParticleBodyTopology.RigidCluster6 : ParticleBodyTopology.RigidCluster8;
                float size = Mathf.Lerp(0.6f, 1.4f, Random01(key, 1u));
                Vector3 half = Dimensions * (0.5f * size);

                ParticleBodyTopology topology = source.BuildTemplate(template, Dimensions, result, out int count);
                Assert.AreEqual(expected, topology, $"template {template} variant");
                Assert.AreEqual(ParticleHullShapes.ClusterParticles(expected), count);
                Vector3[] baseVertices = ParticleHullShapes.BaseVertices(expected);
                for (int i = 0; i < count; i++)
                    ParticleBodyTestKit.AreClose(Vector3.Scale(baseVertices[i], half), result[i], 1e-6f, $"template {template} vertex {i}");

                source.BuildTemplate(template, Dimensions, again, out _);
                for (int i = 0; i < count; i++)
                    Assert.AreEqual(result[i], again[i], "the same index gives the same vertices");
            }
        }

        [Test]
        public void IceFragmentJitterStaysInsideItsBound()
        {
            var source = _kit.Profile<ParticleIceFragmentShapeSource>();
            source.Jitter = ParticleHullShapes.MaximumJitter;
            source.Stretch = 0f;
            var jittered = new Vector3[12];
            var clean = new Vector3[12];
            var plain = _kit.Profile<ParticleIceFragmentShapeSource>();
            plain.Jitter = 0f;
            plain.Stretch = 0f;
            for (int template = 0; template < source.TemplateCount; template++)
            {
                source.BuildTemplate(template, Dimensions, jittered, out int count);
                plain.BuildTemplate(template, Dimensions, clean, out _);
                float size = Mathf.Lerp(0.6f, 1.4f, Random01(unchecked(source.Seed * 73856093 ^ (template + 1) * 19349663), 1u));
                Vector3 half = Dimensions * (0.5f * size);
                for (int i = 0; i < count; i++)
                {
                    Vector3 delta = jittered[i] - clean[i];
                    Assert.That(Mathf.Abs(delta.x), Is.LessThanOrEqualTo(half.x * ParticleHullShapes.MaximumJitter + 1e-6f));
                    Assert.That(Mathf.Abs(delta.y), Is.LessThanOrEqualTo(half.y * ParticleHullShapes.MaximumJitter + 1e-6f));
                    Assert.That(Mathf.Abs(delta.z), Is.LessThanOrEqualTo(half.z * ParticleHullShapes.MaximumJitter + 1e-6f));
                }
            }
        }

        [Test]
        public void TemplateChoiceFollowsTheEmitterSeed()
        {
            ParticleWorld world = _kit.World();
            var source = _kit.Profile<ParticleIceFragmentShapeSource>();
            ParticleEmitter emitter = _kit.Emitter(world, _kit.Layout(ParticleBodyTopology.Chain3, Dimensions, source), _kit.Interaction(), _kit.Behaviour(), enable: false);
            emitter.ShapeSeed = 12345;
            for (int i = 0; i < 40; i++)
            {
                uint value = unchecked((uint)(12345 * 73856093 ^ (i + 1) * 19349663));
                value ^= value >> 16;
                value *= 0x7feb352du;
                value ^= value >> 15;
                value *= 0x846ca68bu;
                value ^= value >> 16;
                Assert.AreEqual((int)(value % (uint)source.TemplateCount), emitter.TemplateFor(i, source.TemplateCount), $"instance {i}");
            }
        }

        [Test]
        public void TheDriverBuildsTheSameBodyThroughThePhysicsParticleProfiles()
        {
            ParticleBodyLayout layout = _kit.Layout(ParticleBodyTopology.GuideChain4, Dimensions);
            ParticleBodyInteraction interaction = _kit.Interaction(bodyMass: 2f, radius: 0.03f);
            ParticleEntityRequest request = PhysicsParticle.BuildRequest(layout, interaction, 0, Pose.identity, 1u);
            Assert.AreEqual(4, request.particles.Count);
            Assert.AreEqual(6, request.constraints.Count);
            Assert.AreEqual(2f, request.particles[0].invMass, 1e-6f, "4 particles share a mass of 2");
            Assert.AreEqual(0.03f, request.particles[0].radius, 1e-9f);
            ParticleBodyTestKit.AreClose(new Vector3(0.06f, 0f, 0f), request.particles[3].position, 1e-6f, "guide");
        }
    }
}

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace Yu5h1Lib.Tests
{
    /// <summary>
    ///   Test double of a body source: entities, appearance and GPU buffers set by the test
    ///   (ParticleExtensions EXT-09).
    /// </summary>
    internal sealed class TestBodySource : MonoBehaviour, IParticleBodySource
    {
        public ParticleWorld World;
        public readonly List<ParticleEntity> EntityList = new List<ParticleEntity>();
        public PhysicsParticleAppearance AppearanceProfile;
        public ComputeBuffer BodyBuffer;
        public ComputeBuffer LifecycleBuffer;
        public int SourceVersion;

        public ParticleWorld BoundWorld => World;
        public IReadOnlyList<ParticleEntity> Entities => EntityList;
        public PhysicsParticleAppearance Appearance => AppearanceProfile;
        public ComputeBuffer Bodies => BodyBuffer;
        public ComputeBuffer Lifecycle => LifecycleBuffer;
        public int Version => SourceVersion;
    }

    /// <summary>
    ///   Platform checks PREND-CHECK-01 to 06 of ParticleRendering 1.0.0: the renderer component,
    ///   the stereo declarations of the shaders, and pixel samples of the grid surface, chain strip,
    ///   articulated mesh and rigid fragments rendered by a temporary camera.
    /// </summary>
    public sealed class PhysicsParticleRendererTests
    {
        const float Dt = 0.02f;
        const string GridShader = "Yu5h1Lib/Particles/GridSurface";
        const string ChainShader = "Yu5h1Lib/Particles/ChainStrip";
        const string ArticulatedShader = "Yu5h1Lib/Particles/ArticulatedMesh";
        const string RigidShader = "Yu5h1Lib/Particles/RigidMesh";

        ParticleTestScope _scope;
        readonly List<Object> _objects = new List<Object>();
        readonly List<ComputeBuffer> _buffers = new List<ComputeBuffer>();

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
            AsyncGPUReadback.WaitAllRequests();
            _scope?.Dispose();
            foreach (ComputeBuffer buffer in _buffers)
                buffer.Release();
            _buffers.Clear();
            foreach (Object item in _objects)
                if (item != null)
                    Object.DestroyImmediate(item);
            _objects.Clear();
        }

        ParticleWorld World(int substeps = 3)
        {
            ParticleWorld world = _scope.World("World");
            world.Settings.substeps = substeps;
            world.Settings.gravity = Vector3.zero;
            world.Settings.damping = 0f;
            world.Settings.groundY = -1000f;
            return world;
        }

        T Appearance<T>(string shaderName, Color color) where T : PhysicsParticleAppearance
        {
            var appearance = _scope.Asset<T>();
            Shader shader = Shader.Find(shaderName);
            Assert.IsNotNull(shader, shaderName);
            var material = new Material(shader);
            _objects.Add(material);
            appearance.Material = material;
            appearance.Color = color;
            return appearance;
        }

        ComputeBuffer Buffer<T>(T[] data, int stride)
        {
            var buffer = new ComputeBuffer(data.Length, stride);
            buffer.SetData(data);
            _buffers.Add(buffer);
            return buffer;
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

        PhysicsParticle Net(ParticleWorld world, GridSurfaceAppearance appearance, int columns, int rows, float spacing, Vector3 position, int count = 1)
        {
            var cloth = _scope.Asset<ClothInteraction>();
            cloth.ParticleMass = 0f;
            cloth.ParticleRadius = 0.02f;
            PhysicsParticle driver = _scope.Driver("Net", _scope.Grid(columns, rows, spacing), cloth, count);
            driver.World = world;
            driver.Appearance = appearance;
            driver.transform.position = position;
            driver.EnableDriver();
            return driver;
        }

        PhysicsParticle Hose(ParticleWorld world, ChainStripAppearance appearance, int particles, float spacing)
        {
            var chain = _scope.Asset<ChainParticleLayout>();
            chain.Count = particles;
            chain.Spacing = spacing;
            var rope = _scope.Asset<ChainInteraction>();
            rope.ParticleMass = 0f;
            rope.ParticleRadius = 0.02f;
            PhysicsParticle driver = _scope.Driver("Hose", chain, rope, 1);
            driver.World = world;
            driver.Appearance = appearance;
            driver.EnableDriver();
            return driver;
        }

        static Mesh Box(Vector3 size)
        {
            Vector3 h = size * 0.5f;
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            void Face(Vector3 normal, Vector3 u, Vector3 v)
            {
                int start = vertices.Count;
                Vector3 center = Vector3.Scale(normal, h);
                Vector3 du = Vector3.Scale(u, h);
                Vector3 dv = Vector3.Scale(v, h);
                vertices.Add(center - du - dv);
                vertices.Add(center - du + dv);
                vertices.Add(center + du + dv);
                vertices.Add(center + du - dv);
                for (int i = 0; i < 4; i++)
                {
                    normals.Add(normal);
                    uvs.Add(new Vector2(i == 2 || i == 3 ? 1f : 0f, i == 1 || i == 2 ? 1f : 0f));
                }
                bool outward = Vector3.Dot(Vector3.Cross(vertices[start + 1] - vertices[start], vertices[start + 2] - vertices[start]), normal) > 0f;
                int[] order = outward ? new[] { 0, 1, 2, 0, 2, 3 } : new[] { 0, 2, 1, 0, 3, 2 };
                foreach (int index in order)
                    triangles.Add(start + index);
            }
            Face(Vector3.right, Vector3.up, Vector3.forward);
            Face(Vector3.left, Vector3.up, Vector3.forward);
            Face(Vector3.up, Vector3.right, Vector3.forward);
            Face(Vector3.down, Vector3.right, Vector3.forward);
            Face(Vector3.forward, Vector3.right, Vector3.up);
            Face(Vector3.back, Vector3.right, Vector3.up);
            var mesh = new Mesh
            {
                vertices = vertices.ToArray(),
                normals = normals.ToArray(),
                uv = uvs.ToArray(),
                triangles = triangles.ToArray()
            };
            mesh.RecalculateBounds();
            return mesh;
        }

        static string RenderingFolder()
        {
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(PhysicsParticleRenderer).Assembly);
            Assert.IsNotNull(package);
            return Path.Combine(package.resolvedPath, "Runtime", "Particles", "Rendering");
        }

        [Test]
        public void Check01_DriverWithAppearanceGetsARenderer_WithoutOneDoesNot()
        {
            ParticleWorld world = World();
            GridSurfaceAppearance appearance = Appearance<GridSurfaceAppearance>(GridShader, Color.red);
            PhysicsParticle drawn = Net(world, appearance, 3, 3, 0.5f, Vector3.zero);
            var cloth = _scope.Asset<ClothInteraction>();
            PhysicsParticle bare = _scope.Driver("Bare", _scope.Grid(3, 3), cloth, 1);
            bare.World = world;
            bare.EnableDriver();

            Assert.IsNotNull(drawn.GetComponent<PhysicsParticleRenderer>());
            Assert.IsNull(bare.GetComponent<PhysicsParticleRenderer>());
            bare.Appearance = appearance;
            Assert.IsNotNull(bare.GetComponent<PhysicsParticleRenderer>(), "Assigning an appearance to an enabled driver adds the renderer.");
            Assert.AreSame(drawn.GetComponent<PhysicsParticleRenderer>(), PhysicsParticleRenderer.Ensure(drawn.gameObject));
        }

        [Test]
        public void Check01_ParkedAndReleasedEntitiesAreNotDrawn_SleepingOnesAre()
        {
            ParticleWorld world = World();
            GridSurfaceAppearance appearance = Appearance<GridSurfaceAppearance>(GridShader, Color.red);
            PhysicsParticle driver = Net(world, appearance, 3, 3, 0.5f, Vector3.zero, count: 3);
            world.Step(Dt);
            var renderer = driver.GetComponent<PhysicsParticleRenderer>();
            renderer.BoundsInterval = 1000f;

            Assert.IsTrue(renderer.Draw(null));
            Assert.AreEqual(3, renderer.DrawnInstanceCount);

            driver.Entities[0].SetParked(true);
            renderer.Draw(null);
            Assert.AreEqual(2, renderer.DrawnInstanceCount, "A parked entity is not drawn.");

            driver.Entities[1].SetSleeping(true);
            renderer.Draw(null);
            Assert.AreEqual(2, renderer.DrawnInstanceCount, "A sleeping entity is drawn like an awake one.");

            driver.Entities[2].Release();
            renderer.Draw(null);
            Assert.AreEqual(1, renderer.DrawnInstanceCount, "A released entity is not drawn.");

            driver.DisableDriver();
            Assert.IsFalse(renderer.Draw(null));
            Assert.AreEqual(0, renderer.DrawnInstanceCount);
        }

        [Test]
        public void Check01_DrawBoundsStartAtTheRestExtent_ThenFollowTheParticlesByReadback()
        {
            ParticleWorld world = World();
            GridSurfaceAppearance appearance = Appearance<GridSurfaceAppearance>(GridShader, Color.red);
            PhysicsParticle driver = Net(world, appearance, 3, 3, 0.5f, new Vector3(5f, 2f, -1f));
            world.Step(Dt);
            var renderer = driver.GetComponent<PhysicsParticleRenderer>();
            renderer.BoundsMargin = 0.25f;
            renderer.BoundsInterval = 0f;

            renderer.Draw(null);
            Bounds initial = renderer.DrawBounds;
            Assert.That(initial.size.x, Is.LessThan(2f), "The bounds must not be a fixed 1000 m box.");
            Assert.IsTrue(initial.Contains(new Vector3(5f, 2f, -1f)));

            AsyncGPUReadback.WaitAllRequests();
            Assert.IsTrue(renderer.BoundsFollowReadback);
            Bounds followed = renderer.DrawBounds;
            var particles = Read(world, driver.Entities[0]).Positions;
            var actual = new Bounds(particles[0], Vector3.zero);
            foreach (Vector3 position in particles)
                actual.Encapsulate(position);
            foreach (Vector3 position in particles)
                Assert.IsTrue(followed.Contains(position), $"Bounds {followed} do not contain particle {position}.");
            float slack = 0.02f + 0.25f;
            Assert.That(followed.min.x, Is.GreaterThanOrEqualTo(actual.min.x - slack - 1e-3f));
            Assert.That(followed.max.x, Is.LessThanOrEqualTo(actual.max.x + slack + 1e-3f));
            Assert.That(followed.min.y, Is.GreaterThanOrEqualTo(actual.min.y - slack - 1e-3f));
            Assert.That(followed.max.y, Is.LessThanOrEqualTo(actual.max.y + slack + 1e-3f));
            Assert.That(followed.min.z, Is.GreaterThanOrEqualTo(actual.min.z - slack - 1e-3f));
            Assert.That(followed.max.z, Is.LessThanOrEqualTo(actual.max.z + slack + 1e-3f));
        }

        [Test]
        public void Check01_BoundsReadbackRunsOncePerInterval_AndRendererNeverReadsBackSynchronously()
        {
            ParticleWorld world = World();
            GridSurfaceAppearance appearance = Appearance<GridSurfaceAppearance>(GridShader, Color.red);
            PhysicsParticle driver = Net(world, appearance, 3, 3, 0.5f, Vector3.zero, count: 3);
            world.Step(Dt);
            var renderer = driver.GetComponent<PhysicsParticleRenderer>();
            renderer.BoundsInterval = 1000f;

            for (int i = 0; i < 4; i++)
                renderer.Draw(null);
            AsyncGPUReadback.WaitAllRequests();
            for (int i = 0; i < 4; i++)
                renderer.Draw(null);

            Assert.AreEqual(1, renderer.ReadbackRequestCount, "One refresh reads the drawn entities' slots in a single request.");

            string[] forbidden = { ".GetData(", "WaitForCompletion", "WaitAllRequests", "ReadPixels" };
            foreach (string file in Directory.GetFiles(RenderingFolder(), "*.cs"))
            {
                string source = File.ReadAllText(file);
                foreach (string token in forbidden)
                    Assert.IsFalse(source.Contains(token), $"{Path.GetFileName(file)} contains the synchronous readback '{token}'.");
            }
        }

        [Test]
        public void Check01_DrawLayerIsTheSourceLayer_AndShadowFieldsDefaultOn()
        {
            ParticleWorld world = World();
            GridSurfaceAppearance appearance = Appearance<GridSurfaceAppearance>(GridShader, Color.red);
            appearance.UseNet = false;
            PhysicsParticle driver = Net(world, appearance, 3, 3, 0.5f, Vector3.zero);
            driver.gameObject.layer = 9;
            world.Step(Dt);
            var renderer = driver.GetComponent<PhysicsParticleRenderer>();
            renderer.BoundsInterval = 1000f;

            using (var rig = new ParticleRenderRig(new Vector3(0f, 0f, -3f), Quaternion.identity, 1.5f))
            {
                rig.Camera.cullingMask = 1 << 9;
                renderer.Draw(rig.Camera);
                rig.Render();
                Assert.IsTrue(rig.IsDrawn(Vector3.zero), "Camera that sees layer 9 draws the net.");

                rig.Camera.cullingMask = ~(1 << 9);
                renderer.Draw(rig.Camera);
                rig.Render();
                Assert.IsFalse(rig.IsDrawn(Vector3.zero), "Camera that culls layer 9 does not draw it.");
            }

            Assert.IsTrue(appearance.CastShadows);
            Assert.IsTrue(appearance.ReceiveShadows);
            Assert.IsTrue(_scope.Asset<ArticulatedMeshAppearance>().CastShadows);
            Assert.IsFalse(_scope.Asset<ChainStripAppearance>().CastShadows, "The strip adds shadows only when asked.");
            Assert.IsTrue(_scope.Asset<ChainStripAppearance>().ReceiveShadows);
        }

        [TestCase(GridShader)]
        [TestCase(ChainShader)]
        [TestCase(ArticulatedShader)]
        [TestCase(RigidShader)]
        public void Check02_EveryShaderDeclaresInstanceIdStereoOutputAndEyeIndex(string shaderName)
        {
            string path = AssetDatabase.GetAssetPath(Shader.Find(shaderName));
            Assert.IsNotEmpty(path, shaderName);
            string source = File.ReadAllText(Path.GetFullPath(path));

            int passes = Regex.Matches(source, @"^\s*Pass\s*$", RegexOptions.Multiline).Count;
            Assert.AreEqual(2, passes, "A drawing pass and a shadow pass.");
            Assert.AreEqual(passes, CountOccurrences(source, "#pragma multi_compile_instancing"), "multi_compile_instancing in every pass.");
            Assert.AreEqual(passes, CountOccurrences(source, "PARTICLE_VERTEX_INSTANCE_ID"), "Instance ID in every vertex input.");
            Assert.AreEqual(passes, CountOccurrences(source, "UNITY_VERTEX_OUTPUT_STEREO"), "Stereo output in every vertex output.");
            Assert.AreEqual(passes, CountOccurrences(source, "UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO"), "Stereo output initialised in every vertex stage.");
            Assert.AreEqual(passes, CountOccurrences(source, "UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX"), "Eye index in every fragment stage.");
            Assert.AreEqual(passes, CountOccurrences(source, "PARTICLE_SETUP_INSTANCE"), "Instance setup in every vertex stage.");
            Assert.IsFalse(source.Contains("SV_InstanceID"), "Shaders must take the instance number from the stereo-aware macro.");
        }

        [Test]
        public void Check02_CommonIncludeMapsInstanceNumberThroughUnityMacros()
        {
            string folder = Path.Combine(RenderingFolder(), "Shaders");
            string common = File.ReadAllText(Path.Combine(folder, "ParticleDrawCommon.hlsl"));

            StringAssert.Contains("unity_InstanceID", common);
            StringAssert.Contains("UNITY_SETUP_INSTANCE_ID", common);
            StringAssert.Contains("UNITY_VERTEX_INPUT_INSTANCE_ID", common);
        }

        [TestCase(GridShader, "STEREO_INSTANCING_ON")]
        [TestCase(ChainShader, "STEREO_INSTANCING_ON")]
        [TestCase(ArticulatedShader, "STEREO_INSTANCING_ON")]
        [TestCase(RigidShader, "STEREO_INSTANCING_ON")]
        [TestCase(GridShader, "STEREO_MULTIVIEW_ON")]
        [TestCase(ChainShader, "STEREO_MULTIVIEW_ON")]
        [TestCase(ArticulatedShader, "STEREO_MULTIVIEW_ON")]
        [TestCase(RigidShader, "STEREO_MULTIVIEW_ON")]
        public void Check02_ShaderCompilesWithStereoKeywords(string shaderName, string keyword)
        {
            Shader shader = Shader.Find(shaderName);
            Assert.IsNotNull(shader, shaderName);
            bool previous = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = false;
            var material = new Material(shader);
            _objects.Add(material);
            try
            {
                material.enableInstancing = true;
                Assert.IsTrue(shader.keywordSpace.keywords.Any(k => k.name == keyword), $"{shaderName} has no {keyword} variant.");
                material.EnableKeyword(keyword);
                for (int pass = 0; pass < material.passCount; pass++)
                    material.SetPass(pass);
                ShaderMessage[] messages = ShaderUtil.GetShaderMessages(shader);
                string[] errors = messages
                    .Where(m => m.severity == ShaderCompilerMessageSeverity.Error)
                    .Select(m => $"{m.file}({m.line}): {m.message}")
                    .ToArray();
                Assert.IsEmpty(errors, string.Join("\n", errors));
                Assert.IsFalse(ShaderUtil.ShaderHasError(shader), $"{shaderName} reports a compile error with {keyword}.");
            }
            finally
            {
                ShaderUtil.allowAsyncCompilation = previous;
            }
        }

        const string BadShaderSource =
            "Shader \"Hidden/PrendBad\" { SubShader { Pass { CGPROGRAM\n" +
            "#pragma vertex vert\n#pragma fragment frag\n" +
            "float4 vert() : SV_POSITION { return undeclared; }\n" +
            "float4 frag() : SV_Target { return 0; }\nENDCG } } }";

        [Test]
        public void Check02_CompileCheckDetectsAShaderThatDoesNotCompile()
        {
            Shader bad = ShaderUtil.CreateShaderAsset(BadShaderSource, false);
            _objects.Add(bad);
            bool previous = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = false;
            var material = new Material(bad);
            _objects.Add(material);
            int errors = 0;
            void Count(string condition, string stackTrace, LogType type)
            {
                if (type == LogType.Error && condition.Contains("Shader error"))
                    errors++;
            }
            Application.logMessageReceived += Count;
            LogAssert.ignoreFailingMessages = true;
            try
            {
                material.SetPass(0);
                Assert.Greater(errors, 0, "A shader that does not compile must log a shader error, which fails the compile checks.");
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
                Application.logMessageReceived -= Count;
                ShaderUtil.allowAsyncCompilation = previous;
            }
        }

        [Test]
        public void Check03_GridMeshHasTheClothIndexLayoutAndFacesTheCamera()
        {
            Mesh single = ParticleGridMesh.Build(4, 3, false);
            Mesh doubled = ParticleGridMesh.Build(4, 3, true);
            _objects.Add(single);
            _objects.Add(doubled);

            Assert.AreEqual(12, single.vertexCount);
            Assert.AreEqual((4 - 1) * (3 - 1) * 2 * 3, single.triangles.Length);
            Assert.AreEqual((4 - 1) * (3 - 1) * 4 * 3, doubled.triangles.Length);
            Assert.AreEqual(new Vector2(0f, 0f), single.uv[0]);
            Assert.AreEqual(new Vector2(1f, 1f), single.uv[11]);
            Assert.AreEqual(new Vector2(1f / 3f, 0.5f), single.uv[1 * 4 + 1]);

            int[] triangles = single.triangles;
            for (int t = 0; t < triangles.Length; t += 3)
            {
                Vector3 a = GridPoint(triangles[t], 4);
                Vector3 b = GridPoint(triangles[t + 1], 4);
                Vector3 c = GridPoint(triangles[t + 2], 4);
                Vector3 normal = Vector3.Cross(b - a, c - a);
                Assert.That(normal.z, Is.LessThan(0f), $"Triangle {t / 3} must face -Z, the side a camera in front of the grid sees.");
            }

            int[] both = doubled.triangles;
            int frontFacing = 0;
            for (int t = 0; t < both.Length; t += 3)
            {
                Vector3 normal = Vector3.Cross(GridPoint(both[t + 1], 4) - GridPoint(both[t], 4), GridPoint(both[t + 2], 4) - GridPoint(both[t], 4));
                if (normal.z < 0f)
                    frontFacing++;
            }
            Assert.AreEqual(both.Length / 6, frontFacing, "Half of the double-sided triangles are the reversed pair.");
        }

        static Vector3 GridPoint(int index, int columns) => new Vector3(index % columns, index / columns, 0f);

        [Test]
        public void Check03_NetDrawsThreadsAndLetsTheBackgroundThroughTheHoles()
        {
            ParticleWorld world = World();
            GridSurfaceAppearance appearance = Appearance<GridSurfaceAppearance>(GridShader, Color.red);
            appearance.NetStyle = GridNetStyle.Square;
            appearance.ThreadWidth = 0.25f;
            appearance.BorderColor = Color.red;
            PhysicsParticle driver = Net(world, appearance, 4, 3, 0.5f, Vector3.zero);
            world.Step(Dt);
            var renderer = driver.GetComponent<PhysicsParticleRenderer>();
            renderer.BoundsInterval = 1000f;

            using (var rig = new ParticleRenderRig(new Vector3(0f, 0f, -3f), Quaternion.identity, 0.4f))
            {
                renderer.Draw(rig.Camera);
                rig.Render();

                Assert.AreEqual(1, renderer.BatchCount);
                Assert.IsTrue(rig.IsDrawn(new Vector3(0.0f, 0.0f, 0f)), "Thread crossing at the centre.");
                Assert.IsTrue(rig.IsDrawn(new Vector3(0.03f, 0.0f, 0f)), "Horizontal thread.");
                Assert.IsTrue(rig.IsDrawn(new Vector3(0.0f, 0.03f, 0f)), "Vertical thread.");
                Assert.IsFalse(rig.IsDrawn(new Vector3(0.0375f, 0.025f, 0f)), "Hole between the threads shows the background.");
                Assert.IsFalse(rig.IsDrawn(new Vector3(-0.3375f, 0.325f, 0f)), "Hole in the upper left.");
                Assert.IsFalse(rig.IsDrawn(new Vector3(0.3375f, -0.325f, 0f)), "Hole in the lower right.");
            }
        }

        [Test]
        public void Check03_ClothWithoutNetIsOpaqueEverywhereInsideTheGrid()
        {
            ParticleWorld world = World();
            GridSurfaceAppearance appearance = Appearance<GridSurfaceAppearance>(GridShader, Color.red);
            appearance.UseNet = false;
            PhysicsParticle driver = Net(world, appearance, 4, 3, 0.5f, Vector3.zero);
            world.Step(Dt);
            var renderer = driver.GetComponent<PhysicsParticleRenderer>();
            renderer.BoundsInterval = 1000f;

            using (var rig = new ParticleRenderRig(new Vector3(0f, 0f, -3f), Quaternion.identity, 1f))
            {
                renderer.Draw(rig.Camera);
                rig.Render();

                Assert.IsTrue(rig.IsDrawn(new Vector3(0.0375f, 0.025f, 0f)));
                Assert.IsTrue(rig.IsDrawn(new Vector3(-0.7f, 0.45f, 0f)));
                Assert.IsTrue(rig.IsDrawn(new Vector3(0.7f, -0.45f, 0f)));
                Assert.IsFalse(rig.IsDrawn(new Vector3(0.8f, 0f, 0f)));
                Assert.IsFalse(rig.IsDrawn(new Vector3(0f, 0.55f, 0f)));
            }
        }

        [Test]
        public void Check03_NetFollowsParticlesWhenTheEntityIsRotatedAndMoved()
        {
            ParticleWorld world = World();
            GridSurfaceAppearance appearance = Appearance<GridSurfaceAppearance>(GridShader, Color.red);
            appearance.UseNet = false;
            PhysicsParticle driver = Net(world, appearance, 4, 3, 0.5f, new Vector3(2f, 1f, 0f));
            driver.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            world.Step(Dt);
            driver.Entities[0].Teleport(new Pose(new Vector3(2f, 1f, 0f), Quaternion.Euler(0f, 0f, 90f)));
            world.Step(Dt);
            var renderer = driver.GetComponent<PhysicsParticleRenderer>();
            renderer.BoundsInterval = 1000f;

            using (var rig = new ParticleRenderRig(new Vector3(2f, 1f, -3f), Quaternion.identity, 1f))
            {
                renderer.Draw(rig.Camera);
                rig.Render();

                Assert.IsTrue(rig.IsDrawn(new Vector3(2f, 1.6f, 0f)), "Quarter turn makes the 1.5 m width vertical.");
                Assert.IsFalse(rig.IsDrawn(new Vector3(2.6f, 1f, 0f)), "The 1 m height is now horizontal.");
            }
        }

        [Test]
        public void Check04_ThreeParticleChainDrawsAStripOfTheWidthFacingTheCamera()
        {
            ParticleWorld world = World();
            ChainStripAppearance appearance = Appearance<ChainStripAppearance>(ChainShader, Color.red);
            appearance.Width = 0.1f;
            PhysicsParticle driver = Hose(world, appearance, 3, 0.5f);
            world.Step(Dt);
            var renderer = driver.GetComponent<PhysicsParticleRenderer>();
            renderer.BoundsInterval = 1000f;
            Vector3 middle = new Vector3(0f, -0.5f, 0f);

            using (var front = new ParticleRenderRig(new Vector3(0f, -0.5f, -3f), Quaternion.identity, 1.5f))
            {
                renderer.Draw(front.Camera);
                front.Render();

                Vector2Int row = front.Pixel(middle);
                float pixelsPerMeter = 256f / 3f;
                Assert.That(front.CountDrawnInRow(row.y), Is.EqualTo(0.2f * pixelsPerMeter).Within(2.5f), "Strip width in pixels.");
                Assert.IsTrue(front.IsDrawn(new Vector3(0f, -0.05f, 0f)));
                Assert.IsTrue(front.IsDrawn(new Vector3(0f, -0.95f, 0f)));
                Assert.IsFalse(front.IsDrawn(new Vector3(0.14f, -0.5f, 0f)));
                Assert.IsFalse(front.IsDrawn(new Vector3(0f, 0.1f, 0f)));
                Assert.IsFalse(front.IsDrawn(new Vector3(0f, -1.1f, 0f)));

                Color centre = front.Sample(middle);
                Color edge = front.Sample(middle + new Vector3(0.09f, 0f, 0f));
                Assert.That(centre.r, Is.GreaterThan(edge.r), "Edges are darker than the centre line.");
            }

            using (var side = new ParticleRenderRig(new Vector3(3f, -0.5f, 0f), Quaternion.LookRotation(Vector3.left), 1.5f))
            {
                renderer.Draw(side.Camera);
                side.Render();

                Vector2Int row = side.Pixel(middle);
                float pixelsPerMeter = 256f / 3f;
                Assert.That(side.CountDrawnInRow(row.y), Is.EqualTo(0.2f * pixelsPerMeter).Within(2.5f), "The strip turns to face a camera on the side.");
            }
        }

        [Test]
        public void Check04_StripMeshHasTwoVerticesPerParticleAndBothFaces()
        {
            Mesh mesh = ParticleStripMesh.Build(5);
            _objects.Add(mesh);

            Assert.AreEqual(10, mesh.vertexCount);
            Assert.AreEqual(4 * 4 * 3, mesh.triangles.Length);
            Assert.AreEqual(new Vector2(0f, 0f), mesh.uv[0]);
            Assert.AreEqual(new Vector2(1f, 0f), mesh.uv[1]);
            Assert.AreEqual(new Vector2(0f, 1f), mesh.uv[8]);
        }

        TestBodySource FishSource(ParticleWorld world, ArticulatedMeshAppearance appearance, Vector3 position, out ParticleEntity entity)
        {
            Quaternion rotation = Quaternion.identity;
            var request = new ParticleEntityRequest
            {
                groupMode = ParticleGroupMode.NewGroup,
                pose = new Pose(position, rotation)
            };
            foreach (Vector3 local in new[] { new Vector3(0f, 0.1f, 0f), Vector3.zero, new Vector3(0f, -0.1f, 0f), new Vector3(0.05f, 0f, 0f) })
                request.particles.Add(new ParticleSpawn { position = local, invMass = 0f, radius = 0.01f });
            Assert.IsTrue(world.TryAllocate(request, out entity, out string error), error);

            const uint White = 0xFFFFFFFFu;
            var body = new ParticleBodyData
            {
                particleStart = (uint)entity.Particles.start,
                particleCount = 4,
                topology = (uint)ParticleBodyTopology.GuideChain4,
                flags = 1u,
                scale = new Vector4(1f, 1f, 1f, 1f),
                spawnRotation = new Vector4(rotation.x, rotation.y, rotation.z, rotation.w),
                dimensions = new Vector3(0.1f, 0.2f, 0.1f),
                color = White
            };
            var go = new GameObject("Fish");
            _objects.Add(go);
            var source = go.AddComponent<TestBodySource>();
            source.World = world;
            source.AppearanceProfile = appearance;
            source.EntityList.Add(entity);
            source.BodyBuffer = Buffer(new[] { body }, ParticleBodyData.Stride);
            source.SourceVersion = 1;
            return source;
        }

        [Test]
        public void Check05_StraightGuideChainMeshIsDrawnAlongTheBody_AndCollapsesWhenHidden()
        {
            ParticleWorld world = World();
            ArticulatedMeshAppearance appearance = Appearance<ArticulatedMeshAppearance>(ArticulatedShader, Color.red);
            var mesh = Box(new Vector3(0.2f, 0.2f, 2f));
            _objects.Add(mesh);
            appearance.Mesh = mesh;
            appearance.ForwardAxis = Vector3.forward;
            Vector3 position = new Vector3(1f, 2f, 3f);
            TestBodySource source = FishSource(world, appearance, position, out ParticleEntity entity);
            world.Step(Dt);
            var renderer = source.gameObject.AddComponent<PhysicsParticleRenderer>();
            renderer.BoundsInterval = 1000f;

            using (var rig = new ParticleRenderRig(new Vector3(1f, 2f, 0f), Quaternion.identity, 0.25f))
            {
                renderer.Draw(rig.Camera);
                rig.Render();

                Assert.AreEqual(1, renderer.DrawnInstanceCount);
                float pixelsPerMeter = 256f / 0.5f;
                Vector2Int centre = rig.Pixel(position);
                Assert.That(rig.CountDrawnInColumn(centre.x), Is.EqualTo(0.2f * pixelsPerMeter).Within(3f), "Mesh length equals the head to tail distance, along the body (world Y).");
                Assert.That(rig.CountDrawnInRow(centre.y), Is.EqualTo(0.1f * pixelsPerMeter).Within(3f), "Mesh width equals the body width.");
                Assert.IsTrue(rig.IsDrawn(position + new Vector3(0f, 0.09f, 0f)));
                Assert.IsTrue(rig.IsDrawn(position + new Vector3(0f, -0.09f, 0f)));
                Assert.IsFalse(rig.IsDrawn(position + new Vector3(0f, 0.13f, 0f)));
                Assert.IsFalse(rig.IsDrawn(position + new Vector3(0.1f, 0f, 0f)));
                Color lit = rig.Sample(position);
                Assert.That(lit.r, Is.GreaterThan(0.2f));
                Assert.That(lit.g, Is.LessThan(0.1f), "Colour is texture x appearance colour x body colour.");

                source.LifecycleBuffer = Buffer(new[] { new Vector4(0f, 0f, 1f, 0f) }, 16);
                renderer.Draw(rig.Camera);
                rig.Render();
                Assert.IsFalse(rig.IsDrawn(position + new Vector3(0f, 0.05f, 0f)), "Hidden fraction 1 collapses the mesh onto the middle.");
                Assert.IsFalse(rig.IsDrawn(position + new Vector3(0f, -0.05f, 0f)));
                Assert.That(rig.CountDrawn(), Is.LessThan(20));

                source.LifecycleBuffer = Buffer(new[] { new Vector4(0f, 0f, 0.5f, 0f) }, 16);
                renderer.Draw(rig.Camera);
                rig.Render();
                Assert.IsTrue(rig.IsDrawn(position + new Vector3(0f, 0.03f, 0f)));
                Assert.IsFalse(rig.IsDrawn(position + new Vector3(0f, 0.07f, 0f)), "Half hidden is half as long.");
            }
        }

        [Test]
        public void Check05_BodiesOfOneSourceShareOneInstancedDraw()
        {
            ParticleWorld world = World();
            ArticulatedMeshAppearance appearance = Appearance<ArticulatedMeshAppearance>(ArticulatedShader, Color.red);
            var mesh = Box(new Vector3(0.2f, 0.2f, 2f));
            _objects.Add(mesh);
            appearance.Mesh = mesh;
            TestBodySource source = FishSource(world, appearance, Vector3.zero, out ParticleEntity first);
            var second = new ParticleEntityRequest { groupMode = ParticleGroupMode.NewGroup, pose = new Pose(new Vector3(1f, 0f, 0f), Quaternion.identity) };
            foreach (Vector3 local in new[] { new Vector3(0f, 0.1f, 0f), Vector3.zero, new Vector3(0f, -0.1f, 0f), new Vector3(0.05f, 0f, 0f) })
                second.particles.Add(new ParticleSpawn { position = local, invMass = 0f, radius = 0.01f });
            Assert.IsTrue(world.TryAllocate(second, out ParticleEntity other, out string error), error);
            ParticleBodyData[] bodies = new ParticleBodyData[2];
            source.BodyBuffer.GetData(bodies, 0, 0, 1);
            bodies[1] = bodies[0];
            bodies[1].particleStart = (uint)other.Particles.start;
            source.BodyBuffer.Release();
            _buffers.Remove(source.BodyBuffer);
            source.BodyBuffer = Buffer(bodies, ParticleBodyData.Stride);
            source.EntityList.Add(other);
            source.SourceVersion++;
            world.Step(Dt);
            var renderer = source.gameObject.AddComponent<PhysicsParticleRenderer>();
            renderer.BoundsInterval = 1000f;

            using (var rig = new ParticleRenderRig(new Vector3(0.5f, 0f, -3f), Quaternion.identity, 1f))
            {
                renderer.Draw(rig.Camera);
                rig.Render();

                Assert.AreEqual(1, renderer.BatchCount);
                Assert.AreEqual(2, renderer.DrawnInstanceCount);
                Assert.IsTrue(rig.IsDrawn(new Vector3(0f, 0.05f, 0f)));
                Assert.IsTrue(rig.IsDrawn(new Vector3(1f, 0.05f, 0f)));
                Assert.IsFalse(rig.IsDrawn(new Vector3(0.5f, 0.05f, 0f)));
            }
        }

        [Test]
        public void Check05_PhysicsParticleIsABodySourceForAChainOfThree()
        {
            ParticleWorld world = World();
            ArticulatedMeshAppearance appearance = Appearance<ArticulatedMeshAppearance>(ArticulatedShader, Color.red);
            var mesh = Box(new Vector3(0.2f, 0.2f, 2f));
            _objects.Add(mesh);
            appearance.Mesh = mesh;
            var chain = _scope.Asset<ChainParticleLayout>();
            chain.Count = 3;
            chain.Spacing = 0.1f;
            chain.Direction = Vector3.down;
            var rope = _scope.Asset<ChainInteraction>();
            rope.ParticleMass = 0f;
            rope.ParticleRadius = 0.02f;
            PhysicsParticle driver = _scope.Driver("Fish", chain, rope, 2);
            driver.World = world;
            driver.Appearance = appearance;
            driver.transform.position = new Vector3(1f, 2f, 3f);
            driver.BatchSpacing = new Vector3(0f, 0f, 1f);
            driver.EnableDriver();
            world.Step(Dt);
            var renderer = driver.GetComponent<PhysicsParticleRenderer>();
            renderer.BoundsInterval = 1000f;

            var data = new ParticleBodyData[2];
            driver.Bodies.GetData(data);
            Assert.AreEqual((uint)ParticleBodyTopology.Chain3, data[0].topology);
            Assert.AreEqual(3u, data[0].particleCount);
            Assert.AreEqual((uint)driver.Entities[1].Particles.start, data[1].particleStart);
            Assert.AreEqual(1u, data[0].flags);
            Assert.That(data[0].dimensions.y, Is.EqualTo(0.2f).Within(1e-5f), "Head to tail distance.");
            int version = driver.Version;

            driver.Entities[1].SetParked(true);
            driver.RefreshBodyFlags();
            Assert.AreNotEqual(version, driver.Version, "Parking changes the body flags and the version.");
            driver.Bodies.GetData(data);
            Assert.AreEqual(0u, data[1].flags);

            using (var rig = new ParticleRenderRig(new Vector3(1f, 1.9f, 0f), Quaternion.identity, 0.4f))
            {
                renderer.Draw(rig.Camera);
                rig.Render();
                Assert.AreEqual(1, renderer.DrawnInstanceCount);
                Assert.IsTrue(rig.IsDrawn(new Vector3(1f, 1.9f, 3f)), "First body hangs from the transform towards -Y.");
            }
        }

        static PointParticleLayout.Variant CubeVariant(float h)
        {
            var corners = new List<Vector3>();
            for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                    for (int z = -1; z <= 1; z += 2)
                        corners.Add(new Vector3(x, y, z) * h);
            return new PointParticleLayout.Variant { positions = corners.ToArray() };
        }

        static PointParticleLayout.Variant TetrahedronVariant(float h) => new PointParticleLayout.Variant
        {
            positions = new[] { new Vector3(h, h, h), new Vector3(h, -h, -h), new Vector3(-h, h, -h), new Vector3(-h, -h, h) }
        };

        static PointParticleLayout.Variant OctahedronVariant(float h) => new PointParticleLayout.Variant
        {
            positions = new[]
            {
                new Vector3(h, 0f, 0f), new Vector3(-h, 0f, 0f), new Vector3(0f, h, 0f),
                new Vector3(0f, -h, 0f), new Vector3(0f, 0f, h), new Vector3(0f, 0f, -h)
            }
        };

        [Test]
        public void Check06_HullFacesMatchTetrahedronOctahedronAndCube()
        {
            Assert.AreEqual(4, ParticleHullMesh.TriangleCount(ParticleHullMesh.Faces(TetrahedronVariant(1f).positions)));
            Assert.AreEqual(8, ParticleHullMesh.TriangleCount(ParticleHullMesh.Faces(OctahedronVariant(1f).positions)));
            List<int[]> cube = ParticleHullMesh.Faces(CubeVariant(1f).positions);
            Assert.AreEqual(6, cube.Count, "Coplanar corners merge into six square faces.");
            Assert.AreEqual(12, ParticleHullMesh.TriangleCount(cube));
            Assert.IsNull(ParticleHullMesh.Faces(new[] { Vector3.zero, Vector3.right, Vector3.up, new Vector3(1f, 1f, 0f) }), "Four coplanar points have no hull.");
        }

        [Test]
        public void Check06_HullMeshIsFlatShadedOutwardAndPushedOutByTheRadius()
        {
            Vector3[] points = CubeVariant(0.1f).positions;
            Mesh mesh = ParticleHullMesh.Build(points, Enumerable.Repeat(0.02f, points.Length).ToArray());
            _objects.Add(mesh);

            Assert.AreEqual(36, mesh.vertexCount);
            Assert.AreEqual(36, mesh.triangles.Length);
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            int[] triangles = mesh.triangles;
            for (int t = 0; t < triangles.Length; t += 3)
            {
                Vector3 a = vertices[triangles[t]];
                Vector3 b = vertices[triangles[t + 1]];
                Vector3 c = vertices[triangles[t + 2]];
                Vector3 facet = Vector3.Cross(b - a, c - a).normalized;
                Vector3 centre = (a + b + c) / 3f;
                Assert.That(Vector3.Dot(facet, centre), Is.GreaterThan(0f), $"Triangle {t / 3} faces outward.");
                for (int k = 0; k < 3; k++)
                    Assert.That(Vector3.Distance(normals[triangles[t + k]], facet), Is.LessThan(1e-4f), "Flat shading: one facet normal per corner.");
            }
            float corner = vertices.Max(v => v.magnitude);
            Assert.That(corner, Is.EqualTo(0.1f * Mathf.Sqrt(3f) + 0.02f).Within(1e-4f), "Corners are pushed away from the centroid by the radius.");
        }

        TestBodySource FragmentSource(ParticleWorld world, PhysicsParticleAppearance appearance, Pose pose, Vector3 spin, out ParticleEntity entity)
        {
            var points = _scope.Asset<PointParticleLayout>();
            points.Variants = new[] { CubeVariant(0.15f) };
            var rigid = _scope.Asset<RigidGroupInteraction>();
            rigid.ParticleRadius = 0.02f;
            ParticleEntityRequest request = PhysicsParticle.BuildRequest(points, rigid, 0, pose, 1u);
            for (int i = 0; i < request.particles.Count; i++)
            {
                ParticleSpawn particle = request.particles[i];
                particle.velocity = Vector3.Cross(spin, particle.position);
                request.particles[i] = particle;
            }
            Assert.IsTrue(world.TryAllocate(request, out entity, out string error), error);
            var go = new GameObject("Fragment");
            _objects.Add(go);
            var source = go.AddComponent<TestBodySource>();
            source.World = world;
            source.AppearanceProfile = appearance;
            source.EntityList.Add(entity);
            source.SourceVersion = 1;
            return source;
        }

        [Test]
        public void Check06_SpinningFragmentIsDrawnAtTheGpuRigidPose()
        {
            ParticleWorld world = World(substeps: 4);
            ParticleHullAppearance appearance = Appearance<ParticleHullAppearance>(RigidShader, Color.red);
            TestBodySource source = FragmentSource(world, appearance, new Pose(new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, 10f)), new Vector3(0f, 0f, 2f), out ParticleEntity entity);
            for (int step = 0; step < 20; step++)
                world.Step(Dt);
            Vector3[] simulated = Read(world, entity).Positions;
            Vector3 centre = simulated.Aggregate(Vector3.zero, (sum, p) => sum + p) / simulated.Length;
            var renderer = source.gameObject.AddComponent<PhysicsParticleRenderer>();
            renderer.BoundsInterval = 1000f;

            using (var rig = new ParticleRenderRig(new Vector3(0f, 0f, -3f), Quaternion.identity, 0.5f))
            {
                renderer.Draw(rig.Camera);
                rig.Render();

                Assert.AreEqual(1, renderer.BatchCount);
                Assert.IsTrue(rig.IsDrawn(centre), "Centre of the fragment.");
                int verified = 0;
                foreach (Vector3 corner in simulated)
                {
                    Vector3 radial = corner - centre;
                    radial.z = 0f;
                    if (radial.magnitude < 0.1f)
                        continue;
                    Assert.IsTrue(rig.IsDrawn(centre + radial * 0.9f), $"Inside the corner {corner:F3} of the simulated pose.");
                    Assert.IsFalse(rig.IsDrawn(centre + radial * 1.35f), $"Outside the corner {corner:F3} of the simulated pose.");
                    verified++;
                }
                Assert.GreaterOrEqual(verified, 4);

                float turned = Vector2.Angle(new Vector2(simulated[7].x - centre.x, simulated[7].y - centre.y), new Vector2(0.15f, 0.15f));
                Assert.That(turned, Is.GreaterThan(20f), "The fragment must have turned, or the check proves nothing.");
            }
        }

        [Test]
        public void Check06_LifecycleHiddenFractionShrinksTheFragmentToItsCentre()
        {
            ParticleWorld world = World();
            ParticleHullAppearance appearance = Appearance<ParticleHullAppearance>(RigidShader, Color.red);
            TestBodySource source = FragmentSource(world, appearance, new Pose(Vector3.zero, Quaternion.identity), Vector3.zero, out _);
            world.Step(Dt);
            var renderer = source.gameObject.AddComponent<PhysicsParticleRenderer>();
            renderer.BoundsInterval = 1000f;

            using (var rig = new ParticleRenderRig(new Vector3(0f, 0f, -3f), Quaternion.identity, 0.5f))
            {
                renderer.Draw(rig.Camera);
                rig.Render();
                Assert.IsTrue(rig.IsDrawn(new Vector3(0.14f, 0f, 0f)), "Visible without a lifecycle buffer.");

                source.LifecycleBuffer = Buffer(new[] { new Vector4(0f, 0f, 0.5f, 0f) }, 16);
                renderer.Draw(rig.Camera);
                rig.Render();
                Assert.IsTrue(rig.IsDrawn(new Vector3(0.05f, 0f, 0f)));
                Assert.IsFalse(rig.IsDrawn(new Vector3(0.14f, 0f, 0f)), "Half hidden is half the size.");

                source.LifecycleBuffer = Buffer(new[] { new Vector4(0f, 0f, 1f, 0f) }, 16);
                renderer.Draw(rig.Camera);
                rig.Render();
                Assert.IsFalse(rig.IsDrawn(Vector3.zero), "Fully hidden collapses onto the centre.");
            }
        }

        [Test]
        public void Check06_OneInstancedDrawPerVariant_AndAuthoredMeshFitsTheRestExtent()
        {
            ParticleWorld world = World();
            var points = _scope.Asset<PointParticleLayout>();
            points.Variants = new[] { TetrahedronVariant(0.1f), OctahedronVariant(0.1f), CubeVariant(0.1f) };
            var rigid = _scope.Asset<RigidGroupInteraction>();
            rigid.ParticleRadius = 0.02f;
            ParticleHullAppearance hull = Appearance<ParticleHullAppearance>(RigidShader, Color.red);
            PhysicsParticle driver = _scope.Driver("Fragments", points, rigid, 7);
            driver.World = world;
            driver.Appearance = hull;
            driver.BatchSpacing = new Vector3(0.5f, 0f, 0f);
            driver.EnableDriver();
            world.Step(Dt);
            var renderer = driver.GetComponent<PhysicsParticleRenderer>();
            renderer.BoundsInterval = 1000f;

            using (var rig = new ParticleRenderRig(new Vector3(1.5f, 0f, -3f), Quaternion.identity, 2f))
            {
                renderer.Draw(rig.Camera);
                rig.Render();

                Assert.AreEqual(3, renderer.BatchCount, "One instanced draw per layout variant.");
                Assert.AreEqual(7, renderer.DrawnInstanceCount);
                CollectionAssert.AreEquivalent(new[] { 12, 24, 36 }, renderer.DrawnMeshes.Select(m => m.vertexCount).ToArray());
                for (int i = 0; i < 7; i++)
                    Assert.IsTrue(rig.IsDrawn(new Vector3(0.5f * i, 0f, 0f)), $"Fragment {i} at its pose.");

                int draws = renderer.BatchCount;
                renderer.Draw(rig.Camera);
                Assert.AreEqual(draws, renderer.BatchCount, "Meshes are built once and reused.");
            }

            ParticleWorld authoredWorld = World();
            AuthoredMeshAppearance authored = Appearance<AuthoredMeshAppearance>(RigidShader, Color.red);
            Mesh box = Box(Vector3.one);
            _objects.Add(box);
            authored.Mesh = box;
            authored.ForwardAxis = Vector3.up;
            authored.FitToDimensions = true;
            TestBodySource source = FragmentSource(authoredWorld, authored, new Pose(Vector3.zero, Quaternion.identity), Vector3.zero, out _);
            authoredWorld.Step(Dt);
            var authoredRenderer = source.gameObject.AddComponent<PhysicsParticleRenderer>();
            authoredRenderer.BoundsInterval = 1000f;

            using (var rig = new ParticleRenderRig(new Vector3(0f, 0f, -3f), Quaternion.identity, 0.5f))
            {
                authoredRenderer.Draw(rig.Camera);
                rig.Render();

                Assert.IsTrue(rig.IsDrawn(new Vector3(0.16f, 0.16f, 0f)), "Fitted to the 0.34 m rest extent.");
                Assert.IsFalse(rig.IsDrawn(new Vector3(0.2f, 0f, 0f)));
                Assert.IsFalse(rig.IsDrawn(new Vector3(0f, 0.2f, 0f)));
            }
        }

        [Test]
        public void Profiles_ValidateTheirLayoutKinds()
        {
            var grid = _scope.Grid(4, 4);
            var chain = _scope.Asset<ChainParticleLayout>();
            chain.Count = 5;
            GridSurfaceAppearance surface = Appearance<GridSurfaceAppearance>(GridShader, Color.red);
            ChainStripAppearance strip = Appearance<ChainStripAppearance>(ChainShader, Color.red);
            ArticulatedMeshAppearance articulated = Appearance<ArticulatedMeshAppearance>(ArticulatedShader, Color.red);
            var cloth = _scope.Asset<ClothInteraction>();
            var rope = _scope.Asset<ChainInteraction>();
            var messages = new List<PhysicsParticleMessage>();

            surface.Validate(grid, cloth, messages);
            strip.Validate(chain, rope, messages);
            Assert.IsEmpty(messages.Where(m => m.severity == PhysicsParticleMessageSeverity.Error));

            surface.Validate(chain, rope, messages);
            Assert.IsTrue(messages.Any(m => m.text.Contains("needs a grid layout")));
            messages.Clear();
            strip.Validate(grid, cloth, messages);
            Assert.IsTrue(messages.Any(m => m.text.Contains("needs a chain layout")));
            messages.Clear();
            articulated.Validate(grid, cloth, messages);
            Assert.IsTrue(messages.Any(m => m.text.Contains("chain or body layout")));
        }

        static int CountOccurrences(string text, string token)
        {
            int count = 0;
            int index = 0;
            while ((index = text.IndexOf(token, index, System.StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += token.Length;
            }
            return count;
        }
    }
}

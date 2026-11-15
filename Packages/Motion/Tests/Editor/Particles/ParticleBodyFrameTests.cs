using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Yu5h1Lib.Tests
{
    /// <summary>
    ///   GPU part of ParticleExtensions PEXT-CHECK-10: the shared body frame
    ///   (Shaders/ParticleBodyFrame.hlsl) of a straight GuideChain4 body points its tangent from
    ///   tail to head and its side at the guide particle, and the body colour unpacks from RGBA8.
    /// </summary>
    public sealed class ParticleBodyFrameTests
    {
        const string KernelPath = "Packages/com.yu5h1.motion/Tests/Editor/Particles/ParticleBodyFrameTest.compute";

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

        [Test]
        public void StraightGuideChainFrame_FollowsHeadAndGuide()
        {
            var shader = AssetDatabase.LoadAssetAtPath<ComputeShader>(KernelPath);
            Assert.IsNotNull(shader, KernelPath);
            ParticleWorld world = _scope.World("World");
            world.Settings.gravity = Vector3.zero;
            Quaternion rotation = Quaternion.Euler(0f, 40f, 90f);
            var request = new ParticleEntityRequest
            {
                groupMode = ParticleGroupMode.NewGroup,
                pose = new Pose(new Vector3(1f, 2f, 3f), rotation)
            };
            foreach (Vector3 local in new[] { new Vector3(0f, 0.1f, 0f), Vector3.zero, new Vector3(0f, -0.1f, 0f), new Vector3(0.05f, 0f, 0f) })
                request.particles.Add(new ParticleSpawn { position = local, invMass = 1f, radius = 0.01f });
            Assert.IsTrue(world.TryAllocate(request, out ParticleEntity entity, out string error), error);
            world.Step(0.02f);

            Color32 color = new Color32(255, 128, 0, 64);
            uint packed = color.r | ((uint)color.g << 8) | ((uint)color.b << 16) | ((uint)color.a << 24);
            var body = new ParticleBodyData
            {
                particleStart = (uint)entity.Particles.start,
                particleCount = 4,
                topology = (uint)ParticleBodyTopology.GuideChain4,
                flags = 1u,
                scale = new Vector4(1f, 1f, 1f, 1f),
                spawnRotation = new Vector4(rotation.x, rotation.y, rotation.z, rotation.w),
                dimensions = new Vector4(0.1f, 0.2f, 0.1f, System.BitConverter.ToSingle(System.BitConverter.GetBytes(packed), 0))
            };
            var bodies = new ComputeBuffer(1, ParticleBodyData.Stride);
            var frames = new ComputeBuffer(4, 16);
            try
            {
                bodies.SetData(new[] { body });
                int kernel = shader.FindKernel("EvaluateFrames");
                Assert.IsTrue(world.TryBind(shader, kernel));
                shader.SetBuffer(kernel, "_TestBodies", bodies);
                shader.SetBuffer(kernel, "_TestFrames", frames);
                shader.SetInt("_TestCount", 1);
                ParticleExtensionContext.Dispatch(shader, kernel, 1);
                Vector4[] result = null;
                AsyncGPUReadback.Request(frames, r => result = r.GetData<Vector4>().ToArray());
                AsyncGPUReadback.WaitAllRequests();

                Assert.That(Vector3.Distance(request.pose.position, result[0]), Is.LessThan(1e-4f), "centre");
                Assert.That(Vector3.Distance(rotation * Vector3.up, result[1]), Is.LessThan(1e-4f), "tangent");
                Assert.That(Vector3.Distance(rotation * Vector3.right, result[2]), Is.LessThan(1e-4f), "side");
                Assert.That(Vector4.Distance(new Vector4(1f, 128f / 255f, 0f, 64f / 255f), result[3]), Is.LessThan(1e-4f), "colour");
            }
            finally
            {
                bodies.Release();
                frames.Release();
            }
        }
    }
}

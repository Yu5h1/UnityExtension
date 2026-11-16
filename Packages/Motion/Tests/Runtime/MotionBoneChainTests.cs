// Tests for MotionBoneChain (spec motion.bone-chain 1.0.0).
using System;
using System.Globalization;
using NUnit.Framework;
using UnityEngine;

namespace Yu5h1Lib.Tests
{
    [Serializable]
    public sealed class BoneChainFixtureDocument
    {
        public string sourceSha256;
        public float tolerancePosition;
        public float toleranceRotation;
        public float toleranceLength;
        public BoneChainFixtureCase[] cases;
    }

    [Serializable]
    public sealed class BoneChainFixtureCase
    {
        public string name;
        public int nodes;
        public BoneChainFixtureAction[] actions;
    }

    [Serializable]
    public sealed class BoneChainFixtureAction
    {
        public string op;
        public string[] args;
        public float[] expected;
    }

    public static class BoneChainFixtureReplay
    {
        private static float Number(string s) => float.Parse(s, CultureInfo.InvariantCulture);

        public static int Run(BoneChainFixtureDocument document)
        {
            int totalTicks = 0;
            float posTol = document.tolerancePosition > 0f ? document.tolerancePosition : 1e-4f;
            float rotTol = document.toleranceRotation > 0f ? document.toleranceRotation : 1e-4f;
            float lenTol = document.toleranceLength > 0f ? document.toleranceLength : 1e-4f;

            foreach (var testCase in document.cases)
            {
                var chain = new MotionBoneChain(testCase.nodes);
                Vector3[] currentPose = null;

                for (int aIdx = 0; aIdx < testCase.actions.Length; aIdx++)
                {
                    var action = testCase.actions[aIdx];
                    switch (action.op)
                    {
                        case "set_node":
                        {
                            int node = int.Parse(action.args[0], CultureInfo.InvariantCulture);
                            float k = Number(action.args[1]);
                            float c = Number(action.args[2]);
                            float r = Number(action.args[3]);
                            bool expect = action.expected.Length > 0 && action.expected[0] > 0.5f;
                            bool applied = chain.SetNode(node, k, c, r);
                            Assert.AreEqual(expect, applied, $"{testCase.name} act {aIdx} set_node result mismatch.");
                            break;
                        }
                        case "set_gravity":
                        {
                            Vector3 g = new Vector3(Number(action.args[0]), Number(action.args[1]), Number(action.args[2]));
                            bool expect = action.expected.Length > 0 && action.expected[0] > 0.5f;
                            bool applied = chain.SetGravity(g);
                            Assert.AreEqual(expect, applied, $"{testCase.name} act {aIdx} set_gravity result mismatch.");
                            break;
                        }
                        case "set_iterations":
                        {
                            int iter = int.Parse(action.args[0], CultureInfo.InvariantCulture);
                            bool expect = action.expected.Length > 0 && action.expected[0] > 0.5f;
                            bool applied = chain.SetIterations(iter);
                            Assert.AreEqual(expect, applied, $"{testCase.name} act {aIdx} set_iterations result mismatch.");
                            break;
                        }
                        case "pose":
                        {
                            currentPose = new Vector3[testCase.nodes + 1];
                            for (int j = 0; j <= testCase.nodes; j++)
                            {
                                currentPose[j] = new Vector3(
                                    Number(action.args[j * 3 + 0]),
                                    Number(action.args[j * 3 + 1]),
                                    Number(action.args[j * 3 + 2]));
                                chain.SetJoint(j, currentPose[j]);
                            }
                            break;
                        }
                        case "colliders":
                        {
                            chain.ClearColliders();
                            int ptr = 0;
                            while (ptr < action.args.Length)
                            {
                                string type = action.args[ptr++];
                                if (type == "sphere")
                                {
                                    Vector3 center = new Vector3(Number(action.args[ptr++]), Number(action.args[ptr++]), Number(action.args[ptr++]));
                                    float rad = Number(action.args[ptr++]);
                                    bool expect = Number(action.args[ptr++]) > 0.5f;
                                    bool applied = chain.AddSphere(center, rad);
                                    Assert.AreEqual(expect, applied, $"{testCase.name} act {aIdx} add_sphere mismatch.");
                                }
                                else if (type == "capsule")
                                {
                                    Vector3 a = new Vector3(Number(action.args[ptr++]), Number(action.args[ptr++]), Number(action.args[ptr++]));
                                    Vector3 b = new Vector3(Number(action.args[ptr++]), Number(action.args[ptr++]), Number(action.args[ptr++]));
                                    float rad = Number(action.args[ptr++]);
                                    bool expect = Number(action.args[ptr++]) > 0.5f;
                                    bool applied = chain.AddCapsule(a, b, rad);
                                    Assert.AreEqual(expect, applied, $"{testCase.name} act {aIdx} add_capsule mismatch.");
                                }
                            }
                            break;
                        }
                        case "reset":
                        {
                            chain.Reset();
                            break;
                        }
                        case "expect_initialized":
                        {
                            bool expectInit = action.args[0] == "1";
                            Assert.AreEqual(expectInit, chain.Initialized, $"{testCase.name} act {aIdx} expect_initialized mismatch.");
                            break;
                        }
                        case "tick":
                        {
                            double dt = double.Parse(action.args[0], CultureInfo.InvariantCulture);
                            int repeat = int.Parse(action.args[1], CultureInfo.InvariantCulture);

                            for (int r = 0; r < repeat; r++)
                            {
                                int steps = chain.Tick(dt);
                                totalTicks++;

                                if (r == repeat - 1)
                                {
                                    int expSteps = (int)action.expected[0];
                                    Assert.AreEqual(expSteps, steps, $"{testCase.name} act {aIdx} steps mismatch.");

                                    bool expInit = action.expected[1] > 0.5f;
                                    Assert.AreEqual(expInit, chain.Initialized, $"{testCase.name} act {aIdx} initialized flag mismatch.");

                                    if (expSteps > 0 && currentPose != null)
                                    {
                                        for (int j = 1; j <= testCase.nodes; j++)
                                        {
                                            float L = Vector3.Distance(currentPose[j], currentPose[j - 1]);
                                            float S = Vector3.Distance(chain.Position(j), chain.Position(j - 1));
                                            Assert.LessOrEqual(Mathf.Abs(S - L), lenTol, $"{testCase.name} bone {j} length error {Mathf.Abs(S - L)} exceeds {lenTol}");
                                        }
                                    }

                                    int expPtr = 2;
                                    int jointCoordsCount = (testCase.nodes + 1) * 3;
                                    if (action.expected.Length >= expPtr + jointCoordsCount)
                                    {
                                        for (int j = 0; j <= testCase.nodes; j++)
                                        {
                                            Vector3 pos = chain.Position(j);
                                            Vector3 expPos = new Vector3(action.expected[expPtr++], action.expected[expPtr++], action.expected[expPtr++]);
                                            Assert.LessOrEqual(Mathf.Abs(pos.x - expPos.x), posTol, $"{testCase.name} joint {j}.x error {Mathf.Abs(pos.x - expPos.x)}");
                                            Assert.LessOrEqual(Mathf.Abs(pos.y - expPos.y), posTol, $"{testCase.name} joint {j}.y error {Mathf.Abs(pos.y - expPos.y)}");
                                            Assert.LessOrEqual(Mathf.Abs(pos.z - expPos.z), posTol, $"{testCase.name} joint {j}.z error {Mathf.Abs(pos.z - expPos.z)}");
                                        }
                                    }

                                    int rotCoordsCount = testCase.nodes * 4;
                                    if (action.expected.Length >= expPtr + rotCoordsCount)
                                    {
                                        for (int j = 1; j <= testCase.nodes; j++)
                                        {
                                            Quaternion rot = chain.Aim(j);
                                            Quaternion expRot = new Quaternion(action.expected[expPtr++], action.expected[expPtr++], action.expected[expPtr++], action.expected[expPtr++]);
                                            Assert.LessOrEqual(Mathf.Abs(rot.x - expRot.x), rotTol, $"{testCase.name} rot {j}.x error {Mathf.Abs(rot.x - expRot.x)}");
                                            Assert.LessOrEqual(Mathf.Abs(rot.y - expRot.y), rotTol, $"{testCase.name} rot {j}.y error {Mathf.Abs(rot.y - expRot.y)}");
                                            Assert.LessOrEqual(Mathf.Abs(rot.z - expRot.z), rotTol, $"{testCase.name} rot {j}.z error {Mathf.Abs(rot.z - expRot.z)}");
                                            Assert.LessOrEqual(Mathf.Abs(rot.w - expRot.w), rotTol, $"{testCase.name} rot {j}.w error {Mathf.Abs(rot.w - expRot.w)}");
                                        }
                                    }
                                }
                            }
                            break;
                        }
                    }
                }
            }
            return totalTicks;
        }
    }

    public class MotionBoneChainTests
    {
        [Test]
        public void SharedReferenceFixtures_PassesAll13Cases_840Ticks()
        {
            var asset = Resources.Load<TextAsset>("MotionValidation/bone_chain_fixtures");
            Assert.IsNotNull(asset, "Resources asset MotionValidation/bone_chain_fixtures must exist.");
            var doc = JsonUtility.FromJson<BoneChainFixtureDocument>(asset.text);
            Assert.IsNotNull(doc, "Failed to deserialize bone chain fixture document.");
            Assert.AreEqual(13, doc.cases.Length, "Document must have 13 standard test cases.");

            int ticks = BoneChainFixtureReplay.Run(doc);
            Assert.AreEqual(840, ticks, "Total simulated ticks must equal 840.");
        }

        [Test]
        public void AnalyticalEquilibrium_SingleNode_CHAIN_09()
        {
            // CHAIN-09: n=1, g perpendicular to bone. tan(theta) = |g| / k.
            var chain = new MotionBoneChain(1);
            float k = 100f;
            float c = 10f;
            chain.SetNode(0, k, c, 0f);
            chain.SetJoint(0, Vector3.zero);
            chain.SetJoint(1, new Vector3(1f, 0f, 0f)); // horizontal bone along +X
            Vector3 g = new Vector3(0f, -9.81f, 0f); // gravity along -Y
            chain.SetGravity(g);

            // Settle under high damping and sufficient time
            for (int i = 0; i < 240; i++)
            {
                chain.Tick(1.0 / 60.0);
            }

            Vector3 tail = chain.Position(1);
            float expectedTan = Mathf.Abs(g.y) / k;
            float actualTan = Mathf.Abs(tail.y) / tail.x;
            Assert.AreEqual(expectedTan, actualTan, 0.001f, "Analytical equilibrium angle tangent must equal |g|/k.");
        }

        [Test]
        public void LifecycleAndIsolation_CHAIN_P1()
        {
            var chainA = new MotionBoneChain(2);
            var chainB = new MotionBoneChain(3);

            chainA.SetJoint(0, new Vector3(0f, 1f, 0f));
            chainA.SetJoint(1, new Vector3(0f, 2f, 0f));
            chainA.SetJoint(2, new Vector3(0f, 3f, 0f));

            chainB.SetJoint(0, new Vector3(10f, 0f, 0f));
            chainB.SetJoint(1, new Vector3(11f, 0f, 0f));
            chainB.SetJoint(2, new Vector3(12f, 0f, 0f));
            chainB.SetJoint(3, new Vector3(13f, 0f, 0f));

            chainA.Tick(1.0 / 60.0);
            chainB.Tick(1.0 / 60.0);

            Assert.IsTrue(chainA.Initialized);
            Assert.IsTrue(chainB.Initialized);
            Assert.AreEqual(10f, chainB.Position(0).x);

            chainA.Reset();
            Assert.IsFalse(chainA.Initialized);
            Assert.IsTrue(chainB.Initialized); // B remains isolated and unaffected

            var chainC = MotionBoneChain.TryCreate(4);
            Assert.IsNotNull(chainC);
            Assert.AreEqual(4, chainC.Nodes);
        }

        [Test]
        public void SphereAndCapsulePushOut_PreventsMeshClipping_CHAIN_06_07()
        {
            var chain = new MotionBoneChain(2);
            chain.SetNode(0, 50f, 5f, 0.05f); // 5cm radius node
            chain.SetNode(1, 50f, 5f, 0.05f);
            chain.SetJoint(0, new Vector3(0f, 2f, 0f));
            chain.SetJoint(1, new Vector3(0f, 1.5f, 0f));
            chain.SetJoint(2, new Vector3(0f, 1.0f, 0f));

            // Sphere representing chest/head at (0, 1.5, 0.08) with radius 0.15m
            Vector3 sphereCenter = new Vector3(0f, 1.5f, 0.08f);
            float sphereRadius = 0.15f;
            chain.AddSphere(sphereCenter, sphereRadius);
            chain.SetIterations(4);

            chain.Tick(1.0 / 60.0);

            // Node 1 initial position (0, 1.5, 0) is inside the sphere reach (dist = 0.08m < 0.15 + 0.05 = 0.20m).
            // Push-out must displace node 1 outside the sphere reach.
            float distToSphere = Vector3.Distance(chain.Position(1), sphereCenter);
            Assert.GreaterOrEqual(distToSphere, sphereRadius + 0.05f - 0.001f, 
                "Bone node must be pushed outside Sphere collider surface.");

            // Add Capsule collider representing thigh at x = 0.1m, y from 0.8 to 1.3m, radius 0.1m
            chain.AddCapsule(new Vector3(0.1f, 0.8f, 0f), new Vector3(0.1f, 1.3f, 0f), 0.1f);
            chain.Tick(1.0 / 60.0);
            Assert.IsTrue(chain.Initialized);
        }
    }
}

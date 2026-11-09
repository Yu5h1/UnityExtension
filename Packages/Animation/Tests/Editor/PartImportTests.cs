using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Yu5h1Lib.EditorExtension;

namespace Yu5h1Lib.Tests
{
    public class PartImportTests
    {
        const string TestFolder = "Assets/__PartImportTests";
        const string DataFolder = "Packages/com.yu5h1.animation/Tests/Data";

        [SetUp]
        public void SetUp()
        {
            if (AssetDatabase.IsValidFolder(TestFolder))
                AssetDatabase.DeleteAsset(TestFolder);
            AssetDatabase.CreateFolder("Assets", "__PartImportTests");
        }

        [TearDown]
        public void TearDown()
        {
            if (AssetDatabase.IsValidFolder(TestFolder))
                AssetDatabase.DeleteAsset(TestFolder);
        }

        [Test]
        public void ImportValidParts_ProducesAssetsWithExpectedProperties()
        {
            string swordJsonPath = $"{DataFolder}/sword.arming.part.json";
            string helmetJsonPath = $"{DataFolder}/helmet.kettle.part.json";

            var sword = PartImporter.Import(swordJsonPath);
            Assert.IsNotNull(sword, "sword.arming asset should be imported");
            Assert.AreEqual("sword.arming", sword.id);
            Assert.AreEqual("1.0.0", sword.version);
            Assert.AreEqual("weapon.sword", sword.kind);
            Assert.AreEqual("yu5h1.part.draft", sword.format);
            Assert.AreEqual("0.1.0", sword.formatVersion);
            Assert.AreEqual("wrist_r", sword.targetBone);
            Assert.AreEqual(0.102f, sword.offsetPosition.x, 1e-4f);
            Assert.AreEqual(0f, sword.offsetPosition.y, 1e-4f);
            Assert.AreEqual(0f, sword.offsetPosition.z, 1e-4f);
            Assert.AreEqual(90f, sword.offsetRotation.x, 1e-4f);
            Assert.AreEqual(0f, sword.offsetRotation.y, 1e-4f);
            Assert.AreEqual(0f, sword.offsetRotation.z, 1e-4f);
            Assert.AreEqual(0f, sword.attachPosition.x, 1e-4f);
            Assert.AreEqual(0.091f, sword.attachPosition.y, 1e-4f);
            Assert.AreEqual(0f, sword.attachPosition.z, 1e-4f);

            var helmet = PartImporter.Import(helmetJsonPath);
            Assert.IsNotNull(helmet, "helmet.kettle asset should be imported");
            Assert.AreEqual("helmet.kettle", helmet.id);
            Assert.AreEqual("1.0.0", helmet.version);
            Assert.AreEqual("helmet", helmet.kind);
            Assert.AreEqual("yu5h1.part.draft", helmet.format);
            Assert.AreEqual("0.1.0", helmet.formatVersion);
            Assert.AreEqual("head", helmet.targetBone);
            Assert.AreEqual(Vector3.zero, helmet.offsetPosition);
            Assert.AreEqual(Vector3.zero, helmet.offsetRotation);
            Assert.AreEqual(Vector3.zero, helmet.attachPosition);

            // Verify assets are loaded by AssetDatabase
            string swordAssetPath = PartImporter.GetAssetPath(swordJsonPath);
            var reloadedSword = AssetDatabase.LoadAssetAtPath<PartAsset>(swordAssetPath);
            Assert.IsNotNull(reloadedSword, "AssetDatabase should load sword.arming.asset");
            Assert.AreEqual("sword.arming", reloadedSword.id);

            string helmetAssetPath = PartImporter.GetAssetPath(helmetJsonPath);
            var reloadedHelmet = AssetDatabase.LoadAssetAtPath<PartAsset>(helmetAssetPath);
            Assert.IsNotNull(reloadedHelmet, "AssetDatabase should load helmet.kettle.asset");
            Assert.AreEqual("helmet.kettle", reloadedHelmet.id);
        }

        [Test]
        public void ImportSheathedSword_MultiAxisEulerRotation_MatchesPartWorldMatrixAndLocalDirection()
        {
            string sheathedJsonPath = $"{DataFolder}/sword.sheathed.part.json";
            var sword = PartImporter.Import(sheathedJsonPath);
            Assert.IsNotNull(sword, "sword.sheathed asset should be imported");
            Assert.AreEqual("sword.sheathed", sword.id);
            Assert.AreEqual("pelvis", sword.targetBone);
            Assert.AreEqual(-0.2f, sword.offsetPosition.x, 1e-4f);
            Assert.AreEqual(0.08f, sword.offsetPosition.y, 1e-4f);
            Assert.AreEqual(0f, sword.offsetPosition.z, 1e-4f);
            Assert.AreEqual(200f, sword.offsetRotation.x, 1e-4f);
            Assert.AreEqual(-90f, sword.offsetRotation.y, 1e-4f);
            Assert.AreEqual(0f, sword.offsetRotation.z, 1e-4f);
            Assert.AreEqual(0f, sword.attachPosition.x, 1e-4f);
            Assert.AreEqual(0.091f, sword.attachPosition.y, 1e-4f);
            Assert.AreEqual(0f, sword.attachPosition.z, 1e-4f);

            // Test local transform calculation (XYZ intrinsic Euler angles)
            sword.GetLocalTransform(mirror: false, out var localPos, out var localRot);
            Assert.AreEqual(-0.2f, localPos.x, 1e-4f);
            Assert.AreEqual(0.165512f, localPos.y, 1e-4f);
            Assert.AreEqual(0.0311238f, localPos.z, 1e-4f);

            // Character Builder attach.js blade direction (+Y) matches Rx(200)*Ry(-90)*(0,1,0)
            Vector3 bladeDir = localRot * Vector3.up;
            Assert.AreEqual(0f, bladeDir.x, 1e-4f);
            Assert.AreEqual(-0.9396926f, bladeDir.y, 1e-4f);
            Assert.AreEqual(-0.3420201f, bladeDir.z, 1e-4f);

            // Attach to punch character and verify world coordinates and orientation
            string charGlbPath = $"{DataFolder}/punch-character.glb";
            var punchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(charGlbPath);
            Assert.IsNotNull(punchPrefab);
            var character = Object.Instantiate(punchPrefab);
            try
            {
                var inst = PartAttachment.Attach(character, sword);
                Assert.IsNotNull(inst);
                var attachNode = inst.transform.Find("attach");
                Assert.IsNotNull(attachNode);

                // Pelvis bone is at (0, 0.95, 0). attach node in world = pelvis + offset = (-0.2, 1.03, 0)
                Vector3 expectedAttachWorld = new Vector3(-0.2f, 1.03f, 0f);
                float dist = Vector3.Distance(attachNode.position, expectedAttachWorld);
                Assert.LessOrEqual(dist, 0.001f, $"Attach node distance {dist * 1000f:F3} mm exceeds 1 mm");

                Vector3 worldBladeDir = inst.transform.TransformDirection(Vector3.up);
                Assert.AreEqual(0f, worldBladeDir.x, 1e-4f);
                Assert.AreEqual(-0.9396926f, worldBladeDir.y, 1e-4f);
                Assert.AreEqual(-0.3420201f, worldBladeDir.z, 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(character);
            }
        }

        [Test]
        public void ImportRealModel_AttachedToPunchCharacter_MatchesExpectedPositions()
        {
            string swordJsonPath = $"{DataFolder}/sword.arming.part.json";
            string helmetJsonPath = $"{DataFolder}/helmet.kettle.part.json";
            string charGlbPath = $"{DataFolder}/punch-character.glb";

            var sword = PartImporter.Import(swordJsonPath);
            var helmet = PartImporter.Import(helmetJsonPath);
            var punchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(charGlbPath);
            Assert.IsNotNull(punchPrefab, "punch-character.glb should load as a GameObject");

            var character = Object.Instantiate(punchPrefab);
            try
            {
                // Attach sword to wrist_r
                var swordInst = PartAttachment.Attach(character, sword);
                Assert.IsNotNull(swordInst);
                var swordAttachNode = swordInst.transform.Find("attach");
                Assert.IsNotNull(swordAttachNode);
                Vector3 expectedSwordPos = new Vector3(0.802f, 1.39f, 0f);
                float swordDist = Vector3.Distance(swordAttachNode.position, expectedSwordPos);
                Assert.LessOrEqual(swordDist, 0.001f, $"Sword distance {swordDist * 1000f:F3} mm exceeds 1 mm");

                // Attach helmet to head
                var helmetInst = PartAttachment.Attach(character, helmet);
                Assert.IsNotNull(helmetInst);
                var helmetAttachNode = helmetInst.transform.Find("attach");
                Assert.IsNotNull(helmetAttachNode);
                Vector3 expectedHelmetPos = new Vector3(0f, 1.56f, 0f);
                float helmetDist = Vector3.Distance(helmetAttachNode.position, expectedHelmetPos);
                Assert.LessOrEqual(helmetDist, 0.001f, $"Helmet distance {helmetDist * 1000f:F3} mm exceeds 1 mm");

                // Attach mirrored sword to wrist_l
                var leftSword = PartAttachment.Attach(character, sword, "wrist_l", mirror: true);
                Assert.IsNotNull(leftSword);
                var leftAttachNode = leftSword.transform.Find("attach");
                Assert.IsNotNull(leftAttachNode);
                Vector3 expectedLeftPos = new Vector3(-0.802f, 1.39f, 0f);
                float leftDist = Vector3.Distance(leftAttachNode.position, expectedLeftPos);
                Assert.LessOrEqual(leftDist, 0.001f, $"Left sword distance {leftDist * 1000f:F3} mm exceeds 1 mm");

                // Detach
                bool detached = PartAttachment.Detach(character, sword);
                Assert.IsTrue(detached);
            }
            finally
            {
                Object.DestroyImmediate(character);
            }
        }

        [Test]
        public void ImportInvalidPart_MissingTarget_LogsErrorAndProducesNoAsset()
        {
            File.Copy($"{DataFolder}/sword.arming.glb", $"{TestFolder}/missing_target.glb", true);

            string json = @"{
  ""format"": ""yu5h1.part.draft"",
  ""formatVersion"": ""0.1.0"",
  ""id"": ""test.missing_target"",
  ""version"": ""1.0.0"",
  ""kind"": ""weapon.sword"",
  ""glb"": ""missing_target.glb"",
  ""attach"": {
    ""offset"": { ""position"": [0,0,0], ""rotation"": [0,0,0] }
  }
}";
            string jsonPath = $"{TestFolder}/missing_target.part.json";
            File.WriteAllText(jsonPath, json);

            LogAssert.Expect(LogType.Error, new Regex(@"/attach/target"));
            var asset = PartImporter.Import(jsonPath);
            Assert.IsNull(asset, "Asset should not be produced when attach.target is missing");

            string expectedAssetPath = PartImporter.GetAssetPath(jsonPath);
            Assert.IsFalse(File.Exists(expectedAssetPath), "No .asset file should be written");
        }

        [Test]
        public void ImportInvalidPart_UnsupportedFormatVersion_LogsErrorAndProducesNoAsset()
        {
            File.Copy($"{DataFolder}/sword.arming.glb", $"{TestFolder}/bad_version.glb", true);

            string json = @"{
  ""format"": ""yu5h1.part.draft"",
  ""formatVersion"": ""0.2.0"",
  ""id"": ""test.bad_version"",
  ""version"": ""1.0.0"",
  ""kind"": ""weapon.sword"",
  ""glb"": ""bad_version.glb"",
  ""attach"": {
    ""target"": ""wrist_r""
  }
}";
            string jsonPath = $"{TestFolder}/bad_version.part.json";
            File.WriteAllText(jsonPath, json);

            LogAssert.Expect(LogType.Error, new Regex(@"/formatVersion"));
            var asset = PartImporter.Import(jsonPath);
            Assert.IsNull(asset, "Asset should not be produced when formatVersion is unsupported");

            string expectedAssetPath = PartImporter.GetAssetPath(jsonPath);
            Assert.IsFalse(File.Exists(expectedAssetPath), "No .asset file should be written");
        }

        [Test]
        public void ImportInvalidPart_GlbMissingAttachNode_LogsErrorAndProducesNoAsset()
        {
            byte[] glbBytes = File.ReadAllBytes($"{DataFolder}/helmet.kettle.glb");
            byte[] targetPattern = Encoding.ASCII.GetBytes("\"attach\"");
            byte[] replacement = Encoding.ASCII.GetBytes("\"dummy_\"");

            for (int i = 20; i <= glbBytes.Length - targetPattern.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < targetPattern.Length; j++)
                {
                    if (glbBytes[i + j] != targetPattern[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    for (int j = 0; j < replacement.Length; j++)
                        glbBytes[i + j] = replacement[j];
                    break;
                }
            }

            string modifiedGlbPath = $"{TestFolder}/no_attach.glb";
            File.WriteAllBytes(modifiedGlbPath, glbBytes);

            string json = @"{
  ""format"": ""yu5h1.part.draft"",
  ""formatVersion"": ""0.1.0"",
  ""id"": ""test.no_attach"",
  ""version"": ""1.0.0"",
  ""kind"": ""helmet"",
  ""glb"": ""no_attach.glb"",
  ""attach"": {
    ""target"": ""head""
  }
}";
            string jsonPath = $"{TestFolder}/no_attach.part.json";
            File.WriteAllText(jsonPath, json);

            LogAssert.Expect(LogType.Error, new Regex(@"attach"));
            var asset = PartImporter.Import(jsonPath);
            Assert.IsNull(asset, "Asset should not be produced when GLB is missing attach node");

            string expectedAssetPath = PartImporter.GetAssetPath(jsonPath);
            Assert.IsFalse(File.Exists(expectedAssetPath), "No .asset file should be written");
        }
    }
}

using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Yu5h1Lib.Tests
{
    public class PartAttachmentPlayModeTests
    {
        GameObject _character;
        PartAsset _swordPart;
        PartAsset _helmetPart;
        GameObject _swordPrefab;
        GameObject _helmetPrefab;

        [SetUp]
        public void SetUp()
        {
            _character = BuildPunchCharacter();

            _swordPrefab = new GameObject("sword.arming");
            var swordAttach = new GameObject("attach");
            swordAttach.transform.SetParent(_swordPrefab.transform, false);
            swordAttach.transform.localPosition = new Vector3(0f, 0.091f, 0f);
            swordAttach.transform.localRotation = Quaternion.identity;

            _swordPart = ScriptableObject.CreateInstance<PartAsset>();
            _swordPart.id = "sword.arming";
            _swordPart.version = "1.0.0";
            _swordPart.kind = "weapon.sword";
            _swordPart.targetBone = "wrist_r";
            _swordPart.offsetPosition = new Vector3(0.102f, 0f, 0f);
            _swordPart.offsetRotation = new Vector3(90f, 0f, 0f);
            _swordPart.attachPosition = new Vector3(0f, 0.091f, 0f);
            _swordPart.attachRotation = Quaternion.identity;
            _swordPart.prefab = _swordPrefab;

            _helmetPrefab = new GameObject("helmet.kettle");
            var helmetAttach = new GameObject("attach");
            helmetAttach.transform.SetParent(_helmetPrefab.transform, false);
            helmetAttach.transform.localPosition = Vector3.zero;
            helmetAttach.transform.localRotation = Quaternion.identity;

            _helmetPart = ScriptableObject.CreateInstance<PartAsset>();
            _helmetPart.id = "helmet.kettle";
            _helmetPart.version = "1.0.0";
            _helmetPart.kind = "helmet";
            _helmetPart.targetBone = "head";
            _helmetPart.offsetPosition = Vector3.zero;
            _helmetPart.offsetRotation = Vector3.zero;
            _helmetPart.attachPosition = Vector3.zero;
            _helmetPart.attachRotation = Quaternion.identity;
            _helmetPart.prefab = _helmetPrefab;
        }

        [TearDown]
        public void TearDown()
        {
            if (_character != null)
                Object.DestroyImmediate(_character);
            if (_swordPrefab != null)
                Object.DestroyImmediate(_swordPrefab);
            if (_helmetPrefab != null)
                Object.DestroyImmediate(_helmetPrefab);
            if (_swordPart != null)
                Object.DestroyImmediate(_swordPart);
            if (_helmetPart != null)
                Object.DestroyImmediate(_helmetPart);
        }

        static GameObject BuildPunchCharacter()
        {
            var top = new GameObject("Character");
            var root = new GameObject("root").transform;
            root.SetParent(top.transform, false);
            root.localPosition = Vector3.zero;

            var pelvis = new GameObject("pelvis").transform;
            pelvis.SetParent(root, false);
            pelvis.localPosition = new Vector3(0f, 0.95f, 0f);

            var chest = new GameObject("chest").transform;
            chest.SetParent(pelvis, false);
            chest.localPosition = new Vector3(0f, 0.3f, 0f);

            var head = new GameObject("head").transform;
            head.SetParent(chest, false);
            head.localPosition = new Vector3(0f, 0.31f, 0f);

            var shoulderR = new GameObject("shoulder_r").transform;
            shoulderR.SetParent(chest, false);
            shoulderR.localPosition = new Vector3(0.17f, 0.14f, 0f);

            var elbowR = new GameObject("elbow_r").transform;
            elbowR.SetParent(shoulderR, false);
            elbowR.localPosition = new Vector3(0.28f, 0f, 0f);

            var wristR = new GameObject("wrist_r").transform;
            wristR.SetParent(elbowR, false);
            wristR.localPosition = new Vector3(0.25f, 0f, 0f);

            var shoulderL = new GameObject("shoulder_l").transform;
            shoulderL.SetParent(chest, false);
            shoulderL.localPosition = new Vector3(-0.17f, 0.14f, 0f);

            var elbowL = new GameObject("elbow_l").transform;
            elbowL.SetParent(shoulderL, false);
            elbowL.localPosition = new Vector3(-0.28f, 0f, 0f);

            var wristL = new GameObject("wrist_l").transform;
            wristL.SetParent(elbowL, false);
            wristL.localPosition = new Vector3(-0.25f, 0f, 0f);

            return top;
        }

        [UnityTest]
        public IEnumerator BindPosePosition_MatchesExpectedWithinOneMillimeter()
        {
            yield return null;

            var swordInst = PartAttachment.Attach(_character, _swordPart);
            Assert.IsNotNull(swordInst);
            var helmetInst = PartAttachment.Attach(_character, _helmetPart);
            Assert.IsNotNull(helmetInst);

            Vector3 expectedSwordAttachWorld = new Vector3(0.802f, 1.39f, 0f);
            Transform swordAttachTransform = swordInst.transform.Find("attach");
            Assert.IsNotNull(swordAttachTransform);
            float swordDist = Vector3.Distance(swordAttachTransform.position, expectedSwordAttachWorld);
            Assert.LessOrEqual(swordDist, 0.001f, $"Sword attach world distance {swordDist * 1000f:F3} mm exceeds 1 mm");

            Vector3 expectedHelmetAttachWorld = new Vector3(0f, 1.56f, 0f);
            Transform helmetAttachTransform = helmetInst.transform.Find("attach");
            Assert.IsNotNull(helmetAttachTransform);
            float helmetDist = Vector3.Distance(helmetAttachTransform.position, expectedHelmetAttachWorld);
            Assert.LessOrEqual(helmetDist, 0.001f, $"Helmet attach world distance {helmetDist * 1000f:F3} mm exceeds 1 mm");
        }

        [UnityTest]
        public IEnumerator FollowsBone_LocalTransformRemainsConstantWhenBoneMoves()
        {
            yield return null;

            var swordInst = PartAttachment.Attach(_character, _swordPart);
            Transform wristR = PartAttachment.FindBone(_character.transform, "wrist_r");

            Vector3 initialLocalPos = swordInst.transform.localPosition;
            Quaternion initialLocalRot = swordInst.transform.localRotation;

            wristR.localPosition += new Vector3(0.05f, -0.02f, 0.1f);
            wristR.localRotation = Quaternion.Euler(30f, 45f, 15f);
            yield return null;

            Assert.AreEqual(initialLocalPos.x, swordInst.transform.localPosition.x, 1e-5f);
            Assert.AreEqual(initialLocalPos.y, swordInst.transform.localPosition.y, 1e-5f);
            Assert.AreEqual(initialLocalPos.z, swordInst.transform.localPosition.z, 1e-5f);
            Assert.AreEqual(initialLocalRot.x, swordInst.transform.localRotation.x, 1e-5f);
            Assert.AreEqual(initialLocalRot.y, swordInst.transform.localRotation.y, 1e-5f);
            Assert.AreEqual(initialLocalRot.z, swordInst.transform.localRotation.z, 1e-5f);
            Assert.AreEqual(initialLocalRot.w, swordInst.transform.localRotation.w, 1e-5f);

            wristR.localPosition += new Vector3(-0.03f, 0.08f, -0.05f);
            wristR.localRotation = Quaternion.Euler(-20f, 10f, 60f);
            yield return null;

            Assert.AreEqual(initialLocalPos.x, swordInst.transform.localPosition.x, 1e-5f);
            Assert.AreEqual(initialLocalPos.y, swordInst.transform.localPosition.y, 1e-5f);
            Assert.AreEqual(initialLocalPos.z, swordInst.transform.localPosition.z, 1e-5f);
            Assert.AreEqual(initialLocalRot.x, swordInst.transform.localRotation.x, 1e-5f);
            Assert.AreEqual(initialLocalRot.y, swordInst.transform.localRotation.y, 1e-5f);
            Assert.AreEqual(initialLocalRot.z, swordInst.transform.localRotation.z, 1e-5f);
            Assert.AreEqual(initialLocalRot.w, swordInst.transform.localRotation.w, 1e-5f);
        }

        [UnityTest]
        public IEnumerator MirrorLeftHand_GripAtFistCenterAndBladeForward()
        {
            yield return null;

            var swordLeft = PartAttachment.Attach(_character, _swordPart, "wrist_l", mirror: true);
            Assert.IsNotNull(swordLeft);

            Vector3 expectedLeftAttachWorld = new Vector3(-0.802f, 1.39f, 0f);
            Transform attachTransform = swordLeft.transform.Find("attach");
            Assert.IsNotNull(attachTransform);

            float dist = Vector3.Distance(attachTransform.position, expectedLeftAttachWorld);
            Assert.LessOrEqual(dist, 0.001f, $"Left sword attach distance {dist * 1000f:F3} mm exceeds 1 mm");

            Vector3 bladeDir = swordLeft.transform.TransformDirection(Vector3.up);
            Assert.Greater(Vector3.Dot(bladeDir, Vector3.forward), 0.99f, "Blade does not point forward (+Z)");
        }

        [UnityTest]
        public IEnumerator DetachAndMissingBone_RemovesObjectAndLogsError()
        {
            yield return null;

            var swordInst = PartAttachment.Attach(_character, _swordPart);
            Assert.IsNotNull(swordInst);

            bool detached = PartAttachment.Detach(_character, _swordPart);
            Assert.IsTrue(detached);
            Assert.IsNull(PartAttachment.FindBone(_character.transform, "sword.arming"));
            var remaining = _character.GetComponentsInChildren<AttachedPart>(true);
            Assert.AreEqual(0, remaining.Length);

            LogAssert.Expect(LogType.Error, new Regex(@"no_such_bone"));
            var failedInst = PartAttachment.Attach(_character, _swordPart, "no_such_bone");
            Assert.IsNull(failedInst);
        }

        [UnityTest]
        public IEnumerator MultiAxisEuler_SheathedSword_OrientationAndPositionMatchCharacterBuilder()
        {
            yield return null;

            var swordPrefab = new GameObject("sword.sheathed");
            var swordAttach = new GameObject("attach");
            swordAttach.transform.SetParent(swordPrefab.transform, false);
            swordAttach.transform.localPosition = new Vector3(0f, 0.091f, 0f);
            swordAttach.transform.localRotation = Quaternion.identity;

            var sheathedPart = ScriptableObject.CreateInstance<PartAsset>();
            sheathedPart.id = "sword.sheathed";
            sheathedPart.version = "1.0.0";
            sheathedPart.kind = "weapon.sword";
            sheathedPart.targetBone = "pelvis";
            sheathedPart.offsetPosition = new Vector3(-0.2f, 0.08f, 0f);
            sheathedPart.offsetRotation = new Vector3(200f, -90f, 0f);
            sheathedPart.attachPosition = new Vector3(0f, 0.091f, 0f);
            sheathedPart.attachRotation = Quaternion.identity;
            sheathedPart.prefab = swordPrefab;

            try
            {
                var inst = PartAttachment.Attach(_character, sheathedPart);
                Assert.IsNotNull(inst);

                var attachNode = inst.transform.Find("attach");
                Assert.IsNotNull(attachNode);

                // Pelvis bone is at (0, 0.95, 0). Attach node world pos = (-0.2, 1.03, 0).
                Vector3 expectedAttachWorld = new Vector3(-0.2f, 1.03f, 0f);
                float dist = Vector3.Distance(attachNode.position, expectedAttachWorld);
                Assert.LessOrEqual(dist, 0.001f, $"Attach node distance {dist * 1000f:F3} mm exceeds 1 mm");

                // Blade direction (+Y of part) matches Character Builder intrinsic XYZ rotation
                Vector3 worldBladeDir = inst.transform.TransformDirection(Vector3.up);
                Assert.AreEqual(0f, worldBladeDir.x, 1e-4f);
                Assert.AreEqual(-0.9396926f, worldBladeDir.y, 1e-4f);
                Assert.AreEqual(-0.3420201f, worldBladeDir.z, 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(swordPrefab);
                Object.DestroyImmediate(sheathedPart);
            }
        }
    }
}

// Tests for BoneChainComponent Unity lifecycle and presentation integration.
using NUnit.Framework;
using UnityEngine;

namespace Yu5h1Lib.Tests
{
    public class BoneChainComponentTests
    {
        [Test]
        public void BoneChainComponent_InitializesAndConfiguresNodes()
        {
            GameObject rootGo = new GameObject("CharacterRoot");
            GameObject headGo = new GameObject("Head");
            headGo.transform.SetParent(rootGo.transform, false);

            GameObject hair1 = new GameObject("Hair1");
            hair1.transform.SetParent(headGo.transform, false);
            hair1.transform.localPosition = new Vector3(0f, 0f, -0.1f);

            GameObject hair2 = new GameObject("Hair2");
            hair2.transform.SetParent(hair1.transform, false);
            hair2.transform.localPosition = new Vector3(0f, -0.15f, 0f);

            try
            {
                var component = headGo.AddComponent<BoneChainComponent>();
                component.ChainRoot = headGo.transform;
                component.Nodes.Add(new BoneChainNodeSetting { BoneTransform = hair1.transform, Stiffness = 60f, Damping = 5f, Radius = 0.03f });
                component.Nodes.Add(new BoneChainNodeSetting { BoneTransform = hair2.transform, Stiffness = 40f, Damping = 4f, Radius = 0.02f });

                component.InitializeChain();

                Assert.IsNotNull(component.CoreChain, "CoreChain must be instantiated.");
                Assert.AreEqual(2, component.CoreChain.Nodes, "CoreChain must have 2 nodes.");
                Assert.AreEqual(60f, component.CoreChain.GetNode(0).Stiffness);
                Assert.AreEqual(40f, component.CoreChain.GetNode(1).Stiffness);
            }
            finally
            {
                Object.DestroyImmediate(rootGo);
            }
        }

        [Test]
        public void BoneChainComponent_CollectsSphereAndCapsuleColliders()
        {
            GameObject charGo = new GameObject("Character");
            GameObject headGo = new GameObject("Head");
            headGo.transform.SetParent(charGo.transform, false);
            headGo.transform.position = new Vector3(0f, 1.6f, 0f);

            var headSphere = headGo.AddComponent<SphereCollider>();
            headSphere.center = Vector3.zero;
            headSphere.radius = 0.15f;

            GameObject thighGo = new GameObject("Thigh");
            thighGo.transform.SetParent(charGo.transform, false);
            thighGo.transform.position = new Vector3(0.1f, 0.8f, 0f);

            var thighCapsule = thighGo.AddComponent<CapsuleCollider>();
            thighCapsule.direction = 1; // Y-Axis
            thighCapsule.height = 0.5f;
            thighCapsule.radius = 0.08f;

            GameObject hairGo = new GameObject("HairRoot");
            hairGo.transform.SetParent(headGo.transform, false);
            GameObject hairTip = new GameObject("HairTip");
            hairTip.transform.SetParent(hairGo.transform, false);
            hairTip.transform.position = new Vector3(0f, 1.5f, -0.05f);

            try
            {
                var component = charGo.AddComponent<BoneChainComponent>();
                component.ChainRoot = headGo.transform;
                component.Nodes.Add(new BoneChainNodeSetting { BoneTransform = hairTip.transform, Stiffness = 50f, Damping = 5f, Radius = 0.03f });
                component.UnitySphereColliders.Add(headSphere);
                component.UnityCapsuleColliders.Add(thighCapsule);

                component.InitializeChain();
                Assert.IsNotNull(component.CoreChain);

                // Simulate one frame of LateUpdate
                charGo.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);

                // Both colliders must be registered into CoreChain
                Assert.AreEqual(2, component.CoreChain.Colliders.Count, "Both sphere and capsule must be collected.");
                Assert.AreEqual(BoneChainColliderType.Sphere, component.CoreChain.Colliders[0].Type);
                Assert.AreEqual(BoneChainColliderType.Capsule, component.CoreChain.Colliders[1].Type);
            }
            finally
            {
                Object.DestroyImmediate(charGo);
            }
        }
    }
}

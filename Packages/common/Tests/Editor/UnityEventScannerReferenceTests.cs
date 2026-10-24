using NUnit.Framework;
using UnityEngine;
using Yu5h1Lib.EditorExtension;

namespace Yu5h1Lib.Tests
{
    /// <summary>
    /// How the exports write objects Unity does not save (HideFlags.DontSaveInEditor and not an asset), such as the mesh
    /// TextMeshPro generates: a reference reads transient, built-in resources stay assets, and transient components do
    /// not count for a component's #n.
    /// </summary>
    public class UnityEventScannerReferenceTests
    {
        [Test]
        public void TransientObject_IsWrittenAsTransient()
        {
            var mesh = new Mesh { name = "Probe/Mesh", hideFlags = HideFlags.HideAndDontSave };
            try
            {
                Assert.AreEqual("transient: Probe\\/Mesh <Mesh>", UnityEventScanner.DescribeReference(mesh));
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void BuiltInResource_IsNotTransient()
        {
            var cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            Assert.IsNotNull(cube);
            StringAssert.DoesNotStartWith("transient:", UnityEventScanner.DescribeReference(cube));
        }

        [Test]
        public void TransientComponent_DoesNotCountForOrdinal()
        {
            var go = new GameObject(nameof(TransientComponent_DoesNotCountForOrdinal));
            try
            {
                go.AddComponent<BoxCollider>().hideFlags = HideFlags.HideAndDontSave;
                var saved = go.AddComponent<BoxCollider>();
                var transient = go.GetComponents<BoxCollider>()[0];
                StringAssert.EndsWith(" (BoxCollider)", UnityEventScanner.DescribePath(saved));
                StringAssert.StartsWith("transient: ", UnityEventScanner.DescribeReference(transient));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}

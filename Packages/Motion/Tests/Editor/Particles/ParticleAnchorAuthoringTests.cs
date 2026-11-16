using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Yu5h1Lib.EditorExtension;

namespace Yu5h1Lib.Tests
{
    /// <summary>
    ///   Anchor authoring tool (PEXT-CHECK-09 in ParticleExtensions.md): the picker's cache and
    ///   search discipline on a 50 × 50 cloth, binding and unbinding under Undo, local offsets, and
    ///   the source assigned when an anchor is added.
    /// </summary>
    public sealed class ParticleAnchorAuthoringTests
    {
        ParticleTestScope _scope;
        readonly List<GameObject> _objects = new List<GameObject>();

        [SetUp]
        public void SetUp() => _scope = new ParticleTestScope();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _objects)
                if (go != null)
                    Object.DestroyImmediate(go);
            _objects.Clear();
            _scope.Dispose();
        }

        GameObject NewObject(string name)
        {
            var go = new GameObject(name);
            _objects.Add(go);
            return go;
        }

        PhysicsParticle Cloth(int columns, int rows, int count = 1)
        {
            PhysicsParticle driver = _scope.Driver("Cloth", _scope.Grid(columns, rows, 0.1f), null, count);
            _objects.Add(driver.gameObject);
            return driver;
        }

        ParticleAnchor Anchor(PhysicsParticle source)
        {
            var anchor = source.gameObject.AddComponent<ParticleAnchor>();
            anchor.Source = source;
            return anchor;
        }

        static Vector2 Project(Vector3 world) => new Vector2(world.x * 100f, -world.y * 100f);

        [Test]
        public void MouseMove_DoesNotRebuild_AndRepaintDoesNotPick()
        {
            PhysicsParticle source = Cloth(50, 50);
            var picker = new ParticleAnchorPicker();
            Assert.IsTrue(picker.Ensure(source));
            Assert.AreEqual(2500, picker.Count);
            Assert.AreEqual(1, picker.RebuildCount);

            int projections = 0;
            System.Func<Vector3, Vector2> counting = world =>
            {
                projections++;
                return Project(world);
            };

            for (int i = 0; i < 200; i++)
            {
                Assert.IsTrue(picker.Ensure(source));
                picker.Process(EventType.MouseMove, new Vector2(i, i), counting);
            }
            Assert.AreEqual(1, picker.RebuildCount);
            Assert.AreEqual(200, picker.SearchCount);
            Assert.AreEqual(200 * 2500, projections);

            int hovered = picker.Hovered;
            projections = 0;
            for (int i = 0; i < 100; i++)
            {
                Assert.IsTrue(picker.Ensure(source));
                Assert.IsFalse(picker.Process(EventType.Repaint, new Vector2(0f, 0f), counting));
                Assert.IsFalse(picker.Process(EventType.Layout, new Vector2(0f, 0f), counting));
            }
            Assert.AreEqual(0, projections);
            Assert.AreEqual(200, picker.SearchCount);
            Assert.AreEqual(1, picker.RebuildCount);
            Assert.AreEqual(hovered, picker.Hovered);
        }

        [Test]
        public void Cache_RebuildsOnlyWhenLayoutCountOrTransformChanges()
        {
            PhysicsParticle source = Cloth(4, 3);
            var picker = new ParticleAnchorPicker();
            picker.Ensure(source);
            picker.Ensure(source);
            Assert.AreEqual(1, picker.RebuildCount);

            source.transform.position = new Vector3(1f, 2f, 3f);
            picker.Ensure(source);
            Assert.AreEqual(2, picker.RebuildCount);

            source.transform.rotation = Quaternion.Euler(0f, 30f, 0f);
            picker.Ensure(source);
            Assert.AreEqual(3, picker.RebuildCount);

            source.Count = 2;
            picker.Ensure(source);
            Assert.AreEqual(4, picker.RebuildCount);
            Assert.AreEqual(24, picker.Count);

            ((GridParticleLayout)source.Layout).Columns = 5;
            picker.Ensure(source);
            Assert.AreEqual(5, picker.RebuildCount);
            Assert.AreEqual(30, picker.Count);

            source.Layout = _scope.Grid(5, 3, 0.2f);
            picker.Ensure(source);
            Assert.AreEqual(6, picker.RebuildCount);

            picker.Ensure(source);
            Assert.AreEqual(6, picker.RebuildCount);
        }

        [Test]
        public void RestPositions_EqualTheDriverRestPositions_AcrossEntities()
        {
            PhysicsParticle source = Cloth(3, 2, 3);
            source.transform.SetPositionAndRotation(new Vector3(1f, 2f, 3f), Quaternion.Euler(10f, 40f, 5f));
            var picker = new ParticleAnchorPicker();
            Assert.IsTrue(picker.Ensure(source));
            Assert.AreEqual(source.FlatParticleCount, picker.Count);
            for (int i = 0; i < picker.Count; i++)
            {
                Assert.IsTrue(source.TryGetRestPosition(i, out Vector3 expected));
                Assert.AreEqual(0f, Vector3.Distance(expected, picker.RestPositions[i]), 1e-5f, $"particle {i}");
            }
        }

        [Test]
        public void GridLayout_DrawsRowAndColumnLines_OtherLayoutsDrawPoints()
        {
            var picker = new ParticleAnchorPicker();
            picker.Ensure(Cloth(50, 50));
            Assert.AreEqual(100, picker.Lines.Count);
            Assert.AreEqual(50, picker.Lines[0].Length);

            PhysicsParticle multiple = Cloth(4, 3, 2);
            picker.Ensure(multiple);
            Assert.AreEqual(2 * (3 + 4), picker.Lines.Count);

            PointParticleLayout points = _scope.Asset<PointParticleLayout>();
            points.Variants = new[]
            {
                new PointParticleLayout.Variant { positions = new[] { Vector3.zero, Vector3.right, Vector3.up } }
            };
            PhysicsParticle pointSource = _scope.Driver("Points", points, null, 1);
            _objects.Add(pointSource.gameObject);
            Assert.IsTrue(picker.Ensure(pointSource));
            Assert.AreEqual(3, picker.Count);
            Assert.AreEqual(0, picker.Lines.Count);
        }

        [Test]
        public void FindNearest_PicksTheNearestParticleInsideTheRadius()
        {
            PhysicsParticle source = Cloth(3, 3);
            var picker = new ParticleAnchorPicker();
            picker.Ensure(source);

            Vector2 target = Project(picker.RestPositions[4]);
            Assert.AreEqual(4, picker.FindNearest(target + new Vector2(3f, -2f), Project));
            Assert.AreEqual(-1, picker.FindNearest(target + new Vector2(0f, 500f), Project));

            Assert.IsTrue(picker.Process(EventType.MouseMove, Project(picker.RestPositions[8]), Project));
            Assert.AreEqual(8, picker.Hovered);
            Assert.IsFalse(picker.Process(EventType.MouseMove, Project(picker.RestPositions[8]), Project));
            Assert.AreEqual("(2, 2)", picker.Describe(8));
        }

        [Test]
        public void Toggle_BindsThenUnbinds_BothUndoable_AndOffsetMatchesRestPosition()
        {
            PhysicsParticle source = Cloth(50, 50);
            source.transform.SetPositionAndRotation(new Vector3(0.5f, 1f, -2f), Quaternion.Euler(0f, 25f, 10f));
            ParticleAnchor anchor = Anchor(source);
            Transform holder = NewObject("Holder").transform;
            holder.SetPositionAndRotation(new Vector3(3f, 1f, 0f), Quaternion.Euler(15f, 60f, 0f));
            holder.localScale = new Vector3(2f, 1f, 0.5f);

            var picker = new ParticleAnchorPicker();
            picker.Ensure(source);
            const int index = 1234;

            Undo.IncrementCurrentGroup();
            Assert.AreEqual(
                ParticleAnchorToggleResult.Bound,
                ParticleAnchorAuthoring.Toggle(anchor, holder, index, picker.RestPositions[index]));
            Assert.AreEqual(1, ParticleAnchorAuthoring.CountPoints(anchor));
            Assert.AreEqual(index, anchor.Bindings[0].points[0].index);
            Assert.AreEqual(holder, anchor.Bindings[0].target);

            Assert.IsTrue(source.TryGetRestPosition(index, out Vector3 rest));
            Vector3 expected = holder.InverseTransformPoint(rest);
            Assert.AreEqual(0f, Vector3.Distance(expected, anchor.Bindings[0].points[0].localOffset), 1e-5f);
            Assert.AreEqual(0f, Vector3.Distance(rest, holder.TransformPoint(anchor.Bindings[0].points[0].localOffset)), 1e-4f);

            Undo.IncrementCurrentGroup();
            Assert.AreEqual(
                ParticleAnchorToggleResult.Unbound,
                ParticleAnchorAuthoring.Toggle(anchor, holder, index, picker.RestPositions[index]));
            Assert.AreEqual(0, ParticleAnchorAuthoring.CountPoints(anchor));

            Undo.PerformUndo();
            Assert.AreEqual(1, ParticleAnchorAuthoring.CountPoints(anchor));
            Assert.AreEqual(index, anchor.Bindings[0].points[0].index);

            Undo.PerformUndo();
            Assert.AreEqual(0, ParticleAnchorAuthoring.CountPoints(anchor));
            Assert.AreEqual(0, anchor.Bindings.Count);

            Undo.PerformRedo();
            Assert.AreEqual(1, ParticleAnchorAuthoring.CountPoints(anchor));
        }

        [Test]
        public void Toggle_UnbindsFromWhicheverHolderHoldsIt_AndNeedsAHolderToBind()
        {
            PhysicsParticle source = Cloth(4, 4);
            ParticleAnchor anchor = Anchor(source);
            Transform first = NewObject("First").transform;
            Transform second = NewObject("Second").transform;
            var picker = new ParticleAnchorPicker();
            picker.Ensure(source);

            Assert.AreEqual(
                ParticleAnchorToggleResult.NoHolder,
                ParticleAnchorAuthoring.Toggle(anchor, null, 3, picker.RestPositions[3]));
            Assert.AreEqual(0, anchor.Bindings.Count);

            ParticleAnchorAuthoring.Toggle(anchor, first, 3, picker.RestPositions[3]);
            ParticleAnchorAuthoring.Toggle(anchor, first, 4, picker.RestPositions[4]);
            ParticleAnchorAuthoring.Toggle(anchor, second, 5, picker.RestPositions[5]);
            Assert.AreEqual(2, anchor.Bindings.Count);
            Assert.AreEqual(3, ParticleAnchorAuthoring.CountPoints(anchor));

            Assert.AreEqual(
                ParticleAnchorToggleResult.Unbound,
                ParticleAnchorAuthoring.Toggle(anchor, second, 3, picker.RestPositions[3]));
            Assert.AreEqual(1, anchor.Bindings[0].points.Count);
            Assert.AreEqual(4, anchor.Bindings[0].points[0].index);
        }

        [Test]
        public void RecaptureOffsets_UsesTheCurrentHolderPose_AndKeepsOutOfRangePoints()
        {
            PhysicsParticle source = Cloth(4, 4);
            ParticleAnchor anchor = Anchor(source);
            Transform holder = NewObject("Holder").transform;
            var picker = new ParticleAnchorPicker();
            picker.Ensure(source);
            ParticleAnchorAuthoring.Toggle(anchor, holder, 2, picker.RestPositions[2]);
            ParticleAnchorAuthoring.Toggle(anchor, holder, 9, picker.RestPositions[9]);
            anchor.Bindings[0].points.Add(new ParticleAnchor.Point { index = 999, localOffset = new Vector3(7f, 7f, 7f) });

            holder.SetPositionAndRotation(new Vector3(5f, -1f, 2f), Quaternion.Euler(0f, 90f, 0f));
            int changed = ParticleAnchorAuthoring.RecaptureOffsets(anchor, picker.RestPositions);

            Assert.AreEqual(2, changed);
            foreach (ParticleAnchor.Point point in anchor.Bindings[0].points)
            {
                if (point.index == 999)
                {
                    Assert.AreEqual(new Vector3(7f, 7f, 7f), point.localOffset);
                    continue;
                }
                Assert.IsTrue(source.TryGetRestPosition(point.index, out Vector3 rest));
                Assert.AreEqual(0f, Vector3.Distance(holder.InverseTransformPoint(rest), point.localOffset), 1e-5f);
            }
        }

        [Test]
        public void ResolveSource_TakesThePhysicsParticleOnTheObjectOrAParent()
        {
            PhysicsParticle source = Cloth(2, 2);
            var sameObject = source.gameObject.AddComponent<ParticleAnchor>();
            Assert.IsTrue(ParticleAnchorAuthoring.ResolveSource(sameObject));
            Assert.AreEqual(source, sameObject.Source);
            Assert.IsFalse(ParticleAnchorAuthoring.ResolveSource(sameObject));

            GameObject child = NewObject("Child");
            child.transform.SetParent(source.transform, false);
            var childAnchor = child.AddComponent<ParticleAnchor>();
            Assert.IsTrue(ParticleAnchorAuthoring.ResolveSource(childAnchor));
            Assert.AreEqual(source, childAnchor.Source);

            var lone = NewObject("Lone").AddComponent<ParticleAnchor>();
            Assert.IsFalse(ParticleAnchorAuthoring.ResolveSource(lone));
            Assert.IsNull(lone.Source);
        }
    }
}

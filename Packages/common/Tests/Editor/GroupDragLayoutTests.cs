using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Yu5h1Lib.Tests
{
    public class GroupDragLayoutTests
    {
        private static readonly Rect Area = new Rect(0, 0, 1000, 800);
        private static readonly IReadOnlyList<Rect> NoObstacles = new List<Rect>();
        private static readonly string[] Three = { "a", "b", "c" };

        private static GroupDragLayout Scattered(IReadOnlyList<Rect> obstacles = null)
        {
            var layout = new GroupDragLayout();
            layout.Resolve(Three, Area, obstacles ?? NoObstacles, compact: false);
            return layout;
        }

        [Test]
        public void EveryItemGetsAPositionInsideTheArea()
        {
            var layout = Scattered();
            Assert.AreEqual(3, layout.Positions.Count);
            foreach (var position in layout.Positions.Values)
                Assert.IsTrue(Area.Contains(position), $"{position} escaped {Area}");
        }

        [Test]
        public void ScatteredItemsDoNotOverlap()
        {
            var layout = Scattered();
            var placed = new List<Vector2>(layout.Positions.Values);
            for (int i = 0; i < placed.Count; i++)
                for (int j = i + 1; j < placed.Count; j++)
                    Assert.Greater(Vector2.Distance(placed[i], placed[j]), 30f);
        }

        [Test]
        public void ItemsKeepClearOfObstacles()
        {
            var obstacle = new Rect(400, 300, 200, 200);
            var layout = Scattered(new[] { obstacle });
            foreach (var position in layout.Positions.Values)
                Assert.IsFalse(obstacle.Contains(position), $"{position} sits on the obstacle");
        }

        [Test]
        public void CompactArrangementUsesTheSortedSize()
        {
            var layout = new GroupDragLayout();
            layout.Resolve(Three, Area, NoObstacles, compact: true);
            Assert.IsTrue(layout.Compact);
            Assert.AreEqual(44f, layout.Size, 1e-4f);
        }

        [Test]
        public void ConstructorSizesReachTheResolvedLayout()
        {
            var layout = new GroupDragLayout(scatterSize: 100, sortedSize: 80, sortedStride: 120);
            layout.Resolve(Three, Area, NoObstacles, compact: false);
            Assert.AreEqual(100f, layout.Size, 1e-4f);
            layout.Resolve(Three, Area, NoObstacles, compact: true);
            Assert.AreEqual(80f, layout.Size, 1e-4f);
        }

        [Test]
        public void DraggingMovesTheGroupAndLeavesItCoasting()
        {
            var layout = Scattered();
            var before = new Dictionary<string, Vector2>(layout.Positions);
            for (int i = 0; i < 10; i++) layout.DragGroup(Area.center, new Vector2(8, 0), 1f / 60, NoObstacles);

            Assert.IsTrue(layout.IsMoving, "a flick should leave momentum behind");
            Assert.Greater(layout.Positions["a"].x, before["a"].x);
        }

        [Test]
        public void CoastingDecaysToRest()
        {
            var layout = Scattered();
            for (int i = 0; i < 10; i++) layout.DragGroup(Area.center, new Vector2(8, 0), 1f / 60, NoObstacles);

            for (int i = 0; i < 600 && layout.IsMoving; i++) layout.AdvanceGroup(1f / 60, NoObstacles);
            Assert.IsFalse(layout.IsMoving, "the group must come to rest rather than drift forever");
        }

        [Test]
        public void CompactGroupsAreNotDraggable()
        {
            var layout = new GroupDragLayout();
            layout.Resolve(Three, Area, NoObstacles, compact: true);
            var before = new Dictionary<string, Vector2>(layout.Positions);
            layout.DragGroup(Area.center, new Vector2(50, 0), 1f / 60, NoObstacles);
            foreach (var pair in before)
                Assert.AreEqual(pair.Value, layout.Positions[pair.Key]);
        }

        [Test]
        public void DegenerateAreaResolvesToNothingRatherThanThrowing()
        {
            var layout = new GroupDragLayout();
            layout.Resolve(Three, new Rect(0, 0, 0, 800), NoObstacles, compact: false);
            Assert.AreEqual(0, layout.Positions.Count);

            layout.Resolve(Three, new Rect(0, 0, float.NaN, 800), NoObstacles, compact: false);
            Assert.AreEqual(0, layout.Positions.Count);
        }

        [Test]
        public void TryPlaceReturnsTheWantedSpotWhenItIsFree()
        {
            Assert.IsTrue(GroupDragLayout.TryPlace(new Vector2(500, 400), Area, 30, NoObstacles, out var result));
            Assert.AreEqual(new Vector2(500, 400), result);
        }

        [Test]
        public void TryPlaceFailsWhenTheAreaIsSmallerThanTheItem()
        {
            Assert.IsFalse(GroupDragLayout.TryPlace(Vector2.zero, new Rect(0, 0, 20, 20), 30, NoObstacles, out _));
        }

        [Test]
        public void SpreadWidensTheSeedRingForUncachedItems()
        {
            var narrow = new GroupDragLayout { Spread = .1f };
            narrow.Resolve(Three, Area, NoObstacles, compact: false, origin: Area.center);
            var wide = new GroupDragLayout { Spread = 4f };
            wide.Resolve(Three, Area, NoObstacles, compact: false, origin: Area.center);

            Assert.Greater(Vector2.Distance(wide.Positions["a"], Area.center),
                Vector2.Distance(narrow.Positions["a"], Area.center));
        }

        [Test]
        public void SpreadDoesNotMoveAnItemThatAlreadyHasACommittedPlacement()
        {
            var layout = Scattered();
            layout.Commit();
            var before = new Dictionary<string, Vector2>(layout.Positions);

            layout.Spread = 5f;
            layout.Resolve(Three, Area, NoObstacles, compact: false);

            foreach (var pair in before)
                Assert.AreEqual(pair.Value, layout.Positions[pair.Key]);
        }

        [Test]
        public void PullDrawsAnItemTowardTheTargetWhenWithinRadius()
        {
            var layout = Scattered();
            Vector2 current = layout.Positions["a"];
            Vector2 target = current + new Vector2(200, 0);

            bool pulled = layout.Pull("a", current, target, radius: 300, force: 5, dt: 1f / 60,
                Three, Area, NoObstacles);

            Assert.IsTrue(pulled);
            Assert.Greater(layout.Positions["a"].x, current.x);
            Assert.Less(layout.Positions["a"].x, target.x);
        }

        [Test]
        public void PullDoesNothingWhenTheItemIsBeyondTheRadius()
        {
            var layout = Scattered();
            Vector2 current = layout.Positions["a"];
            Vector2 target = current + new Vector2(400, 0);

            bool pulled = layout.Pull("a", current, target, radius: 50, force: 5, dt: 1f / 60,
                Three, Area, NoObstacles);

            Assert.IsFalse(pulled);
            Assert.AreEqual(current, layout.Positions["a"]);
        }

        [Test]
        public void PullDoesNothingOnACompactLayout()
        {
            var layout = new GroupDragLayout();
            layout.Resolve(Three, Area, NoObstacles, compact: true);
            Vector2 current = layout.Positions["a"];

            bool pulled = layout.Pull("a", current, Area.center, radius: 1000, force: 5, dt: 1f / 60,
                Three, Area, NoObstacles);

            Assert.IsFalse(pulled);
        }
    }
}

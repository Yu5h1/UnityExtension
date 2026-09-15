using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Yu5h1Lib.UIToolkit.Tests
{
    public class ElementTargetTests
    {
        private VisualElement element;
        private ElementTarget target;

        [SetUp]
        public void SetUp()
        {
            element = new VisualElement();
            target = new ElementTarget(element);
        }

        [Test]
        public void ItSatisfiesBothReactionContracts()
        {
            Assert.IsInstanceOf<Reaction.ISpatial>(target);
            Assert.IsInstanceOf<Reaction.IPivot>(target);
        }

        [Test]
        public void RectReachesTheElementAndReadsBack()
        {
            var rect = new Rect(12, 34, 56, 78);
            target.Rect = rect;

            Assert.AreEqual(rect, target.Rect);
            Assert.AreEqual(12f, element.style.left.value.value, 1e-4f);
            Assert.AreEqual(34f, element.style.top.value.value, 1e-4f);
            Assert.AreEqual(56f, element.style.width.value.value, 1e-4f);
            Assert.AreEqual(78f, element.style.height.value.value, 1e-4f);
        }

        [Test]
        public void RotationIsWrittenInDegrees()
        {
            target.Rotation = 30;
            Assert.AreEqual(30f, target.Rotation, 1e-4f);
            Assert.AreEqual(30f, element.style.rotate.value.angle.value, 1e-4f);
            Assert.AreEqual(AngleUnit.Degree, element.style.rotate.value.angle.unit);
        }

        [Test]
        public void ScaleIsWrittenPerAxisAndLeavesZAlone()
        {
            target.Scale = new Vector2(2, 3);
            Assert.AreEqual(new Vector2(2, 3), target.Scale);
            Assert.AreEqual(2f, element.style.scale.value.value.x, 1e-4f);
            Assert.AreEqual(3f, element.style.scale.value.value.y, 1e-4f);
            Assert.AreEqual(1f, element.style.scale.value.value.z, 1e-4f);
        }

        [Test]
        public void PivotIsConvertedFromNormalizedToPercent()
        {
            target.Pivot = new Vector2(0, 1);
            Assert.AreEqual(new Vector2(0, 1), target.Pivot);
            Assert.AreEqual(0f, element.style.transformOrigin.value.x.value, 1e-4f);
            Assert.AreEqual(100f, element.style.transformOrigin.value.y.value, 1e-4f);
            Assert.AreEqual(LengthUnit.Percent, element.style.transformOrigin.value.y.unit);
        }

        [Test]
        public void OpacityReachesTheElement()
        {
            target.Opacity = .25f;
            Assert.AreEqual(.25f, target.Opacity, 1e-4f);
            Assert.AreEqual(.25f, element.style.opacity.value, 1e-4f);
        }

        [Test]
        public void DefaultsAreTheRestingPose()
        {
            Assert.AreEqual(1f, target.Opacity);
            Assert.AreEqual(0f, target.Rotation);
            Assert.AreEqual(Vector2.one, target.Scale);
            Assert.AreEqual(new Vector2(.5f, .5f), target.Pivot);
        }

        [Test]
        public void RestorePutsBackAnUnsetPropertyRatherThanAnExplicitZero()
        {
            // Nothing was ever assigned, so every property must go back to Null - not to 0, which would
            // override whatever USS has to say about it.
            Assert.AreEqual(StyleKeyword.Null, element.style.rotate.keyword);

            target.Rotation = 45;
            target.Scale = new Vector2(2, 2);
            target.Opacity = .5f;
            target.Rect = new Rect(1, 2, 3, 4);
            target.Restore();

            Assert.AreEqual(StyleKeyword.Null, element.style.rotate.keyword);
            Assert.AreEqual(StyleKeyword.Null, element.style.scale.keyword);
            Assert.AreEqual(StyleKeyword.Null, element.style.opacity.keyword);
            Assert.AreEqual(StyleKeyword.Null, element.style.left.keyword);
        }

        [Test]
        public void RestoreKeepsAStyleThatWasAlreadySet()
        {
            element.style.opacity = .8f;
            var fresh = new ElementTarget(element);
            fresh.Opacity = .1f;
            fresh.Restore();
            Assert.AreEqual(.8f, element.style.opacity.value, 1e-4f);
        }

        [Test]
        public void AnElementOutsideAPanelIsNotAlive()
        {
            Assert.IsFalse(target.IsAlive, "a detached element must be droppable by whatever drives it");
        }

        [Test]
        public void ANullElementIsInertRatherThanFatal()
        {
            var orphan = new ElementTarget(null);
            Assert.IsFalse(orphan.IsAlive);
            Assert.DoesNotThrow(() =>
            {
                orphan.Rect = new Rect(1, 2, 3, 4);
                orphan.Rotation = 10;
                orphan.Scale = Vector2.one * 2;
                orphan.Pivot = Vector2.zero;
                orphan.Opacity = .5f;
                orphan.Restore();
            });
            Assert.AreEqual(new Rect(1, 2, 3, 4), orphan.Rect, "values are still tracked");
        }
    }
}

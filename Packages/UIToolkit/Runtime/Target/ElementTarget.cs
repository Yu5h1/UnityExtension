using UnityEngine;
using UnityEngine.UIElements;

namespace Yu5h1Lib.UIToolkit
{
    /// <summary>
    /// Drives a <see cref="VisualElement"/> from <c>com.yu5h1.animation</c>'s reactions.
    /// <para>
    /// This is the only place in the stack that knows UI Toolkit exists. Everything upstream - springs,
    /// swings, wobbles, travel - works in plain numbers, which is what lets the same motion drive a
    /// Transform, a test double, or nothing at all.
    /// </para>
    /// <para>
    /// Written values are remembered rather than read back, because UI Toolkit style getters return an
    /// unresolved <c>StyleLength</c> that is not the number that was set. The resting style is snapshotted
    /// verbatim at construction so <see cref="Restore"/> can put back an <em>unset</em> property rather than
    /// an explicit zero - the two render differently once USS has an opinion.
    /// </para>
    /// </summary>
    public sealed class ElementTarget : Reaction.ISpatial, Reaction.IPivot
    {
        private readonly VisualElement element;
        private readonly StyleRotate restRotate;
        private readonly StyleScale restScale;
        private readonly StyleTransformOrigin restOrigin;
        private readonly StyleFloat restOpacity;
        private readonly StyleLength restLeft, restTop, restWidth, restHeight;

        private Rect rect;
        private float opacity = 1, rotation;
        private Vector2 scale = Vector2.one, pivot = new Vector2(.5f, .5f);

        public ElementTarget(VisualElement element)
        {
            this.element = element;
            if (element == null) return;

            restRotate = element.style.rotate;
            restScale = element.style.scale;
            restOrigin = element.style.transformOrigin;
            restOpacity = element.style.opacity;
            restLeft = element.style.left;
            restTop = element.style.top;
            restWidth = element.style.width;
            restHeight = element.style.height;
            rect = element.layout;
        }

        /// <summary>The element being driven.</summary>
        public VisualElement Element => element;

        /// <summary>False once the element is gone or has left the panel, so a reaction can drop it.</summary>
        public bool IsAlive => element != null && element.panel != null;

        /// <summary>Absolute position and size. Only meaningful while the element is absolutely positioned.</summary>
        public Rect Rect
        {
            get => rect;
            set
            {
                rect = value;
                if (element == null) return;
                element.style.left = value.x;
                element.style.top = value.y;
                element.style.width = value.width;
                element.style.height = value.height;
            }
        }

        /// <inheritdoc/>
        public float Opacity
        {
            get => opacity;
            set { opacity = value; if (element != null) element.style.opacity = value; }
        }

        /// <inheritdoc/>
        public float Rotation
        {
            get => rotation;
            set { rotation = value; if (element != null) element.style.rotate = new Rotate(Angle.Degrees(value)); }
        }

        /// <inheritdoc/>
        public Vector2 Scale
        {
            get => scale;
            set { scale = value; if (element != null) element.style.scale = new Scale(new Vector3(value.x, value.y, 1)); }
        }

        /// <summary>Normalized pivot; converted to the percentage transform-origin UI Toolkit expects.</summary>
        public Vector2 Pivot
        {
            get => pivot;
            set
            {
                pivot = value;
                if (element == null) return;
                element.style.transformOrigin =
                    new TransformOrigin(Length.Percent(value.x * 100), Length.Percent(value.y * 100));
            }
        }

        /// <summary>
        /// Puts every property this target touched back exactly as it was found, including properties that
        /// were never set in the first place. Call it when a reaction finishes and the element should look
        /// untouched again.
        /// </summary>
        public void Restore()
        {
            if (element == null) return;
            element.style.rotate = restRotate;
            element.style.scale = restScale;
            element.style.transformOrigin = restOrigin;
            element.style.opacity = restOpacity;
            element.style.left = restLeft;
            element.style.top = restTop;
            element.style.width = restWidth;
            element.style.height = restHeight;
        }
    }
}

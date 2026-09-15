using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Yu5h1Lib.UIToolkit
{
    /// <summary>
    /// A panel that rises from the bottom edge, hosts whatever content it is given, and can be dragged
    /// taller, shorter or away.
    /// <para>
    /// Drag and animation share one bounded progress value, which is what makes the sheet interruptible:
    /// grabbing it mid-animation continues from where it visibly is instead of snapping. Releasing projects
    /// the throw a little way forward, so a quick flick down dismisses even when the sheet has barely moved.
    /// </para>
    /// <para>
    /// <b>Required structure.</b> The root handed in must contain elements named <c>sheet-layer</c>,
    /// <c>sheet</c>, <c>handle</c>, <c>scrim</c>, <c>sheet-title</c>, <c>sheet-kicker</c>,
    /// <c>sheet-scroll</c> and <c>sheet-close</c>, and the stylesheet must define a <c>hidden</c> class that
    /// takes the layer out of the layout. A missing name throws on construction rather than failing later.
    /// The handle is made focusable here, so the caller's UXML need not remember to.
    /// </para>
    /// </summary>
    public sealed class BottomSheet
    {
        private const float AnimationDuration = .35f;
        private const float MinimumHeightFraction = .25f;

        private readonly VisualElement layer, sheet, handle;
        private readonly Button scrim;
        private readonly Label title, kicker;

        private float progress = 1, target = 1, animationStart = 1, animationTime = AnimationDuration;
        private int pointer = -1;
        private float origin, lastY, lastTime, velocity, downY;
        private bool moved;
        private float heightFraction, savedHeightFraction, dragHeightFraction, availableHeight = 600;
        private VisualElement returnFocus;

        /// <summary>The scrolling region the content lives in.</summary>
        public ScrollView Scroll { get; }

        /// <summary>Where callers put their content.</summary>
        public VisualElement Content => Scroll.contentContainer;

        /// <summary>True from <see cref="Open"/> until the closing animation has finished.</summary>
        public bool IsOpen => !layer.ClassListContains("hidden");

        /// <param name="root">Element containing the required structure; see the type summary.</param>
        /// <param name="initialHeightFraction">Share of the available height the sheet opens at.</param>
        public BottomSheet(VisualElement root, float initialHeightFraction = .72f)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            heightFraction = savedHeightFraction = Mathf.Clamp(initialHeightFraction, MinimumHeightFraction, 1);

            layer = Require<VisualElement>(root, "sheet-layer");
            sheet = Require<VisualElement>(root, "sheet");
            handle = Require<VisualElement>(root, "handle");
            scrim = Require<Button>(root, "scrim");
            title = Require<Label>(root, "sheet-title");
            kicker = Require<Label>(root, "sheet-kicker");
            Scroll = Require<ScrollView>(root, "sheet-scroll");
            var close = Require<Button>(root, "sheet-close");

            // The control both focuses the handle and listens for keys on it, so it makes sure the handle can
            // actually take focus rather than depending on the caller's UXML to have said so. Focus() on a
            // non-focusable element does nothing at all, silently.
            handle.focusable = true;

            scrim.clicked += Close;
            close.clicked += Close;
            handle.RegisterCallback<PointerDownEvent>(Down);
            handle.RegisterCallback<PointerMoveEvent>(Move);
            handle.RegisterCallback<PointerUpEvent>(e => End(e.pointerId, false));
            handle.RegisterCallback<PointerCancelEvent>(e => End(e.pointerId, true));
            // Losing capture is a cancel too: without this the sheet would stay stuck to a pointer it no
            // longer receives events from.
            handle.RegisterCallback<PointerCaptureOutEvent>(e => End(e.pointerId, true));
            handle.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode != KeyCode.Return && e.keyCode != KeyCode.Space) return;
                Close();
                e.StopPropagation();
            });
            layer.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode != KeyCode.Escape) return;
                Close();
                e.StopPropagation();
            });
        }

        private static T Require<T>(VisualElement root, string name) where T : VisualElement
        {
            var found = root.Q<T>(name);
            if (found == null)
                throw new ArgumentException(
                    $"BottomSheet needs a {typeof(T).Name} named '{name}' inside the root it is given.", nameof(root));
            return found;
        }

        /// <summary>Clears the content, shows the sheet and moves focus into it.</summary>
        public void Open(string heading, string context)
        {
            if (!IsOpen) returnFocus = layer.panel?.focusController.focusedElement as VisualElement;
            CancelDrag();
            heightFraction = savedHeightFraction;
            title.text = heading;
            kicker.text = context;
            Content.Clear();
            Scroll.scrollOffset = Vector2.zero;
            layer.RemoveFromClassList("hidden");
            AnimateTo(0);
            Render();
            handle.Focus();
        }

        /// <summary>Starts the sheet travelling away. It is not hidden until the animation finishes.</summary>
        public void Close()
        {
            if (!IsOpen) return;
            CancelDrag();
            AnimateTo(1);
        }

        /// <summary>
        /// Advances the sheet. While a drag is in progress the pointer owns the position and the animation
        /// stands aside.
        /// </summary>
        public void Tick(float deltaTime, bool reduceMotion)
        {
            if (!IsOpen) return;
            if (pointer < 0)
            {
                animationTime = reduceMotion
                    ? AnimationDuration
                    : Mathf.Min(AnimationDuration, animationTime + Mathf.Max(0, deltaTime));
                float t = animationTime / AnimationDuration;
                float eased = 1 - Mathf.Pow(1 - t, 3);
                progress = Mathf.Lerp(animationStart, target, eased);
            }
            Render();

            if (pointer >= 0 || target != 1 || animationTime < AnimationDuration) return;

            layer.AddToClassList("hidden");
            // Deferred: returning focus while the layer is still being hidden this frame lands nowhere.
            var focus = returnFocus;
            layer.schedule.Execute(() => focus?.Focus());
            returnFocus = null;
        }

        /// <summary>Tells the sheet how much room it has, usually the panel height.</summary>
        public void SetAvailableHeight(float height)
        {
            availableHeight = Mathf.Max(1, height);
            Render();
        }

        private void AnimateTo(float destination)
        {
            animationStart = progress;
            target = destination;
            animationTime = Mathf.Approximately(progress, target) ? AnimationDuration : 0;
        }

        private void Render()
        {
            sheet.style.height = availableHeight * heightFraction;
            float visibility = 1 - Mathf.Clamp01(progress);
            sheet.style.translate = new Translate(0, new Length((1 - visibility) * 100, LengthUnit.Percent));
            scrim.style.opacity = visibility;
        }

        private void Down(PointerDownEvent e)
        {
            if (pointer >= 0 || e.button != 0) return;
            pointer = e.pointerId;
            downY = lastY = e.position.y;
            dragHeightFraction = heightFraction;
            origin = heightFraction * availableHeight;
            progress = 0;
            lastTime = Time.unscaledTime;
            velocity = 0;
            moved = false;
            handle.CapturePointer(pointer);
            e.StopPropagation();
        }

        private void Move(PointerMoveEvent e)
        {
            if (e.pointerId != pointer) return;
            float now = Time.unscaledTime;
            velocity = (e.position.y - lastY) / Mathf.Max(.008f, now - lastTime);
            lastY = e.position.y;
            lastTime = now;
            moved |= Mathf.Abs(lastY - downY) > 5;
            heightFraction = Mathf.Clamp((origin + downY - lastY) / availableHeight, MinimumHeightFraction, 1);
            Render();
            e.StopPropagation();
        }

        private void End(int id, bool cancelled)
        {
            if (id != pointer) return;

            // Stale velocity is worse than none: a pointer that paused before releasing was not thrown.
            float speed = Time.unscaledTime - lastTime < .1f ? velocity : 0;
            float projected = (origin + downY - lastY - speed * .12f) / availableHeight;

            // A tap on the handle dismisses; so does a drag projected below the minimum height.
            bool dismiss = !cancelled && (!moved || projected < MinimumHeightFraction);
            if (cancelled) heightFraction = dragHeightFraction;
            else if (!dismiss) savedHeightFraction = heightFraction;

            CancelDrag();
            AnimateTo(dismiss ? 1 : 0);
        }

        private void CancelDrag()
        {
            int id = pointer;
            pointer = -1;
            if (id >= 0 && handle.HasPointerCapture(id)) handle.ReleasePointer(id);
        }
    }
}

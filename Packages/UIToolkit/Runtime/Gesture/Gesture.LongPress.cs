using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Yu5h1Lib.UIToolkit
{
    public static partial class Gesture
    {
        /// <summary>
        /// Press and hold to trigger, and swallow the click that would otherwise follow.
        /// <para>
        /// Swallowing is the part that is easy to miss and impossible to ignore once missed: without it a
        /// hold that opens a picker also fires the button's normal click on release, so the user gets both
        /// actions from one press.
        /// </para>
        /// <para>
        /// The caller decides what holding means. This reports when the hold succeeds and whether the
        /// release belongs to it; opening a panel, capturing the pointer for a drag, or anything else is
        /// the caller's own work in <see cref="Triggered"/>.
        /// </para>
        /// </summary>
        public sealed class LongPress : PointerManipulator
        {
            private IVisualElementScheduledItem pending;
            private int pointer = -1;
            private Vector2 origin;
            private bool moved;

            /// <summary>How long the press must be held, in milliseconds.</summary>
            public long Duration { get; set; } = 400;

            /// <summary>
            /// How far the pointer may wander before the hold is abandoned, in pixels. Zero or less never
            /// abandons - use that when the hold opens something the same drag then operates, so sliding
            /// straight from the hold into a selection is one continuous gesture.
            /// </summary>
            public float MoveTolerance { get; set; } = 6;

            /// <summary>Raised once per press, when it has been held long enough.</summary>
            public event Action Triggered;

            /// <summary>
            /// True from the moment <see cref="Triggered"/> fires until the pointer is released. While it is
            /// true the caller should stop the release event, so the press does not also read as a click.
            /// </summary>
            public bool IsHolding { get; private set; }

            public LongPress() => activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });

            protected override void RegisterCallbacksOnTarget()
            {
                target.RegisterCallback<PointerDownEvent>(OnDown, TrickleDown.TrickleDown);
                target.RegisterCallback<PointerMoveEvent>(OnMove, TrickleDown.TrickleDown);
                target.RegisterCallback<PointerUpEvent>(OnUp, TrickleDown.TrickleDown);
                target.RegisterCallback<PointerCancelEvent>(OnCancel, TrickleDown.TrickleDown);
                target.RegisterCallback<DetachFromPanelEvent>(OnDetach);
            }

            protected override void UnregisterCallbacksFromTarget()
            {
                target.UnregisterCallback<PointerDownEvent>(OnDown, TrickleDown.TrickleDown);
                target.UnregisterCallback<PointerMoveEvent>(OnMove, TrickleDown.TrickleDown);
                target.UnregisterCallback<PointerUpEvent>(OnUp, TrickleDown.TrickleDown);
                target.UnregisterCallback<PointerCancelEvent>(OnCancel, TrickleDown.TrickleDown);
                target.UnregisterCallback<DetachFromPanelEvent>(OnDetach);
                Abandon();
            }

            private void OnDown(PointerDownEvent e)
            {
                if (pointer >= 0 || !CanStartManipulation(e)) return;
                pointer = e.pointerId;
                origin = e.position;
                moved = false;
                IsHolding = false;
                pending = target.schedule.Execute(Fire).StartingIn(Duration);
            }

            private void OnMove(PointerMoveEvent e)
            {
                if (e.pointerId != pointer || IsHolding || MoveTolerance <= 0) return;
                Vector2 position = e.position;
                moved |= (position - origin).sqrMagnitude > MoveTolerance * MoveTolerance;
                if (moved) Abandon();
            }

            private void OnUp(PointerUpEvent e)
            {
                if (e.pointerId != pointer) return;
                pending?.Pause();
                pending = null;
                pointer = -1;
                // IsHolding survives this call on purpose: the caller reads it in its own PointerUp handler
                // to decide whether to swallow the click. Release() clears it once that is done.
            }

            private void OnCancel(PointerCancelEvent e)
            {
                if (e.pointerId != pointer) return;
                Abandon();
            }

            private void OnDetach(DetachFromPanelEvent e) => Abandon();

            private void Fire()
            {
                if (pointer < 0) return;
                IsHolding = true;
                Triggered?.Invoke();
            }

            /// <summary>
            /// Marks the hold as dealt with. Call it after swallowing the release, so the next press starts
            /// clean.
            /// </summary>
            public void Release() => IsHolding = false;

            /// <summary>Drops a press in progress without firing, and forgets any hold already reported.</summary>
            public void Abandon()
            {
                pending?.Pause();
                pending = null;
                pointer = -1;
                IsHolding = false;
            }
        }
    }
}

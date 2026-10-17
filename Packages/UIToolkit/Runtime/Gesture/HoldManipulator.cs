using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Yu5h1Lib.UIToolkit
{
    /// <summary>
    /// Cross-platform Hold &amp; Hold-and-Slide interaction manipulator implementing
    /// Yu5h1Lib.UX <c>Foundation/Input</c> (3.0.1):
    /// <list type="bullet">
    /// <item><description><c>INPUT-PRESS-TAP</c>: Short press inside half-open target bounds <c>[0, width) x [0, height)</c>.</description></item>
    /// <item><description><c>INPUT-PRESS-HOLD</c>: Hold for <see cref="HoldDurationMs"/> (default 400ms) within <see cref="DragThreshold"/> (default 6px local logical px).</description></item>
    /// <item><description><c>INPUT-PRESS-SUPPRESS</c>: Suppresses native pointer click/activate after hold, drag past threshold, outside release, or interruption.</description></item>
    /// <item><description><c>INPUT-CHOICE-HOLD</c>: Captures pointer on hold and streams <see cref="HoldMoved"/> / <see cref="HoldEnded"/> for slide-to-select.</description></item>
    /// <item><description><c>INPUT-CANCEL</c> &amp; <c>INPUT-LIFECYCLE</c>: Cancels cleanly on second pointer, Escape, capture loss, pointer cancel, or panel detach.</description></item>
    /// </list>
    /// </summary>
    public class HoldManipulator : PointerManipulator
    {
        public const long DefaultHoldDurationMs = 400;
        public const float DefaultDragThreshold = 6f;

        private IVisualElementScheduledItem holdSchedule;
        private int activePointerId = -1;
        private Vector2 originLocalPos;
        private Vector2 lastWorldPos;
        private long pressStartTimeMs;
        private long virtualClockMs;
        private bool suppressNextClick;
        private bool pendingCanHold;

        /// <summary>Hold duration in milliseconds before <see cref="HoldTriggered"/> fires. Default: 400ms.</summary>
        public long HoldDurationMs { get; set; } = DefaultHoldDurationMs;

        /// <summary>Maximum movement in target local logical px before a pending hold is canceled. Default: 6px.</summary>
        public float DragThreshold { get; set; } = DefaultDragThreshold;

        /// <summary>Whether to capture the pointer on <see cref="Manipulator.target"/> when hold triggers.</summary>
        public bool CaptureOnHold { get; set; } = true;

        /// <summary>Whether to suppress native pointer click/activate after hold, cancel, or outside release.</summary>
        public bool SuppressClickOnHold { get; set; } = true;

        /// <summary>Optional predicate evaluated on primary pointer down to decide whether hold timer should start.</summary>
        public Func<PointerDownEvent, bool> CanStartHold { get; set; }

        /// <summary>True while a primary pointer press is active (pending or holding).</summary>
        public bool IsPressed => activePointerId >= 0;

        /// <summary>True from the moment <see cref="HoldTriggered"/> fires until release or cancel.</summary>
        public bool IsHolding { get; private set; }

        /// <summary>The active pointer ID, or -1 when idle.</summary>
        public int ActivePointerId => activePointerId;

        /// <summary>Raised when a valid primary press begins.</summary>
        public event Action<PointerDownEvent> HoldStarted;

        /// <summary>Raised once per press when held for <see cref="HoldDurationMs"/> without exceeding <see cref="DragThreshold"/>.</summary>
        public event Action<Vector2> HoldTriggered;

        /// <summary>Raised on pointer move while <see cref="IsHolding"/> is true (Hold-and-Slide).</summary>
        public event Action<Vector2> HoldMoved;

        /// <summary>Raised on pointer up when an active hold finishes.</summary>
        public event Action<Vector2> HoldEnded;

        /// <summary>Raised when a pending press or active hold is interrupted/canceled.</summary>
        public event Action HoldCanceled;

        /// <summary>Raised when a short press releases inside target bounds without triggering hold or canceling.</summary>
        public event Action TapTriggered;

        public HoldManipulator()
        {
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
            target.RegisterCallback<PointerCancelEvent>(OnPointerCancel, TrickleDown.TrickleDown);
            target.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut, TrickleDown.TrickleDown);
            target.RegisterCallback<ClickEvent>(OnClick, TrickleDown.TrickleDown);
            target.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            target.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
            target.UnregisterCallback<PointerCancelEvent>(OnPointerCancel, TrickleDown.TrickleDown);
            target.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut, TrickleDown.TrickleDown);
            target.UnregisterCallback<ClickEvent>(OnClick, TrickleDown.TrickleDown);
            target.UnregisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            target.UnregisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            CancelInternal(emitCancel: activePointerId >= 0, suppressClick: false);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (target == null || !target.enabledInHierarchy) return;

            // INPUT-CANCEL: Second pointer down while a gesture is in progress cancels the active gesture.
            if (activePointerId >= 0)
            {
                if (evt.pointerId != activePointerId)
                {
                    CancelInternal(emitCancel: true, suppressClick: true);
                }
                return;
            }

            if (evt.button != 0) return;

            suppressNextClick = false;
            activePointerId = evt.pointerId;
            IsHolding = false;
            lastWorldPos = evt.position;
            originLocalPos = WorldToLocalSafe(evt.position);
            pressStartTimeMs = virtualClockMs;

            HoldStarted?.Invoke(evt);

            holdSchedule?.Pause();
            holdSchedule = null;

            bool allowHold = CanStartHold == null || CanStartHold(evt);
            pendingCanHold = allowHold;
            if (allowHold && HoldDurationMs >= 0)
            {
                holdSchedule = target.schedule.Execute(TriggerHold).StartingIn(HoldDurationMs);
            }
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (evt.pointerId != activePointerId) return;

            lastWorldPos = evt.position;

            if (!IsHolding)
            {
                if (DragThreshold >= 0f)
                {
                    Vector2 currentLocal = WorldToLocalSafe(evt.position);
                    float dist = Vector2.Distance(currentLocal, originLocalPos);
                    // INPUT-PRESS-HOLD / INPUT-DRAG-START: strictly greater than DragThreshold cancels hold.
                    if (dist > DragThreshold)
                    {
                        CancelInternal(emitCancel: true, suppressClick: true);
                    }
                }
            }
            else
            {
                HoldMoved?.Invoke(evt.position);
                if (SuppressClickOnHold)
                {
                    evt.StopImmediatePropagation();
                }
            }
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerId != activePointerId) return;

            lastWorldPos = evt.position;
            holdSchedule?.Pause();
            holdSchedule = null;

            int releasedPointerId = activePointerId;
            bool wasHolding = IsHolding;

            activePointerId = -1;
            IsHolding = false;

            if (wasHolding)
            {
                suppressNextClick = SuppressClickOnHold;
                if (SuppressClickOnHold)
                {
                    evt.StopImmediatePropagation();
                }

                HoldEnded?.Invoke(evt.position);

                if (target != null && target.HasPointerCapture(releasedPointerId))
                {
                    target.ReleasePointer(releasedPointerId);
                }
            }
            else
            {
                if (target != null && target.HasPointerCapture(releasedPointerId))
                {
                    target.ReleasePointer(releasedPointerId);
                }

                if (IsInsideTargetHalfOpen(evt.position))
                {
                    TapTriggered?.Invoke();
                }
                else
                {
                    // Released outside target bounds: silent, suppress native click if captured.
                    suppressNextClick = SuppressClickOnHold;
                    if (SuppressClickOnHold)
                    {
                        evt.StopImmediatePropagation();
                    }
                }
            }
        }

        private void OnPointerCancel(PointerCancelEvent evt)
        {
            if (evt.pointerId != activePointerId) return;
            CancelInternal(emitCancel: true, suppressClick: true);
        }

        private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            if (activePointerId >= 0 && !IsHolding)
            {
                CancelInternal(emitCancel: true, suppressClick: true);
            }
        }

        private void OnClick(ClickEvent evt)
        {
            if (suppressNextClick && SuppressClickOnHold)
            {
                suppressNextClick = false;
                evt.StopImmediatePropagation();
            }
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape && activePointerId >= 0)
            {
                CancelInternal(emitCancel: true, suppressClick: true);
            }
            else if (evt.keyCode == KeyCode.Return && evt.shiftKey && activePointerId < 0 && target != null && target.enabledInHierarchy)
            {
                // INPUT-KEY-COMMAND: Shift+Enter -> hold
                HoldTriggered?.Invoke(target.worldBound.center);
            }
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            CancelInternal(emitCancel: activePointerId >= 0, suppressClick: false);
        }

        private void TriggerHold()
        {
            if (activePointerId < 0 || IsHolding || target == null || !target.enabledInHierarchy) return;

            holdSchedule?.Pause();
            holdSchedule = null;
            IsHolding = true;
            suppressNextClick = SuppressClickOnHold;

            if (CaptureOnHold && target.panel != null)
            {
                target.CapturePointer(activePointerId);
            }

            HoldTriggered?.Invoke(lastWorldPos);
        }

        /// <summary>
        /// Programmatically cancels any active press or hold gesture.
        /// </summary>
        public void Cancel()
        {
            CancelInternal(emitCancel: activePointerId >= 0, suppressClick: activePointerId >= 0);
        }

        private void CancelInternal(bool emitCancel, bool suppressClick)
        {
            holdSchedule?.Pause();
            holdSchedule = null;

            int pid = activePointerId;
            bool hadActive = activePointerId >= 0 || IsHolding;
            activePointerId = -1;
            IsHolding = false;

            if (suppressClick && SuppressClickOnHold)
            {
                suppressNextClick = true;
            }

            if (pid >= 0 && target != null && target.HasPointerCapture(pid))
            {
                target.ReleasePointer(pid);
            }

            if (emitCancel && hadActive)
            {
                HoldCanceled?.Invoke();
            }
        }

        /// <summary>
        /// Advances virtual clock for deterministic testing when panel scheduler is not running frames.
        /// Fires <see cref="HoldTriggered"/> if a press is pending and <paramref name="timestampMs"/> - pressStartTimeMs &gt;= <see cref="HoldDurationMs"/>.
        /// </summary>
        public void AdvanceClockTo(long timestampMs)
        {
            virtualClockMs = Math.Max(virtualClockMs, timestampMs);
            if (activePointerId >= 0 && !IsHolding && pendingCanHold && HoldDurationMs >= 0)
            {
                if (virtualClockMs - pressStartTimeMs >= HoldDurationMs)
                {
                    TriggerHold();
                }
            }
        }

        /// <summary>
        /// Deterministic simulation helper for unit and neutrality case tests.
        /// </summary>
        public void SimulatePointerDown(int pointerId, int button, Vector2 localOrWorldPos, long timestampMs, bool canHold = true)
        {
            AdvanceClockTo(timestampMs);
            if (target != null && !target.enabledInHierarchy) return;

            if (activePointerId >= 0)
            {
                if (pointerId != activePointerId)
                {
                    CancelInternal(emitCancel: true, suppressClick: true);
                }
                return;
            }

            if (button != 0) return;

            suppressNextClick = false;
            activePointerId = pointerId;
            IsHolding = false;
            lastWorldPos = localOrWorldPos;
            originLocalPos = WorldToLocalSafe(localOrWorldPos);
            pressStartTimeMs = virtualClockMs;
            pendingCanHold = canHold && (CanStartHold == null || CanStartHold(null));

            HoldStarted?.Invoke(null);

            holdSchedule?.Pause();
            holdSchedule = null;
            if (pendingCanHold && HoldDurationMs == 0)
            {
                TriggerHold();
            }
            else if (pendingCanHold && target != null)
            {
                holdSchedule = target.schedule.Execute(TriggerHold).StartingIn(HoldDurationMs);
            }
        }

        /// <summary>
        /// Deterministic simulation helper for pointer move.
        /// </summary>
        public void SimulatePointerMove(int pointerId, Vector2 localOrWorldPos, long timestampMs)
        {
            AdvanceClockTo(timestampMs);
            if (pointerId != activePointerId) return;

            lastWorldPos = localOrWorldPos;
            if (!IsHolding)
            {
                if (DragThreshold >= 0f)
                {
                    Vector2 currentLocal = WorldToLocalSafe(localOrWorldPos);
                    if (Vector2.Distance(currentLocal, originLocalPos) > DragThreshold)
                    {
                        CancelInternal(emitCancel: true, suppressClick: true);
                    }
                }
            }
            else
            {
                HoldMoved?.Invoke(localOrWorldPos);
            }
        }

        /// <summary>
        /// Deterministic simulation helper for pointer up.
        /// </summary>
        public void SimulatePointerUp(int pointerId, Vector2 localOrWorldPos, long timestampMs, Rect? explicitTargetRect = null)
        {
            AdvanceClockTo(timestampMs);
            if (pointerId != activePointerId) return;

            lastWorldPos = localOrWorldPos;
            holdSchedule?.Pause();
            holdSchedule = null;

            int releasedPointerId = activePointerId;
            bool wasHolding = IsHolding;

            activePointerId = -1;
            IsHolding = false;

            if (target != null && target.HasPointerCapture(releasedPointerId))
            {
                target.ReleasePointer(releasedPointerId);
            }

            if (wasHolding)
            {
                suppressNextClick = SuppressClickOnHold;
                HoldEnded?.Invoke(localOrWorldPos);
            }
            else
            {
                bool inside = explicitTargetRect.HasValue
                    ? IsInsideHalfOpenRect(explicitTargetRect.Value, localOrWorldPos)
                    : IsInsideTargetHalfOpen(localOrWorldPos);

                if (inside)
                {
                    TapTriggered?.Invoke();
                }
                else
                {
                    suppressNextClick = SuppressClickOnHold;
                }
            }
        }

        private Vector2 WorldToLocalSafe(Vector2 worldPos)
        {
            if (target == null) return worldPos;
            Vector2 local = target.WorldToLocal(worldPos);
            if (float.IsNaN(local.x) || float.IsNaN(local.y))
            {
                return worldPos;
            }
            return local;
        }

        private bool IsInsideTargetHalfOpen(Vector2 worldPos)
        {
            if (target == null) return false;

            Rect wb = target.worldBound;
            if (!float.IsNaN(wb.width) && wb.width > 0f && !float.IsNaN(wb.height) && wb.height > 0f)
            {
                return IsInsideHalfOpenRect(wb, worldPos);
            }

            float w = target.resolvedStyle.width;
            float h = target.resolvedStyle.height;
            if (float.IsNaN(w) || w <= 0f) w = target.style.width.value.value;
            if (float.IsNaN(h) || h <= 0f) h = target.style.height.value.value;

            if (w > 0f && h > 0f)
            {
                Vector2 local = WorldToLocalSafe(worldPos);
                return local.x >= 0f && local.x < w && local.y >= 0f && local.y < h;
            }

            return true;
        }

        /// <summary>
        /// Half-open rectangle hit test per UX <c>INPUT-PRESS-TAP</c> (1.3.0+):
        /// left and top edges inclusive, right and bottom edges exclusive.
        /// </summary>
        public static bool IsInsideHalfOpenRect(Rect rect, Vector2 point)
        {
            return point.x >= rect.xMin && point.x < rect.xMax &&
                   point.y >= rect.yMin && point.y < rect.yMax;
        }
    }
}

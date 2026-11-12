
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
    /// <summary>
    /// A vertical ScrollRect with bottom alignment and an optional follow policy.
    /// Its content uses a VerticalLayoutGroup. Give direct items stable, unique names and call
    /// RefreshContent after changing their layout or replacing them to preserve the reading position.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FollowingScrollView : ScrollRect
    {
        public const float DefaultResumeTolerance = 8f;

        private readonly List<(string Key, float Top, float Bottom)> itemBounds = new();
        private PointerEventData drag;
        private bool scrollbarHeld;
        private bool userScrollPending;
        private Vector2 viewportSize;

        public bool Following { get; private set; } = true;
        public bool BottomAnchor { get; set; } = true;
        public float ResumeTolerance { get; set; } = DefaultResumeTolerance;
        public float ContentSize { get; private set; }
        public float MaxOffset => viewport == null ? 0f : Mathf.Max(0f, ContentSize - viewport.rect.height);
        public float Offset => content == null ? 0f : Mathf.Clamp(content.anchoredPosition.y, 0f, MaxOffset);
        public bool UserScrolling => drag != null || scrollbarHeld || userScrollPending;

        protected override void OnEnable()
        {
            horizontal = false;
            vertical = true;
            movementType = MovementType.Clamped;
            verticalScrollbarVisibility = ScrollbarVisibility.AutoHide;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            drag = null;
            scrollbarHeld = userScrollPending = false;
            base.OnDisable();
        }

        public void RequestFollow()
        {
            if (drag != null)
            {
                base.OnEndDrag(drag);
                drag = null;
            }
            Following = true;
            userScrollPending = false;
            StopMovement();
            SetOffset(MaxOffset);
        }

        public void StopFollowing() => Following = false;

        public void ScrollToOffset(float offset)
        {
            StopMovement();
            SetOffset(offset);
        }

        /// <summary>Rebuilds native layout and restores the first visible item's prior screen position.</summary>
        public void RefreshContent()
        {
            if (content == null || viewport == null)
                return;

            float oldOffset = Mathf.Max(0f, content.anchoredPosition.y);
            string key = null;
            float relativeTop = 0f;
            if (!Following)
            {
                foreach (var item in itemBounds)
                {
                    if (item.Bottom <= oldOffset + 0.001f)
                        continue;
                    key = item.Key;
                    relativeTop = item.Top - oldOffset;
                    break;
                }
            }

            viewportSize = viewport.rect.size;
            if (content.TryGetComponent(out VerticalLayoutGroup layout))
                layout.childAlignment = BottomAnchor ? TextAnchor.LowerLeft : TextAnchor.UpperLeft;
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            ContentSize = LayoutUtility.GetPreferredHeight(content);
            float height = Mathf.Max(ContentSize, viewportSize.y);
            if (!Mathf.Approximately(content.rect.height, height))
            {
                content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            }

            itemBounds.Clear();
            float offset = oldOffset;
            foreach (RectTransform child in content)
            {
                if (!child.gameObject.activeInHierarchy)
                    continue;
                float top = content.rect.yMax - child.localPosition.y - child.rect.yMax;
                itemBounds.Add((child.name, top, top + child.rect.height));
                if (child.name == key)
                    offset = top - relativeTop;
            }
            SetOffset(Following ? MaxOffset : offset);
            if (drag != null && !Mathf.Approximately(oldOffset, Offset))
                base.OnBeginDrag(drag);
            if (verticalScrollbar != null)
                verticalScrollbar.gameObject.SetActive(MaxOffset > 0f);
        }

        private void SetOffset(float offset)
        {
            if (content != null)
                SetContentAnchoredPosition(new Vector2(content.anchoredPosition.x, Mathf.Clamp(offset, 0f, MaxOffset)));
        }

        private bool StartUserScroll()
        {
            if (MaxOffset <= 0f)
                return false;
            Following = false;
            userScrollPending = true;
            return true;
        }

        private void EndUserScroll()
        {
            userScrollPending = false;
            if (MaxOffset > 0f && MaxOffset - Offset <= ResumeTolerance)
                Following = true;
        }

        public override void OnScroll(PointerEventData eventData)
        {
            if (eventData.scrollDelta.sqrMagnitude == 0f || !StartUserScroll())
                return;
            base.OnScroll(eventData);
            if (drag == null && !scrollbarHeld)
                EndUserScroll();
        }

        public override void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || drag != null || !StartUserScroll())
                return;
            drag = eventData;
            base.OnBeginDrag(eventData);
        }

        public override void OnDrag(PointerEventData eventData)
        {
            if (drag != null && drag.pointerId == eventData.pointerId)
                base.OnDrag(eventData);
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            if (drag == null || drag.pointerId != eventData.pointerId)
                return;
            base.OnEndDrag(eventData);
            drag = null;
            if (!inertia && !scrollbarHeld)
                EndUserScroll();
        }

        internal void BeginScrollbarScroll()
        {
            scrollbarHeld = StartUserScroll();
            if (scrollbarHeld)
                StopMovement();
        }

        internal void EndScrollbarScroll()
        {
            if (!scrollbarHeld)
                return;
            scrollbarHeld = false;
            if (drag == null)
                EndUserScroll();
        }

        protected override void LateUpdate()
        {
            if (viewport != null && viewport.rect.size != viewportSize)
                RefreshContent();

            base.LateUpdate();
            if (userScrollPending && drag == null && !scrollbarHeld && velocity.sqrMagnitude < 1f)
                EndUserScroll();
        }
    }
}

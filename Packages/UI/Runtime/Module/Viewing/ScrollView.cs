
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UI;
using Yu5h1Lib.Recycling;

namespace Yu5h1Lib.UI
{
    public enum ContentUpdateBehavior
    {
        FollowLatest = 0,
        PreservePosition = 1,
    }

    /// <summary>A native ScrollRect with content update policies and optional recycling of fixed-size items.</summary>
    [DisallowMultipleComponent]
    [MovedFrom(true, "Yu5h1Lib.UI", "Yu5h1Lib.UI", "FollowingScrollView")]
    public sealed class ScrollView : ScrollRect
    {
        public const float DefaultResumeTolerance = 8f;

        [SerializeField] private ContentUpdateBehavior contentUpdateBehavior;
        [SerializeField] private bool loop;
        private readonly List<(string Key, float Start, float End)> itemBounds = new();
        private readonly Dictionary<int, RectTransform> visibleItems = new();
        private readonly List<int> retiredItems = new();
        private Recycler<RectTransform> recycler;
        private RectTransform itemPrefab;
        private Action<RectTransform, int> bindItem;
        private int itemCount;
        private float itemSize;
        private bool looping;
        private bool appliedLoop;
        private ContentUpdateBehavior appliedUpdateBehavior;
        private bool syncingScrollbars;
        private MovementType finiteMovement;
        private PointerEventData drag;
        private bool scrollbarHeld;
        private bool userScrollPending;
        private Vector2 viewportSize;

        private bool following = true;
        public bool Following => following && CanFollow;
        public ContentUpdateBehavior ContentUpdateBehavior
        {
            get => contentUpdateBehavior;
            set
            {
                if (contentUpdateBehavior == value)
                    return;
                contentUpdateBehavior = value;
                following = CanFollow;
                RefreshContent();
            }
        }

        /// <summary>Wraps a virtualized list along its single enabled axis when the content overflows.</summary>
        public bool Loop
        {
            get => loop;
            set
            {
                if (loop == value)
                    return;
                loop = value;
                RefreshContent();
            }
        }

        private bool CanFollow => !looping && contentUpdateBehavior == ContentUpdateBehavior.FollowLatest;
        private bool IsHorizontal => horizontal && !vertical;
        private float ViewSize => viewport == null ? 0f : (IsHorizontal ? viewport.rect.width : viewport.rect.height);
        private float RawOffset => content == null ? 0f : (IsHorizontal ? -content.anchoredPosition.x : content.anchoredPosition.y);
        public bool BottomAnchor { get; set; } = true;
        public float ResumeTolerance { get; set; } = DefaultResumeTolerance;
        public float ContentSize { get; private set; }
        public float MaxOffset => looping ? ContentSize : Mathf.Max(0f, ContentSize - ViewSize);
        public float Offset => looping ? Mathf.Repeat(RawOffset, ContentSize) : Mathf.Clamp(RawOffset, 0f, MaxOffset);
        public int CreatedItemCount { get; private set; }
        public bool UserScrolling => drag != null || scrollbarHeld || userScrollPending;

        protected override void OnDisable()
        {
            drag = null;
            scrollbarHeld = userScrollPending = false;
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            foreach (var item in visibleItems.Values)
                DestroyItem(item);
            visibleItems.Clear();
            recycler?.Clear();
            base.OnDestroy();
        }

        /// <summary>Supplies a fixed-size, single-axis list. The view owns the items under an empty Content rect.</summary>
        public void SetItems(RectTransform prefab, int count, float size, Action<RectTransform, int> bind)
        {
            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));
            if (bind == null)
                throw new ArgumentNullException(nameof(bind));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (size <= 0f || float.IsNaN(size) || float.IsInfinity(size) || float.IsInfinity(size * count))
                throw new ArgumentOutOfRangeException(nameof(size));
            if (content == null || viewport == null || horizontal == vertical)
                throw new InvalidOperationException("Assign a viewport, content and one scroll axis before setting items.");
            if (content.childCount != visibleItems.Count || content.GetComponent<LayoutGroup>() != null)
                throw new InvalidOperationException("Virtual items require an empty content rect without a layout group.");

            ReleaseItems();
            if (recycler == null || itemPrefab != prefab)
            {
                recycler?.Clear();
                itemPrefab = prefab;
                recycler = new Recycler<RectTransform>(prefab, CreateItem, DestroyItem);
                CreatedItemCount = 0;
            }
            itemCount = count;
            itemSize = size;
            bindItem = bind;
            RefreshContent();
        }

        public void RequestFollow()
        {
            if (!CanFollow)
                return;
            if (drag != null)
            {
                base.OnEndDrag(drag);
                drag = null;
            }
            following = true;
            userScrollPending = false;
            StopMovement();
            SetOffset(MaxOffset);
        }

        public void StopFollowing() => following = false;

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
            appliedLoop = loop;
            if (appliedUpdateBehavior != contentUpdateBehavior)
            {
                appliedUpdateBehavior = contentUpdateBehavior;
                following = CanFollow;
            }

            if (recycler != null)
            {
                RefreshVirtualContent();
                return;
            }

            float oldOffset = Mathf.Max(0f, RawOffset);
            string key = null;
            float relativeTop = 0f;
            if (!Following)
            {
                foreach (var item in itemBounds)
                {
                    if (item.End <= oldOffset + 0.001f)
                        continue;
                    key = item.Key;
                    relativeTop = item.Start - oldOffset;
                    break;
                }
            }

            viewportSize = viewport.rect.size;
            var axis = IsHorizontal ? RectTransform.Axis.Horizontal : RectTransform.Axis.Vertical;
            if (content.TryGetComponent(out HorizontalOrVerticalLayoutGroup layout))
            {
                if (!IsHorizontal)
                    layout.childAlignment = BottomAnchor ? TextAnchor.LowerLeft : TextAnchor.UpperLeft;
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                ContentSize = LayoutUtility.GetPreferredSize(content, (int)axis);
                float size = Mathf.Max(ContentSize, ViewSize);
                if (!Mathf.Approximately(content.rect.size[(int)axis], size))
                {
                    content.SetSizeWithCurrentAnchors(axis, size);
                    LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                }
            }
            else
                ContentSize = content.rect.size[(int)axis];

            itemBounds.Clear();
            float offset = oldOffset;
            foreach (RectTransform child in content)
            {
                if (!child.gameObject.activeInHierarchy)
                    continue;
                float top = IsHorizontal
                    ? child.localPosition.x + child.rect.xMin - content.rect.xMin
                    : content.rect.yMax - child.localPosition.y - child.rect.yMax;
                itemBounds.Add((child.name, top, top + child.rect.size[(int)axis]));
                if (child.name == key)
                    offset = top - relativeTop;
            }
            SetOffset(Following ? MaxOffset : offset);
            if (drag != null && !Mathf.Approximately(oldOffset, Offset))
                base.OnBeginDrag(drag);
            UpdateBarVisibility();
        }

        private void RefreshVirtualContent()
        {
            viewportSize = viewport.rect.size;
            var position = Offset;
            ContentSize = itemCount * itemSize;
            bool nextLoop = Loop && itemCount > 1 && ContentSize > ViewSize && ViewSize > 0f;
            if (nextLoop != looping)
            {
                ReleaseItems();
                if (nextLoop)
                {
                    finiteMovement = movementType;
                    movementType = MovementType.Unrestricted;
                    following = false;
                }
                else
                    movementType = finiteMovement;
                looping = nextLoop;
            }

            recycler.MaxAvailable = Mathf.Min(itemCount, Mathf.CeilToInt(ViewSize / itemSize) + 3);
            content.anchorMin = content.anchorMax = content.pivot = Vector2.up;
            content.sizeDelta = IsHorizontal
                ? new Vector2(Mathf.Max(ContentSize, ViewSize), viewportSize.y)
                : new Vector2(viewportSize.x, Mathf.Max(ContentSize, ViewSize));
            foreach (var item in visibleItems.Values)
                ResizeItem(item);
            SetOffset(Following ? MaxOffset : position);
            if (drag != null)
                base.OnBeginDrag(drag);
            UpdateBarVisibility();
            RefreshVisibleItems();
        }

        private void UpdateBarVisibility()
        {
            var bar = IsHorizontal ? horizontalScrollbar : verticalScrollbar;
            if (bar != null)
            {
                if (recycler != null && bar is ScrollViewScrollbar overlay)
                    overlay.UseEdgeReveal(this);
                bar.gameObject.SetActive(MaxOffset > 0f);
            }
        }

        private RectTransform CreateItem()
        {
            var item = Instantiate(itemPrefab);
            CreatedItemCount++;
            return item;
        }

        private static void DestroyItem(RectTransform item)
        {
            if (item == null)
                return;
            if (Application.isPlaying)
                Destroy(item.gameObject);
            else
                DestroyImmediate(item.gameObject);
        }

        private void ReturnItem(RectTransform item)
        {
            if (item != null)
            {
                item.gameObject.SetActive(false);
                item.SetParent(transform, false);
            }
            recycler.Return(item);
        }

        private void ReleaseItems()
        {
            foreach (var item in visibleItems.Values)
                ReturnItem(item);
            visibleItems.Clear();
        }

        private void RefreshVisibleItems()
        {
            if (recycler == null || !isActiveAndEnabled || ViewSize <= 0f || itemCount == 0)
                return;
            int first = Mathf.FloorToInt(RawOffset / itemSize) - 1;
            int end = Mathf.CeilToInt((RawOffset + ViewSize) / itemSize) + 1;
            if (!looping)
            {
                first = Mathf.Clamp(first, 0, itemCount);
                end = Mathf.Clamp(end, 0, itemCount);
            }
            foreach (var entry in visibleItems)
            {
                if (entry.Key < first || entry.Key >= end)
                    retiredItems.Add(entry.Key);
            }
            foreach (int index in retiredItems)
            {
                ReturnItem(visibleItems[index]);
                visibleItems.Remove(index);
            }
            retiredItems.Clear();

            for (int index = first; index < end; index++)
            {
                if (visibleItems.ContainsKey(index))
                    continue;
                var item = recycler.Get();
                visibleItems.Add(index, item);
                item.SetParent(content, false);
                item.anchorMin = item.anchorMax = item.pivot = Vector2.up;
                ResizeItem(item);
                item.anchoredPosition = IsHorizontal ? new Vector2(index * itemSize, 0f) : new Vector2(0f, -index * itemSize);
                int dataIndex = index % itemCount;
                bindItem(item, dataIndex < 0 ? dataIndex + itemCount : dataIndex);
                item.gameObject.SetActive(true);
            }
        }

        private void ResizeItem(RectTransform item) =>
            item.sizeDelta = IsHorizontal ? new Vector2(itemSize, viewportSize.y) : new Vector2(viewportSize.x, itemSize);

        private void SetOffset(float offset)
        {
            if (content != null)
            {
                offset = looping ? Mathf.Repeat(offset, ContentSize) : Mathf.Clamp(offset, 0f, MaxOffset);
                var position = content.anchoredPosition;
                if (IsHorizontal)
                    position.x = -offset;
                else
                    position.y = offset;
                SetContentAnchoredPosition(position);
            }
        }

        protected override void SetContentAnchoredPosition(Vector2 position)
        {
            base.SetContentAnchoredPosition(position);
            if (looping && Mathf.Abs(RawOffset) >= 8f * ContentSize)
            {
                var before = content.anchoredPosition;
                if (IsHorizontal)
                    position.x = -Offset;
                else
                    position.y = Offset;
                base.SetContentAnchoredPosition(position);
                m_ContentStartPosition += content.anchoredPosition - before;
                UpdatePrevData();
            }
            RefreshVisibleItems();
        }

        protected override void SetNormalizedPosition(float value, int axis)
        {
            if (!looping)
            {
                base.SetNormalizedPosition(value, axis);
                RefreshVisibleItems();
                return;
            }
            if (syncingScrollbars || CanvasUpdateRegistry.IsRebuildingLayout() || axis != (IsHorizontal ? 0 : 1))
                return;
            float progress = IsHorizontal ? value : 1f - value;
            ScrollToOffset(Mathf.Min(Mathf.Clamp01(progress) * ContentSize, ContentSize - Mathf.Min(.001f, itemSize / 2f)));
        }

        public override void Rebuild(CanvasUpdate executing)
        {
            syncingScrollbars = true;
            try { base.Rebuild(executing); }
            finally { syncingScrollbars = false; }
            if (executing == CanvasUpdate.PostLayout)
                SyncLoopScrollbar();
        }

        private void SyncLoopScrollbar()
        {
            var bar = IsHorizontal ? horizontalScrollbar : verticalScrollbar;
            if (!looping || bar == null)
                return;
            bar.size = Mathf.Clamp01(ViewSize / ContentSize);
            float progress = Offset / ContentSize;
            bar.SetValueWithoutNotify(IsHorizontal ? progress : 1f - progress);
        }

        private bool StartUserScroll()
        {
            if (MaxOffset <= 0f)
                return false;
            following = false;
            userScrollPending = true;
            return true;
        }

        private void EndUserScroll()
        {
            userScrollPending = false;
            if (CanFollow && MaxOffset > 0f && MaxOffset - Offset <= ResumeTolerance)
                following = true;
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
            if (viewport != null && (viewport.rect.size != viewportSize || appliedLoop != loop ||
                appliedUpdateBehavior != contentUpdateBehavior))
                RefreshContent();
            else if (recycler != null && visibleItems.Count == 0)
                RefreshVisibleItems();

            syncingScrollbars = true;
            try { base.LateUpdate(); }
            finally { syncingScrollbars = false; }
            SyncLoopScrollbar();
            if (userScrollPending && drag == null && !scrollbarHeld && velocity.sqrMagnitude < 1f)
                EndUserScroll();
        }
    }
}

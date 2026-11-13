
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
    /// <summary>Reports user scrollbar gestures without treating programmatic value changes as input.</summary>
    [MovedFrom(true, "Yu5h1Lib.UI", "Yu5h1Lib.UI", "FollowingScrollViewScrollbar")]
    public sealed class ScrollViewScrollbar : Scrollbar
    {
        [SerializeField] private ScrollView scrollView;
        private CanvasGroup edgeOverlay;
        private bool pointerNear;
        private bool held;
        private bool selected;

        public ScrollView ScrollView
        {
            get => scrollView;
            set => scrollView = value;
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && IsInteractable())
            {
                held = true;
                RefreshOverlay();
                scrollView?.BeginScrollbarScroll();
            }
            base.OnPointerDown(eventData);
        }

        public override void OnDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && IsInteractable())
                scrollView?.BeginScrollbarScroll();
            base.OnDrag(eventData);
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                held = false;
                RefreshOverlay();
                scrollView?.EndScrollbarScroll();
            }
        }

        internal void UseEdgeReveal(ScrollView owner)
        {
            scrollView = owner;
            if (edgeOverlay == null && !TryGetComponent(out edgeOverlay))
                edgeOverlay = gameObject.AddComponent<CanvasGroup>();
            RefreshOverlay();
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            pointerNear = true;
            RefreshOverlay();
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            pointerNear = false;
            RefreshOverlay();
        }

        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
            selected = true;
            RefreshOverlay();
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            base.OnDeselect(eventData);
            selected = false;
            RefreshOverlay();
        }

        private void RefreshOverlay()
        {
            if (edgeOverlay != null)
                edgeOverlay.alpha = pointerNear || held || selected ? 1f : 0f;
        }

        public override void OnMove(AxisEventData eventData)
        {
            float before = value;
            base.OnMove(eventData);
            if (!Mathf.Approximately(value, before) && scrollView != null)
            {
                scrollView.BeginScrollbarScroll();
                scrollView.EndScrollbarScroll();
            }
        }

        protected override void OnDisable()
        {
            held = pointerNear = selected = false;
            RefreshOverlay();
            scrollView?.EndScrollbarScroll();
            base.OnDisable();
        }
    }
}

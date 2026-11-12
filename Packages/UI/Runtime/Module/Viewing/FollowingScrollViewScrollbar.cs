
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
    /// <summary>Reports user scrollbar gestures without treating programmatic value changes as input.</summary>
    public sealed class FollowingScrollViewScrollbar : Scrollbar
    {
        [SerializeField] private FollowingScrollView scrollView;

        public FollowingScrollView ScrollView
        {
            get => scrollView;
            set => scrollView = value;
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && IsInteractable())
                scrollView?.BeginScrollbarScroll();
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
                scrollView?.EndScrollbarScroll();
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
            scrollView?.EndScrollbarScroll();
            base.OnDisable();
        }
    }
}

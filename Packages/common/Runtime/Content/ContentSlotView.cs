using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib
{
    /// <summary>
    /// Turns a <see cref="ContentSlot"/>'s state into scene events, so each piece of the indicator and the placeholder is
    /// an ordinary scene object switched in the Inspector: GameObject.SetActive for the bool events, a text component's
    /// SetValue for <c>percent</c> (TextAddon formats it, for example "Loading… {0}%"). This component holds no text.
    /// The owner of the slot calls <see cref="Bind"/>.
    /// <code>
    /// indicatorVisible    any of the three below
    ///   percentVisible + percent(int, rounded down) | indeterminateVisible | preparingVisible
    /// placeholderVisible  the place shows the placeholder: waiting for the selection, or as the fallback
    ///   waitingVisible    waiting: the placeholder's progress (the indicator follows the same load then)
    ///   fallbackVisible   the selection failed and the placeholder stands as the fallback
    /// noticeVisible       the non-blocking failure notice
    /// </code>
    /// A UI Toolkit panel subscribes to <see cref="ContentSlot.changed"/> and reads the slot directly instead.
    /// </summary>
    public class ContentSlotView : MonoBehaviour
    {
        [SerializeField] private UnityEvent<bool> _indicatorVisible = new UnityEvent<bool>();
        [SerializeField] private UnityEvent<bool> _percentVisible = new UnityEvent<bool>();
        [SerializeField] private UnityEvent<int> _percent = new UnityEvent<int>();
        [SerializeField] private UnityEvent<bool> _indeterminateVisible = new UnityEvent<bool>();
        [SerializeField] private UnityEvent<bool> _preparingVisible = new UnityEvent<bool>();
        [SerializeField] private UnityEvent<bool> _placeholderVisible = new UnityEvent<bool>();
        [SerializeField] private UnityEvent<bool> _waitingVisible = new UnityEvent<bool>();
        [SerializeField] private UnityEvent<bool> _fallbackVisible = new UnityEvent<bool>();
        [SerializeField] private UnityEvent<bool> _noticeVisible = new UnityEvent<bool>();

        private ContentSlot _slot;

        /// <summary>The events, for a view built in code; a scene wires them in the Inspector.</summary>
        public UnityEvent<bool> indicatorVisible => _indicatorVisible;
        public UnityEvent<bool> percentVisible => _percentVisible;
        public UnityEvent<int> percent => _percent;
        public UnityEvent<bool> indeterminateVisible => _indeterminateVisible;
        public UnityEvent<bool> preparingVisible => _preparingVisible;
        public UnityEvent<bool> placeholderVisible => _placeholderVisible;
        public UnityEvent<bool> waitingVisible => _waitingVisible;
        public UnityEvent<bool> fallbackVisible => _fallbackVisible;
        public UnityEvent<bool> noticeVisible => _noticeVisible;

        public void Bind(ContentSlot slot)
        {
            if (_slot != null)
                _slot.changed -= Present;
            _slot = slot;
            if (_slot == null)
                return;
            _slot.changed += Present;
            Present();
        }

        private void OnDestroy() => Bind(null);

        private void Present()
        {
            var indicator = _slot.indicator;
            _indicatorVisible.Invoke(indicator != ContentSlot.Indicator.Hidden);
            _percentVisible.Invoke(indicator == ContentSlot.Indicator.Percent);
            if (indicator == ContentSlot.Indicator.Percent)
                _percent.Invoke(_slot.percent);
            _indeterminateVisible.Invoke(indicator == ContentSlot.Indicator.Indeterminate);
            _preparingVisible.Invoke(indicator == ContentSlot.Indicator.Preparing);

            var shown = _slot.shown;
            _placeholderVisible.Invoke(shown == ContentSlot.Placeholder || shown == ContentSlot.Fallback);
            _waitingVisible.Invoke(shown == ContentSlot.Placeholder);
            _fallbackVisible.Invoke(shown == ContentSlot.Fallback);
            _noticeVisible.Invoke(_slot.noticeVisible);
        }
    }
}

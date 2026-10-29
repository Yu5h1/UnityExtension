using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib
{
    /// <summary>
    /// Turns a <see cref="ContentSlot"/> into scene events, so the progress indicator, the placeholder and the failure
    /// notice are ordinary scene objects wired in the Inspector (for example GameObject.SetActive and TMP_Text.text).
    /// The owner of the slot calls <see cref="Bind"/>. What each event carries is <see cref="ContentSlotPresentation"/>'s.
    /// </summary>
    public class ContentSlotView : MonoBehaviour
    {
        [SerializeField] private string _percentFormat = ContentSlotPresentation.DefaultPercentFormat;
        [SerializeField] private string _indeterminateText = ContentSlotPresentation.DefaultIndeterminateText;
        [SerializeField] private string _preparingText = ContentSlotPresentation.DefaultPreparingText;

        [SerializeField] private UnityEvent<bool> _indicatorVisible = new UnityEvent<bool>();
        [SerializeField] private UnityEvent<string> _progressText = new UnityEvent<string>();
        [SerializeField] private UnityEvent<bool> _placeholderVisible = new UnityEvent<bool>();
        [SerializeField] private UnityEvent<string> _placeholderText = new UnityEvent<string>();
        [SerializeField] private UnityEvent<bool> _noticeVisible = new UnityEvent<bool>();

        private ContentSlot _slot;
        private ContentSlotPresentation _presentation;

        /// <summary>The events, for a view built in code; a scene wires them in the Inspector.</summary>
        public UnityEvent<bool> indicatorVisible => _indicatorVisible;
        public UnityEvent<string> progressText => _progressText;
        public UnityEvent<bool> placeholderVisible => _placeholderVisible;
        public UnityEvent<string> placeholderText => _placeholderText;
        public UnityEvent<bool> noticeVisible => _noticeVisible;

        public void Bind(ContentSlot slot)
        {
            if (_slot != null)
                _slot.changed -= Present;
            _slot = slot;
            if (_slot == null)
                return;
            _presentation = new ContentSlotPresentation(_percentFormat, _indeterminateText, _preparingText);
            _slot.changed += Present;
            Present();
        }

        private void OnDestroy() => Bind(null);

        private void Present()
        {
            _indicatorVisible.Invoke(_presentation.IndicatorVisible(_slot));
            _progressText.Invoke(_presentation.ProgressText(_slot));
            _placeholderVisible.Invoke(_presentation.PlaceholderVisible(_slot));
            _placeholderText.Invoke(_presentation.PlaceholderText(_slot));
            _noticeVisible.Invoke(_presentation.NoticeVisible(_slot));
        }
    }
}

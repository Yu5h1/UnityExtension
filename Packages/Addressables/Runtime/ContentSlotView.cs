using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib.AddressableAssets
{
    /// <summary>
    /// Turns a <see cref="ContentSlot"/> into scene events, so the progress indicator, the placeholder and the failure
    /// notice are ordinary scene objects wired in the Inspector (for example GameObject.SetActive and TMP_Text.text).
    /// The owner of the slot calls <see cref="Bind"/>. While the placeholder stands for a loading selection it shows that
    /// load's progress (the indicator follows the same load then); as the fallback after a failure it shows no progress.
    /// </summary>
    public class ContentSlotView : MonoBehaviour
    {
        [SerializeField] private string _percentFormat = "Loading… {0}%";
        [SerializeField] private string _indeterminateText = "Loading…";
        [SerializeField] private string _preparingText = "準備中…";

        [SerializeField] private UnityEvent<bool> _indicatorVisible = new UnityEvent<bool>();
        [SerializeField] private UnityEvent<string> _progressText = new UnityEvent<string>();
        [SerializeField] private UnityEvent<bool> _placeholderVisible = new UnityEvent<bool>();
        [SerializeField] private UnityEvent<string> _placeholderText = new UnityEvent<string>();
        [SerializeField] private UnityEvent<bool> _noticeVisible = new UnityEvent<bool>();

        private ContentSlot _slot;

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
            var text = indicator switch
            {
                ContentSlot.Indicator.Hidden => string.Empty,
                ContentSlot.Indicator.Preparing => _preparingText,
                ContentSlot.Indicator.Indeterminate => _indeterminateText,
                _ => string.Format(_percentFormat, _slot.percent),
            };
            _indicatorVisible.Invoke(indicator != ContentSlot.Indicator.Hidden);
            _progressText.Invoke(text);
            var shown = _slot.shown;
            _placeholderVisible.Invoke(shown == ContentSlot.Placeholder || shown == ContentSlot.Fallback);
            _placeholderText.Invoke(shown == ContentSlot.Placeholder ? text : string.Empty);
            _noticeVisible.Invoke(_slot.noticeVisible);
        }
    }
}

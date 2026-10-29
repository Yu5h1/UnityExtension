namespace Yu5h1Lib
{
    /// <summary>
    /// What a <see cref="ContentSlot"/> puts on screen, independent of the UI technology: whether the progress indicator,
    /// the placeholder and the failure notice are visible and what the indicator and the placeholder read. A uGUI scene
    /// wires it through <see cref="ContentSlotView"/>; a UI Toolkit panel can set Label.text and style.display from the
    /// same values. The texts are the host's; <c>{0}</c> in the percent format is the whole percent.
    /// <para>
    /// While the placeholder stands for a loading selection it reads that load's progress (the indicator follows the
    /// same load then); as the fallback after a failure it reads nothing.
    /// </para>
    /// </summary>
    public class ContentSlotPresentation
    {
        public const string DefaultPercentFormat = "Loading… {0}%";
        public const string DefaultIndeterminateText = "Loading…";
        public const string DefaultPreparingText = "準備中…";

        public string percentFormat { get; }
        public string indeterminateText { get; }
        public string preparingText { get; }

        public ContentSlotPresentation(string percentFormat = DefaultPercentFormat,
            string indeterminateText = DefaultIndeterminateText, string preparingText = DefaultPreparingText)
        {
            this.percentFormat = percentFormat;
            this.indeterminateText = indeterminateText;
            this.preparingText = preparingText;
        }

        public bool IndicatorVisible(ContentSlot slot) => slot.indicator != ContentSlot.Indicator.Hidden;

        /// <summary>The indicator's text; empty while it is hidden.</summary>
        public string ProgressText(ContentSlot slot) => slot.indicator switch
        {
            ContentSlot.Indicator.Hidden => string.Empty,
            ContentSlot.Indicator.Preparing => preparingText,
            ContentSlot.Indicator.Indeterminate => indeterminateText,
            _ => string.Format(percentFormat, slot.percent),
        };

        /// <summary>The placeholder is up while the selection loads and as the fallback after it failed.</summary>
        public bool PlaceholderVisible(ContentSlot slot)
            => slot.shown == ContentSlot.Placeholder || slot.shown == ContentSlot.Fallback;

        public string PlaceholderText(ContentSlot slot)
            => slot.shown == ContentSlot.Placeholder ? ProgressText(slot) : string.Empty;

        public bool NoticeVisible(ContentSlot slot) => slot.noticeVisible;
    }
}

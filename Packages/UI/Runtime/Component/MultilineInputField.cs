using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
    /// <summary>Fits a multiline uGUI InputField to its visible lines and scrolls its full native editor when needed.</summary>
    [DisallowMultipleComponent]
    public sealed class MultilineInputField : UIBehaviour, ILayoutElement
    {
        private const float ScrollbarWidth = 16f;

        private TextGenerator measurement;
        private InputField field;
        private Text label;
        private RectTransform viewport;
        private RectTransform content;
        private TextGenerationSettings previousSettings;
        private string previousText;
        private int maxVisibleLines;
        private float minHeightValue;
        private float measuredFullHeight;
        private float measuredVisibleHeight;
        private int lastCaret = -1;
        private bool revealAfterLayout;

        public float PreferredHeight { get; private set; }
        public int VisibleLineCount { get; private set; } = 1;
        public bool HasOverflow { get; private set; }
        public ScrollRect ScrollView { get; private set; }
        public float maxWidth => -1f;
        public float maxHeight => -1f;

        float ILayoutElement.minWidth => -1f;
        float ILayoutElement.preferredWidth => -1f;
        float ILayoutElement.flexibleWidth => -1f;
        float ILayoutElement.minHeight => field != null ? minHeightValue : -1f;
        float ILayoutElement.preferredHeight => field != null ? PreferredHeight : -1f;
        float ILayoutElement.flexibleHeight => field != null ? 0f : -1f;
        int ILayoutElement.layoutPriority => 2;

        /// <summary>
        /// Configures a direct child input whose Text is stretched across the input rectangle.
        /// The viewport supplies clipping and a vertical scrollbar, while the input retains its native editing behavior.
        /// </summary>
        public void Configure(InputField input, float minHeight = 44f, int maxVisibleLines = 5)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            var text = input.textComponent;
            if (input.transform.parent != transform || text == null || text.transform.parent != input.transform ||
                text.rectTransform.anchorMin != Vector2.zero || text.rectTransform.anchorMax != Vector2.one)
                throw new ArgumentException("The input must be a direct child and its Text must stretch across it.", nameof(input));

            if (field != null && field != input)
                throw new InvalidOperationException("This viewport is already configured for another input.");

            minHeightValue = Mathf.Max(1f, minHeight);
            this.maxVisibleLines = Mathf.Max(1, maxVisibleLines);
            previousText = null;

            if (field == null)
            {
                measurement = new TextGenerator();
                field = input;
                label = text;
                viewport = (RectTransform)transform;
                content = (RectTransform)input.transform;
                content.anchorMin = Vector2.up;
                content.anchorMax = Vector2.one;
                content.pivot = new Vector2(.5f, 1f);
                content.anchoredPosition = new Vector2(-ScrollbarWidth * .5f, 0f);
                content.sizeDelta = new Vector2(-ScrollbarWidth, minHeightValue);

                if (!TryGetComponent<RectMask2D>(out _))
                    gameObject.AddComponent<RectMask2D>();

                ScrollView = GetComponent<ScrollRect>();
                if (ScrollView == null)
                    ScrollView = gameObject.AddComponent<ScrollRect>();
                ScrollView.viewport = viewport;
                ScrollView.content = content;
                ScrollView.horizontal = false;
                ScrollView.vertical = true;
                ScrollView.movementType = ScrollRect.MovementType.Clamped;
                ScrollView.scrollSensitivity = 24f;
                ScrollView.verticalScrollbar = CreateScrollbar();
                ScrollView.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.verticalOverflow = VerticalWrapMode.Overflow;
                if (isActiveAndEnabled)
                    field.onValueChanged.AddListener(OnValueChanged);
            }

            RefreshLayout();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (field != null)
            {
                field.onValueChanged.AddListener(OnValueChanged);
                RefreshLayout();
            }
        }

        protected override void OnDisable()
        {
            if (field != null)
                field.onValueChanged.RemoveListener(OnValueChanged);
            lastCaret = -1;
            base.OnDisable();
        }

        void ILayoutElement.CalculateLayoutInputHorizontal() { }

        void ILayoutElement.CalculateLayoutInputVertical() => UpdateMeasurements();

        private void OnValueChanged(string _) => RefreshLayout();

        public void RefreshLayout()
        {
            if (UpdateMeasurements() && !CanvasUpdateRegistry.IsRebuildingLayout())
                LayoutRebuilder.MarkLayoutForRebuild(viewport);
        }

        private void LateUpdate()
        {
            if (field == null)
                return;

            RefreshLayout();
            int caret = field.isFocused ? field.selectionFocusPosition : -1;
            if (caret >= 0 && (caret != lastCaret || revealAfterLayout))
                RevealCaret(caret);

            lastCaret = caret;
            revealAfterLayout = false;
        }

        /// <summary>Brings the current native selection focus into the viewport.</summary>
        public void ScrollToCaret()
        {
            RefreshLayout();
            if (field != null)
                RevealCaret(field.selectionFocusPosition);
        }

        private bool UpdateMeasurements()
        {
            if (field == null)
                return false;

            string text = GetDisplayedValue();
            var settings = label.GetGenerationSettings(new Vector2(Mathf.Max(1f, label.rectTransform.rect.width), 0f));
            settings.verticalOverflow = VerticalWrapMode.Overflow;
            settings.generateOutOfBounds = true;
            bool remeasure = previousText != text || !settings.Equals(previousSettings);

            if (remeasure)
            {
                previousText = text;
                previousSettings = settings;
                string measured = string.IsNullOrEmpty(text) || text.EndsWith("\n", StringComparison.Ordinal)
                    ? text + " "
                    : text;
                if (label.font != null)
                    measurement.Populate(measured, settings);
                int lineCount = Mathf.Max(1, measurement.lineCount);
                VisibleLineCount = Mathf.Min(lineCount, maxVisibleLines);
                HasOverflow = lineCount > VisibleLineCount;
                measuredFullHeight = GetLineDepth(measurement.lines, lineCount - 1);
                measuredVisibleHeight = GetLineDepth(measurement.lines, VisibleLineCount - 1);
            }

            float padding = Mathf.Max(0f, label.rectTransform.offsetMin.y - label.rectTransform.offsetMax.y);
            float preferred = Mathf.Max(minHeightValue, Mathf.Ceil(measuredVisibleHeight + padding));
            float contentHeight = Mathf.Max(preferred, Mathf.Ceil(measuredFullHeight + padding), viewport.rect.height);
            bool preferredChanged = !Mathf.Approximately(PreferredHeight, preferred);
            bool contentChanged = !Mathf.Approximately(content.rect.height, contentHeight);
            PreferredHeight = preferred;
            if (contentChanged)
                content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);

            revealAfterLayout |= remeasure || contentChanged || preferredChanged;
            return preferredChanged;
        }

        private string GetDisplayedValue()
        {
            string value = field.text;
            if (field.isFocused && EventSystem.current?.currentSelectedGameObject == field.gameObject)
            {
                string composition = EventSystem.current.currentInputModule?.input.compositionString;
                if (!string.IsNullOrEmpty(composition))
                {
                    int start = Mathf.Clamp(field.selectionAnchorPosition - composition.Length, 0, value.Length);
                    value = value.Insert(start, composition);
                }
            }

            return field.inputType == InputField.InputType.Password
                ? new string(field.asteriskChar, value.Length)
                : value;
        }

        private float GetLineDepth(IList<UILineInfo> lines, int index)
        {
            if (index < 0 || index >= lines.Count)
                return label.fontSize;

            var line = lines[index];
            return (lines[0].topY - line.topY + line.height - line.leading) / label.pixelsPerUnit;
        }

        private void RevealCaret(int caret)
        {
            var lines = measurement.lines;
            if (lines.Count == 0)
                return;

            float range = content.rect.height - viewport.rect.height;
            if (range <= 0f)
                return;

            int index = lines.Count - 1;
            while (index > 0 && lines[index].startCharIdx > caret)
                index--;

            float topInset = -label.rectTransform.offsetMax.y;
            float slack = Mathf.Max(0f, label.rectTransform.rect.height - measuredFullHeight);
            switch (label.alignment)
            {
                case TextAnchor.MiddleLeft:
                case TextAnchor.MiddleCenter:
                case TextAnchor.MiddleRight:
                    topInset += slack * .5f;
                    break;
                case TextAnchor.LowerLeft:
                case TextAnchor.LowerCenter:
                case TextAnchor.LowerRight:
                    topInset += slack;
                    break;
            }

            float top = topInset + (lines[0].topY - lines[index].topY) / label.pixelsPerUnit;
            float bottom = topInset + GetLineDepth(lines, index);
            float current = content.anchoredPosition.y;
            float target = Mathf.Min(current, top);
            target = Mathf.Max(target, bottom - viewport.rect.height);

            if (!Mathf.Approximately(target, current))
            {
                ScrollView.StopMovement();
                ScrollView.verticalNormalizedPosition = 1f - Mathf.Clamp01(target / range);
            }
        }

        private Scrollbar CreateScrollbar()
        {
            var track = new GameObject("Draft scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            var trackRect = (RectTransform)track.transform;
            trackRect.SetParent(viewport, false);
            trackRect.anchorMin = Vector2.right;
            trackRect.anchorMax = Vector2.one;
            trackRect.pivot = new Vector2(1f, .5f);
            trackRect.sizeDelta = new Vector2(ScrollbarWidth, 0f);
            trackRect.anchoredPosition = Vector2.zero;
            track.GetComponent<Image>().color = new Color(1f, 1f, 1f, .10f);

            var handle = new GameObject("Thumb", typeof(RectTransform), typeof(Image));
            var handleRect = (RectTransform)handle.transform;
            handleRect.SetParent(trackRect, false);
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = new Vector2(4f, 2f);
            handleRect.offsetMax = new Vector2(-4f, -2f);
            var graphic = handle.GetComponent<Image>();
            graphic.color = new Color(1f, 1f, 1f, .68f);

            var scrollbar = track.GetComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = graphic;
            scrollbar.navigation = new Navigation { mode = Navigation.Mode.None };
            track.SetActive(false);
            return scrollbar;
        }
    }
}

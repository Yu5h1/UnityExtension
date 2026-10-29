using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Yu5h1Lib.UI.Acceptance
{
    /// <summary>
    /// The BackgroundLoad demo page. Left: the simulated loader's controls and an input field. Middle: a stage that shows
    /// one of four contents (A fast, B slow, C without a known ratio, D always failing), started on A when the page opens,
    /// with its indicator in the bottom-right corner. Right: a list whose images each are a one-content slot.
    /// <para>
    /// Loading an image when it scrolls into view and letting it finish after it scrolls out are this demo's host
    /// policy, not BackgroundLoad rules (the spec leaves what to load and when to the host); list loads have no corner
    /// indicator because the spec does not cover combined progress.
    /// </para>
    /// </summary>
    public class UxUiBackgroundLoadDemo : MonoBehaviour
    {
        private class Profile
        {
            public readonly float share;
            public readonly bool knownRatio;
            public readonly float failAt;
            public readonly Color color;

            public Profile(float share, bool knownRatio, float failAt, Color color)
            {
                this.share = share;
                this.knownRatio = knownRatio;
                this.failAt = failAt;
                this.color = color;
            }
        }

        private class Cell
        {
            public string key;
            public RectTransform rect;
            public ContentSlot slot;
            public bool requested;
        }

        private const string NoticeText = "此內容異常";
        private const float PrepareSeconds = 1f;
        private const int CellCount = 8;
        private const int FailingCell = 5;

        private static readonly Dictionary<string, Profile> StageProfiles = new Dictionary<string, Profile>
        {
            ["A"] = new Profile(0.3f, true, -1f, new Color(0.30f, 0.55f, 0.85f)),
            ["B"] = new Profile(1f, true, -1f, new Color(0.35f, 0.70f, 0.40f)),
            ["C"] = new Profile(1f, false, -1f, new Color(0.85f, 0.60f, 0.25f)),
            ["D"] = new Profile(0.6f, true, 0.6f, new Color(0.65f, 0.40f, 0.75f)),
        };

        private Font _font;
        private Slider _seconds;
        private ContentSlot _stage;
        private Image _stageContent;
        private Text _stageLabel;
        private RectTransform _viewport;
        private readonly List<Cell> _cells = new List<Cell>();

        private void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Build(8f);
        }

        /// <summary>Drops every load and rebuilds the page, as if the app had just opened.</summary>
        private void Restart()
        {
            var seconds = _seconds.value;
            StopAllCoroutines();
            for (var i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);
            _cells.Clear();
            Build(seconds);
        }

        private void Build(float seconds)
        {
            BuildControls(seconds);
            BuildStage();
            BuildList();
            _stage.Start("A");
        }

        private void Update()
        {
            if (_viewport == null)
                return;
            var view = VerticalRange(_viewport);
            foreach (var cell in _cells)
            {
                if (cell.requested)
                    continue;
                var range = VerticalRange(cell.rect);
                if (range.y > view.x && range.x < view.y)
                {
                    cell.requested = true;
                    cell.slot.Select(cell.key);
                }
            }
        }

        private static Vector2 VerticalRange(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return new Vector2(corners[0].y, corners[1].y);
        }

        private IEnumerator Simulate(ContentSlot slot, string key, Profile profile)
        {
            var duration = Mathf.Max(0.1f, _seconds.value * profile.share);
            var elapsed = 0f;
            while (elapsed < duration)
            {
                yield return null;
                elapsed += Time.deltaTime;
                var ratio = Mathf.Clamp01(elapsed / duration);
                if (profile.failAt >= 0f && ratio >= profile.failAt)
                {
                    slot.Complete(key, false);
                    yield break;
                }
                if (profile.knownRatio && ratio < 1f)
                    slot.Report(key, ratio);
            }
            slot.Report(key, 1f);
            yield return new WaitForSeconds(PrepareSeconds);
            slot.Complete(key, true);
        }

        private void BuildControls(float seconds)
        {
            var panel = Rect("Controls", transform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(16f, 16f), new Vector2(276f, -16f));
            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            Label(panel, "模擬讀取", 16, TextAnchor.MiddleLeft, FontStyle.Bold);
            var secondsLabel = Label(panel, string.Empty, 13, TextAnchor.MiddleLeft);
            _seconds = SliderField(panel, 1f, 30f, seconds);
            _seconds.onValueChanged.AddListener(value => secondsLabel.text = $"B 讀完一次需要 {value:0} 秒（下一次讀取生效）");
            secondsLabel.text = $"B 讀完一次需要 {seconds:0} 秒（下一次讀取生效）";

            Label(panel, "角色台選擇：A 快、B 慢、C 比例未知、D 一定失敗", 13, TextAnchor.MiddleLeft);
            var row = Rect("Select", panel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 32f;
            var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 6f;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            foreach (var key in StageProfiles.Keys)
            {
                var button = ButtonAddon.Create(row, icon: null, label: key, appearance: ButtonAppearance.Solid,
                    showIcon: false, showLabel: true, size: 32f);
                button.ui.onClick.AddListener(() => _stage.Select(key));
            }
            var restart = ButtonAddon.Create(panel, icon: null, label: "重新啟動（像剛開 App）", appearance: ButtonAppearance.Solid,
                showIcon: false, showLabel: true, size: 32f);
            restart.ui.onClick.AddListener(Restart);

            Label(panel, "讀取中在這裡打字：", 13, TextAnchor.MiddleLeft);
            InputBox(panel);
        }

        private void BuildStage()
        {
            var stage = Rect("Stage", transform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(300f, 60f), new Vector2(760f, -60f));
            Paint(stage, new Color(0.15f, 0.16f, 0.20f));

            _stageContent = Paint(Fill(Rect("Content", stage)), Color.white);
            _stageLabel = Label(_stageContent.rectTransform, string.Empty, 64, TextAnchor.MiddleCenter, FontStyle.Bold);
            Fill(_stageLabel.rectTransform);
            _stageContent.gameObject.SetActive(false);

            var placeholder = Paint(Fill(Rect("Placeholder", stage)), new Color(0f, 0f, 0f, 0.35f));
            var placeholderText = Label(placeholder.rectTransform, string.Empty, 20, TextAnchor.MiddleCenter);
            Fill(placeholderText.rectTransform);
            var notice = Label(stage, NoticeText, 18, TextAnchor.MiddleCenter, FontStyle.Bold);
            notice.color = new Color(1f, 0.55f, 0.5f);
            Place(notice.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 8f), new Vector2(0f, 40f));

            var indicator = Label(transform, string.Empty, 16, TextAnchor.LowerRight);
            Place(indicator.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-320f, 12f), new Vector2(-16f, 40f));

            _stage = new ContentSlot(StageProfiles.Keys);
            _stage.loadRequested += key => StartCoroutine(Simulate(_stage, key, StageProfiles[key]));
            _stage.changed += () =>
            {
                var ready = _stage.shown != null && StageProfiles.TryGetValue(_stage.shown, out var shown) ? shown : null;
                _stageContent.gameObject.SetActive(ready != null);
                if (ready == null)
                    return;
                _stageContent.color = ready.color;
                _stageLabel.text = _stage.shown;
            };
            var view = stage.gameObject.AddComponent<ContentSlotView>();
            view.indicatorVisible.AddListener(indicator.gameObject.SetActive);
            view.progressText.AddListener(text => indicator.text = text);
            view.placeholderVisible.AddListener(placeholder.gameObject.SetActive);
            view.placeholderText.AddListener(text => placeholderText.text = text);
            view.noticeVisible.AddListener(notice.gameObject.SetActive);
            view.Bind(_stage);
        }

        private void BuildList()
        {
            var scroll = Rect("List", transform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(784f, 60f), new Vector2(-16f, -16f));
            Paint(scroll, new Color(0.10f, 0.11f, 0.14f)).raycastTarget = true;
            var scrollRect = scroll.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.scrollSensitivity = 20f;

            _viewport = Fill(Rect("Viewport", scroll));
            _viewport.gameObject.AddComponent<RectMask2D>();
            var content = Rect("Content", _viewport, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            content.pivot = new Vector2(0.5f, 1f);
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.viewport = _viewport;
            scrollRect.content = content;

            for (var i = 1; i <= CellCount; i++)
                _cells.Add(BuildCell(content, i));
        }

        private Cell BuildCell(RectTransform parent, int number)
        {
            var key = "#" + number;
            var profile = new Profile(0.5f, true, number == FailingCell ? 0.5f : -1f,
                Color.HSVToRGB(number / (float)CellCount, 0.45f, 0.8f));
            var rect = Rect("Cell " + key, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            rect.gameObject.AddComponent<LayoutElement>().preferredHeight = 180f;
            Paint(rect, new Color(0.20f, 0.21f, 0.26f));

            var image = Paint(Fill(Rect("Image", rect)), profile.color);
            Fill(Label(image.rectTransform, key, 40, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform);
            image.gameObject.SetActive(false);
            var placeholder = Paint(Fill(Rect("Placeholder", rect)), new Color(0f, 0f, 0f, 0.35f));
            var placeholderText = Label(placeholder.rectTransform, string.Empty, 18, TextAnchor.MiddleCenter);
            Fill(placeholderText.rectTransform);
            var notice = Label(rect, NoticeText, 16, TextAnchor.MiddleCenter, FontStyle.Bold);
            notice.color = new Color(1f, 0.55f, 0.5f);
            Place(notice.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 6f), new Vector2(0f, 32f));

            var cell = new Cell { key = key, rect = rect, slot = new ContentSlot(new[] { key }) };
            cell.slot.loadRequested += k => StartCoroutine(Simulate(cell.slot, k, profile));
            cell.slot.changed += () => image.gameObject.SetActive(cell.slot.shown == key);
            var view = rect.gameObject.AddComponent<ContentSlotView>();
            view.placeholderVisible.AddListener(placeholder.gameObject.SetActive);
            view.placeholderText.AddListener(text => placeholderText.text = text);
            view.noticeVisible.AddListener(notice.gameObject.SetActive);
            view.Bind(cell.slot);
            return cell;
        }

        private static RectTransform Rect(string name, Transform parent)
            => Rect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        private static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Place(rect, anchorMin, anchorMax, offsetMin, offsetMax);
            return rect;
        }

        private static RectTransform Fill(RectTransform rect)
        {
            Place(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return rect;
        }

        private static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        /// <summary>Graphics never take pointer input (BGL-NOBLOCK); a control that needs it turns it back on.</summary>
        private static Image Paint(RectTransform rect, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private Text Label(Transform parent, string text, int size, TextAnchor alignment, FontStyle style = FontStyle.Normal)
        {
            var label = new GameObject("Text", typeof(RectTransform)).AddComponent<Text>();
            label.transform.SetParent(parent, false);
            label.font = _font;
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = alignment;
            label.color = Color.white;
            label.text = text;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            return label;
        }

        private Slider SliderField(Transform parent, float min, float max, float value)
        {
            var rect = Rect("Slider", parent);
            rect.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
            var background = Paint(Fill(Rect("Background", rect)), new Color(0.25f, 0.26f, 0.30f));
            var fillArea = Rect("Fill Area", rect, Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-12f, -4f));
            var fill = Paint(Rect("Fill", fillArea), new Color(0.35f, 0.60f, 0.90f));
            var handleArea = Rect("Handle Area", rect, Vector2.zero, Vector2.one, new Vector2(8f, 0f), new Vector2(-8f, 0f));
            var handle = Paint(Rect("Handle", handleArea, Vector2.zero, new Vector2(0f, 1f), new Vector2(-8f, 0f), new Vector2(8f, 0f)), Color.white);
            handle.raycastTarget = true;
            background.raycastTarget = true;
            var slider = rect.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = true;
            slider.value = value;
            return slider;
        }

        private void InputBox(Transform parent)
        {
            var rect = Rect("Input", parent);
            rect.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;
            var background = Paint(rect, Color.white);
            background.raycastTarget = true;
            var text = Label(rect, string.Empty, 14, TextAnchor.MiddleLeft);
            text.color = Color.black;
            text.supportRichText = false;
            Place(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 2f), new Vector2(-8f, -2f));
            var input = rect.gameObject.AddComponent<InputField>();
            input.textComponent = text;
            input.targetGraphic = background;
        }
    }
}

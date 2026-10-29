using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Yu5h1Lib.UI.Acceptance
{
    /// <summary>
    /// Shared acceptance demo application host for Yu5h1Lib.UX modules on Yu5h1Lib.UI (UGUI).
    /// Provides durable target resolution, modular case registration, and instant switching without coordinator branching.
    /// </summary>
    public class UxUiAcceptanceHost : MonoBehaviour
    {
        private RectTransform _rootContainer;
        private RectTransform _contentContainer;
        private RectTransform _selectorContainer;
        private Image _statusBannerImage;
        private Text _statusText;

        private IUxUiModuleDemoCase _currentCase;
        private readonly List<string> _caseIds = new List<string>();

        public IUxUiModuleDemoCase CurrentCase => _currentCase;
        public string StatusText => _statusText != null ? _statusText.text : string.Empty;
        public RectTransform ContentContainer => _contentContainer;

        private void Start()
        {
            EnsureEventSystem();
            InitializeHost();
        }

        private void OnDestroy()
        {
            if (_currentCase != null)
            {
                _currentCase.Unmount();
                _currentCase = null;
            }
        }

        public void InitializeHost()
        {
            EnsureCanvas();
            RegisterBuiltInCases();
            if (_rootContainer == null)
            {
                BuildHostUI();
            }
            string targetId = UxUiAcceptanceTarget.ResolveCurrentTargetId();
            SelectCase(targetId);
        }

        public void RegisterBuiltInCases()
        {
            if (!UxUiModuleRegistry.Contains("button"))
            {
                UxUiModuleRegistry.Register(new UxUiButtonDemoCase());
            }
            if (!UxUiModuleRegistry.Contains("backgroundload"))
            {
                UxUiModuleRegistry.Register(new UxUiBackgroundLoadDemoCase());
            }
        }

        public void SelectCase(string targetId)
        {
            EnsureCanvas();
            if (_rootContainer == null)
            {
                BuildHostUI();
            }

            RefreshSelectorButtons(targetId);

            var targetCase = UxUiModuleRegistry.Get(targetId);
            if (targetCase == null)
            {
                if (_currentCase != null)
                {
                    _currentCase.Unmount();
                    _currentCase = null;
                }
                ClearChildren(_contentContainer);

                _statusBannerImage.color = new Color(0.85f, 0.25f, 0.25f, 1f);
                _statusText.text = $"[Blocked] Acceptance target '{targetId}' is not registered or prerequisites missing.";

                var blockedGo = new GameObject(
                    "BlockedView",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text));
                blockedGo.transform.SetParent(_contentContainer, false);
                var blockedRect = blockedGo.GetComponent<RectTransform>();
                blockedRect.anchorMin = Vector2.zero;
                blockedRect.anchorMax = Vector2.one;
                blockedRect.offsetMin = Vector2.zero;
                blockedRect.offsetMax = Vector2.zero;

                var msg = blockedGo.GetComponent<Text>();
                msg.font = GetDefaultFont();
                msg.fontSize = 15;
                msg.alignment = TextAnchor.MiddleCenter;
                msg.color = new Color(0.85f, 0.25f, 0.25f, 1f);
                msg.text = $"Target '{targetId}' not found in registry.\nPlease register the module case via UxUiModuleRegistry.Register().";
                return;
            }

            if (_currentCase != null)
            {
                _currentCase.Unmount();
                _currentCase = null;
            }
            ClearChildren(_contentContainer);

            _currentCase = targetCase;
            _currentCase.Mount(_contentContainer);

            _statusBannerImage.color = new Color(0.20f, 0.55f, 0.35f, 1f);
            _statusText.text = $"Active Acceptance Target: {targetCase.DisplayName} | Spec: {targetCase.ModuleSpec} | ID: {targetCase.Id}";

            LayoutRebuilder.ForceRebuildLayoutImmediate(_rootContainer);
        }

        public void ResetCurrentCase()
        {
            if (_currentCase != null)
            {
                string id = _currentCase.Id;
                _currentCase.Unmount();
                _currentCase = null;
                ClearChildren(_contentContainer);
                SelectCase(id);
            }
        }

        private void EnsureCanvas()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            if (GetComponent<CanvasScaler>() == null)
            {
                var scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280f, 720f);
            }
            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        private static void EnsureEventSystem()
        {
            var es = EventSystem.current != null ? EventSystem.current : ObjectUtility.FindObject<EventSystem>(false);
            if (es == null)
            {
                var go = new GameObject("EventSystem", typeof(EventSystem), typeof(UIInputHandler), typeof(StandaloneInputModule));
                go.GetComponent<StandaloneInputModule>().inputOverride = go.GetComponent<UIInputHandler>();
            }
            else
            {
                var module = es.GetComponent<BaseInputModule>();
                if (module != null && module.inputOverride == null)
                {
                    var handler = es.GetComponent<UIInputHandler>();
                    if (handler == null)
                    {
                        handler = es.gameObject.AddComponent<UIInputHandler>();
                    }
                    module.inputOverride = handler;
                }
            }
        }

        private void BuildHostUI()
        {
            var rootGo = new GameObject(
                "HostRoot",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup));
            rootGo.transform.SetParent(transform, false);

            _rootContainer = rootGo.GetComponent<RectTransform>();
            _rootContainer.anchorMin = Vector2.zero;
            _rootContainer.anchorMax = Vector2.one;
            _rootContainer.offsetMin = Vector2.zero;
            _rootContainer.offsetMax = Vector2.zero;

            var rootLayout = rootGo.GetComponent<VerticalLayoutGroup>();
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childForceExpandHeight = false;
            rootLayout.spacing = 0f;

            var topBarGo = new GameObject(
                "TopBar",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(HorizontalLayoutGroup),
                typeof(LayoutElement));
            topBarGo.transform.SetParent(_rootContainer, false);
            topBarGo.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.18f, 1f);

            var topBarLayout = topBarGo.GetComponent<HorizontalLayoutGroup>();
            topBarLayout.padding = new RectOffset(16, 16, 8, 8);
            topBarLayout.spacing = 12f;
            topBarLayout.childAlignment = TextAnchor.MiddleLeft;
            topBarLayout.childControlWidth = true;
            topBarLayout.childControlHeight = true;
            topBarLayout.childForceExpandWidth = false;
            topBarLayout.childForceExpandHeight = false;

            var topBarElement = topBarGo.GetComponent<LayoutElement>();
            topBarElement.minHeight = 48f;
            topBarElement.preferredHeight = 48f;

            var titleGo = new GameObject(
                "Title",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text),
                typeof(LayoutElement));
            titleGo.transform.SetParent(topBarGo.transform, false);
            var titleText = titleGo.GetComponent<Text>();
            titleText.font = GetDefaultFont();
            titleText.fontSize = 15;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.text = "Yu5h1Lib.UI (UGUI) UX Acceptance Host";
            titleGo.GetComponent<LayoutElement>().flexibleWidth = 1f;

            var selectorGo = new GameObject(
                "ModuleSelectors",
                typeof(RectTransform),
                typeof(HorizontalLayoutGroup));
            selectorGo.transform.SetParent(topBarGo.transform, false);
            _selectorContainer = selectorGo.GetComponent<RectTransform>();
            var selectorLayout = selectorGo.GetComponent<HorizontalLayoutGroup>();
            selectorLayout.spacing = 6f;
            selectorLayout.childAlignment = TextAnchor.MiddleRight;
            selectorLayout.childControlWidth = true;
            selectorLayout.childControlHeight = true;
            selectorLayout.childForceExpandWidth = false;
            selectorLayout.childForceExpandHeight = false;

            var resetAddon = ButtonAddon.Create(
                topBarGo.transform,
                icon: null,
                label: "Reset",
                appearance: ButtonAppearance.Solid,
                showIcon: false,
                showLabel: true,
                size: 28f);
            resetAddon.ui.onClick.AddListener(ResetCurrentCase);

            var bannerGo = new GameObject(
                "StatusBanner",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(HorizontalLayoutGroup),
                typeof(LayoutElement));
            bannerGo.transform.SetParent(_rootContainer, false);
            _statusBannerImage = bannerGo.GetComponent<Image>();
            _statusBannerImage.color = new Color(0.20f, 0.55f, 0.35f, 1f);

            var bannerLayout = bannerGo.GetComponent<HorizontalLayoutGroup>();
            bannerLayout.padding = new RectOffset(16, 16, 4, 4);
            bannerLayout.childAlignment = TextAnchor.MiddleLeft;
            bannerLayout.childControlWidth = true;
            bannerLayout.childControlHeight = true;

            var bannerElement = bannerGo.GetComponent<LayoutElement>();
            bannerElement.minHeight = 28f;
            bannerElement.preferredHeight = 28f;

            var statusTextGo = new GameObject(
                "StatusText",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            statusTextGo.transform.SetParent(bannerGo.transform, false);
            _statusText = statusTextGo.GetComponent<Text>();
            _statusText.font = GetDefaultFont();
            _statusText.fontSize = 12;
            _statusText.color = Color.white;
            _statusText.alignment = TextAnchor.MiddleLeft;

            var contentGo = new GameObject(
                "ContentContainer",
                typeof(RectTransform),
                typeof(LayoutElement));
            contentGo.transform.SetParent(_rootContainer, false);
            _contentContainer = contentGo.GetComponent<RectTransform>();
            var contentElement = contentGo.GetComponent<LayoutElement>();
            contentElement.flexibleHeight = 1f;
            contentElement.minHeight = 520f;
            contentElement.preferredHeight = 600f;
        }

        private void RefreshSelectorButtons(string activeId)
        {
            _caseIds.Clear();
            ClearChildren(_selectorContainer);

            foreach (var demoCase in UxUiModuleRegistry.AllCases)
            {
                string caseId = demoCase.Id;
                _caseIds.Add(caseId);
                bool isCurrent = string.Equals(caseId, activeId, System.StringComparison.OrdinalIgnoreCase);
                var btn = ButtonAddon.Create(
                    _selectorContainer,
                    icon: null,
                    label: $"{demoCase.DisplayName} ({caseId})",
                    appearance: ButtonAppearance.Solid,
                    showIcon: false,
                    showLabel: true,
                    isSelected: isCurrent,
                    size: 28f);
                btn.ui.onClick.AddListener(() => SelectCase(caseId));
            }
        }

        private static void ClearChildren(RectTransform parent)
        {
            if (parent == null)
            {
                return;
            }
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var childGo = parent.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    Destroy(childGo);
                }
                else
                {
                    DestroyImmediate(childGo);
                }
            }
        }

        private static Font GetDefaultFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            return font;
        }
    }
}

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Yu5h1Lib.UI.Acceptance
{
    /// <summary>Hosts the acceptance catalog and mounts one registered demo at a time.</summary>
    public class UxUiAcceptanceHost : MonoBehaviour
    {
        internal static readonly UguiControlPalette Palette = UguiControlPalette.Dark;

        [SerializeField, Min(1f)] private float preferredCardWidth = 240f;
        [SerializeField, Min(0f)] private float columnGap = 10f;
        [SerializeField] private bool openCurrentOnStart;

        private RectTransform root;
        private string originatingCardId;
        private RectTransform casePage;
        private Text caseTitle;
        private Button backButton;
        private Button resetButton;
        private UxUiAcceptanceCatalog catalog;

        public IUxUiModuleDemoCase CurrentCase { get; private set; }
        public string StatusText { get; private set; } = string.Empty;
        public RectTransform ContentContainer { get; private set; }

        private void Start()
        {
            EnsureEventSystem();
            if (root != null)
                return;
            if (openCurrentOnStart)
                InitializeHost();
            else
            {
                ShowCatalog();
                catalog.FocusFirstCard();
            }
        }

        private void OnEnable()
        {
            UxUiModuleRegistry.OnRegistryChanged += RefreshCatalog;
            RefreshCatalog();
        }

        private void OnDisable()
        {
            UxUiModuleRegistry.OnRegistryChanged -= RefreshCatalog;
            if (casePage == null || !casePage.gameObject.activeSelf)
                return;
            UnmountCase();
            originatingCardId = null;
            StatusText = string.Empty;
            casePage.gameObject.SetActive(false);
            catalog.Root.gameObject.SetActive(true);
        }

        private void OnDestroy() => ReleaseCase(CurrentCase);

        private void LateUpdate()
        {
            if (catalog != null && catalog.Root.gameObject.activeSelf)
                catalog.Reflow();
        }

        /// <summary>Opens the configured acceptance target for callers that explicitly request it.</summary>
        public void InitializeHost()
        {
            EnsureHost();
            SelectCase(UxUiAcceptanceTarget.ResolveCurrentTargetId());
        }

        public void RegisterBuiltInCases()
        {
            if (!UxUiModuleRegistry.Contains("button"))
                UxUiModuleRegistry.Register(new UxUiButtonDemoCase(), "按鈕狀態、樣式與版面");
            if (!UxUiModuleRegistry.Contains("backgroundload"))
                UxUiModuleRegistry.Register(new UxUiBackgroundLoadDemoCase(), "背景載入、進度與失敗狀態");
            if (!UxUiModuleRegistry.Contains("chatpanel"))
                UxUiModuleRegistry.Register(new UxUiChatPanelDemoCase(), "聊天面板、逐字回覆與圖片檢視");
            if (!UxUiModuleRegistry.Contains("scrollview"))
                UxUiModuleRegistry.Register(new UxUiScrollViewDemoCase(), "切換方向、循環與項目重用");
            if (!UxUiModuleRegistry.Contains("settingspanel"))
                UxUiModuleRegistry.Register(new UxUiSettingsDemoCase(), "Config、Setting 與確認保存");
        }

        public void ShowCatalog()
        {
            EnsureHost();
            var returning = casePage.gameObject.activeSelf;
            var previousId = originatingCardId;
            originatingCardId = null;
            UnmountCase();
            casePage.gameObject.SetActive(false);
            catalog.Root.gameObject.SetActive(true);
            StatusText = string.Empty;
            RefreshCatalog();
            LayoutRebuilder.ForceRebuildLayoutImmediate(catalog.Root);
            catalog.Reflow();
            if (returning)
                catalog.FocusCard(previousId);
        }

        public void SelectCase(string targetId) => OpenCase(targetId, null);

        public void ResetCurrentCase()
        {
            if (CurrentCase != null)
                OpenCase(CurrentCase.Id, originatingCardId);
        }

        private void OpenCase(string targetId, string sourceCardId)
        {
            EnsureHost();
            var fromCatalog = catalog.Root.gameObject.activeSelf;
            UnmountCase();
            originatingCardId = sourceCardId;
            catalog.Root.gameObject.SetActive(false);
            casePage.gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(casePage);

            var requested = UxUiModuleRegistry.Get(targetId);
            if (requested == null)
            {
                ShowBlocked($"[Blocked] 找不到驗收案例 '{targetId}'。");
            }
            else
            {
                try
                {
                    requested.Mount(ContentContainer);
                    caseTitle.text = requested.DisplayName;
                    StatusText = $"Active Acceptance Target: {requested.DisplayName} | Spec: {requested.ModuleSpec} | ID: {requested.Id}";
                    CurrentCase = requested;
                }
                catch (System.Exception error)
                {
                    ReleaseCase(requested);
                    ClearChildren(ContentContainer);
                    ShowBlocked($"[Blocked] 無法開啟 {requested.DisplayName}。{error.Message}");
                }
            }

            resetButton.interactable = CurrentCase != null;
            if (fromCatalog)
                backButton.Select();
        }

        private void ShowBlocked(string message)
        {
            StatusText = message;
            caseTitle.text = "展示無法開啟";
            var label = CreateText(ContentContainer, "BlockedView", message, 18, Palette.ItemText, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform, 16f);
        }

        private void UnmountCase()
        {
            var previous = CurrentCase;
            CurrentCase = null;
            ReleaseCase(previous);
            ClearChildren(ContentContainer);
        }

        private void ReleaseCase(IUxUiModuleDemoCase demo)
        {
            try
            {
                demo?.Unmount();
            }
            catch (System.Exception error)
            {
                Debug.LogWarning($"Unable to release acceptance demo '{demo.Id}': {error.Message}", this);
            }
        }

        private void EnsureHost()
        {
            if (root != null)
                return;
            RegisterBuiltInCases();
            EnsureCanvas();

            root = CreateRect(transform, "HostRoot");
            Stretch(root);
            root.gameObject.AddComponent<Image>().color = Palette.PageBackground;
            catalog = new UxUiAcceptanceCatalog(root, id => OpenCase(id, id),
                Mathf.Max(1f, preferredCardWidth), Mathf.Max(0f, columnGap));
            BuildCasePage();
        }

        private void RefreshCatalog()
        {
            if (catalog != null && catalog.Root.gameObject.activeInHierarchy)
                catalog.Refresh(UxUiAcceptanceTarget.ResolveCurrentTargetId());
        }

        private void EnsureCanvas()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960f, 640f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Shrink;

            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();
        }

        private static void EnsureEventSystem()
        {
            var eventSystem = EventSystem.current != null ? EventSystem.current : ObjectUtility.FindObject<EventSystem>(false);
            if (eventSystem == null)
            {
                var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
                var inputSystemModule = System.Type.GetType(
                    "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (inputSystemModule != null)
                {
                    go.AddComponent(inputSystemModule);
                    return;
                }
#endif
                go.AddComponent<StandaloneInputModule>().inputOverride = go.AddComponent<UIInputHandler>();
                return;
            }

            var module = eventSystem.GetComponent<BaseInputModule>();
            if (module != null && module.inputOverride == null)
            {
                var handler = eventSystem.GetComponent<UIInputHandler>();
                if (handler == null)
                    handler = eventSystem.gameObject.AddComponent<UIInputHandler>();
                module.inputOverride = handler;
            }
        }

        private void BuildCasePage()
        {
            casePage = CreateRect(root, "CasePage");
            Stretch(casePage);
            var pageLayout = casePage.gameObject.AddComponent<VerticalLayoutGroup>();
            pageLayout.childControlHeight = true;
            pageLayout.childControlWidth = true;
            pageLayout.childForceExpandHeight = false;

            var bar = CreateRect(casePage, "CaseBar");
            bar.gameObject.AddComponent<Image>().color = Palette.PanelBackground;
            bar.gameObject.AddComponent<LayoutElement>().preferredHeight = 54f;
            var barLayout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            barLayout.padding = new RectOffset(10, 10, 4, 4);
            barLayout.spacing = 8f;
            barLayout.childAlignment = TextAnchor.MiddleLeft;
            barLayout.childControlWidth = true;
            barLayout.childControlHeight = true;
            barLayout.childForceExpandWidth = false;
            barLayout.childForceExpandHeight = false;

            backButton = AddBarButton(bar, "返回", ShowCatalog);
            caseTitle = CreateText(bar, "CaseTitle", "", 18, Palette.ItemText, TextAnchor.MiddleLeft);
            caseTitle.fontStyle = FontStyle.Bold;
            caseTitle.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            resetButton = AddBarButton(bar, "重設", ResetCurrentCase);

            ContentContainer = CreateRect(casePage, "ContentContainer");
            ContentContainer.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            casePage.gameObject.SetActive(false);
        }

        private static Button AddBarButton(RectTransform bar, string label, UnityEngine.Events.UnityAction action)
        {
            var addon = ButtonAddon.Create(bar, label: label, showIcon: false, size: 44f);
            addon.labelText.fontSize = 16;
            Palette.ApplyButton(addon.ui, addon.labelText);
            addon.ui.onClick.AddListener(action);
            return addon.ui;
        }

        internal static RectTransform CreateRect(Transform parent, string name)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            return rect;
        }

        internal static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.one * inset;
            rect.offsetMax = -Vector2.one * inset;
        }

        internal static Text CreateText(RectTransform parent, string name, string value, int size, Color color, TextAnchor alignment)
        {
            var rect = CreateRect(parent, name);
            var label = rect.gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = size;
            label.color = color;
            label.text = value;
            label.alignment = alignment;
            label.raycastTarget = false;
            return label;
        }

        internal static void ClearChildren(RectTransform parent)
        {
            for (int index = parent.childCount - 1; index >= 0; index--)
            {
                var child = parent.GetChild(index);
                if (Application.isPlaying)
                {
                    child.gameObject.SetActive(false);
                    child.SetParent(null, false);
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }
    }
}

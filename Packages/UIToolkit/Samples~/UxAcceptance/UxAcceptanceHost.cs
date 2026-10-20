using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Yu5h1Lib.UIToolkit.Acceptance;

namespace Yu5h1LibTest
{
    /// <summary>
    /// Shared acceptance demo application host for Yu5h1Lib.UX modules on UI Toolkit.
    /// Conforms to referenced-module-acceptance § "Prepare the acceptance application for the user".
    /// Provides durable target resolution, modular case registration, and instant switching without coordinator branching.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class UxAcceptanceHost : MonoBehaviour
    {
        private UIDocument document;
        private PanelSettings settings;

        private VisualElement rootContainer;
        private VisualElement contentContainer;
        private DropdownField moduleDropdown;
        private Label statusLabel;

        private IUxModuleDemoCase currentCase;
        private readonly List<string> caseIds = new List<string>();
        private readonly List<string> caseDisplayNames = new List<string>();

        private void Start()
        {
            document = GetComponent<UIDocument>();
            if (document == null)
            {
                document = gameObject.AddComponent<UIDocument>();
            }

            if (document.panelSettings == null)
            {
                settings = ScriptableObject.CreateInstance<PanelSettings>();
#if UNITY_EDITOR
                var defaultTheme = UnityEditor.AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(
                    "Assets/UI Toolkit/UnityThemes/UnityDefaultRuntimeTheme.tss");
                if (defaultTheme != null)
                {
                    settings.themeStyleSheet = defaultTheme;
                }
#endif
                document.panelSettings = settings;
            }
            else if (document.panelSettings.themeStyleSheet == null)
            {
#if UNITY_EDITOR
                var defaultTheme = UnityEditor.AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(
                    "Assets/UI Toolkit/UnityThemes/UnityDefaultRuntimeTheme.tss");
                if (defaultTheme != null)
                {
                    document.panelSettings.themeStyleSheet = defaultTheme;
                }
#endif
            }

            // Register known built-in cases
            RegisterBuiltInCases();

            // Build UI framework
            BuildHostUI(document.rootVisualElement);

            // Resolve durable target ID (referenced-module-acceptance § 3)
            string targetId = UxAcceptanceTarget.ResolveCurrentTargetId();
            SelectCase(targetId);
        }

        private void OnDestroy()
        {
            if (currentCase != null)
            {
                currentCase.Unmount();
                currentCase = null;
            }
            if (settings != null)
            {
                Destroy(settings);
                settings = null;
            }
        }

        private void RegisterBuiltInCases()
        {
            if (!UxModuleRegistry.Contains("button"))
            {
                UxModuleRegistry.Register(new UxButtonDemoCase());
            }
            if (!UxModuleRegistry.Contains("optionpicker"))
            {
                UxModuleRegistry.Register(new UxOptionPickerDemoCase());
            }
            if (!UxModuleRegistry.Contains("input"))
            {
                UxModuleRegistry.Register(new UxInputHoldDemoCase());
            }
            if (!UxModuleRegistry.Contains("visualfoundation-light"))
            {
                UxModuleRegistry.Register(new UxVisualFoundationDemoCase(VisualFoundationScheme.Light));
            }
            if (!UxModuleRegistry.Contains("visualfoundation-dark"))
            {
                UxModuleRegistry.Register(new UxVisualFoundationDemoCase(VisualFoundationScheme.Dark));
            }
        }

        private void BuildHostUI(VisualElement root)
        {
            root.Clear();

            rootContainer = new VisualElement();
            rootContainer.style.flexGrow = 1;
            rootContainer.style.flexDirection = FlexDirection.Column;
            root.Add(rootContainer);

            // 1. Top Navigation Bar
            var topBar = new VisualElement();
            topBar.style.height = 48;
            topBar.style.minHeight = 48;
            topBar.style.flexDirection = FlexDirection.Row;
            topBar.style.alignItems = Align.Center;
            topBar.style.justifyContent = Justify.SpaceBetween;
            topBar.style.backgroundColor = new Color(0.12f, 0.14f, 0.18f, 1f);
            topBar.style.paddingLeft = 16;
            topBar.style.paddingRight = 16;
            rootContainer.Add(topBar);

            var titleContainer = new VisualElement();
            titleContainer.style.flexDirection = FlexDirection.Row;
            titleContainer.style.alignItems = Align.Center;

            var title = new Label("Yu5h1Lib.UX Acceptance Host");
            title.style.fontSize = 15;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = Color.white;
            titleContainer.Add(title);

            topBar.Add(titleContainer);

            var controlContainer = new VisualElement();
            controlContainer.style.flexDirection = FlexDirection.Row;
            controlContainer.style.alignItems = Align.Center;

            var label = new Label("Module:");
            label.style.color = new Color(0.8f, 0.82f, 0.88f, 1f);
            label.style.marginRight = 6;
            controlContainer.Add(label);

            RefreshCaseLists();

            moduleDropdown = new DropdownField(caseDisplayNames, 0);
            moduleDropdown.style.minWidth = 200;
            moduleDropdown.style.marginRight = 12;
            moduleDropdown.RegisterValueChangedCallback(evt =>
            {
                int index = moduleDropdown.index;
                if (index >= 0 && index < caseIds.Count)
                {
                    SelectCase(caseIds[index]);
                }
            });
            controlContainer.Add(moduleDropdown);

            var resetBtn = new Button(() =>
            {
                if (currentCase != null)
                {
                    string id = currentCase.Id;
                    currentCase.Unmount();
                    currentCase = null;
                    contentContainer.Clear();
                    SelectCase(id);
                }
            })
            {
                text = "Reset"
            };
            resetBtn.style.height = 28;
            controlContainer.Add(resetBtn);

            topBar.Add(controlContainer);

            // 2. Status Banner
            statusLabel = new Label();
            statusLabel.style.display = DisplayStyle.None;
            statusLabel.style.paddingTop = statusLabel.style.paddingBottom = 6;
            statusLabel.style.paddingLeft = statusLabel.style.paddingRight = 16;
            statusLabel.style.fontSize = 12;
            rootContainer.Add(statusLabel);

            // 3. Main Content Container
            contentContainer = new VisualElement();
            contentContainer.style.flexGrow = 1;
            rootContainer.Add(contentContainer);
        }

        private void RefreshCaseLists()
        {
            caseIds.Clear();
            caseDisplayNames.Clear();

            foreach (var c in UxModuleRegistry.AllCases)
            {
                caseIds.Add(c.Id);
                caseDisplayNames.Add($"{c.DisplayName} ({c.Id})");
            }
        }

        public void SelectCase(string targetId)
        {
            RefreshCaseLists();
            if (moduleDropdown != null)
            {
                moduleDropdown.choices = caseDisplayNames;
            }

            var targetCase = UxModuleRegistry.Get(targetId);

            if (targetCase == null)
            {
                // Skill § 3: "Unknown IDs or missing prerequisites surface as blocked, never as a different module or a false readiness result."
                if (currentCase != null)
                {
                    currentCase.Unmount();
                    currentCase = null;
                }
                contentContainer.Clear();

                statusLabel.style.display = DisplayStyle.Flex;
                statusLabel.style.backgroundColor = new Color(0.85f, 0.25f, 0.25f, 1f);
                statusLabel.style.color = Color.white;
                statusLabel.text = $"[Blocked] Acceptance target '{targetId}' is not registered or prerequisites missing.";

                var blockedView = new VisualElement();
                blockedView.style.flexGrow = 1;
                blockedView.style.alignItems = Align.Center;
                blockedView.style.justifyContent = Justify.Center;
                var msg = new Label($"Target '{targetId}' not found in registry.\nPlease register the module case via UxModuleRegistry.Register().");
                msg.style.fontSize = 14;
                msg.style.color = new Color(0.3f, 0.35f, 0.45f, 1f);
                blockedView.Add(msg);
                contentContainer.Add(blockedView);
                return;
            }

            // Clean up previous
            if (currentCase != null)
            {
                currentCase.Unmount();
                currentCase = null;
            }
            contentContainer.Clear();

            // Mount new case
            currentCase = targetCase;
            currentCase.Mount(contentContainer);

            // Update status banner
            statusLabel.style.display = DisplayStyle.Flex;
            statusLabel.style.backgroundColor = new Color(0.2f, 0.55f, 0.35f, 1f);
            statusLabel.style.color = Color.white;
            statusLabel.text = $"Active Acceptance Target: {targetCase.DisplayName} | Spec: {targetCase.ModuleSpec} | ID: {targetCase.Id}";

            // Sync dropdown without triggering duplicate change callback
            int idx = caseIds.IndexOf(targetCase.Id);
            if (idx >= 0 && moduleDropdown != null && moduleDropdown.index != idx)
            {
                moduleDropdown.SetValueWithoutNotify(caseDisplayNames[idx]);
            }
        }
    }
}

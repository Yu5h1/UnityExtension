using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Yu5h1Lib.UI.SettingsLayout;

namespace Yu5h1Lib.UI
{
    /// <summary>Composes the Config dialog and single-group Setting around the same tabs and save controls.</summary>
    public sealed class SettingsPanel : MonoBehaviour
    {
        public enum Presentation { Config, Setting, Embedded }

        public sealed class Group
        {
            public string Id { get; }
            public string Label { get; }
            public IReadOnlyList<Tabs.Item> Categories { get; }

            public Group(string id, string label, IReadOnlyList<Tabs.Item> categories)
            {
                Id = id;
                Label = label;
                Categories = categories?.ToArray();
            }
        }

        private readonly Dictionary<string, string> selectedCategories = new();
        private Group[] groups = Array.Empty<Group>();
        private RectTransform surface;
        private RectTransform dialog;
        private Text title;
        private Text intro;
        private Image dialogBackground;
        private Image backdropShade;
        private ButtonAddon opener;
        private ButtonAddon dismiss;
        private Button backdrop;
        private ButtonAddon reset;
        private CanvasGroup contentGate;
        private SaveBar.State state;
        private Presentation presentation;

        public Button Opener => opener == null ? null : opener.ui;
        /// <summary>The title bar in which the caller may place and style additional actions.</summary>
        public RectTransform HeaderActions { get; private set; }
        public GroupList Groups { get; private set; }
        public Tabs Tabs { get; private set; }
        public SaveBar SaveBar { get; private set; }
        public string ActiveGroup { get; private set; }
        public bool IsOpen => surface.gameObject.activeSelf;
        public bool CanEdit => state.Enabled && !state.Busy && !state.RetryApply;
        public event Action Opened;
        public event Action Closed;
        public event Action ResetRequested;

        public static SettingsPanel Create(RectTransform parent, Presentation presentation,
            UguiControlPalette palette = null)
        {
            palette ??= UguiControlPalette.Light;
            var root = Rect(parent, presentation.ToString());
            Stretch(root);
            var panel = root.gameObject.AddComponent<SettingsPanel>();
            panel.presentation = presentation;
            if (presentation != Presentation.Embedded)
            {
                panel.opener = MakeButton(root, "設定", "⚙");
                var rect = (RectTransform)panel.opener.transform;
                rect.anchorMin = rect.anchorMax = Vector2.one;
                rect.pivot = Vector2.one;
                rect.sizeDelta = new Vector2(38, 36);
                panel.Opener.onClick.AddListener(() => panel.Show());
            }

            panel.surface = Rect(root, "Settings surface");
            Stretch(panel.surface);
            if (presentation != Presentation.Embedded)
            {
                var overlay = Rect(panel.surface, "Backdrop");
                Stretch(overlay);
                panel.backdropShade = overlay.gameObject.AddComponent<Image>();
                panel.backdrop = overlay.gameObject.AddComponent<Button>();
                panel.backdrop.transition = Selectable.Transition.None;
                panel.backdrop.navigation = new Navigation { mode = Navigation.Mode.None };
                panel.backdrop.onClick.AddListener(() => panel.Close());
            }

            panel.dialog = Rect(panel.surface, "Settings panel");
            Stretch(panel.dialog);
            panel.dialogBackground = panel.dialog.gameObject.AddComponent<Image>();
            var layout = Layout<VerticalLayoutGroup>(panel.dialog, 8);
            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.childForceExpandWidth = true;
            panel.HeaderActions = Rect(panel.dialog, "Header");
            Layout<HorizontalLayoutGroup>(panel.HeaderActions, 4);
            panel.title = MakeText(panel.HeaderActions, "Title", "介面設定");
            panel.title.fontSize = 20;
            Size(panel.title, height: 32, flexibleWidth: 1);
            if (presentation != Presentation.Embedded)
            {
                panel.dismiss = MakeButton(panel.HeaderActions, "關閉", "×");
                panel.dismiss.ui.onClick.AddListener(() => panel.Close());
            }

            panel.intro = MakeText(panel.dialog, "Instructions", "");
            Size(panel.intro, height: 40);
            var body = Rect(panel.dialog, "Settings body");
            Size(body, flexibleHeight: 1);
            Layout<HorizontalLayoutGroup>(body, 8).childForceExpandHeight = true;
            if (presentation == Presentation.Config)
            {
                panel.Groups = GroupList.Create(body, palette);
                Size(panel.Groups, 124);
                panel.Groups.Changed += panel.SetGroup;
            }

            panel.Tabs = Tabs.Create(body, palette);
            panel.Tabs.Changed += id => panel.RememberCategory(id);
            panel.contentGate = panel.Tabs.ContentRoot.gameObject.AddComponent<CanvasGroup>();
            Size(panel.Tabs, flexibleWidth: 1);
            panel.SaveBar = SaveBar.Create(panel.dialog, palette);
            panel.reset = MakeButton(panel.SaveBar.Actions, "Reset", "恢復預設");
            panel.reset.ui.onClick.AddListener(() =>
            {
                if (panel.reset.ui.IsInteractable())
                    panel.ResetRequested?.Invoke();
            });
            panel.surface.gameObject.SetActive(presentation == Presentation.Embedded);
            panel.ApplySurfacePalette(palette);
            panel.ResizeDialog();
            panel.SetState(new SaveBar.State(autoSave: true, enabled: false));
            return panel;
        }

        /// <summary>Restyles the existing panel controls. The caller styles content and actions it supplies.</summary>
        public void ApplyPalette(UguiControlPalette palette)
        {
            if (palette == null)
                throw new ArgumentNullException(nameof(palette));

            ApplySurfacePalette(palette);
            Groups?.ApplyPalette(palette);
            Tabs.ApplyPalette(palette);
            SaveBar.ApplyPalette(palette);
        }

        private void ApplySurfacePalette(UguiControlPalette palette)
        {
            dialogBackground.color = palette.PanelBackground;
            title.color = palette.PanelHeading;
            intro.color = palette.SecondaryText;
            if (backdropShade != null)
            {
                backdropShade.color = palette.Overlay;
                palette.ApplyButton(opener.ui, opener.labelText);
                palette.ApplyButton(dismiss.ui, dismiss.labelText);
            }

            palette.ApplyButton(reset.ui, reset.labelText);
        }

        public void SetGroups(IReadOnlyList<Group> value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (presentation != Presentation.Config && value.Count > 1)
                throw new ArgumentException("Setting accepts one group.", nameof(value));

            var next = value.ToArray();
            if (next.Any(group => group == null))
                throw new ArgumentException("A settings group is missing.", nameof(value));
            var options = next.Select(group => (group.Id, group.Label)).ToArray();
            Tabs.ValidateItems(GroupList.MakeItems(options));
            var contents = new HashSet<RectTransform>();
            foreach (var group in next)
            {
                Tabs.ValidateItems(group.Categories);
                if (group.Categories.Any(tab => tab.Content != null && !contents.Add(tab.Content)))
                    throw new ArgumentException("Each group must have independent content.", nameof(value));
            }
            foreach (var group in next)
            {
                foreach (var tab in group.Categories)
                    if (tab.Content != null)
                        tab.Content.gameObject.SetActive(false);
            }
            groups = next;
            Groups?.SetGroups(options);
            foreach (var id in selectedCategories.Keys.ToArray())
            {
                if (!groups.Any(group => group.Id == id))
                    selectedCategories.Remove(id);
            }
            SetGroup(Groups?.SelectedId ?? groups.FirstOrDefault()?.Id);
        }

        public void SetState(SaveBar.State value)
        {
            state = value;
            var hasTabs = Tabs.SelectedId != null;
            SaveBar.SetState(new SaveBar.State(value.AutoSave, value.Busy, value.Enabled, value.CanSave && hasTabs,
                value.RetrySave, value.RetryApply, value.Message, value.LockMode));
            Tabs.Interactable = !value.Busy;
            if (Groups != null)
                Groups.Interactable = !value.Busy;
            contentGate.interactable = contentGate.blocksRaycasts = CanEdit;
            reset.ui.interactable = CanEdit && hasTabs;
            if (dismiss != null)
                dismiss.ui.interactable = backdrop.interactable = !value.Busy;
            intro.text = value.AutoSave
                ? "修改後自動保存，下次開啟仍會保留。"
                : "修改先保留在草稿，按套用後才保存；關閉會放棄尚未提交的修改。";
        }

        public bool Show()
        {
            if (IsOpen || state.Busy || Opener == null || !Opener.IsInteractable())
                return false;
            transform.SetAsLastSibling();
            surface.gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();
            (Tabs.ActiveButton ?? dismiss.ui).Select();
            Opened?.Invoke();
            return true;
        }

        public bool Close()
        {
            if (!IsOpen || state.Busy || presentation == Presentation.Embedded)
                return false;
            surface.gameObject.SetActive(false);
            Opener.Select();
            Closed?.Invoke();
            return true;
        }

        private void RememberCategory(string id)
        {
            if (ActiveGroup != null)
                selectedCategories[ActiveGroup] = id;
        }

        private void SetGroup(string id)
        {
            ActiveGroup = id;
            var group = Array.Find(groups, candidate => candidate.Id == id);
            if (presentation != Presentation.Config)
                title.text = group?.Label ?? "設定";
            Tabs.SetItems(group?.Categories ?? Array.Empty<Tabs.Item>());
            if (group != null && group.Categories.Count > 0)
                Tabs.Select(selectedCategories.TryGetValue(id, out var tab)
                    && group.Categories.Any(category => category.Id == tab) ? tab : group.Categories[0].Id);
            SetState(state);
        }

        private void ResizeDialog()
        {
            if (dialog == null || presentation == Presentation.Embedded)
                return;
            var size = ((RectTransform)transform).rect.size;
            dialog.anchorMin = dialog.anchorMax = new Vector2(.5f, .5f);
            dialog.sizeDelta = new Vector2(Mathf.Max(0, Mathf.Min(size.x - 20, 620)),
                Mathf.Max(0, Mathf.Min(size.y - 20, 540)));
        }

        private void OnRectTransformDimensionsChange() => ResizeDialog();

        private void Update()
        {
            if (!IsOpen || state.Busy || presentation == Presentation.Embedded)
                return;
            if (InputHandler.GetKeyDown(KeyCode.Escape))
                Close();
            else if (InputHandler.GetKeyDown(KeyCode.Tab))
                MoveFocus(InputHandler.GetKey(KeyCode.LeftShift) || InputHandler.GetKey(KeyCode.RightShift));
        }

        private void MoveFocus(bool backwards)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
                return;
            var controls = dialog.GetComponentsInChildren<Selectable>()
                .Where(control => control.IsInteractable() && control.IsActive()
                    && (control.GetComponent<TabNavigation>() == null || control == Tabs.ActiveButton
                        || control == Groups?.ActiveButton))
                .ToArray();
            if (controls.Length == 0)
                return;
            var index = Array.FindIndex(controls, control => control.gameObject == eventSystem.currentSelectedGameObject);
            if (index < 0)
                index = backwards ? 0 : -1;
            controls[(index + (backwards ? -1 : 1) + controls.Length) % controls.Length].Select();
        }
    }
}

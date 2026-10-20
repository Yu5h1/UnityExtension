using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Yu5h1Lib.UIToolkit;
using Yu5h1Lib.UIToolkit.Acceptance;

namespace Yu5h1LibTest
{
    /// <summary>
    /// One acceptance page of Yu5h1Lib.UX VisualFoundation 1.3.0 goal 1 on UI Toolkit: the page and panel surfaces, and Button
    /// and OptionPicker in every state of the state-to-role table, in one fixed scheme.
    /// </summary>
    public class UxVisualFoundationDemoCase : IUxModuleDemoCase
    {
        private const string PageStyleResource = "Yu5h1VfPage";
        private const string ButtonStyleResource = "Yu5h1Lib.UIToolkit/Yu5h1Button";

        private readonly VisualFoundationScheme scheme;
        private Texture2D iconTexture;
        private VisualElement page;

        public UxVisualFoundationDemoCase(VisualFoundationScheme scheme)
        {
            this.scheme = scheme;
        }

        public string Id => scheme == VisualFoundationScheme.Light ? "visualfoundation-light" : "visualfoundation-dark";
        public string DisplayName => "VisualFoundation (1.3.0) " + SchemeName;
        public string ModuleSpec => "Yu5h1Lib.UX/VisualFoundation";

        private string SchemeName => scheme == VisualFoundationScheme.Light ? "light" : "dark";

        public void Mount(VisualElement container)
        {
            Unmount();

            iconTexture = CreateIconTexture();
            var pageStyle = Resources.Load<StyleSheet>(PageStyleResource);
            var buttonStyle = Resources.Load<StyleSheet>(ButtonStyleResource);

            page = new ScrollView();
            page.style.flexGrow = 1;
            page.AddToClassList("vf-page");
            if (pageStyle != null) page.styleSheets.Add(pageStyle);
            VisualFoundationTheme.Apply(page, scheme);
            container.Add(page);

            page.Add(Text("Yu5h1Lib.UX VisualFoundation 1.3.0 (" + SchemeName + ")", "vf-page__heading"));
            page.Add(Text("頁面上的一般內容：itemText 在 pageBackground 上。", "vf-page__text"));
            page.Add(Text("次要文字：secondaryText。按 Tab 把鍵盤焦點移到各按鈕，看焦點外框。", "vf-page__secondary"));

            var icon = Background.FromTexture2D(iconTexture);

            var buttons = Panel("Button 的狀態（panelBackground 上，標題 panelHeading，分隔 border）");
            buttons.Add(StateRow("實心 solid", buttonStyle, icon, ghost: false));
            buttons.Add(StateRow("無底 ghost", buttonStyle, icon, ghost: true));
            page.Add(buttons);

            var picker = Panel("OptionPicker（觸發鈕與選項用 Button 的狀態；solid 面板用 panelBackground）");
            var row = new VisualElement();
            row.AddToClassList("vf-panel__row");
            row.Add(Picker(icon, OptionPickerAppearance.Solid));
            row.Add(Picker(icon, OptionPickerAppearance.Ghost));
            picker.Add(row);
            page.Add(picker);
        }

        public void Unmount()
        {
            if (page != null)
            {
                page.RemoveFromHierarchy();
                page = null;
            }
            if (iconTexture != null)
            {
                Object.Destroy(iconTexture);
                iconTexture = null;
            }
        }

        private static Label Text(string text, string className)
        {
            var label = new Label(text);
            label.AddToClassList(className);
            return label;
        }

        private static VisualElement Panel(string heading)
        {
            var panel = new VisualElement();
            panel.AddToClassList("vf-panel");
            panel.Add(Text(heading, "vf-panel__heading"));
            return panel;
        }

        private static VisualElement StateRow(string caption, StyleSheet buttonStyle, Background icon, bool ghost)
        {
            var row = new VisualElement();
            row.AddToClassList("vf-panel__row");
            row.Add(Text(caption, "vf-panel__caption"));
            row.Add(StateButton(buttonStyle, icon, ghost, "一般"));
            row.Add(StateButton(buttonStyle, icon, ghost, "滑過", hover: true));
            row.Add(StateButton(buttonStyle, icon, ghost, "選取", selected: true));
            row.Add(StateButton(buttonStyle, icon, ghost, "停用", disabled: true));
            row.Add(StateButton(buttonStyle, icon, ghost, null));
            return row;
        }

        private static Button StateButton(StyleSheet buttonStyle, Background icon, bool ghost, string label,
            bool hover = false, bool selected = false, bool disabled = false)
        {
            var button = new Button();
            if (buttonStyle != null) button.styleSheets.Add(buttonStyle);
            button.AddToClassList("yu5h1-button");
            if (ghost) button.AddToClassList("yu5h1-button--ghost");
            if (hover) button.AddToClassList("yu5h1-button--hover");
            if (selected) button.AddToClassList("yu5h1-button--selected");
            button.iconImage = icon;
            if (label != null)
            {
                button.text = label;
            }
            else
            {
                button.AddToClassList("yu5h1-button--icon-only");
                button.style.width = 32;
                button.style.minWidth = 32;
            }
            if (disabled) button.SetEnabled(false);
            return button;
        }

        private static OptionPicker Picker(Background icon, OptionPickerAppearance panelAppearance)
        {
            var picker = new OptionPicker { PanelAppearance = panelAppearance };
            picker.Model.interaction = OptionPickerInteraction.Picker;
            picker.Model.orientation = OptionPickerOrientation.Vertical;
            picker.Model.placement = OptionPickerPlacement.Adjacent;
            picker.SetOptions(new List<OptionItem>
            {
                new OptionItem("a", "選項 A", icon),
                new OptionItem("b", "選項 B", icon),
                new OptionItem("c", "選項 C（停用）", icon, disabled: true)
            }, "a");
            picker.Model.OnChosen += picker.SetValue;
            return picker;
        }

        private static Texture2D CreateIconTexture()
        {
            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    bool inside = Vector2.Distance(new Vector2(x, y), new Vector2(16, 16)) <= 11f;
                    texture.SetPixel(x, y, inside ? new Color(0.5f, 0.5f, 0.5f, 1f) : Color.clear);
                }
            }
            texture.Apply();
            return texture;
        }
    }
}

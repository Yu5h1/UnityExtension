using UnityEngine;
using UnityEngine.UIElements;
using Yu5h1Lib.UIToolkit.Acceptance;

namespace Yu5h1LibTest
{
    public class UxButtonDemoCase : IUxModuleDemoCase
    {
        private const string ButtonStyleResource = "Yu5h1Lib.UIToolkit/Yu5h1Button";

        public string Id => "button";
        public string DisplayName => "Button (2.0.0)";
        public string ModuleSpec => "Yu5h1Lib.UX/Button";

        private Texture2D iconTexture;
        private StyleSheet buttonStyle;
        private ScrollView containerScrollView;

        public void Mount(VisualElement container)
        {
            Unmount();

            buttonStyle = Resources.Load<StyleSheet>(ButtonStyleResource);
            iconTexture = CreateDarkIconTexture();

            containerScrollView = new ScrollView();
            containerScrollView.style.flexGrow = 1;
            containerScrollView.style.paddingLeft = 24;
            containerScrollView.style.paddingRight = 24;
            containerScrollView.style.paddingTop = 24;
            containerScrollView.style.paddingBottom = 24;
            containerScrollView.style.backgroundColor = new Color(0.94f, 0.94f, 0.96f, 1f);
            container.Add(containerScrollView);

            var title = new Label("Native Button Demo (Yu5h1Lib.UX Button 2.0.0)");
            title.style.fontSize = 20;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = new Color(0.12f, 0.14f, 0.18f, 1f);
            title.style.marginBottom = 16;
            containerScrollView.Add(title);

            var icon = Background.FromTexture2D(iconTexture);

            // 1. 外觀：實心、無底各一顆（圖示加名稱）
            var sectionAppearance = CreateSection("外觀 (Appearance)", containerScrollView);
            var solidBtn = CreateButton(icon, "Solid", isGhost: false);
            var ghostBtn = CreateButton(icon, "Ghost", isGhost: true);
            sectionAppearance.Add(solidBtn);
            sectionAppearance.Add(ghostBtn);

            // 2. 呈現：圖示+文字、純圖示、純文字
            var sectionDisplay = CreateSection("呈現 (Display Combinations)", containerScrollView);
            var withIconText = CreateButton(icon, "Icon + Label");
            var iconOnly = CreateButton(icon, "Icon Only", isIconOnly: true);
            var textOnly = CreateButton(default, "Label Only");
            sectionDisplay.Add(withIconText);
            sectionDisplay.Add(iconOnly);
            sectionDisplay.Add(textOnly);

            // 3. 尺寸：大 (40px)、預設 (32px)、小 (24px)
            var sectionSize = CreateSection("尺寸 (Sizes: 40px, 32px, 24px)", containerScrollView);
            var largeBtn = CreateButton(icon, "Large", size: 40);
            var defaultBtn = CreateButton(icon, "Default", size: 32);
            var smallBtn = CreateButton(icon, "Small", size: 24);
            sectionSize.Add(largeBtn);
            sectionSize.Add(defaultBtn);
            sectionSize.Add(smallBtn);

            // 純圖示不同尺寸驗證方形與 75% 圖示比例
            var sectionIconSize = CreateSection("純圖示尺寸 (Square & 75% Icon Ratio)", containerScrollView);
            var iconLarge = CreateButton(icon, "L", isIconOnly: true, size: 40);
            var iconDefault = CreateButton(icon, "D", isIconOnly: true, size: 32);
            var iconSmall = CreateButton(icon, "S", isIconOnly: true, size: 24);
            sectionIconSize.Add(iconLarge);
            sectionIconSize.Add(iconDefault);
            sectionIconSize.Add(iconSmall);

            // 4. 狀態：預設、選中 (Selected)、停用 (Disabled)
            var sectionStates = CreateSection("狀態 (States: Default, Selected, Disabled)", containerScrollView);
            var normalStateBtn = CreateButton(icon, "Normal");
            var selectedSolidBtn = CreateButton(icon, "Selected (Solid)", isSelected: true);
            var selectedGhostBtn = CreateButton(icon, "Selected (Ghost)", isGhost: true, isSelected: true);
            var disabledBtn = CreateButton(icon, "Disabled");
            disabledBtn.SetEnabled(false);
            sectionStates.Add(normalStateBtn);
            sectionStates.Add(selectedSolidBtn);
            sectionStates.Add(selectedGhostBtn);
            sectionStates.Add(disabledBtn);

            // 5. 互動測試反饋區
            var feedbackSection = CreateSection("互動測試 (Click to toggle selection & log)", containerScrollView);
            var toggleBtn = CreateButton(icon, "Click Me to Toggle");
            var logLabel = new Label("Last clicked: None");
            logLabel.style.marginLeft = 16;
            logLabel.style.alignSelf = Align.Center;
            logLabel.style.color = new Color(0.3f, 0.35f, 0.4f, 1f);

            toggleBtn.clicked += () =>
            {
                if (toggleBtn.ClassListContains("yu5h1-button--selected"))
                {
                    toggleBtn.RemoveFromClassList("yu5h1-button--selected");
                    logLabel.text = $"Last clicked: Button Unselected at {System.DateTime.Now:HH:mm:ss}";
                }
                else
                {
                    toggleBtn.AddToClassList("yu5h1-button--selected");
                    logLabel.text = $"Last clicked: Button Selected at {System.DateTime.Now:HH:mm:ss}";
                }
            };

            feedbackSection.Add(toggleBtn);
            feedbackSection.Add(logLabel);
        }

        public void Unmount()
        {
            if (containerScrollView != null)
            {
                containerScrollView.RemoveFromHierarchy();
                containerScrollView = null;
            }
            if (iconTexture != null)
            {
                Object.Destroy(iconTexture);
                iconTexture = null;
            }
        }

        private VisualElement CreateSection(string titleText, VisualElement parent)
        {
            var header = new Label(titleText);
            header.style.fontSize = 14;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.color = new Color(0.25f, 0.28f, 0.35f, 1f);
            header.style.marginTop = 12;
            header.style.marginBottom = 6;
            parent.Add(header);

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 12;
            parent.Add(row);

            return row;
        }

        private Button CreateButton(
            Background icon,
            string label,
            bool isGhost = false,
            bool isIconOnly = false,
            bool isSelected = false,
            float size = 32)
        {
            var btn = new Button();
            if (buttonStyle != null)
            {
                btn.styleSheets.Add(buttonStyle);
            }
            btn.AddToClassList("yu5h1-button");

            if (isGhost)
            {
                btn.AddToClassList("yu5h1-button--ghost");
            }

            if (isSelected)
            {
                btn.AddToClassList("yu5h1-button--selected");
            }

            if (icon.texture != null)
            {
                btn.iconImage = icon;
            }

            if (!isIconOnly && !string.IsNullOrEmpty(label))
            {
                btn.text = label;
            }
            else
            {
                btn.text = string.Empty;
            }

            if (isIconOnly)
            {
                btn.AddToClassList("yu5h1-button--icon-only");
                btn.style.width = size;
                btn.style.minWidth = size;
            }

            btn.style.height = size;
            btn.style.minHeight = size;
            btn.style.marginRight = 12;

            return btn;
        }

        private Texture2D CreateDarkIconTexture()
        {
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            var darkColor = new Color(0.18f, 0.20f, 0.24f, 1f);
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    bool isCircle = Vector2.Distance(new Vector2(x, y), new Vector2(16, 16)) <= 11f;
                    tex.SetPixel(x, y, isCircle ? darkColor : Color.clear);
                }
            }
            tex.Apply();
            return tex;
        }
    }
}

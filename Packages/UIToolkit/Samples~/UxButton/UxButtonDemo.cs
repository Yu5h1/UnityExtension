using UnityEngine;
using UnityEngine.UIElements;
using Yu5h1Lib.UIToolkit;

namespace Yu5h1LibTest
{
    /// <summary>
    /// Minimal visual sample for Yu5h1Lib.UX Button (UxButton) under the default runtime theme.
    /// Demonstrates appearances, display combinations, sizes, and selection states.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class UxButtonDemo : MonoBehaviour
    {
        private UIDocument document;
        private PanelSettings settings;
        private Texture2D iconTexture;

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

            iconTexture = CreateDarkIconTexture();
            BuildUI(document.rootVisualElement);
        }

        private void OnDestroy()
        {
            if (iconTexture != null)
            {
                Destroy(iconTexture);
                iconTexture = null;
            }
        }

        private void BuildUI(VisualElement root)
        {
            root.Clear();

            var container = new ScrollView();
            container.style.flexGrow = 1;
            container.style.paddingLeft = 24;
            container.style.paddingRight = 24;
            container.style.paddingTop = 24;
            container.style.paddingBottom = 24;
            container.style.backgroundColor = new Color(0.94f, 0.94f, 0.96f, 1f);
            root.Add(container);

            var title = new Label("UxButton Demo (Yu5h1Lib.UX Button 1.1.0)");
            title.style.fontSize = 20;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = new Color(0.12f, 0.14f, 0.18f, 1f);
            title.style.marginBottom = 16;
            container.Add(title);

            var icon = Background.FromTexture2D(iconTexture);

            // 1. 外觀：實心、無底各一顆（圖示加名稱）
            var sectionAppearance = CreateSection("外觀 (Appearance)", container);
            var solidBtn = CreateButton(icon, "Solid", appearance: ButtonAppearance.Solid);
            var ghostBtn = CreateButton(icon, "Ghost", appearance: ButtonAppearance.Ghost);
            sectionAppearance.Add(solidBtn);
            sectionAppearance.Add(ghostBtn);

            // 2. 顯示組合：四顆，分別是圖示加名稱、只顯示圖示、只顯示名稱、兩者都關（應該仍顯示名稱）；
            //    另加一顆沒有圖示但 ShowLabel=false 的（也應該顯示名稱）。
            var sectionDisplay = CreateSection("顯示組合 (Display Combinations)", container);
            var btnIconLabel = CreateButton(icon, "Icon + Label", showIcon: true, showLabel: true);
            var btnIconOnly = CreateButton(icon, "Icon Only", showIcon: true, showLabel: false);
            var btnLabelOnly = CreateButton(icon, "Label Only", showIcon: false, showLabel: true);
            var btnBothOff = CreateButton(icon, "Both Off (Shows Label)", showIcon: false, showLabel: false);
            var btnNoIconLabelOff = CreateButton(default, "No Icon, Label Off (Shows Label)", showIcon: true, showLabel: false);
            sectionDisplay.Add(btnIconLabel);
            sectionDisplay.Add(btnIconOnly);
            sectionDisplay.Add(btnLabelOnly);
            sectionDisplay.Add(btnBothOff);
            sectionDisplay.Add(btnNoIconLabelOff);

            // 3. 大小：size 24、32、40，每種各一顆只顯示圖示、一顆圖示加名稱。
            var sectionSize = CreateSection("大小 (Size: 24, 32, 40)", container);
            var s24Icon = CreateButton(icon, "24", size: 24, showIcon: true, showLabel: false);
            var s24Both = CreateButton(icon, "Size 24", size: 24, showIcon: true, showLabel: true);
            var s32Icon = CreateButton(icon, "32", size: 32, showIcon: true, showLabel: false);
            var s32Both = CreateButton(icon, "Size 32", size: 32, showIcon: true, showLabel: true);
            var s40Icon = CreateButton(icon, "40", size: 40, showIcon: true, showLabel: false);
            var s40Both = CreateButton(icon, "Size 40", size: 40, showIcon: true, showLabel: true);
            sectionSize.Add(s24Icon);
            sectionSize.Add(s24Both);
            sectionSize.Add(s32Icon);
            sectionSize.Add(s32Both);
            sectionSize.Add(s40Icon);
            sectionSize.Add(s40Both);

            // 4. 選中：實心與無底各一顆 Selected=true，旁邊各放一顆未選中的對照。
            var sectionSelected = CreateSection("選中 (Selected State)", container);
            var solidNormal = CreateButton(icon, "Solid Normal", appearance: ButtonAppearance.Solid, selected: false);
            var solidSelected = CreateButton(icon, "Solid Selected", appearance: ButtonAppearance.Solid, selected: true);
            var ghostNormal = CreateButton(icon, "Ghost Normal", appearance: ButtonAppearance.Ghost, selected: false);
            var ghostSelected = CreateButton(icon, "Ghost Selected", appearance: ButtonAppearance.Ghost, selected: true);
            sectionSelected.Add(solidNormal);
            sectionSelected.Add(solidSelected);
            sectionSelected.Add(ghostNormal);
            sectionSelected.Add(ghostSelected);
        }

        private static VisualElement CreateSection(string titleText, VisualElement parent)
        {
            var header = new Label(titleText);
            header.style.fontSize = 13;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.color = new Color(0.25f, 0.28f, 0.35f, 1f);
            header.style.marginTop = 14;
            header.style.marginBottom = 6;
            parent.Add(header);

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.flexWrap = Wrap.Wrap;
            row.style.marginBottom = 6;
            parent.Add(row);

            return row;
        }

        private static UxButton CreateButton(
            Background icon,
            string labelText,
            ButtonAppearance appearance = ButtonAppearance.Solid,
            float size = ButtonDesign.Size,
            bool showIcon = true,
            bool showLabel = true,
            bool selected = false)
        {
            var btn = new UxButton
            {
                Appearance = appearance,
                Size = size,
                Icon = icon,
                ShowIcon = showIcon,
                ShowLabel = showLabel,
                LabelText = labelText,
                Selected = selected
            };
            btn.style.marginRight = 10;
            btn.style.marginTop = 4;
            btn.style.marginBottom = 4;
            btn.clicked += () => { btn.Selected = !btn.Selected; };
            return btn;
        }

        private static Texture2D CreateDarkIconTexture()
        {
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "UxButtonDarkIcon",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var dark = new Color(0.18f, 0.20f, 0.24f, 1f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    if (px >= 9f && px <= 23f)
                    {
                        float progress = (px - 9f) / (23f - 9f);
                        float halfHeight = (1f - progress) * 7.5f;
                        float dy = Mathf.Abs(py - 16f);
                        float alpha = Mathf.Clamp01(halfHeight - dy + 0.5f);
                        tex.SetPixel(x, y, new Color(dark.r, dark.g, dark.b, dark.a * alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            return tex;
        }
    }
}

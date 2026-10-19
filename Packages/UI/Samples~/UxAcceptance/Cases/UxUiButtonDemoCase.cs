using UnityEngine;
using UnityEngine.UI;

namespace Yu5h1Lib.UI.Acceptance
{
    /// <summary>
    /// Shared acceptance demo case for Yu5h1Lib.UX Button (2.0.0) in Yu5h1Lib.UI (UGUI).
    /// </summary>
    public class UxUiButtonDemoCase : IUxUiModuleDemoCase
    {
        public string Id => "button";
        public string DisplayName => "Button (2.0.0)";
        public string ModuleSpec => "Yu5h1Lib.UX/Button";

        private GameObject _rootPanel;
        private Texture2D _iconTexture;
        private Sprite _iconSprite;

        public void Mount(RectTransform container)
        {
            Unmount();

            if (container == null)
            {
                return;
            }

            _iconTexture = CreateDarkIconTexture();
            _iconSprite = Sprite.Create(_iconTexture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 100f);

            _rootPanel = new GameObject(
                "ButtonDemoPanel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(VerticalLayoutGroup));
            _rootPanel.transform.SetParent(container, false);

            var rootRect = _rootPanel.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            var bg = _rootPanel.GetComponent<Image>();
            bg.color = new Color(0.94f, 0.94f, 0.96f, 1f);

            var vLayout = _rootPanel.GetComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(24, 24, 20, 20);
            vLayout.spacing = 10f;
            vLayout.childAlignment = TextAnchor.UpperLeft;
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = true;
            vLayout.childForceExpandWidth = true;
            vLayout.childForceExpandHeight = false;

            CreateText(
                _rootPanel.transform,
                "Title",
                "UGUI Button Demo (Yu5h1Lib.UX Button 2.0.0)",
                18,
                FontStyle.Bold,
                new Color(0.12f, 0.14f, 0.18f, 1f));

            var rowAppearance = CreateSection("1. 外觀 (Appearance: Solid / Ghost)", _rootPanel.transform);
            ButtonAddon.Create(rowAppearance, _iconSprite, "Solid", ButtonAppearance.Solid);
            ButtonAddon.Create(rowAppearance, _iconSprite, "Ghost", ButtonAppearance.Ghost);

            var rowDisplay = CreateSection("2. 呈現 (Display Combinations & Non-empty Fallback)", _rootPanel.transform);
            ButtonAddon.Create(rowDisplay, _iconSprite, "Icon + Label", ButtonAppearance.Solid, showIcon: true, showLabel: true);
            ButtonAddon.Create(rowDisplay, _iconSprite, "Icon Only", ButtonAppearance.Solid, showIcon: true, showLabel: false);
            ButtonAddon.Create(rowDisplay, _iconSprite, "Label Only", ButtonAppearance.Solid, showIcon: false, showLabel: true);
            ButtonAddon.Create(rowDisplay, _iconSprite, "Fallback (Both Off)", ButtonAppearance.Solid, showIcon: false, showLabel: false);

            var rowSize = CreateSection("3. 尺寸 (Sizes: 40px, 32px, 24px)", _rootPanel.transform);
            ButtonAddon.Create(rowSize, _iconSprite, "Large (40)", ButtonAppearance.Solid, size: 40f);
            ButtonAddon.Create(rowSize, _iconSprite, "Default (32)", ButtonAppearance.Solid, size: 32f);
            ButtonAddon.Create(rowSize, _iconSprite, "Small (24)", ButtonAppearance.Solid, size: 24f);

            var rowIconSize = CreateSection("4. 純圖示尺寸 (Square & 75% Icon Ratio)", _rootPanel.transform);
            ButtonAddon.Create(rowIconSize, _iconSprite, "L", ButtonAppearance.Solid, showIcon: true, showLabel: false, size: 40f);
            ButtonAddon.Create(rowIconSize, _iconSprite, "D", ButtonAppearance.Solid, showIcon: true, showLabel: false, size: 32f);
            ButtonAddon.Create(rowIconSize, _iconSprite, "S", ButtonAppearance.Solid, showIcon: true, showLabel: false, size: 24f);

            var rowStates = CreateSection("5. 狀態與互動 (Normal, Selected, Disabled, Click to Toggle)", _rootPanel.transform);
            ButtonAddon.Create(rowStates, _iconSprite, "Normal", ButtonAppearance.Solid);
            ButtonAddon.Create(rowStates, _iconSprite, "Selected (Solid)", ButtonAppearance.Solid, isSelected: true);
            ButtonAddon.Create(rowStates, _iconSprite, "Selected (Ghost)", ButtonAppearance.Ghost, isSelected: true);
            var disabledBtn = ButtonAddon.Create(rowStates, _iconSprite, "Disabled", ButtonAppearance.Solid);
            disabledBtn.ui.interactable = false;

            var toggleBtn = ButtonAddon.Create(rowStates, _iconSprite, "Click Me to Toggle", ButtonAppearance.Solid);
            var logText = CreateText(
                rowStates,
                "ToggleLog",
                "Last clicked: None",
                13,
                FontStyle.Normal,
                new Color(0.30f, 0.35f, 0.40f, 1f));

            toggleBtn.ui.onClick.AddListener(() =>
            {
                toggleBtn.isSelected = !toggleBtn.isSelected;
                string state = toggleBtn.isSelected ? "Selected" : "Unselected";
                logText.text = $"Last clicked: {state} at {System.DateTime.Now:HH:mm:ss}";
            });

            LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
        }

        public void Unmount()
        {
            if (_rootPanel != null)
            {
                DestroyObject(_rootPanel);
                _rootPanel = null;
            }
            if (_iconSprite != null)
            {
                DestroyObject(_iconSprite);
                _iconSprite = null;
            }
            if (_iconTexture != null)
            {
                DestroyObject(_iconTexture);
                _iconTexture = null;
            }
        }

        private static RectTransform CreateSection(string title, Transform parent)
        {
            CreateText(parent, "SectionHeader", title, 14, FontStyle.Bold, new Color(0.25f, 0.28f, 0.35f, 1f));

            var rowGo = new GameObject(
                "Row",
                typeof(RectTransform),
                typeof(HorizontalLayoutGroup),
                typeof(LayoutElement));
            rowGo.transform.SetParent(parent, false);

            var hLayout = rowGo.GetComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 12f;
            hLayout.childAlignment = TextAnchor.MiddleLeft;
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = true;
            hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight = false;

            var rowLayout = rowGo.GetComponent<LayoutElement>();
            rowLayout.minHeight = 44f;
            rowLayout.preferredHeight = 44f;

            return rowGo.GetComponent<RectTransform>();
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string content,
            int fontSize,
            FontStyle fontStyle,
            Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var txt = go.GetComponent<Text>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            txt.font = font;
            txt.fontSize = fontSize;
            txt.fontStyle = fontStyle;
            txt.color = color;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.text = content;
            return txt;
        }

        private static Texture2D CreateDarkIconTexture()
        {
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            var darkColor = new Color(0.18f, 0.20f, 0.24f, 1f);
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    bool isCircle = Vector2.Distance(new Vector2(x, y), new Vector2(16f, 16f)) <= 11f;
                    tex.SetPixel(x, y, isCircle ? darkColor : Color.clear);
                }
            }
            tex.Apply();
            return tex;
        }

        private static void DestroyObject(Object obj)
        {
            if (obj == null)
            {
                return;
            }
            if (Application.isPlaying)
            {
                Object.Destroy(obj);
            }
            else
            {
                Object.DestroyImmediate(obj);
            }
        }
    }
}

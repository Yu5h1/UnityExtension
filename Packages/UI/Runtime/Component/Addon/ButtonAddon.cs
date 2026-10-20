using UnityEngine;
using UnityEngine.UI;

namespace Yu5h1Lib.UI
{
    /// <summary>
    /// Appearance variants defined by Yu5h1Lib.UX Button (BTN-APPEARANCE).
    /// </summary>
    public enum ButtonAppearance
    {
        Solid = 0,
        Ghost = 1
    }

    /// <summary>
    /// UGUI addon for <see cref="Button"/> implementing Yu5h1Lib.UX Button 2.0.0 rules
    /// (BTN-DISPLAY, BTN-APPEARANCE, BTN-SIZE, BTN-LAYOUT) where native UGUI components
    /// do not provide built-in support.
    /// </summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Button)), AddonFor(typeof(Button))]
    public class ButtonAddon : UIControl<Button>
    {
        public const float DefaultSize = 32f;
        public const float IconRatio = 0.75f;
        public const float IconLabelGap = 8f;
        private const int HorizontalPadding = 12;

        [SerializeField] private bool _showIcon = true;
        [SerializeField] private bool _showLabel = true;
        [SerializeField] private ButtonAppearance _appearance = ButtonAppearance.Solid;
        [SerializeField] private float _size = DefaultSize;
        [SerializeField] private bool _isSelected;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Text _labelText;

        public bool showIcon
        {
            get => _showIcon;
            set
            {
                _showIcon = value;
                Apply();
            }
        }

        public bool showLabel
        {
            get => _showLabel;
            set
            {
                _showLabel = value;
                Apply();
            }
        }

        public ButtonAppearance appearance
        {
            get => _appearance;
            set
            {
                _appearance = value;
                Apply();
            }
        }

        public float size
        {
            get => _size;
            set
            {
                _size = value;
                Apply();
            }
        }

        public bool isSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                Apply();
            }
        }

        public Image iconImage
        {
            get => _iconImage;
            set
            {
                _iconImage = value;
                Apply();
            }
        }

        public Text labelText
        {
            get => _labelText;
            set
            {
                _labelText = value;
                Apply();
            }
        }

        public Sprite icon
        {
            get => _iconImage != null ? _iconImage.sprite : null;
            set
            {
                EnsureChildReferences();
                if (_iconImage != null)
                {
                    _iconImage.sprite = value;
                }
                Apply();
            }
        }

        public string label
        {
            get => _labelText != null ? _labelText.text : string.Empty;
            set
            {
                EnsureChildReferences();
                if (_labelText != null)
                {
                    _labelText.text = value ?? string.Empty;
                }
                Apply();
            }
        }

        protected override void OnInitializing()
        {
            base.OnInitializing();
            EnsureChildReferences();
            Apply();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureChildReferences();
            if (!Application.isPlaying)
            {
                UnityEditor.EditorApplication.delayCall -= DelayApply;
                UnityEditor.EditorApplication.delayCall += DelayApply;
                return;
            }
            Apply();
        }

        private void DelayApply()
        {
            if (this != null)
            {
                Apply();
            }
        }
#else
        private void OnValidate()
        {
            EnsureChildReferences();
            Apply();
        }
#endif

        public void InvokeOnClick()
        {
            ui.onClick?.Invoke();
        }

        /// <summary>
        /// Applies BTN-DISPLAY, BTN-SIZE, BTN-LAYOUT, and BTN-APPEARANCE rules to the underlying UGUI components.
        /// </summary>
        public void Apply()
        {
            EnsureChildReferences();

            var rootRect = rectTransform != null ? rectTransform : GetComponent<RectTransform>();
            if (rootRect == null)
            {
                return;
            }

            float resolvedSize = _size > 0f ? _size : DefaultSize;
            float iconEdge = resolvedSize * IconRatio;

            bool hasIconSprite = _iconImage != null && _iconImage.sprite != null;
            bool visibleIcon = _showIcon && hasIconSprite;
            bool visibleLabel = _showLabel || !visibleIcon;
            bool isIconOnly = visibleIcon && !visibleLabel;

            if (_iconImage != null)
            {
                _iconImage.gameObject.SetActive(visibleIcon);
                _iconImage.preserveAspect = true;
                var iconRect = _iconImage.rectTransform;
                iconRect.sizeDelta = new Vector2(iconEdge, iconEdge);

                var iconLayout = _iconImage.GetComponent<LayoutElement>();
                if (iconLayout == null)
                {
                    iconLayout = _iconImage.gameObject.AddComponent<LayoutElement>();
                }
                iconLayout.minWidth = iconEdge;
                iconLayout.preferredWidth = iconEdge;
                iconLayout.minHeight = iconEdge;
                iconLayout.preferredHeight = iconEdge;
                iconLayout.flexibleWidth = 0f;
                iconLayout.flexibleHeight = 0f;
            }

            if (_labelText != null)
            {
                _labelText.gameObject.SetActive(visibleLabel);
            }

            var layoutGroup = GetComponent<HorizontalLayoutGroup>();
            if (layoutGroup == null)
            {
                layoutGroup = gameObject.AddComponent<HorizontalLayoutGroup>();
            }
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.spacing = IconLabelGap;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            int padH = isIconOnly ? 0 : HorizontalPadding;
            layoutGroup.padding = new RectOffset(padH, padH, 0, 0);

            var rootLayout = GetComponent<LayoutElement>();
            if (rootLayout == null)
            {
                rootLayout = gameObject.AddComponent<LayoutElement>();
            }
            rootLayout.minHeight = resolvedSize;
            rootLayout.preferredHeight = resolvedSize;
            rootLayout.flexibleHeight = 0f;
            rootLayout.minWidth = isIconOnly ? resolvedSize : -1f;
            rootLayout.preferredWidth = isIconOnly ? resolvedSize : -1f;
            rootLayout.flexibleWidth = 0f;

            ApplyAppearance();

            rootRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, resolvedSize);
            if (isIconOnly)
            {
                rootRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, resolvedSize);
            }
            else
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
                float preferredWidth = Mathf.Max(resolvedSize, LayoutUtility.GetPreferredWidth(rootRect));
                rootRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, preferredWidth);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
        }

        private void EnsureChildReferences()
        {
            if (_iconImage == null)
            {
                var iconTransform = transform.Find("Icon");
                if (iconTransform != null)
                {
                    _iconImage = iconTransform.GetComponent<Image>();
                }
            }

            if (_labelText == null)
            {
                var labelTransform = transform.Find("Label");
                if (labelTransform != null)
                {
                    _labelText = labelTransform.GetComponent<Text>();
                }
            }
        }

        private void ApplyAppearance()
        {
            if (ui == null)
            {
                return;
            }

            if (ui.targetGraphic != null)
            {
                ui.targetGraphic.color = Color.white;
            }

            var colors = ui.colors;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;

            if (_appearance == ButtonAppearance.Ghost)
            {
                if (_isSelected)
                {
                    colors.normalColor = new Color(0.72f, 0.80f, 0.94f, 0.55f);
                    colors.highlightedColor = new Color(0.66f, 0.75f, 0.92f, 0.75f);
                    colors.pressedColor = new Color(0.60f, 0.70f, 0.88f, 0.90f);
                    colors.selectedColor = new Color(0.72f, 0.80f, 0.94f, 0.55f);
                    colors.disabledColor = new Color(0.72f, 0.80f, 0.94f, 0.25f);
                }
                else
                {
                    colors.normalColor = new Color(0.82f, 0.84f, 0.88f, 0f);
                    colors.highlightedColor = new Color(0.82f, 0.84f, 0.88f, 0.65f);
                    colors.pressedColor = new Color(0.74f, 0.77f, 0.82f, 0.85f);
                    colors.selectedColor = new Color(0.82f, 0.84f, 0.88f, 0.65f);
                    colors.disabledColor = new Color(0.82f, 0.84f, 0.88f, 0f);
                }
            }
            else
            {
                if (_isSelected)
                {
                    colors.normalColor = new Color(0.72f, 0.80f, 0.94f, 1f);
                    colors.highlightedColor = new Color(0.66f, 0.75f, 0.92f, 1f);
                    colors.pressedColor = new Color(0.60f, 0.70f, 0.88f, 1f);
                    colors.selectedColor = new Color(0.72f, 0.80f, 0.94f, 1f);
                    colors.disabledColor = new Color(0.72f, 0.80f, 0.94f, 0.45f);
                }
                else
                {
                    colors.normalColor = new Color(0.89f, 0.90f, 0.92f, 1f);
                    colors.highlightedColor = new Color(0.82f, 0.84f, 0.88f, 1f);
                    colors.pressedColor = new Color(0.74f, 0.77f, 0.82f, 1f);
                    colors.selectedColor = new Color(0.82f, 0.84f, 0.88f, 1f);
                    colors.disabledColor = new Color(0.89f, 0.90f, 0.92f, 0.45f);
                }
            }

            ui.colors = colors;
        }

#if UNITY_EDITOR
        public const string SolidPrefabPath = "Packages/com.yu5h1.ui/Runtime/Prefabs/Button.prefab";
        public const string GhostPrefabPath = "Packages/com.yu5h1.ui/Runtime/Prefabs/Button-Ghost.prefab";
        private const string FallbackSolidPath = "Packages/UI/Runtime/Prefabs/Button.prefab";
        private const string FallbackGhostPath = "Packages/UI/Runtime/Prefabs/Button-Ghost.prefab";
#endif

        /// <summary>
        /// Creates or instantiates a UGUI <see cref="Button"/> hierarchy wired with <see cref="ButtonAddon"/>.
        /// In Editor mode, instantiates the delivered Prefab asset (<see cref="SolidPrefabPath"/> / <see cref="GhostPrefabPath"/>)
        /// to guarantee true asset parity and minimal token cost.
        /// </summary>
        public static ButtonAddon Create(
            Transform parent,
            Sprite icon = null,
            string label = "Button",
            ButtonAppearance appearance = ButtonAppearance.Solid,
            bool showIcon = true,
            bool showLabel = true,
            bool isSelected = false,
            float size = DefaultSize)
        {
#if UNITY_EDITOR
            string targetPath = appearance == ButtonAppearance.Ghost ? GhostPrefabPath : SolidPrefabPath;
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(targetPath);
            if (prefab == null)
            {
                string fallback = appearance == ButtonAppearance.Ghost ? FallbackGhostPath : FallbackSolidPath;
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(fallback);
            }
            if (prefab != null)
            {
                var instance = Object.Instantiate(prefab, parent);
                instance.name = appearance == ButtonAppearance.Ghost ? "Button-Ghost" : "Button";
                var prefabAddon = instance.GetComponent<ButtonAddon>();
                if (prefabAddon != null)
                {
                    prefabAddon.appearance = appearance;
                    prefabAddon.icon = icon;
                    prefabAddon.label = label;
                    prefabAddon.showIcon = showIcon;
                    prefabAddon.showLabel = showLabel;
                    prefabAddon.isSelected = isSelected;
                    prefabAddon.size = size;
                    prefabAddon.Apply();
                    return prefabAddon;
                }
            }
#endif
            var go = new GameObject(
                "Button",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(HorizontalLayoutGroup),
                typeof(LayoutElement),
                typeof(ButtonAddon));

            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            var bgImage = go.GetComponent<Image>();
            bgImage.color = Color.white;
            bgImage.raycastTarget = true;

            var button = go.GetComponent<Button>();
            button.targetGraphic = bgImage;

            var iconGo = new GameObject(
                "Icon",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement));
            iconGo.transform.SetParent(go.transform, false);
            var iconImg = iconGo.GetComponent<Image>();
            iconImg.sprite = icon;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            var labelGo = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var labelTxt = labelGo.GetComponent<Text>();
            labelTxt.font = GetDefaultFont();
            labelTxt.fontSize = 14;
            labelTxt.alignment = TextAnchor.MiddleLeft;
            labelTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            labelTxt.verticalOverflow = VerticalWrapMode.Overflow;
            labelTxt.color = new Color(0.12f, 0.14f, 0.18f, 1f);
            labelTxt.raycastTarget = false;
            labelTxt.text = label ?? string.Empty;

            var addon = go.GetComponent<ButtonAddon>();
            addon._iconImage = iconImg;
            addon._labelText = labelTxt;
            addon._appearance = appearance;
            addon._showIcon = showIcon;
            addon._showLabel = showLabel;
            addon._isSelected = isSelected;
            addon._size = size;
            addon.Init();
            addon.Apply();

            return addon;
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

using UnityEngine;
using UnityEngine.UIElements;

namespace Yu5h1Lib.UIToolkit
{
    /// <summary>Appearance of a <see cref="UxButton"/> at rest (UX BTN-APPEARANCE).</summary>
    public enum ButtonAppearance
    {
        /// <summary>Has a background at rest.</summary>
        Solid,
        /// <summary>No background at rest; hover, focus and selection still show.</summary>
        Ghost,
    }

    /// <summary>
    /// The Yu5h1Lib.UX Button for UI Toolkit: a native <see cref="Button"/> that shows a host-provided icon
    /// and a label by the BTN-DISPLAY rules, sizes itself and its icon from <see cref="Size"/> (BTN-SIZE),
    /// puts the icon before the label (BTN-LAYOUT) and switches between solid and ghost (BTN-APPEARANCE).
    /// <para>
    /// Pressing and keyboard activation are the native button's. The label is a child <see cref="Label"/>,
    /// so the inherited <c>text</c> stays empty; set <see cref="LabelText"/> instead. Colours come from the
    /// Unity default theme's button variables until VisualFoundation is adopted in this package.
    /// </para>
    /// </summary>
    [UxmlElement]
    public partial class UxButton : Button
    {
        /// <summary>USS class of the button.</summary>
        public new const string ussClassName = "yu5h1-button";
        /// <summary>USS class of the icon element.</summary>
        public const string iconElementUssClassName = ussClassName + "__icon";
        /// <summary>USS class of the label element.</summary>
        public const string labelElementUssClassName = ussClassName + "__label";
        /// <summary>USS class while the appearance is <see cref="ButtonAppearance.Ghost"/>.</summary>
        public const string ghostUssClassName = ussClassName + "--ghost";
        /// <summary>USS class while <see cref="Selected"/> is true.</summary>
        public const string selectedUssClassName = ussClassName + "--selected";
        /// <summary>USS class while only the icon is shown.</summary>
        public const string iconOnlyElementUssClassName = ussClassName + "--icon-only";

        private const string StyleSheetResource = "Yu5h1Lib.UIToolkit/UxButton";

        private readonly VisualElement iconElement = new VisualElement();
        private readonly Label labelElement = new Label();
        private Background icon;
        private bool showIcon = true, showLabel = true, selected;
        private ButtonAppearance appearance;
        private float size = ButtonDesign.Size;
        private float labelFontSize;

        /// <summary>Creates a solid button of the default size with neither icon nor label.</summary>
        public UxButton()
        {
            AddToClassList(ussClassName);
            iconElement.AddToClassList(iconElementUssClassName);
            iconElement.pickingMode = PickingMode.Ignore;
            labelElement.AddToClassList(labelElementUssClassName);
            labelElement.pickingMode = PickingMode.Ignore;
            Add(iconElement);
            Add(labelElement);

            var sheet = Resources.Load<StyleSheet>(StyleSheetResource);
            if (sheet != null) styleSheets.Add(sheet);

            labelElement.RegisterCallback<GeometryChangedEvent>(OnLabelGeometryChanged);
            Refresh();
        }

        /// <summary>The icon the host provides: a texture, sprite, render texture or vector image.</summary>
        public Background Icon
        {
            get => icon;
            set { icon = value; Refresh(); }
        }

        /// <summary>The label, which is also the button's name.</summary>
        [UxmlAttribute]
        public string LabelText
        {
            get => labelElement.text;
            set { labelElement.text = value; Refresh(); }
        }

        /// <summary>Whether to show the icon when there is one (BTN-DISPLAY <c>icon</c>).</summary>
        [UxmlAttribute]
        public bool ShowIcon
        {
            get => showIcon;
            set { showIcon = value; Refresh(); }
        }

        /// <summary>Whether to show the label (BTN-DISPLAY <c>label</c>). The label still shows when nothing else would.</summary>
        [UxmlAttribute]
        public bool ShowLabel
        {
            get => showLabel;
            set { showLabel = value; Refresh(); }
        }

        /// <summary>Solid or ghost (BTN-APPEARANCE <c>appearance</c>).</summary>
        [UxmlAttribute]
        public ButtonAppearance Appearance
        {
            get => appearance;
            set { appearance = value; Refresh(); }
        }

        /// <summary>
        /// Button size in px (BTN-SIZE <c>size</c>): the side when only the icon shows, otherwise the height.
        /// The icon follows it; it is never set on its own.
        /// </summary>
        [UxmlAttribute]
        public float Size
        {
            get => size;
            set { size = Mathf.Max(0, value); Refresh(); }
        }

        /// <summary>The selected state that toggles and choices report; shown like the pressed state.</summary>
        [UxmlAttribute]
        public bool Selected
        {
            get => selected;
            set { selected = value; Refresh(); }
        }

        /// <summary>True when the icon is shown and the label is not.</summary>
        public bool IsIconOnly => IconVisible && !LabelVisible;

        private bool IconVisible => showIcon && !icon.IsEmpty();

        private bool LabelVisible => showLabel || !IconVisible;

        /// <summary>
        /// Icon side in px: <see cref="Size"/> × <see cref="ButtonDesign.IconRatio"/> when only the icon shows;
        /// beside a label it scales with the label's font size from <see cref="ButtonDesign.IconWithLabelSize"/>
        /// at <see cref="ButtonDesign.LabelFontSize"/>, capped at that same ratio of the size.
        /// </summary>
        public float IconSide
        {
            get
            {
                float cap = size * ButtonDesign.IconRatio;
                if (!LabelVisible) return cap;
                float fontSize = labelFontSize > 0 ? labelFontSize : ButtonDesign.LabelFontSize;
                return Mathf.Min(fontSize * ButtonDesign.IconWithLabelSize / ButtonDesign.LabelFontSize, cap);
            }
        }

        private void OnLabelGeometryChanged(GeometryChangedEvent evt)
        {
            float fontSize = labelElement.resolvedStyle.fontSize;
            if (fontSize <= 0 || Mathf.Approximately(fontSize, labelFontSize)) return;
            labelFontSize = fontSize;
            Refresh();
        }

        private void Refresh()
        {
            bool iconVisible = IconVisible, labelVisible = LabelVisible, iconOnly = IsIconOnly;

            iconElement.style.display = iconVisible ? DisplayStyle.Flex : DisplayStyle.None;
            iconElement.style.backgroundImage = icon;
            iconElement.style.width = iconElement.style.height = IconSide;
            iconElement.style.marginRight = iconVisible && labelVisible ? ButtonDesign.IconLabelGap : 0;
            labelElement.style.display = labelVisible ? DisplayStyle.Flex : DisplayStyle.None;

            style.height = size;
            style.minHeight = size;
            style.width = iconOnly ? size : new StyleLength(StyleKeyword.Auto);
            style.minWidth = iconOnly ? size : new StyleLength(StyleKeyword.Null);

            EnableInClassList(iconOnlyElementUssClassName, iconOnly);
            EnableInClassList(ghostUssClassName, appearance == ButtonAppearance.Ghost);
            EnableInClassList(selectedUssClassName, selected);
        }
    }
}

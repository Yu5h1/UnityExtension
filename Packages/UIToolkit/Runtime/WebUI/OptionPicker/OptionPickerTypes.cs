using System;
using UnityEngine.UIElements;

namespace Yu5h1Lib.UIToolkit
{
    public enum OptionPickerInteraction
    {
        Picker,
        Cycle,
        Both
    }

    public enum OptionPickerOrientation
    {
        Horizontal,
        Vertical
    }

    public enum OptionPickerPlacement
    {
        Aligned,
        Adjacent
    }

    public enum OptionPickerAppearance
    {
        Solid,
        Ghost
    }

    [Serializable]
    public class OptionItem
    {
        public string id;
        public string label;
        public Background icon;
        public bool disabled;

        public OptionItem() { }

        public OptionItem(string id, string label, Background icon = default, bool disabled = false)
        {
            this.id = id;
            this.label = label;
            this.icon = icon;
            this.disabled = disabled;
        }
    }

    [Serializable]
    public struct OptionPickerButtonConfig
    {
        public bool showIcon;
        public bool showLabel;
        public OptionPickerAppearance appearance;
        public float size;

        public static OptionPickerButtonConfig Default => new OptionPickerButtonConfig
        {
            showIcon = true,
            showLabel = true,
            appearance = OptionPickerAppearance.Solid,
            size = 32f
        };
    }
}

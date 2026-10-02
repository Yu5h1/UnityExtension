using System;
using System.Globalization;
using UnityEngine.UIElements;
using Yu5h1Lib.MVVM;

namespace Yu5h1Lib.UIToolkit
{
    /// <summary>
    /// Recognizes a value control and wraps it as an <see cref="IValuePort"/> with the exact wire
    /// format its uGUI-side counterpart uses, per 偏好設定.md's UI Toolkit correspondence table — so
    /// one save file works whichever UI a field happens to be bound to. An element not covered here
    /// (e.g. <c>EnumField</c>, deliberately deferred) is silently skipped, matching how Dropdown/
    /// TMP_Dropdown/Scrollbar are already silently skipped on the uGUI side.
    /// </summary>
    internal static class VisualElementPortFactory
    {
        /// <summary>The value type <see cref="TryCreate"/> would bind for an element of this UXML type name, or
        /// null when the element is not bound. Kept beside <see cref="TryCreate"/> so the two cannot drift.</summary>
        public static System.Type GetValueType(string elementTypeName)
        {
            switch (elementTypeName)
            {
                case nameof(Toggle): return typeof(bool);
                case nameof(SliderInt): return typeof(int);
                case nameof(Slider): return typeof(float);
                case nameof(DropdownField): return typeof(string);
                case nameof(TextField): return typeof(string);
                default: return null;
            }
        }

        public static IValuePort TryCreate(VisualElement element)
        {
            switch (element)
            {
                case Toggle toggle:
                    return new VisualElementValuePort<bool>(toggle, toggle,
                        () => toggle.value ? "true" : "false",
                        s => toggle.value = s.Equals("true", StringComparison.OrdinalIgnoreCase) || s == "1");

                case SliderInt sliderInt:
                    return new VisualElementValuePort<int>(sliderInt, sliderInt,
                        () => sliderInt.value.ToString(CultureInfo.InvariantCulture),
                        s =>
                        {
                            if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ||
                                int.TryParse(s, NumberStyles.Integer, CultureInfo.CurrentCulture, out n))
                                sliderInt.value = n;
                        });

                case Slider slider:
                    return new VisualElementValuePort<float>(slider, slider,
                        () => slider.value.ToString(CultureInfo.InvariantCulture),
                        s =>
                        {
                            if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var f) ||
                                float.TryParse(s, NumberStyles.Float, CultureInfo.CurrentCulture, out f))
                                slider.value = f;
                        });

                case DropdownField dropdown:
                    return new VisualElementValuePort<string>(dropdown, dropdown,
                        () => dropdown.value, s => dropdown.value = s);

                case TextField textField:
                    return new VisualElementValuePort<string>(textField, textField,
                        () => textField.value, s => textField.value = s);

                default:
                    return null;
            }
        }
    }
}

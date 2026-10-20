using UnityEngine;
using UnityEngine.UIElements;

namespace Yu5h1Lib.UIToolkit
{
    /// <summary>Which of the two fixed VisualFoundation palettes a scope uses.</summary>
    public enum VisualFoundationScheme
    {
        Light,
        Dark
    }

    /// <summary>
    /// Scopes the Yu5h1Lib.UX VisualFoundation palette (1.3.0) on a UI Toolkit subtree.
    /// <c>Yu5h1VisualFoundation.uss</c> declares the 15 colour roles as <c>--yu5h1-color-*</c> variables under
    /// <c>.yu5h1-theme--light</c> and <c>.yu5h1-theme--dark</c>; <c>Yu5h1Button.uss</c> and
    /// <c>Yu5h1OptionPicker.uss</c> read them. A subtree that carries neither class has no palette variables.
    /// </summary>
    public static class VisualFoundationTheme
    {
        public const string LightClass = "yu5h1-theme--light";
        public const string DarkClass = "yu5h1-theme--dark";
        public const string StyleResource = "Yu5h1Lib.UIToolkit/Yu5h1VisualFoundation";

        private static StyleSheet sheet;

        /// <summary>The generated palette style sheet, loaded once.</summary>
        public static StyleSheet Sheet
        {
            get
            {
                if (sheet == null) sheet = Resources.Load<StyleSheet>(StyleResource);
                return sheet;
            }
        }

        /// <summary>Makes <paramref name="scope"/> and everything below it use <paramref name="scheme"/>.</summary>
        public static void Apply(VisualElement scope, VisualFoundationScheme scheme)
        {
            if (scope == null) return;
            var palette = Sheet;
            if (palette != null && !scope.styleSheets.Contains(palette)) scope.styleSheets.Add(palette);
            scope.EnableInClassList(LightClass, scheme == VisualFoundationScheme.Light);
            scope.EnableInClassList(DarkClass, scheme == VisualFoundationScheme.Dark);
        }

        /// <summary>The scheme of the nearest scope at or above <paramref name="element"/>, or null when there is none.</summary>
        public static VisualFoundationScheme? Find(VisualElement element)
        {
            for (var current = element; current != null; current = current.parent)
            {
                if (current.ClassListContains(DarkClass)) return VisualFoundationScheme.Dark;
                if (current.ClassListContains(LightClass)) return VisualFoundationScheme.Light;
            }
            return null;
        }
    }
}

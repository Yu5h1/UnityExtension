using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Yu5h1Lib.UIToolkit.Tests
{
    /// <summary>
    /// Acceptance checks for UX Button 2.0.0 on native <see cref="Button"/> with <c>Yu5h1Button.uss</c>.
    /// Verifies numeric rules and display logic (BTN-DISPLAY, BTN-SIZE, BTN-LAYOUT) by measured layout.
    /// Appearance (colors, states) is judged directly by the user per decision 65.
    /// </summary>
    public class NativeButtonTests
    {
        private const int PanelWidth = 320, PanelHeight = 240;
        private const string ButtonStyleResource = "Yu5h1Lib.UIToolkit/Yu5h1Button";

        private GameObject host;
        private PanelSettings settings;
        private RenderTexture target;
        private VisualElement root;
        private Texture2D texture;
        private StyleSheet buttonStyle;

        [SetUp]
        public void SetUp()
        {
            target = new RenderTexture(PanelWidth, PanelHeight, 0, RenderTextureFormat.ARGB32);
            settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.targetTexture = target;
            host = new GameObject("NativeButtonTests");
            var document = host.AddComponent<UIDocument>();
            document.panelSettings = settings;

            root = document.rootVisualElement;
            root.style.width = PanelWidth;
            root.style.height = PanelHeight;
            root.style.alignItems = Align.Center;
            root.style.justifyContent = Justify.Center;

            texture = new Texture2D(48, 48);
            buttonStyle = Resources.Load<StyleSheet>(ButtonStyleResource);
            Assert.IsNotNull(buttonStyle, "Yu5h1Button.uss style sheet could not be loaded");
        }

        [TearDown]
        public void TearDown()
        {
            if (host != null) Object.DestroyImmediate(host);
            if (settings != null) Object.DestroyImmediate(settings);
            if (target != null) Object.DestroyImmediate(target);
            if (texture != null) Object.DestroyImmediate(texture);
        }

        private static IEnumerator Settle()
        {
            for (int i = 0; i < 6; i++) yield return null;
        }

        private Button CreateButton(
            bool showIcon = true,
            bool showLabel = true,
            bool hasIcon = true,
            float size = 32f,
            string labelText = "Label")
        {
            var button = new Button();
            button.styleSheets.Add(buttonStyle);
            button.AddToClassList("yu5h1-button");

            bool displayIcon = showIcon && hasIcon;
            bool displayLabel = showLabel || !displayIcon;
            bool iconOnly = displayIcon && !displayLabel;

            if (displayIcon)
            {
                button.iconImage = Background.FromTexture2D(texture);
            }
            if (displayLabel)
            {
                button.text = labelText;
            }

            if (iconOnly)
            {
                button.AddToClassList("yu5h1-button--icon-only");
                button.style.width = size;
                button.style.minWidth = size;
            }

            button.style.height = size;
            button.style.minHeight = size;

            root.Add(button);
            return button;
        }

        private static Image IconOf(Button button) =>
            button.Q<Image>(className: "unity-button__image");

        private static TextElement ChildTextOf(Button button)
        {
            for (int i = 0; i < button.hierarchy.childCount; i++)
            {
                if (button.hierarchy[i] is TextElement te && te != button)
                {
                    return te;
                }
            }
            return null;
        }

        private static void AreClose(float expected, float actual, string what) =>
            Assert.AreEqual(expected, actual, 0.5f, what);

        [UnityTest]
        public IEnumerator BT01_BothShownWhenIconAndLabelAreOn()
        {
            var button = CreateButton(showIcon: true, showLabel: true);
            yield return Settle();

            var icon = IconOf(button);
            var text = ChildTextOf(button);

            Assert.IsNotNull(icon, "Icon element should exist");
            Assert.IsNotNull(text, "Text element should exist");
            Assert.IsFalse(string.IsNullOrEmpty(text.text), "Text should not be empty");
            AreClose(32f, button.worldBound.height, "Height with label should be size (32)");
        }

        [UnityTest]
        public IEnumerator BT01_IconOnlyIsASquareOfSize()
        {
            var button = CreateButton(showIcon: true, showLabel: false);
            yield return Settle();

            var icon = IconOf(button);
            var text = ChildTextOf(button);

            Assert.IsNotNull(icon, "Icon element should exist");
            Assert.IsTrue(text == null || string.IsNullOrEmpty(text.text), "Text should be absent or empty when label is off");
            AreClose(32f, button.worldBound.width, "Width of icon-only button should be size (32)");
            AreClose(32f, button.worldBound.height, "Height of icon-only button should be size (32)");
        }

        [UnityTest]
        public IEnumerator BT01_LabelOnlyWhenIconIsOff()
        {
            var button = CreateButton(showIcon: false, showLabel: true);
            yield return Settle();

            var icon = IconOf(button);
            Assert.IsNull(icon, "Icon element should not exist when icon is off");
            Assert.IsFalse(string.IsNullOrEmpty(button.text), "Button text should not be empty");
            AreClose(32f, button.worldBound.height, "Height with label should be size (32)");
        }

        [UnityTest]
        public IEnumerator BT01_LabelFallbackWhenBothAreOff()
        {
            var button = CreateButton(showIcon: false, showLabel: false);
            yield return Settle();

            var icon = IconOf(button);
            Assert.IsNull(icon, "Icon element should not exist when icon is off");
            Assert.IsFalse(string.IsNullOrEmpty(button.text), "Button fallback text should not be empty");
            AreClose(32f, button.worldBound.height, "Height with fallback label should be size (32)");
        }

        [UnityTest]
        public IEnumerator BT01_LabelFallbackWhenNoIconProvided()
        {
            var button = CreateButton(showIcon: true, showLabel: false, hasIcon: false);
            yield return Settle();

            var icon = IconOf(button);
            Assert.IsNull(icon, "Icon element should not exist when no icon provided");
            Assert.IsFalse(string.IsNullOrEmpty(button.text), "Button fallback text should not be empty when no icon provided");
            AreClose(32f, button.worldBound.height, "Height with fallback label should be size (32)");
        }

        [UnityTest]
        public IEnumerator BT03_IconOnlyFollowsSize([Values(32f, 40f, 24f)] float size)
        {
            var button = CreateButton(showIcon: true, showLabel: false, size: size);
            yield return Settle();

            AreClose(size, button.worldBound.width, "Button width");
            AreClose(size, button.worldBound.height, "Button height");

            var icon = IconOf(button);
            Assert.IsNotNull(icon, "Icon element should exist");
            AreClose(size * 0.75f, icon.worldBound.width, "Icon width (size * 0.75)");
            AreClose(size * 0.75f, icon.worldBound.height, "Icon height (size * 0.75)");
        }

        [UnityTest]
        public IEnumerator BT03_IconBesideLabelFollowsSize([Values(32f, 24f)] float size)
        {
            var button = CreateButton(showIcon: true, showLabel: true, size: size);
            yield return Settle();

            AreClose(size, button.worldBound.height, "Button height");

            var icon = IconOf(button);
            Assert.IsNotNull(icon, "Icon element should exist beside label");
            AreClose(size * 0.75f, icon.worldBound.height, "Icon height beside label should be size * 0.75");
        }

        [UnityTest]
        public IEnumerator BT03_LayoutIconComesFirstWithGapAndCentered()
        {
            var button = CreateButton(showIcon: true, showLabel: true);
            yield return Settle();

            var icon = IconOf(button);
            var text = ChildTextOf(button);

            Assert.IsNotNull(icon, "Icon element exists");
            Assert.IsNotNull(text, "Text element exists");

            Rect iconBound = icon.worldBound;
            Rect textBound = text.worldBound;

            Assert.Less(iconBound.xMax, textBound.xMin + 0.5f, "Icon must come before text in reading order");
            AreClose(8f, textBound.xMin - iconBound.xMax, "Gap between icon and text must be 8px");
            AreClose(iconBound.center.y, textBound.center.y, "Icon and text must be vertically centered");
        }
    }
}

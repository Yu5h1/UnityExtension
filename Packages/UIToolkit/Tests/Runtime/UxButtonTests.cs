using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Yu5h1Lib.UIToolkit.Tests
{
    /// <summary>
    /// UX Button 1.1.0 checks for <see cref="UxButton"/> against a live panel: BTN-DISPLAY, BTN-SIZE,
    /// BTN-LAYOUT by measured layout, and BTN-APPEARANCE structure by pixels read back from the panel's
    /// render texture. PlayMode for the same reason as <c>WorldPanelTests</c>: a runtime panel only resolves
    /// layout while it is rendering. Hover is not simulated, as in <c>ChatPresenterScrollTests</c>.
    /// </summary>
    public class UxButtonTests
    {
        private const int PanelWidth = 320, PanelHeight = 240;
        private static readonly Color PageColor = new Color(1f, 0f, 1f);

        private GameObject host;
        private PanelSettings settings;
        private RenderTexture target;
        private VisualElement root;
        private Texture2D texture;

        [SetUp]
        public void SetUp()
        {
            target = new RenderTexture(PanelWidth, PanelHeight, 0, RenderTextureFormat.ARGB32);
            settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.targetTexture = target;
            settings.clearColor = true;
            settings.colorClearValue = PageColor;
            host = new GameObject("UxButtonTests");
            var document = host.AddComponent<UIDocument>();
            document.panelSettings = settings;

            root = document.rootVisualElement;
            root.style.width = PanelWidth;
            root.style.height = PanelHeight;
            root.style.backgroundColor = PageColor;
            root.style.alignItems = Align.Center;
            root.style.justifyContent = Justify.Center;

            texture = new Texture2D(48, 48);
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

        private UxButton Add(bool withIcon = true, string label = "Label")
        {
            var button = new UxButton { LabelText = label };
            if (withIcon) button.Icon = Background.FromTexture2D(texture);
            root.Add(button);
            return button;
        }

        private static VisualElement IconOf(UxButton button) => button.Q(className: UxButton.iconElementUssClassName);

        private static Label LabelOf(UxButton button) => button.Q<Label>(className: UxButton.labelElementUssClassName);

        private static bool Shown(VisualElement element) => element.resolvedStyle.display == DisplayStyle.Flex;

        private static void AreClose(float expected, float actual, string what) => Assert.AreEqual(expected, actual, .5f, what);

        [UnityTest]
        public IEnumerator BothShownWhenIconAndLabelAreOn()
        {
            var button = Add();
            yield return Settle();
            Assert.IsTrue(Shown(IconOf(button)));
            Assert.IsTrue(Shown(LabelOf(button)));
            AreClose(ButtonDesign.Size, button.worldBound.height, "height with a label");
        }

        [UnityTest]
        public IEnumerator IconOnlyIsASquareOfSize()
        {
            var button = Add();
            button.ShowLabel = false;
            yield return Settle();
            Assert.IsTrue(Shown(IconOf(button)));
            Assert.IsFalse(Shown(LabelOf(button)));
            AreClose(ButtonDesign.Size, button.worldBound.width, "width");
            AreClose(ButtonDesign.Size, button.worldBound.height, "height");
        }

        [UnityTest]
        public IEnumerator LabelOnlyWhenTheIconIsOff()
        {
            var button = Add();
            button.ShowIcon = false;
            yield return Settle();
            Assert.IsFalse(Shown(IconOf(button)));
            Assert.IsTrue(Shown(LabelOf(button)));
            AreClose(ButtonDesign.Size, button.worldBound.height, "height with a label");
        }

        [UnityTest]
        public IEnumerator LabelStillShowsWhenBothAreOff()
        {
            var button = Add();
            button.ShowIcon = false;
            button.ShowLabel = false;
            yield return Settle();
            Assert.IsFalse(Shown(IconOf(button)));
            Assert.IsTrue(Shown(LabelOf(button)));
        }

        [UnityTest]
        public IEnumerator LabelStillShowsWhenThereIsNoIcon()
        {
            var button = Add(withIcon: false);
            button.ShowLabel = false;
            yield return Settle();
            Assert.IsFalse(Shown(IconOf(button)));
            Assert.IsTrue(Shown(LabelOf(button)));
        }

        [UnityTest]
        public IEnumerator AcceptsTextureSpriteAndVectorIcons()
        {
            var sprite = Sprite.Create(texture, new Rect(0, 0, 48, 48), Vector2.one * .5f);
            var vector = ScriptableObject.CreateInstance<VectorImage>();
            try
            {
                var icons = new[] { Background.FromTexture2D(texture), Background.FromSprite(sprite), Background.FromVectorImage(vector) };
                var buttons = new UxButton[icons.Length];
                for (int i = 0; i < icons.Length; i++)
                {
                    buttons[i] = Add(withIcon: false);
                    buttons[i].ShowLabel = false;
                    buttons[i].Icon = icons[i];
                }
                yield return Settle();
                foreach (var button in buttons)
                {
                    Assert.IsTrue(Shown(IconOf(button)));
                    AreClose(ButtonDesign.Size, button.worldBound.width, "width");
                    AreClose(ButtonDesign.Size * ButtonDesign.IconRatio, IconOf(button).worldBound.width, "icon side");
                }
            }
            finally
            {
                Object.DestroyImmediate(sprite);
                Object.DestroyImmediate(vector);
            }
        }

        [UnityTest]
        public IEnumerator IconOnlyIconFollowsSize([Values(32f, 40f, 24f)] float size)
        {
            var button = Add();
            button.ShowLabel = false;
            button.Size = size;
            yield return Settle();
            AreClose(size, button.worldBound.width, "width");
            AreClose(size, button.worldBound.height, "height");
            AreClose(size * ButtonDesign.IconRatio, IconOf(button).worldBound.width, "icon width");
            AreClose(size * ButtonDesign.IconRatio, IconOf(button).worldBound.height, "icon height");
        }

        [UnityTest]
        public IEnumerator IconBesideALabelFollowsTheFontWithinTheCap([Values(32f, 24f)] float size)
        {
            var button = Add();
            button.Size = size;
            LabelOf(button).style.fontSize = ButtonDesign.LabelFontSize;
            yield return Settle();
            float expected = Mathf.Min(ButtonDesign.IconWithLabelSize, size * ButtonDesign.IconRatio);
            AreClose(expected, IconOf(button).worldBound.width, "icon side");
            AreClose(size, button.worldBound.height, "height");
        }

        [UnityTest]
        public IEnumerator IconComesFirstWithTheGapAndBothCentred()
        {
            var button = Add();
            yield return Settle();
            Rect icon = IconOf(button).worldBound, label = LabelOf(button).worldBound;
            Assert.Less(icon.xMax, label.xMin + .5f, "icon before label");
            AreClose(ButtonDesign.IconLabelGap, label.xMin - icon.xMax, "gap");
            AreClose(icon.center.y, label.center.y, "vertical centres");
        }

        private Color Pixel(UxButton button)
        {
            var bound = button.worldBound;
            int x = Mathf.RoundToInt(bound.xMin + 2);
            int y = Mathf.RoundToInt(PanelHeight - bound.center.y);
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var read = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            read.ReadPixels(new Rect(x, y, 1, 1), 0, 0);
            read.Apply();
            RenderTexture.active = previous;
            var color = read.GetPixel(0, 0);
            Object.DestroyImmediate(read);
            return color;
        }

        private static bool Same(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) < .02f && Mathf.Abs(a.g - b.g) < .02f && Mathf.Abs(a.b - b.b) < .02f;

        [UnityTest]
        public IEnumerator SolidHasABackgroundAtRest()
        {
            var button = Add();
            button.ShowLabel = false;
            yield return Settle();
            Assert.IsFalse(Same(PageColor, Pixel(button)), "solid at rest shows a background");
        }

        [UnityTest]
        public IEnumerator GhostHasNoBackgroundAtRestButShowsFocusAndSelection()
        {
            var button = Add();
            button.ShowLabel = false;
            button.Appearance = ButtonAppearance.Ghost;
            yield return Settle();
            Assert.IsTrue(Same(PageColor, Pixel(button)), "ghost at rest shows the page");

            button.Focus();
            yield return Settle();
            Assert.IsFalse(Same(PageColor, Pixel(button)), "ghost with focus differs from rest");

            button.Blur();
            button.Selected = true;
            yield return Settle();
            Assert.IsFalse(Same(PageColor, Pixel(button)), "ghost when selected differs from rest");
        }
    }
}

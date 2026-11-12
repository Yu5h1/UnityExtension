using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Yu5h1Lib.UI.Tests
{
    /// <summary>Exercises native field state and multiline viewport geometry without device input.</summary>
    public sealed class InputFieldLayoutTests
    {
        private RectTransform canvas;

        [SetUp]
        public void SetUp()
        {
            var root = new GameObject("Input test canvas", typeof(RectTransform), typeof(Canvas));
            root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            canvas = (RectTransform)root.transform;
            canvas.sizeDelta = new Vector2(640f, 480f);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(canvas.gameObject);

        [Test]
        public void FieldStateTracksNativeValuesAndCanvasGroupAvailability()
        {
            var input = CreateInput(canvas);
            var state = input.gameObject.AddComponent<InputFieldState>();
            state.RefreshState();
            int notifications = 0;
            state.Changed += _ => notifications++;

            Assert.That(state.IsEmpty, Is.True);
            Assert.That(state.IsInteractable, Is.True);
            Assert.That(state.IsFocused, Is.False);

            input.text = " ";
            state.RefreshState();
            Assert.That(state.IsEmpty, Is.False);
            Assert.That(state.HasNonWhitespace, Is.False);

            input.SetTextWithoutNotify("搜尋");
            state.RefreshState();
            Assert.That(state.Value, Is.EqualTo("搜尋"));
            Assert.That(state.HasNonWhitespace, Is.True);

            canvas.gameObject.AddComponent<CanvasGroup>().interactable = false;
            state.RefreshState();
            Assert.That(state.IsInteractable, Is.False);
            Assert.That(notifications, Is.EqualTo(3));
        }

        [Test]
        public void MultilineFieldGrowsToFiveLinesAndKeepsMoreTextScrollable()
        {
            var viewport = CreateRect("Viewport", canvas);
            viewport.sizeDelta = new Vector2(280f, 44f);
            var input = CreateInput(viewport);
            input.lineType = InputField.LineType.MultiLineNewline;
            var layout = viewport.gameObject.AddComponent<MultilineInputField>();
            layout.Configure(input, minHeight: 44f, maxVisibleLines: 5);

            Assert.That(layout.PreferredHeight, Is.EqualTo(44f));
            Assert.That(layout.VisibleLineCount, Is.EqualTo(1));

            input.text = "One\nTwo\nThree\nFour\nFive";
            layout.RefreshLayout();
            float cap = layout.PreferredHeight;
            Assert.That(cap, Is.GreaterThan(44f));
            Assert.That(layout.VisibleLineCount, Is.EqualTo(5));
            Assert.That(layout.HasOverflow, Is.False);

            input.text += "\nSix\nSeven";
            layout.RefreshLayout();
            viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, layout.PreferredHeight);
            Canvas.ForceUpdateCanvases();

            Assert.That(layout.PreferredHeight, Is.EqualTo(cap).Within(.05f));
            Assert.That(layout.VisibleLineCount, Is.EqualTo(5));
            Assert.That(layout.HasOverflow, Is.True);
            Assert.That(layout.ScrollView.content.rect.height, Is.GreaterThan(layout.ScrollView.viewport.rect.height));
            Assert.That(layout.ScrollView.verticalScrollbar, Is.Not.Null);

            input.caretPosition = input.text.Length;
            layout.ScrollToCaret();
            Assert.That(layout.ScrollView.content.anchoredPosition.y, Is.GreaterThan(0f));

            input.text = "Short";
            layout.RefreshLayout();
            Assert.That(layout.PreferredHeight, Is.EqualTo(44f));
            Assert.That(layout.VisibleLineCount, Is.EqualTo(1));
            Assert.That(layout.HasOverflow, Is.False);
        }

        [Test]
        public void MultilineFieldReflowsExistingTextWhenViewportWidthChanges()
        {
            var viewport = CreateRect("Viewport", canvas);
            viewport.sizeDelta = new Vector2(480f, 44f);
            var input = CreateInput(viewport);
            input.lineType = InputField.LineType.MultiLineNewline;
            var layout = viewport.gameObject.AddComponent<MultilineInputField>();
            layout.Configure(input);
            input.text = "a short draft with words that need more room on a narrow screen";
            layout.RefreshLayout();
            var wideHeight = layout.PreferredHeight;

            viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 160f);
            layout.RefreshLayout();

            Assert.That(layout.VisibleLineCount, Is.GreaterThan(2));
            Assert.That(layout.PreferredHeight, Is.GreaterThan(wideHeight));
        }

        [Test]
        public void AddonMultilineSwitchKeepsTheDraftAndControlsViewportOverflow()
        {
            var viewport = CreateRect("Viewport", canvas);
            viewport.sizeDelta = new Vector2(280f, 44f);
            var field = CreateRect("Input", viewport);
            var label = CreateLabel(field);
            var addon = InputFieldAddon.Create(field, label, null, null);
            var input = (InputField)addon.Raw;
            var layout = viewport.GetComponent<MultilineInputField>();
            Assert.That(addon.Multiline, Is.False);
            Assert.That(input.lineType, Is.EqualTo(InputField.LineType.SingleLine));
            Assert.That(layout.PreferredHeight, Is.EqualTo(44f));

            addon.Multiline = true;
            addon.text = "One\nTwo\nThree\nFour\nFive\nSix";
            Canvas.ForceUpdateCanvases();
            float expanded = layout.PreferredHeight;
            Assert.That(addon.Multiline, Is.True);
            Assert.That(layout.VisibleLineCount, Is.EqualTo(5));
            Assert.That(layout.HasOverflow, Is.True);
            Assert.That(layout.ScrollView.vertical, Is.True);
            Assert.That(expanded, Is.GreaterThan(44f));

            int edits = 0;
            addon.textChanged += _ => edits++;
            addon.Multiline = false;
            Assert.That(addon.text, Is.EqualTo("One\nTwo\nThree\nFour\nFive\nSix"));
            Assert.That(layout.PreferredHeight, Is.EqualTo(44f));
            Assert.That(layout.VisibleLineCount, Is.EqualTo(1));
            Assert.That(layout.HasOverflow, Is.False);
            Assert.That(layout.ScrollView.vertical, Is.False);
            Assert.That(layout.ScrollView.verticalScrollbar.gameObject.activeSelf, Is.False);

            addon.Multiline = true;
            Assert.That(layout.PreferredHeight, Is.EqualTo(expanded));
            Assert.That(layout.HasOverflow, Is.True);
            Assert.That(edits, Is.Zero);
        }

        [Test]
        public void AddonExtendsAnExistingNativeFieldAndPreservesItsConfiguredMode()
        {
            var input = CreateInput(canvas);
            input.lineType = InputField.LineType.MultiLineNewline;
            var addon = input.gameObject.AddComponent<InputFieldAddon>();
            addon.Init(false);

            Assert.That(addon.Raw, Is.SameAs(input));
            Assert.That(addon.Multiline, Is.True);
            Assert.That(input.lineType, Is.EqualTo(InputField.LineType.MultiLineNewline));
            addon.Multiline = false;
            Assert.That(input.lineType, Is.EqualTo(InputField.LineType.SingleLine));
            addon.Multiline = true;
            Assert.That(input.lineType, Is.EqualTo(InputField.LineType.MultiLineSubmit));
        }

        private static InputField CreateInput(RectTransform parent)
        {
            var rect = CreateRect("Input", parent);
            var input = rect.gameObject.AddComponent<InputField>();
            input.textComponent = CreateLabel(rect);
            return input;
        }

        private static Text CreateLabel(RectTransform field)
        {
            var label = CreateRect("Text", field).gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 18;
            label.alignment = TextAnchor.UpperLeft;
            label.supportRichText = false;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(6f, 4f);
            label.rectTransform.offsetMax = new Vector2(-6f, -4f);
            return label;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            return rect;
        }
    }
}

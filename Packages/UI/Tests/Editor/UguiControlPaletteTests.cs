using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Yu5h1Lib.UI.Tests
{
    /// <summary>
    /// Verifies the uGUI palette's native tints, inset focus border, and borderless input behavior.
    /// </summary>
    public sealed class UguiControlPaletteTests
    {
        private GameObject canvasObject;

        [SetUp]
        public void SetUp() => canvasObject = new GameObject("Palette test canvas", typeof(RectTransform), typeof(Canvas));

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(canvasObject);

        [TestCase(false)]
        [TestCase(true)]
        public void AccentAndNeutralButtonsKeepReadableForegroundInEveryEnabledTint(bool dark)
        {
            var palette = dark ? UguiControlPalette.Dark : UguiControlPalette.Light;
            var button = CreateControl<Button>();
            var text = AddText(button.transform);
            var detail = AddText(button.transform);

            palette.ApplyButton(button, text, accent: true, secondary: detail);
            Assert.That(button.colors.normalColor, Is.EqualTo(palette.Accent));
            Assert.That(text.color, Is.EqualTo(palette.OnAccent));
            Assert.That(detail.color, Is.EqualTo(palette.OnAccent));
            CheckTextContrast(button.colors, text.color);

            palette.ApplyButton(button, text, secondary: detail);
            Assert.That(button.colors.normalColor, Is.EqualTo(palette.PanelBackground));
            Assert.That(text.color, Is.EqualTo(palette.ItemText));
            Assert.That(detail.color, Is.EqualTo(palette.SecondaryText));
            CheckTextContrast(button.colors, text.color);
            CheckTextContrast(button.colors, detail.color);

            Assert.That(button.GetComponentsInChildren<LayoutElement>(), Has.Length.EqualTo(1));
            var decoration = GetDecoration(button);
            Assert.That(decoration.raycastTarget, Is.False);
            Assert.That(decoration.GetComponent<LayoutElement>().ignoreLayout, Is.True);
        }

        [Test]
        public void KeyboardSelectionShowsTwoUnitInsetFocusAndDisableClearsIt()
        {
            var palette = UguiControlPalette.Dark;
            var button = CreateControl<Button>();
            var label = AddText(button.transform);
            palette.ApplyButton(button, label);
            var decoration = GetDecoration(button);
            CheckOutline(decoration, 1f, palette.ControlBorder);

            var previousEvents = EventSystem.current;
            var events = new GameObject("Palette test events", typeof(EventSystem)).GetComponent<EventSystem>();
            InvokeLifecycle(events, "OnEnable");
            EventSystem.current = events;
            try
            {
                button.Select();
                InvokeLifecycle(decoration, "LateUpdate");
                CheckOutline(decoration, 2f, palette.Focus);

                button.interactable = false;
                InvokeLifecycle(decoration, "LateUpdate");
                CheckOutline(decoration, 1f, palette.ControlBorder);
                Assert.That(label.color, Is.EqualTo(palette.Disabled));
            }
            finally
            {
                events.SetSelectedGameObject(null);
                if (previousEvents != null)
                    EventSystem.current = previousEvents;
                InvokeLifecycle(events, "OnDisable");
                Object.DestroyImmediate(events.gameObject);
            }
        }

        [Test]
        public void BorderlessFieldKeepsEveryBackgroundTintTransparentAndSetsCaret()
        {
            var palette = UguiControlPalette.Light;
            var field = CreateControl<InputField>();
            field.textComponent = AddText(field.transform);
            field.placeholder = AddText(field.transform);

            palette.ApplyField(field, border: false);
            Assert.That(field.colors.normalColor, Is.EqualTo(Color.clear));
            Assert.That(field.colors.highlightedColor, Is.EqualTo(Color.clear));
            Assert.That(field.colors.pressedColor, Is.EqualTo(Color.clear));
            Assert.That(field.colors.selectedColor, Is.EqualTo(Color.clear));
            Assert.That(field.colors.disabledColor, Is.EqualTo(Color.clear));
            Assert.That(field.customCaretColor, Is.True);
            Assert.That(field.caretColor, Is.EqualTo(palette.ItemText));
            Assert.That(field.textComponent.color, Is.EqualTo(palette.ItemText));
            Assert.That(field.placeholder.color, Is.EqualTo(palette.SecondaryText));

            using var mesh = new VertexHelper();
            InvokeLifecycle(GetDecoration(field), "OnPopulateMesh", mesh);
            Assert.That(mesh.currentVertCount, Is.Zero);
        }

        private T CreateControl<T>() where T : Selectable
        {
            var controlObject = new GameObject(typeof(T).Name, typeof(RectTransform), typeof(Image), typeof(T));
            var rect = (RectTransform)controlObject.transform;
            rect.SetParent(canvasObject.transform, false);
            rect.sizeDelta = new Vector2(160f, 44f);
            var control = controlObject.GetComponent<T>();
            control.targetGraphic = controlObject.GetComponent<Image>();
            return control;
        }

        private static Text AddText(Transform parent)
        {
            var label = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(parent, false);
            return label;
        }

        private static MaskableGraphic GetDecoration(Selectable control) =>
            control.transform.Find("Control appearance").GetComponent<MaskableGraphic>();

        private static void CheckTextContrast(ColorBlock tints, Color text)
        {
            foreach (var tint in new[] { tints.normalColor, tints.highlightedColor, tints.pressedColor, tints.selectedColor })
            {
                float foreground = RelativeLuminance(text);
                float background = RelativeLuminance(tint);
                float contrast = (Mathf.Max(foreground, background) + 0.05f) / (Mathf.Min(foreground, background) + 0.05f);
                Assert.That(contrast, Is.GreaterThanOrEqualTo(4.5f));
            }
        }

        private static float RelativeLuminance(Color color)
        {
            var linear = color.linear;
            return 0.2126f * linear.r + 0.7152f * linear.g + 0.0722f * linear.b;
        }

        private static void CheckOutline(MaskableGraphic decoration, float width, Color color)
        {
            using var mesh = new VertexHelper();
            InvokeLifecycle(decoration, "OnPopulateMesh", mesh);
            Assert.That(mesh.currentVertCount, Is.EqualTo(16));
            UIVertex bottom = default;
            UIVertex top = default;
            mesh.PopulateUIVertex(ref bottom, 0);
            mesh.PopulateUIVertex(ref top, 1);
            Assert.That(top.position.y - bottom.position.y, Is.EqualTo(width).Within(0.01f));
            Assert.That(top.color, Is.EqualTo((Color32)color));
        }

        private static void InvokeLifecycle(Component target, string method, params object[] arguments) =>
            target.GetType()
                .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic, null,
                    System.Array.ConvertAll(arguments, argument => argument.GetType()), null)
                .Invoke(target, arguments);
    }
}

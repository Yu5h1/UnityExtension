using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Yu5h1Lib.UIToolkit.Tests
{
    /// <summary>
    /// Send-enable state, draft clearing and the recording glyph - attached to a real (if unlaid-out)
    /// panel, because <c>TextField</c>'s value-changed event does not dispatch to a fully detached element.
    /// Same reason as <c>GestureLongPressTests</c>, a real click runs through UI Toolkit's own pointer
    /// pipeline and needs a live, laid-out panel, so <see cref="ChatComposer.Sent"/>/
    /// <see cref="ChatComposer.MicClicked"/> actually firing on a click is verified through the demo, not
    /// simulated here.
    /// </summary>
    public class ChatComposerTests
    {
        private GameObject host;
        private PanelSettings settings;
        private ChatComposer composer;

        [SetUp]
        public void SetUp()
        {
            settings = ScriptableObject.CreateInstance<PanelSettings>();
            host = new GameObject("ChatComposerTests");
            var document = host.AddComponent<UIDocument>();
            document.panelSettings = settings;

            composer = new ChatComposer();
            document.rootVisualElement.Add(composer.Root);
        }

        [TearDown]
        public void TearDown()
        {
            if (host != null) Object.DestroyImmediate(host);
            if (settings != null) Object.DestroyImmediate(settings);
        }

        [Test]
        public void SendStartsDisabledWithAnEmptyDraft()
        {
            Assert.IsFalse(composer.SendButton.enabledSelf);
        }

        [Test]
        public void SendEnablesOnceTheDraftHasNonWhitespaceText()
        {
            composer.Draft.value = "hello";
            Assert.IsTrue(composer.SendButton.enabledSelf);
        }

        [Test]
        public void SendStaysDisabledForWhitespaceOnlyText()
        {
            composer.Draft.value = "   ";
            Assert.IsFalse(composer.SendButton.enabledSelf);
        }

        [Test]
        public void ClearDraftEmptiesTheFieldAndDisablesSend()
        {
            composer.Draft.value = "hello";
            Assert.IsTrue(composer.SendButton.enabledSelf);

            composer.ClearDraft();

            Assert.AreEqual("", composer.Draft.value);
            Assert.IsFalse(composer.SendButton.enabledSelf);
        }

        [Test]
        public void ClearDraftDoesNotFireTheChangedCallbackAsASend()
        {
            // SetValueWithoutNotify is the point: clearing after a successful send must not itself look
            // like the user typed something and re-trigger whatever the caller wired to value changes.
            bool sent = false;
            composer.Sent += _ => sent = true;
            composer.Draft.value = "hello";
            composer.ClearDraft();
            Assert.IsFalse(sent);
        }

        [Test]
        public void MicButtonHasNoTextGlyph()
        {
            // The mic icon is vector-drawn (Painter2D), not a text/emoji glyph - a font with no glyph for
            // the old 🎤 would otherwise render a tofu box. True regardless of Recording.
            Assert.IsFalse(composer.Recording);
            Assert.IsTrue(string.IsNullOrEmpty(composer.MicButton.text));

            composer.Recording = true;
            Assert.IsTrue(string.IsNullOrEmpty(composer.MicButton.text));
        }

        [Test]
        public void RecordingReadsBackWhatWasLastSet()
        {
            composer.Recording = true;
            Assert.IsTrue(composer.Recording);
            composer.Recording = false;
            Assert.IsFalse(composer.Recording);
        }
    }
}

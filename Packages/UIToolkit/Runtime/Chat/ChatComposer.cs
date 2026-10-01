using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Yu5h1Lib.UIToolkit
{
    /// <summary>
    /// The message input row: a text field, a microphone toggle and a send button, all inside one pill-
    /// shaped container - the layout practically every chat app on every platform has converged on.
    /// <para>
    /// Enter submits, matching that same convention - guarded against an IME still composing (checked via
    /// <see cref="Input.compositionString"/>) so confirming a candidate is never mistaken for sending.
    /// </para>
    /// <para>
    /// The mic and send glyphs are drawn with <see cref="Painter2D"/>, not text/emoji - a font with no glyph
    /// for 🎤 renders it as a tofu box, which is not "ready to use". This is a small, local vector draw for
    /// exactly these two icons, not the general-purpose, themeable icon subsystem deferred to
    /// Documentation/向量圖示系統.md - that one is still a separate, independent concern.
    /// </para>
    /// </summary>
    public sealed class ChatComposer
    {
        private const float Height = 52;
        private const float Radius = Height / 2;
        private const float SendSize = 38;
        private const float MicSize = 34;

        private static readonly Color PillBackground = new Color(.95f, .95f, .95f);
        private static readonly Color IconColor = new Color(.27f, .29f, .31f);
        private static readonly Color RecordingColor = new Color(.85f, .25f, .25f);
        private static readonly Color AccentColor = new Color32(79, 140, 255, 255);
        // Dark-on-light-pill on purpose: the pill is a fixed near-white regardless of the host app's own
        // theme, so the draft text needs a color that is never going to pick up a light-on-dark default
        // from wherever this is embedded and vanish against it.
        private static readonly Color TextColor = new Color(.15f, .16f, .18f);
        private static readonly Color PlaceholderColor = new Color(0f, 0f, 0f, .666f);

        private bool recording;

        public VisualElement Root { get; } = new VisualElement { name = "chat-composer" };
        public TextField Draft { get; } = new TextField { name = "chat-composer-draft", multiline = false };
        public Button MicButton { get; } = new Button { name = "chat-composer-mic" };
        public Button SendButton { get; } = new Button { name = "chat-composer-send" };

        /// <summary>Stands in for a native placeholder - this Unity version's <see cref="TextField"/> has no
        /// public placeholder API, so this is a plain label laid over the input, hidden the moment
        /// <see cref="Draft"/> has anything in it. Public so a caller can restyle or retext it, same as any
        /// other element here.</summary>
        public Label Placeholder { get; } = new Label("想說什麼?") { pickingMode = PickingMode.Ignore };

        /// <summary>Raised when the draft is submitted - <see cref="SendButton"/> clicked, or Enter pressed
        /// outside an active IME composition - with the draft's current text. This class never clears the
        /// draft or knows what "sent" means beyond that - the caller decides when to call
        /// <see cref="ClearDraft"/>, typically once its own send actually succeeds.</summary>
        public event Action<string> Sent;

        /// <summary>Raised on every click of <see cref="MicButton"/>. This class has no idea what recording
        /// means - only the caller's platform speech bridge does - so a click never starts or stops
        /// anything by itself; the caller sets <see cref="Recording"/> back once it knows the real state.</summary>
        public event Action MicClicked;

        /// <summary>Whether the mic button shows its recording glyph. Set this from whatever the platform's
        /// speech bridge actually reports - clicking the button alone does not change it.</summary>
        public bool Recording
        {
            get => recording;
            set
            {
                recording = value;
                MicButton.MarkDirtyRepaint();
            }
        }

        public ChatComposer()
        {
            Root.AddToClassList("chat-composer");
            Root.style.flexDirection = FlexDirection.Row;
            Root.style.alignItems = Align.Center;
            Root.style.minHeight = Height;
            Root.style.paddingLeft = 14;
            Root.style.paddingRight = 6;
            Root.style.paddingTop = Root.style.paddingBottom = (Height - SendSize) / 2;
            Root.style.backgroundColor = PillBackground;
            Root.style.borderTopLeftRadius = Root.style.borderTopRightRadius =
                Root.style.borderBottomLeftRadius = Root.style.borderBottomRightRadius = Radius;

            Draft.style.flexGrow = 1;
            Draft.style.marginLeft = Draft.style.marginRight =
                Draft.style.marginTop = Draft.style.marginBottom = 0;
            Draft.style.color = TextColor;
            var input = Draft.Q(className: TextField.inputUssClassName);
            if (input != null)
            {
                input.style.backgroundColor = Color.clear;
                input.style.color = TextColor;
                input.style.borderLeftWidth = input.style.borderRightWidth =
                    input.style.borderTopWidth = input.style.borderBottomWidth = 0;
                input.style.paddingLeft = input.style.paddingRight = 6;

                // A child of the input itself, not of Draft - so its position:absolute resolves against
                // the same padding box the real text renders into, and the two line up without having to
                // duplicate the input's own padding here.
                Placeholder.style.position = Position.Absolute;
                Placeholder.style.left = Placeholder.style.right =
                    Placeholder.style.top = Placeholder.style.bottom = 0;
                Placeholder.style.color = PlaceholderColor;
                Placeholder.style.unityTextAlign = TextAnchor.MiddleLeft;
                input.Add(Placeholder);
            }
            Root.Add(Draft);

            MicButton.AddToClassList("chat-composer-mic");
            StyleRoundButton(MicButton, MicSize, Color.clear);
            MicButton.style.marginRight = 2;
            MicButton.generateVisualContent += ctx => DrawMicIcon(ctx.painter2D, MicSize);
            MicButton.clicked += () => MicClicked?.Invoke();
            Root.Add(MicButton);

            SendButton.AddToClassList("chat-composer-send");
            StyleRoundButton(SendButton, SendSize, AccentColor);
            SendButton.generateVisualContent += ctx => DrawSendIcon(ctx.painter2D, SendSize);
            SendButton.SetEnabled(false);
            SendButton.clicked += Submit;
            Root.Add(SendButton);

            Draft.RegisterValueChangedCallback(e =>
            {
                SendButton.SetEnabled(!string.IsNullOrWhiteSpace(e.newValue));
                UpdatePlaceholderVisibility();
            });

            // Trickle-down so this sees Return before the field's own handling of it, and an IME still
            // composing keeps the key entirely - checked here, not swallowed, so confirming a candidate
            // is never mistaken for sending.
            Draft.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode != KeyCode.Return && e.keyCode != KeyCode.KeypadEnter) return;
                if (!string.IsNullOrEmpty(Input.compositionString)) return;
                if (string.IsNullOrWhiteSpace(Draft.value)) return;
                Submit();
                e.StopPropagation();
            }, TrickleDown.TrickleDown);
        }

        private void Submit() => Sent?.Invoke(Draft.value);

        private void UpdatePlaceholderVisibility() =>
            Placeholder.style.display = string.IsNullOrEmpty(Draft.value) ? DisplayStyle.Flex : DisplayStyle.None;

        /// <summary>A plain upward triangle - the shape every "send" button on every mainstream chat app
        /// boils down to once you strip the decoration. Filled white to read clearly against
        /// <see cref="AccentColor"/>.</summary>
        private static void DrawSendIcon(Painter2D p, float size)
        {
            float cx = size / 2f, cy = size / 2f;
            float w = size * .17f, h = size * .17f;
            p.fillColor = Color.white;
            p.BeginPath();
            p.MoveTo(new Vector2(cx, cy - h));
            p.LineTo(new Vector2(cx + w, cy + h));
            p.LineTo(new Vector2(cx - w, cy + h));
            p.ClosePath();
            p.Fill(FillRule.NonZero);
        }

        private void DrawMicIcon(Painter2D p, float size)
        {
            float cx = size / 2f;

            if (recording)
            {
                float r = size * .22f;
                RoundedRectPath(p, cx - r, size * .5f - r, r * 2, r * 2, r);
                p.fillColor = RecordingColor;
                p.Fill(FillRule.NonZero);
                return;
            }

            float capsuleW = size * .22f;
            float capsuleTop = size * .16f;
            float capsuleBottom = size * .56f;
            RoundedRectPath(p, cx - capsuleW / 2f, capsuleTop, capsuleW, capsuleBottom - capsuleTop, capsuleW / 2f);
            p.fillColor = IconColor;
            p.Fill(FillRule.NonZero);

            // The stand: a shallow cradling curve under the capsule, a stem, and a base - the three strokes
            // that read as "microphone" even reduced to a handful of pixels.
            float standHalf = size * .18f;
            float standTop = capsuleBottom + size * .04f;
            float standDepth = size * .14f;
            p.strokeColor = IconColor;
            p.lineWidth = size * .07f;
            p.lineCap = LineCap.Round;

            p.BeginPath();
            p.MoveTo(new Vector2(cx - standHalf, standTop));
            p.QuadraticCurveTo(new Vector2(cx, standTop + standDepth), new Vector2(cx + standHalf, standTop));
            p.Stroke();

            float stemBottom = standTop + standDepth + size * .14f;
            p.BeginPath();
            p.MoveTo(new Vector2(cx, standTop + standDepth));
            p.LineTo(new Vector2(cx, stemBottom));
            p.Stroke();

            float baseHalf = size * .14f;
            p.BeginPath();
            p.MoveTo(new Vector2(cx - baseHalf, stemBottom));
            p.LineTo(new Vector2(cx + baseHalf, stemBottom));
            p.Stroke();
        }

        /// <summary>Builds a rounded-rect path the same way HTML canvas code traditionally does - four
        /// corners via <see cref="Painter2D.ArcTo"/>, which blends a corner from the line before it into the
        /// line after it rather than needing an angle convention. A square radius (half the shorter side)
        /// turns this into the stadium/capsule shape the mic body and the recording dot both use.</summary>
        private static void RoundedRectPath(Painter2D p, float x, float y, float w, float h, float r)
        {
            p.BeginPath();
            p.MoveTo(new Vector2(x + r, y));
            p.ArcTo(new Vector2(x + w, y), new Vector2(x + w, y + h), r);
            p.ArcTo(new Vector2(x + w, y + h), new Vector2(x, y + h), r);
            p.ArcTo(new Vector2(x, y + h), new Vector2(x, y), r);
            p.ArcTo(new Vector2(x, y), new Vector2(x + w, y), r);
            p.ClosePath();
        }

        private static void StyleRoundButton(Button button, float size, Color background)
        {
            button.style.width = button.style.height = size;
            button.style.minWidth = button.style.minHeight = size;
            button.style.borderTopLeftRadius = button.style.borderTopRightRadius =
                button.style.borderBottomLeftRadius = button.style.borderBottomRightRadius = size / 2;
            button.style.borderLeftWidth = button.style.borderRightWidth =
                button.style.borderTopWidth = button.style.borderBottomWidth = 0;
            button.style.marginLeft = button.style.marginRight =
                button.style.marginTop = button.style.marginBottom = 0;
            button.style.paddingLeft = button.style.paddingRight =
                button.style.paddingTop = button.style.paddingBottom = 0;
            button.style.backgroundColor = background;
            button.style.flexShrink = 0;
        }

        /// <summary>Empties the draft and disables send, as if nothing had been typed. Call after the caller
        /// has actually done something with the text from <see cref="Sent"/> - this class never clears it
        /// on its own, so a failed send can leave the draft exactly as the user left it.</summary>
        public void ClearDraft()
        {
            Draft.SetValueWithoutNotify("");
            SendButton.SetEnabled(false);
            UpdatePlaceholderVisibility();
        }
    }
}

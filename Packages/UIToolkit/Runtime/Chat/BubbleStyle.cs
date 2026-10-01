using System;
using UnityEngine;

namespace Yu5h1Lib.UIToolkit
{
    /// <summary>
    /// How one message bubble looks: a colour, a corner radius, and an optional background image - a
    /// bubble is never required to have all three. A transparent <see cref="BackgroundColor"/> with no
    /// <see cref="BackgroundImage"/> renders as plain text with no bubble at all, which is HealthAI's
    /// current assistant-side look.
    /// </summary>
    [Serializable]
    public sealed class BubbleStyle
    {
        public Color BackgroundColor = Color.clear;

        // White by default - the near-universal convention for a message sat on a solid colour bubble.
        // Reserved as its own field (same reasoning as BackgroundColor) rather than left for whatever
        // ambient theme the host app happens to define, which is exactly what made text unreadable before
        // this existed.
        public Color TextColor = Color.white;

        [Min(0)] public float CornerRadius = 12;

        [Tooltip("Optional. Drawn under the text in place of (or blended with) BackgroundColor.")]
        public Sprite BackgroundImage;
    }
}

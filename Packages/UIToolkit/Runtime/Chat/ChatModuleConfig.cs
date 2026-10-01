using System;
using UnityEngine;

namespace Yu5h1Lib.UIToolkit
{
    /// <summary>Every toggle a caller has decided the chat module needs. See Documentation/聊天面板模組.md
    /// in UnityExtension for the discussion behind each field.</summary>
    [Serializable]
    public sealed class ChatModuleConfig
    {
        [Tooltip("Only Unified is implemented so far; SelfAndOthers and PerParticipant are reserved.")]
        public ChatStyleMode StyleMode = ChatStyleMode.Unified;

        [Tooltip("Used when StyleMode is Unified - every message, regardless of sender.")]
        public BubbleStyle UnifiedStyle = new BubbleStyle();

        [Tooltip("Reserved for StyleMode.SelfAndOthers. Not read by this version.")]
        public BubbleStyle SelfStyle = new BubbleStyle();

        [Tooltip("Reserved for StyleMode.SelfAndOthers. Not read by this version.")]
        public BubbleStyle OthersStyle = new BubbleStyle();

        public bool ShowAvatars = true;

        public ChatNameDisplay NameDisplay = ChatNameDisplay.Adaptive;

        [Tooltip("On: self's messages align to the right. Off: everyone, including self, aligns left. " +
                 "Every non-self participant always aligns left either way.")]
        public bool SelfAlignRight = true;

        public bool ShowTimestamps = false;
    }
}

using UnityEngine;

namespace Yu5h1Lib.UIToolkit
{
    /// <summary>
    /// Someone (or something) that can send a <see cref="ChatMessage"/>. Whether a given participant is
    /// "self" for alignment purposes is not stored here - it is a viewpoint, decided by whoever is
    /// rendering the chat, not a fact about the participant.
    /// <para>
    /// <see cref="Style"/> is reserved from day one so a later move from
    /// <see cref="Yu5h1Lib.UIToolkit.ChatStyleMode.Unified"/> to <see cref="Yu5h1Lib.UIToolkit.ChatStyleMode.PerParticipant"/>
    /// only changes which mode reads this field - it never requires adding it after the fact.
    /// </para>
    /// </summary>
    public sealed class ChatParticipant
    {
        public string Id;
        public string Name;

        /// <summary>
        /// Reserved for the vector-icon subsystem (deferred - see Documentation/聊天面板模組.md). A plain
        /// <see cref="Sprite"/> for now; swap the type once that subsystem lands. Null means "no avatar
        /// provided" - the presenter falls back to a built-in placeholder, not an empty slot.
        /// </summary>
        public Sprite Avatar;

        /// <summary>Reserved for <see cref="ChatStyleMode.PerParticipant"/>. Unread in every mode this
        /// version implements.</summary>
        public BubbleStyle Style;

        public ChatParticipant(string id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}

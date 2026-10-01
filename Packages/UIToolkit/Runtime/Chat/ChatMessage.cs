using System;

namespace Yu5h1Lib.UIToolkit
{
    /// <summary>
    /// One message in a chat. Static content only - whether it is still streaming in is a separate,
    /// runtime-only concern the presenter tracks alongside this, not a field here.
    /// </summary>
    public sealed class ChatMessage
    {
        public string Id;
        public string ParticipantId;
        public string Text;
        public DateTimeOffset CreatedAt;

        public ChatMessage(string id, string participantId, string text, DateTimeOffset createdAt)
        {
            Id = id;
            ParticipantId = participantId;
            Text = text;
            CreatedAt = createdAt;
        }
    }
}

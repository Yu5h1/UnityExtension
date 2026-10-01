namespace Yu5h1Lib.UIToolkit
{
    /// <summary>
    /// How many <see cref="BubbleStyle"/>s a chat actually uses. Shared by bubble appearance and colour -
    /// they are the same choice, not two separate settings that happen to agree.
    /// </summary>
    public enum ChatStyleMode
    {
        /// <summary>Every participant renders with the same one style. The only mode this version implements.</summary>
        Unified,

        /// <summary>One style for "self", another shared by everyone else. Reserved for a later version.</summary>
        SelfAndOthers,

        /// <summary>Each participant supplies its own <see cref="ChatParticipant.Style"/>. Reserved for a later version.</summary>
        PerParticipant,
    }
}

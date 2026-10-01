namespace Yu5h1Lib.UIToolkit
{
    /// <summary>Whether a message shows its sender's name above it.</summary>
    public enum ChatNameDisplay
    {
        /// <summary>Two participants: no names. Three or more: every non-self message shows its name.</summary>
        Adaptive,

        /// <summary>Every non-self message shows its name, regardless of participant count.</summary>
        AlwaysShow,

        /// <summary>Self's own messages never show a name; every non-self message still does, regardless
        /// of participant count. Not "hide every name" - only self's.</summary>
        HideSelf,
    }
}

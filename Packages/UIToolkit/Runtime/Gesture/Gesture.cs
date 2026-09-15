namespace Yu5h1Lib.UIToolkit
{
    /// <summary>
    /// Pointer gestures for UI Toolkit, each one a manipulator you attach to the element it belongs to.
    /// <para>
    /// Attaching to the element rather than listening on a shared root is the rule here, not a preference.
    /// A gesture that watches the root has to win an event-ordering race against every other controller
    /// registered there, and losing that race fails silently - nothing throws, the gesture just never fires.
    /// </para>
    /// </summary>
    public static partial class Gesture
    {
    }
}

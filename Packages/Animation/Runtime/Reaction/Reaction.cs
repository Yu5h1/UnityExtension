namespace Yu5h1Lib
{
    /// <summary>
    /// Motion that answers to events rather than to a clock: a spring chasing a target, a hung element
    /// swinging after a knock, a wobble after a drop.
    /// <para>
    /// The distinction from <see cref="Transition"/> is the whole reason both exist. A transition is told
    /// where to finish and how long to take, so interrupting it means cancelling it. A reaction has no end
    /// time and carries its velocity, so a new target mid-flight bends the path instead of restarting it -
    /// which is what makes a grabbed, released and re-grabbed element feel continuous.
    /// </para>
    /// <para>
    /// Nothing here knows what it drives. Callers either read the value each frame, or hand in a
    /// <see cref="Drive"/> target; the UI Toolkit adapter lives in <c>com.yu5h1.uitoolkit</c>.
    /// </para>
    /// </summary>
    public static partial class Reaction
    {
    }
}

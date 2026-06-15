using UnityEngine.Timeline;

namespace Yu5h1Lib.Timeline
{
    /// <summary>
    /// A positional landmark on a Timeline. Carries no value — its <c>time</c> is the
    /// skip destination. <see cref="Yu5h1Lib.Animation.PlayableDirectorAddon.TrySkipToNext"/>
    /// seeks to the next SkipPoint after the current time.
    /// Author by adding it as a marker on any track (or the marker track).
    /// </summary>
    public class SkipPoint : Marker { }
}

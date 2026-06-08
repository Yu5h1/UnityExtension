using UnityEngine.Timeline;

namespace Yu5h1Lib.Timeline
{
    [TrackClipType(typeof(ParentSwitcherPlayableAsset))]
    [TrackBindingType(typeof(ParentSwitcher))]
    public sealed class ParentSwitcherTrack : TrackAsset
    {
    }
}
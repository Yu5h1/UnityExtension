using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Yu5h1Lib.Timeline
{
    public sealed class ParentSwitcherPlayableAsset : PlayableAsset, ITimelineClipAsset
    {
        [Tooltip("Parent index to apply while this clip is active.")]
        public int parentIndex = 0;
        public bool worldPositionStays = true;
        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(
            PlayableGraph graph,
            GameObject owner)
        {
            var playable = ScriptPlayable<ParentSwitcherPlayableBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();

            behaviour.parentIndex = parentIndex;
            behaviour.worldPositionStays = worldPositionStays;
            return playable;
        }
    }
}
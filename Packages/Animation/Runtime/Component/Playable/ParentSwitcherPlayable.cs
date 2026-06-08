using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

namespace Yu5h1Lib.Timeline
{
    public sealed class ParentSwitcherPlayableBehaviour : PlayableBehaviour
    {
        public int parentIndex;
        public bool worldPositionStays;

        public override void ProcessFrame(
            Playable playable,
            FrameData info,
            object playerData)
        {
            var switcher = playerData as ParentSwitcher;
            if (switcher == null)
                return;

            if (!switcher.TryApply(parentIndex, worldPositionStays))
                return;
        }
    }
}

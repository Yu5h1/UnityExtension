using System.Collections.Generic;
using UnityEditor.ShortcutManagement;
using UnityEditor.Timeline;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>Editor-side extension methods for Timeline assets / directors.</summary>
    internal static class TimelineEx
    {
        private const double TimeEpsilon = 1e-6;

        /// <summary>All marker times on the asset — marker track plus every output track.</summary>
        public static IEnumerable<double> EnumerateMarkerTimes(this TimelineAsset asset)
        {
            if (asset == null)
                yield break;

            if (asset.markerTrack != null)
                foreach (var m in asset.markerTrack.GetMarkers())
                    yield return m.time;

            foreach (var track in asset.GetOutputTracks())
                foreach (var m in track.GetMarkers())
                    yield return m.time;
        }

        /// <summary>
        /// Nearest marker time strictly after (forward) / before (!forward) <paramref name="from"/>,
        /// or null when none exists in that direction (or the asset is null).
        /// </summary>
        public static double? FindAdjacentMarkerTime(this TimelineAsset asset, double from, bool forward)
        {
            if (asset == null)
                return null;

            double? best = null;
            foreach (double t in asset.EnumerateMarkerTimes())
            {
                if (forward)
                {
                    if (t > from + TimeEpsilon && (best == null || t < best.Value))
                        best = t;
                }
                else
                {
                    if (t < from - TimeEpsilon && (best == null || t > best.Value))
                        best = t;
                }
            }
            return best;
        }

        /// <summary>
        /// Seeks this director to its previous/next marker and refreshes the Timeline window.
        /// Safe to call on null. Returns false (no-op) when null, not bound to a TimelineAsset,
        /// or no marker lies ahead in that direction.
        /// </summary>
        public static bool SeekToAdjacentMarker(this PlayableDirector director, bool forward)
        {
            if (director == null || director.playableAsset is not TimelineAsset asset)
                return false;

            double? time = asset.FindAdjacentMarkerTime(director.time, forward);
            if (time == null)
                return false;

            director.time = time.Value;
            director.Evaluate();
            TimelineEditor.Refresh(RefreshReason.SceneNeedsUpdate);
            return true;
        }
    }

    /// <summary>
    /// Registers previous/next marker as Shortcut commands so they appear in the Shortcuts Manager
    /// (under the "Timeline" category, by id prefix) and can be bound to keys.
    ///
    /// Global shortcuts — Unity's TimelineWindow is internal and can't be a Shortcut context, so we
    /// can't scope them to the window. Each command no-ops unless a Timeline is being inspected
    /// (the extension is null-safe and direction-safe), so binding them won't misfire elsewhere.
    /// No default keys assigned — bind them in Edit > Shortcuts.
    /// </summary>
    public static class TimelineMarkerShortcuts
    {
        [Shortcut("Timeline/Next Marker")]
        private static void NextMarker() => TimelineEditor.inspectedDirector.SeekToAdjacentMarker(forward: true);

        [Shortcut("Timeline/Prev Marker")]
        private static void PreviousMarker() => TimelineEditor.inspectedDirector.SeekToAdjacentMarker(forward: false);
    }
}

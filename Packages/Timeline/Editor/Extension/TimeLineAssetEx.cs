using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>
    /// Context-menu tools on a TimelineAsset (.playable). Currently: find AnimationClip sub-assets
    /// no longer referenced by any clip, infinite clip, or recorded clip — orphans left behind after
    /// deleting recorded clips. Report is non-destructive; Delete removes them from the asset.
    ///
    /// Reference model: an embedded AnimationClip is "alive" if it is an
    /// <see cref="AnimationTrack.infiniteClip"/> or the <see cref="AnimationPlayableAsset.clip"/>
    /// of any clip on any track. Any other AnimationClip sub-asset is an orphan.
    /// </summary>
    public static class TimeLineAssetMenu
    {
        [MenuItem("CONTEXT/TimelineAsset/Report Orphan AnimationClips")]
        private static void Report(MenuCommand command)
        {
            if (command.context is not TimelineAsset timeline)
                return;

            var orphans = FindOrphans(timeline);
            if (orphans.Count == 0)
            {
                Debug.Log($"[{nameof(TimeLineAssetMenu)}] No orphan AnimationClips in '{timeline.name}'.", timeline);
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"[{nameof(TimeLineAssetMenu)}] {orphans.Count} orphan AnimationClip(s) in '{timeline.name}':");
            foreach (var orphan in orphans)
                sb.AppendLine($"  • {orphan.name}");
            Debug.LogWarning(sb.ToString(), timeline);
        }

        [MenuItem("CONTEXT/TimelineAsset/Delete Orphan AnimationClips")]
        private static void Delete(MenuCommand command)
        {
            if (command.context is not TimelineAsset timeline)
                return;

            var orphans = FindOrphans(timeline);
            if (orphans.Count == 0)
            {
                EditorUtility.DisplayDialog(nameof(TimeLineAssetMenu),
                    $"No orphan AnimationClips in '{timeline.name}'.", "OK");
                return;
            }

            var names = string.Join("\n", orphans.Select(o => "• " + o.name));
            if (!EditorUtility.DisplayDialog("Delete Orphan AnimationClips",
                    $"Remove {orphans.Count} orphan AnimationClip(s) from '{timeline.name}'?\n\n{names}\n\nThis cannot be undone.",
                    "Delete", "Cancel"))
                return;

            foreach (var orphan in orphans)
                AssetDatabase.RemoveObjectFromAsset(orphan);

            var path = AssetDatabase.GetAssetPath(timeline);
            EditorUtility.SetDirty(timeline);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path);
            Debug.Log($"[{nameof(TimeLineAssetMenu)}] Removed {orphans.Count} orphan AnimationClip(s) from '{timeline.name}'.", timeline);
        }

        private static List<AnimationClip> FindOrphans(TimelineAsset timeline)
        {
            var orphans = new List<AnimationClip>();
            var path = AssetDatabase.GetAssetPath(timeline);
            if (string.IsNullOrEmpty(path))
                return orphans;

            var all = AssetDatabase.LoadAllAssetsAtPath(path);

            // Build the set of AnimationClips actually referenced by tracks/clips.
            var referenced = new HashSet<AnimationClip>();
            foreach (var obj in all)
            {
                if (obj is not TrackAsset track)
                    continue;

                if (track is AnimationTrack animTrack && animTrack.infiniteClip != null)
                    referenced.Add(animTrack.infiniteClip);

                foreach (var clip in track.GetClips())
                    if (clip.asset is AnimationPlayableAsset playableAsset && playableAsset.clip != null)
                        referenced.Add(playableAsset.clip);
            }

            // Any AnimationClip sub-asset not in the referenced set is an orphan.
            foreach (var obj in all)
                if (obj is AnimationClip clip && !referenced.Contains(clip))
                    orphans.Add(clip);

            return orphans;
        }
    }
}

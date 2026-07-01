using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Yu5h1Lib.Animation;
using Yu5h1Lib.Timeline;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>
    /// Inspector for a <see cref="SkipPoint"/> marker. Two responsibilities:
    /// <list type="bullet">
    /// <item>Marker navigation — jump the inspected director's playhead to the previous/next
    /// marker in the Timeline (so you can scrub between landmarks without leaving the Inspector).</item>
    /// <item>Skip event authoring — the marker itself carries no event (it lives in the .playable
    /// asset, which can't hold scene references). This editor reaches the inspected
    /// <see cref="PlayableDirector"/> (<see cref="TimelineEditor.inspectedDirector"/>), finds its
    /// <see cref="PlayableDirectorAddon"/>, and draws the UnityEvent the addon maps to this marker —
    /// so the event is authored here but stored (and able to reference scene objects) on the
    /// scene-side component.</item>
    /// </list>
    /// </summary>
    [CustomEditor(typeof(SkipPoint))]
    public class SkipPointEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // The marker's own serialized fields (time, etc.).
            DrawDefaultInspector();

            EditorGUILayout.Space();

            var marker = (SkipPoint)target;

            var director = TimelineEditor.inspectedDirector;
            if (director == null)
            {
                EditorGUILayout.HelpBox(
                    "Open this Timeline through a PlayableDirector in the scene to navigate markers and author skip events.",
                    MessageType.Info);
                return;
            }

            DrawMarkerNavigation(director);

            EditorGUILayout.Space();

            DrawSkipEvent(director, marker);
        }

        #region Marker Navigation

        private void DrawMarkerNavigation(PlayableDirector director)
        {
            var asset = director.playableAsset as TimelineAsset;
            double? prev = asset.FindAdjacentMarkerTime(director.time, forward: false);
            double? next = asset.FindAdjacentMarkerTime(director.time, forward: true);

            EditorGUILayout.LabelField("Marker Navigation", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(prev == null))
                    if (GUILayout.Button("◀ Prev Marker"))
                        director.SeekToAdjacentMarker(forward: false);

                using (new EditorGUI.DisabledScope(next == null))
                    if (GUILayout.Button("Next Marker ▶"))
                        director.SeekToAdjacentMarker(forward: true);
            }
        }

        #endregion

        #region Skip Event

        private void DrawSkipEvent(PlayableDirector director, SkipPoint marker)
        {
            var addon = director.GetComponent<PlayableDirectorAddon>();
            if (addon == null)
            {
                EditorGUILayout.HelpBox(
                    $"'{director.name}' has no PlayableDirectorAddon — add one to wire skip events.",
                    MessageType.Info);
                return;
            }

            // Ensure a clean (empty) entry exists before we draw it. Done on the live object so the
            // UnityEvent is default-constructed — avoids InsertArrayElementAtIndex duplicating a
            // neighbouring entry's listeners.
            if (!addon.TryGetSkipEvent(marker, out _))
            {
                Undo.RecordObject(addon, "Add Skip Event");
                addon.GetOrCreateSkipEvent(marker);
                EditorUtility.SetDirty(addon);
            }

            MarkerEventDrawer.DrawKeyedEvent(addon, "_skipPointEvents", marker, $"Skip Event  →  {addon.name}");
        }

        #endregion
    }
}

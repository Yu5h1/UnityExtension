using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using Yu5h1Lib.Timeline;

namespace Yu5h1Lib.EditorExtension
{
    /// <summary>
    /// Inspector for an <see cref="EventSignal"/> marker. The marker carries no data; this editor
    /// reaches the inspected director (<see cref="TimelineEditor.inspectedDirector"/>), resolves the
    /// <see cref="EventSignalReceiver"/> on the GameObject this marker's track routes to (the top
    /// marker track routes to the director's GameObject; a bound track routes to its binding), and
    /// draws the UnityEvent the receiver maps to this marker — so the event is authored here but
    /// stored (and able to reference scene objects) on the scene-side receiver.
    /// </summary>
    [CustomEditor(typeof(EventSignal))]
    public class EventSignalEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // The marker's own serialized fields (time, flags).
            DrawDefaultInspector();

            EditorGUILayout.Space();

            var marker = (EventSignal)target;

            var director = TimelineEditor.inspectedDirector;
            if (director == null)
            {
                EditorGUILayout.HelpBox(
                    "Open this Timeline through a PlayableDirector in the scene to author its event.",
                    MessageType.Info);
                return;
            }
            // A notification routes to the GameObject the marker's track is bound to — the top marker
            // track has no binding and routes to the director; a custom bound track routes to its
            // binding. So the receiver lives on that host, not necessarily on the director.
            var host = ResolveHost(marker, director);

            var receiver = host.GetComponent<EventSignalReceiver>();
            if (receiver == null)
            {
                EditorGUILayout.HelpBox(
                    $"'{host.name}' has no EventSignalReceiver — add one to wire events.",
                    MessageType.Info);
                if (GUILayout.Button("Add EventSignalReceiver"))
                    Undo.AddComponent<EventSignalReceiver>(host);
                return;
            }

            // Ensure a clean (empty) entry exists before we draw it. Done on the live object so the
            // UnityEvent is default-constructed — avoids InsertArrayElementAtIndex duplicating a
            // neighbouring entry's listeners.
            if (!receiver.TryGetEvent(marker, out _))
            {
                Undo.RecordObject(receiver, "Add Event");
                receiver.GetOrCreateEvent(marker);
                EditorUtility.SetDirty(receiver);
            }

            MarkerEventDrawer.DrawKeyedEvent(receiver, "_events", marker, $"Event  →  {receiver.name}");
        }

        /// <summary>
        /// The GameObject that <paramref name="marker"/>'s notification routes to: the binding of its
        /// track, or the director's GameObject when the track is unbound (the marker track).
        /// </summary>
        private static GameObject ResolveHost(EventSignal marker, UnityEngine.Playables.PlayableDirector director)
        {
            var track = marker.parent;
            Object bound = track != null ? director.GetGenericBinding(track) : null;
            return bound switch
            {
                GameObject go => go,
                Component comp => comp.gameObject,
                _ => director.gameObject,
            };
        }

    }
}

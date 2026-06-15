using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Yu5h1Lib.Serialization;
using Yu5h1Lib.Timeline;

namespace Yu5h1Lib.Animation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayableDirector))]
    public class PlayableDirectorAddon : ComponentController<PlayableDirector>
    {
        public PlayableDirector director => component;

        [Header("Events")]
        [SerializeField] private UnityEvent _Played;
        [SerializeField] private UnityEvent _Paused;
        [SerializeField] private UnityEvent _Stopped;

        [Header("Options")]
        [SerializeField] private bool invokeStoppedOnDisable = true;

        [Header("Skip")]
        [SerializeField] private bool _canSkip = true;

        /// <summary>
        /// Per-SkipPoint events. Key = the SkipPoint marker (lives in the .playable asset);
        /// value = a scene-side UnityEvent (lives here, so its listeners can target scene objects).
        /// Authored from the SkipPoint's own Inspector via SkipPointEditor — not meant to be edited here,
        /// hence [HideInInspector].
        /// </summary>
        [HideInInspector]
        [SerializeField] private KeyValues<SkipPoint, UnityEvent> _skipPointEvents = new();

        [Tooltip("Fired once after any successful skip, regardless of which SkipPoint was reached.")]
        [SerializeField] private UnityEvent _skipped;

        /// <summary>
        /// Gate for skipping. Settable at runtime (e.g. by a ParameterReceiver writing a
        /// ParameterObject named "CanSkip"). Decides *whether* skipping is allowed, not where to.
        /// The destination is the next <see cref="SkipPoint"/> marker — see <see cref="TrySkipToNext"/>.
        /// </summary>
        public bool CanSkip { get => _canSkip; set => _canSkip = value; }

        private bool subscribed;

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();

            if (invokeStoppedOnDisable)
                _Stopped?.Invoke();
        }

        private void Subscribe()
        {
            if (subscribed)
                return;

            if (!director)
                return;

            director.played += HandlePlayed;
            director.paused += HandlePaused;
            director.stopped += HandleStopped;

            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || !director)
                return;

            director.played -= HandlePlayed;
            director.paused -= HandlePaused;
            
            director.stopped -= HandleStopped;

            subscribed = false;
        }

        private void HandlePlayed(PlayableDirector d)
        {
            _Played?.Invoke();
        }

        private void HandlePaused(PlayableDirector d)
        {
            _Paused?.Invoke();
        }

        private void HandleStopped(PlayableDirector d)
        {
            _Stopped?.Invoke();
        }
        [ContextMenu(nameof(Stop))]
        public void Stop() => director.Stop();

        /// <summary>
        /// Seeks the director to the next <see cref="SkipPoint"/> marker after the current time.
        /// Returns false (no-op) when <see cref="CanSkip"/> is off, there is no director,
        /// the bound asset isn't a <see cref="TimelineAsset"/>, or no later SkipPoint exists.
        /// </summary>
        public bool TrySkipToNext()
        {
            if (!CanSkip || !director || director.playableAsset is not TimelineAsset timeline)
                return false;

            double current = director.time;
            double target = double.MaxValue;
            SkipPoint next = null;

            foreach (var marker in EnumerateMarkers(timeline))
            {
                if (marker is not SkipPoint point || point.time <= current || point.time >= target)
                    continue;
                target = point.time;
                next = point;
            }

            if (next == null)
                return false;

            director.time = target;
            director.Evaluate();
            Raise(next);
            _skipped?.Invoke();
            return true;
        }

        [ContextMenu(nameof(SkipToNext))]
        public void SkipToNext() => TrySkipToNext();

        /// <summary>Invoke the UnityEvent mapped to <paramref name="point"/>, if any.</summary>
        private void Raise(SkipPoint point)
        {
            if (TryGetSkipEvent(point, out var evt))
                evt.Invoke();
        }

        /// <summary>True when <paramref name="point"/> has a non-null mapped event; outputs it.</summary>
        public bool TryGetSkipEvent(SkipPoint point, out UnityEvent evt)
        {
            evt = null;
            return point != null && _skipPointEvents.TryGetValue(point, out evt) && evt != null;
        }

        /// <summary>
        /// Returns the event mapped to <paramref name="point"/>, creating an empty one if absent.
        /// Used by SkipPointEditor to guarantee a clean entry before drawing it.
        /// </summary>
        public UnityEvent GetOrCreateSkipEvent(SkipPoint point)
        {
            if (point == null)
                return null;

            if (!_skipPointEvents.TryGetValue(point, out var evt) || evt == null)
            {
                evt = new UnityEvent();
                _skipPointEvents[point] = evt;
            }
            return evt;
        }

        private static System.Collections.Generic.IEnumerable<IMarker> EnumerateMarkers(TimelineAsset timeline)
        {
            if (timeline.markerTrack != null)
                foreach (var marker in timeline.markerTrack.GetMarkers())
                    yield return marker;

            foreach (var track in timeline.GetOutputTracks())
                foreach (var marker in track.GetMarkers())
                    yield return marker;
        }
    }
}
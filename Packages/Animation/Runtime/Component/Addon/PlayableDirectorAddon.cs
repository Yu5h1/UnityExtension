using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.Timeline;
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
        /// Gate for skipping. Settable at runtime (e.g. by a ParameterReceiver writing a
        /// ParameterObject named "CanSkip"). Decides *whether* skipping is allowed, not where to.
        /// The destination is the next <see cref="SkipPoint"/> marker — see <see cref="SkipToNext"/>.
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
        public bool SkipToNext()
        {
            if (!CanSkip || !director || director.playableAsset is not TimelineAsset timeline)
                return false;

            double current = director.time;
            double target = double.MaxValue;
            bool found = false;

            foreach (var marker in EnumerateMarkers(timeline))
            {
                if (marker is not SkipPoint || marker.time <= current || marker.time >= target)
                    continue;
                target = marker.time;
                found = true;
            }

            if (!found)
                return false;

            director.time = target;
            director.Evaluate();
            return true;
        }

        [ContextMenu("Skip To Next")]
        private void SkipToNextMenu() => SkipToNext();

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
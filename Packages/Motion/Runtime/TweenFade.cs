// Fades a CanvasGroup using the Motion package's built-in scalar tween runner.
using UnityEngine;

namespace Yu5h1Lib.Motion
{
    /// <summary>Plays a single-cycle alpha tween on a CanvasGroup.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    [AddComponentMenu("Yu5h1Lib/Motion/Tween Fade")]
    public sealed class TweenFade : MonoBehaviour, Tween.ISettings
    {
        [SerializeField, Range(0f, 1f)] private float _from;
        [SerializeField, Range(0f, 1f)] private float _to = 1f;
        [SerializeField, Min(0.001f)] private float _duration = 0.25f;
        [SerializeField] private bool _playOnEnable = true;
        [SerializeField] private bool _useUnscaledTime;

        private CanvasGroup _target;
        private Tween.IHandle<float> _handle;

        double Tween.ISettings.Duration => _duration;
        double Tween.ISettings.Delay => 0d;
        string Tween.ISettings.Ease => "linear";
        int Tween.ISettings.Cycles => 1;
        Tween.LoopMode Tween.ISettings.LoopMode => Tween.LoopMode.Restart;
        bool Tween.ISettings.UnscaledTime => _useUnscaledTime;

        /// <summary>Reports whether the current handle is advancing.</summary>
        public bool IsPlaying => _handle?.IsPlaying == true;

        private void Awake() => _target = GetComponent<CanvasGroup>();

        private void OnEnable()
        {
            if (_playOnEnable)
                Play();
        }

        private void OnDisable()
        {
            Rewind();
            Stop();
        }

        /// <summary>Starts from the configured alpha or resumes from the current position.</summary>
        public void Play()
        {
            if (isActiveAndEnabled)
                GetOrCreateHandle().Play();
        }

        /// <summary>Moves back toward the starting alpha on the same timeline.</summary>
        public void PlayBackwards() => _handle?.PlayBackwards();

        /// <summary>Pauses while retaining the current alpha and position.</summary>
        public void Pause() => _handle?.Pause();

        /// <summary>Invalidates the current handle, retaining the last written alpha.</summary>
        public void Stop()
        {
            _handle?.Stop();
            _handle = null;
        }

        /// <summary>Returns an existing tween to its starting alpha.</summary>
        public void Rewind() => _handle?.Rewind();

        /// <summary>Sets the alpha to its endpoint and stops playback.</summary>
        public void Complete()
        {
            if (isActiveAndEnabled)
                GetOrCreateHandle().Complete();
        }

        private Tween.IHandle<float> GetOrCreateHandle()
        {
            if (_handle == null)
            {
                var runner = UnityTweenRunner.GetRunner(_useUnscaledTime);
                _handle = runner.Create(_target, this, _from, _to, SetAlpha);
                _handle.Rewind();
            }

            return _handle;
        }

        private void SetAlpha(float value)
        {
            if (_target)
                _target.alpha = value;
        }
    }
}

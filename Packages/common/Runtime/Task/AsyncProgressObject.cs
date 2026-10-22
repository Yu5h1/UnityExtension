using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib
{
    /// <summary>
    /// An <see cref="AsyncTaskObject"/> that reports how far its work has got, from 0 to 1, between Begin and End:
    /// bytes of a download, scene loading, a timer. Call <see cref="ReportProgress"/> from
    /// <see cref="AsyncTaskObject.Processing"/>.
    /// </summary>
    public abstract class AsyncProgressObject : AsyncTaskObject
    {
        [SerializeField] private UnityEvent<float> _progress = new UnityEvent<float>();

        /// <summary>Raised with each reported value; values never decrease within a run and stay within 0..1.</summary>
        public UnityEvent<float> onProgress => _progress;
        /// <summary>The last reported value of the current or last run.</summary>
        public float Progress { get; private set; }

        protected override void OnEnter()
        {
            Progress = 0f;
            base.OnEnter();
        }

        /// <summary>
        /// Raises Progress with <paramref name="value"/> clamped to 0..1. A value below the last report is raised as
        /// the last report, so listeners never see progress go back.
        /// </summary>
        protected void ReportProgress(float value)
        {
            Progress = Mathf.Max(Progress, Mathf.Clamp01(value));
            _progress?.Invoke(Progress);
        }
    }
}

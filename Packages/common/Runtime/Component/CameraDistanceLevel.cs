using UnityEngine;
using UnityEngine.Events;

namespace Yu5h1Lib
{
    /// <summary>
    /// Fires a level-changed event based on this object's distance to a camera.
    /// With N thresholds you get N+1 levels (0..N):
    ///   distance &lt; thresholds[0]              → 0
    ///   thresholds[i] ≤ distance &lt; t[i+1]      → i+1
    ///   distance ≥ thresholds[last]            → N
    /// Default thresholds [10, 30] = 3 levels (near / mid / far).
    /// </summary>
    public class CameraDistanceLevel : BaseMonoBehaviour
    {
        [Tooltip("Camera to measure against. Leave empty to use Camera.main.")]
        [SerializeField] private Camera _camera;

        [Tooltip("Sorted ascending. N thresholds → N+1 levels (0..N).")]
        [SerializeField] private float[] thresholds = new[] { 10f, 30f };

        [Tooltip("Distance buffer applied at every threshold to avoid flicker. 0 = disabled.")]
        [SerializeField, Min(0f)] private float hysteresis = 1f;

        [Tooltip("Seconds between checks. 0 = every frame. 0.1 ~ 0.2 is usually plenty for LOD-style logic.")]
        [SerializeField, Min(0f)] private float checkInterval = 0.1f;

        [Tooltip("Fires when level changes. Argument = new level.")]
        [SerializeField] private UnityEvent<int> _LevelChanged;

        /// <summary>Current level (0..thresholds.Length). -1 means uninitialized.</summary>
        public int CurrentLevel { get; private set; } = -1;

        public int LevelCount => thresholds == null ? 1 : thresholds.Length + 1;

        private float _nextCheckTime;

        protected override void OnInitializing() {}

        private void Start()
        {
            if (_camera == null) _camera = Camera.main;
            ForceEvaluate();
        }

        private void Update()
        {
            if (checkInterval > 0f)
            {
                if (Time.time < _nextCheckTime) return;
                _nextCheckTime = Time.time + checkInterval;
            }
            Evaluate();
        }

        /// <summary>
        /// Recompute level immediately. Used at startup and when you change camera/thresholds at runtime.
        /// Re-fires the event even if the level didn't change (useful for initial state propagation).
        /// </summary>
        [ContextMenu(nameof(ForceEvaluate))]
        public void ForceEvaluate()
        {
            CurrentLevel = -1;
            Evaluate();
        }

        private void Evaluate()
        {
            int newLevel = ComputeLevel();
            if (newLevel == CurrentLevel) return;
            CurrentLevel = newLevel;
            _LevelChanged?.Invoke(newLevel);
        }

        private int ComputeLevel()
        {
            if (_camera == null || thresholds == null || thresholds.Length == 0)
                return 0;

            // Use squared distance — avoids sqrt per check.
            Vector3 delta = transform.position - _camera.transform.position;
            float sqrDist = delta.sqrMagnitude;

            for (int i = 0; i < thresholds.Length; i++)
            {
                // Hysteresis: if we're already above threshold i, lower the bar to drop back;
                //             if we're below it, raise the bar to climb up.
                float t = thresholds[i];
                t += (CurrentLevel > i) ? -hysteresis : hysteresis;
                if (t < 0f) t = 0f;
                float sqrT = t * t;

                if (sqrDist < sqrT) return i;
            }
            return thresholds.Length;
        }

        private void OnValidate()
        {
            // Keep thresholds sorted ascending — silent fix.
            if (thresholds == null || thresholds.Length < 2) return;
            for (int i = 1; i < thresholds.Length; i++)
                if (thresholds[i] < thresholds[i - 1]) thresholds[i] = thresholds[i - 1];
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (thresholds == null) return;
            Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
            for (int i = 0; i < thresholds.Length; i++)
                Gizmos.DrawWireSphere(transform.position, thresholds[i]);
        }
#endif
    }
}

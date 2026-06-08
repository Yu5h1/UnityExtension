using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// Procedural sinusoidal oscillation around the starting local value.
    /// Choose <see cref="Target"/> to drive localPosition, localEulerAngles, or localScale.
    /// Designed to be placed on a child / bone transform so the parent handles forward motion
    /// (e.g. SplineFollower, Locomotor) while this component layers per-axis waves on top.
    ///
    /// Common use cases:
    ///   - Snake / dragon body: <see cref="Target.EulerAngles"/> on each bone with phase offset by index
    ///     (creates a traveling wave down the spine).
    ///   - Bird hover bobbing: <see cref="Target.Position"/> on Y only.
    ///   - Breathing / pulsing FX: <see cref="Target.Scale"/> with small symmetric amplitude.
    /// </summary>
    [DisallowMultipleComponent]
    public class WaveMotion : MonoBehaviour
    {
        public enum Target { Position, EulerAngles, Scale }
        public enum UpdateMode { Update, LateUpdate, FixedUpdate }

        [Header("Target")]
        [Tooltip("Which local property to oscillate. Add multiple WaveMotion components if you need combined effects.")]
        [SerializeField] private Target target = Target.Position;

        [Header("Wave")]
        [Tooltip("Peak displacement per axis. Unit depends on Target — Position: meters, EulerAngles: degrees, Scale: scale units.")]
        [SerializeField] private Vector3 amplitude = new Vector3(0.5f, 0.2f, 0f);

        [Tooltip("Cycles per second per axis (Hz). 0 disables that axis.")]
        [SerializeField] private Vector3 frequency = new Vector3(1f, 1.5f, 0f);

        [Tooltip("Phase offset in radians per axis. For traveling-wave bodies, set explicit phase per bone and disable randomize.")]
        [SerializeField] private Vector3 phase = Vector3.zero;

        [Header("Options")]
        [Tooltip("On enable, randomize phase so multiple instances don't oscillate in sync.")]
        [SerializeField] private bool randomizePhaseOnEnable = true;

        [Tooltip("When to apply the offset. LateUpdate is safest after follower/locomotion logic.")]
        [SerializeField] private UpdateMode updateMode = UpdateMode.LateUpdate;

        private Vector3 _baseValue;

        private const float TwoPi = Mathf.PI * 2f;

        private void OnEnable()
        {
            CaptureBase();
            if (randomizePhaseOnEnable)
                phase = new Vector3(Random.value, Random.value, Random.value) * TwoPi;
        }

        private void Update()      { if (updateMode == UpdateMode.Update)      Tick(); }
        private void LateUpdate()  { if (updateMode == UpdateMode.LateUpdate)  Tick(); }
        private void FixedUpdate() { if (updateMode == UpdateMode.FixedUpdate) Tick(); }

        private void Tick()
        {
            float t = Time.time;
            Vector3 offset = new Vector3(
                amplitude.x * Mathf.Sin(t * frequency.x * TwoPi + phase.x),
                amplitude.y * Mathf.Sin(t * frequency.y * TwoPi + phase.y),
                amplitude.z * Mathf.Sin(t * frequency.z * TwoPi + phase.z)
            );

            switch (target)
            {
                case Target.Position:    transform.localPosition    = _baseValue + offset; break;
                case Target.EulerAngles: transform.localEulerAngles = _baseValue + offset; break;
                case Target.Scale:       transform.localScale       = _baseValue + offset; break;
            }
        }

        private void CaptureBase()
        {
            switch (target)
            {
                case Target.Position:    _baseValue = transform.localPosition;    break;
                case Target.EulerAngles: _baseValue = transform.localEulerAngles; break;
                case Target.Scale:       _baseValue = transform.localScale;       break;
            }
        }

        /// <summary>Re-capture the current local value as the new oscillation center.</summary>
        [ContextMenu(nameof(ResetBaseValue))]
        public void ResetBaseValue() => CaptureBase();
    }
}

using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace Yu5h1Lib
{
    /// <summary>
    /// Drives a Transform DIRECTLY along a <see cref="SplineContainer"/> — the
    /// Transform IS the point on the spline at the current progress, no lerp,
    /// no lag, no "virtual target" middleman.
    ///
    /// Use this for NPCs / cameras / props that travel a fixed path without
    /// physics constraints. Each frame: progress advances, the Transform is
    /// snapped to the evaluated pose (position exact, rotation optionally
    /// smoothed). Because there's no catch-up model, end behaviour (Loop /
    /// PingPong / Stop) is clean — the mover exactly reaches the endpoint
    /// with no permanent offset.
    ///
    /// Contrast with <see cref="SplineFollower"/>: that one emits Move/Turn
    /// to an <see cref="ILocomotor"/> so physics-driven vehicles (boats, cars)
    /// can approximate the path through their own dynamics. That indirection
    /// is unnecessary — and counter-productive — for a character on a fixed
    /// deck path.
    ///
    /// Setup:
    ///   1. Drop on an NPC/camera/prop GameObject.
    ///   2. Reference a <see cref="SplineContainer"/>. Evaluate respects its
    ///      transform, so it can be a child of a moving platform (e.g. a boat
    ///      deck) and the pose automatically tracks the platform's tilt.
    ///   3. Set <see cref="speed"/> (m/s). This is also fed verbatim to the
    ///      Animator speed parameter — no need to estimate it from motion.
    ///
    /// For smooth looping, set the SplineContainer's Spline as Closed. Otherwise
    /// Loop mode will teleport from end back to start.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(50)] // run after Animator & boat controllers so we write the final pose
    public class SplinePoseFollower : MonoBehaviour
    {
        public enum EndBehavior { Stop, Loop, PingPong, Disable }

        [Header("References")]
        [Tooltip("The spline to travel along. Evaluate respects this container's transform, so it can be a child of a moving platform.")]
        [SerializeField] private SplineContainer spline;

        [Header("Motion")]
        [Tooltip("Travel speed in metres / second along the spline. Zero pauses. This value is also fed to the Animator speed parameter verbatim.")]
        [SerializeField, Min(0f)] private float speed = 1.2f;

        [Tooltip("Starting position along the spline (0..1). Overridden if 'Snap To Nearest At Start' is on.")]
        [Range(0f, 1f)]
        [SerializeField] private float startProgress = 0f;

        [Tooltip("On Awake, project the current transform position onto the spline and start from there. Convenient — drop the NPC near the path and it finds the nearest point.")]
        [SerializeField] private bool snapToNearestAtStart = true;

        [Header("End behaviour")]
        [SerializeField] private EndBehavior endBehavior = EndBehavior.Loop;

        [Header("Rotation")]
        [Tooltip("Drive the Transform's rotation from the spline tangent + up. Turn off if you want rotation controlled elsewhere (e.g. a look-at target).")]
        [SerializeField] private bool alignToSpline = true;

        [Tooltip("0 = snap rotation to spline tangent instantly. Higher = smoother, with visible 'shoulder-turn' lag at sharp corners (natural walking feel).")]
        [SerializeField, Min(0f)] private float rotationSmooth = 0f;

        [Header("Animator")]
        [Tooltip("Optional Animator to drive. Its 'applyRootMotion' is forced off in Awake — root motion would fight the spline-driven position.")]
        [SerializeField] private Animator animator;

        [Tooltip("Float parameter on the Animator that receives the travel speed. Leave empty to skip.")]
        [SerializeField] private string speedParameter = "Speed";

#if UNITY_EDITOR
        [Header("Debug")]
        [SerializeField] private bool drawGizmos = true; 
#endif

        // --- runtime ---
        private float _progress;        // normalized 0..1
        private int _direction = 1;     // +1 forward, -1 backward (PingPong)
        private float _splineLength;
        private int _speedHash;
        private bool _hasSpeedParam;
        private bool _stopped;

        public SplineContainer Spline
        {
            get => spline;
            set { spline = value; CacheLength(); }
        }

        /// <summary>Current normalized progress on the spline (0..1). Setting resets any stop latch.</summary>
        public float Progress
        {
            get => _progress;
            set { _progress = Mathf.Clamp01(value); _stopped = false; }
        }

        /// <summary>Travel speed in m/s. Zero to pause without changing state.</summary>
        public float Speed
        {
            get => speed;
            set => speed = Mathf.Max(0f, value);
        }

        /// <summary>True once the mover has reached the end in Stop / Disable mode.</summary>
        public bool IsStopped => _stopped;

        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (animator != null) animator.applyRootMotion = false;

            _hasSpeedParam = !string.IsNullOrEmpty(speedParameter);
            if (_hasSpeedParam) _speedHash = Animator.StringToHash(speedParameter);

            CacheLength();

            if (snapToNearestAtStart && SplineIsUsable())
            {
                float3 localPos = spline.transform.InverseTransformPoint(transform.position);
                SplineUtility.GetNearestPoint(spline.Spline, localPos, out _, out float t);
                _progress = Mathf.Clamp01(t);
            }
            else
            {
                _progress = Mathf.Clamp01(startProgress);
            }
        }

        private bool SplineIsUsable()
            => spline != null && spline.Spline != null && spline.Spline.Count > 1;

        private void CacheLength()
        {
            _splineLength = SplineIsUsable() ? spline.CalculateLength() : 0f;
        }

        private void LateUpdate()
        {
            if (!SplineIsUsable() || _splineLength <= 0f) return;

            float dt = Time.deltaTime;

            // 1. Advance progress (skipped when stopped or paused).
            if (!_stopped && speed > 0f)
            {
                float delta01 = (speed * dt / _splineLength) * _direction;
                _progress += delta01;
                ApplyEndBehavior();
            }

            // 2. Evaluate pose in world space.
            spline.Evaluate(_progress, out float3 posF3, out float3 tangentF3, out float3 upF3);
            Vector3 targetPos = (Vector3)posF3;

            // 3. Position — exact, no smoothing. The spline itself is already C1 smooth.
            transform.position = targetPos;

            // 4. Rotation — optional smoothing for corner softness.
            if (alignToSpline)
            {
                Vector3 tangent = (Vector3)tangentF3;
                Vector3 up = (Vector3)upF3;

                // Face travel direction (flip tangent when going backward in PingPong).
                Vector3 facing = _direction >= 0 ? tangent : -tangent;

                if (facing.sqrMagnitude > 1e-6f)
                {
                    if (up.sqrMagnitude < 1e-6f) up = spline.transform.up; // defensive
                    Quaternion targetRot = Quaternion.LookRotation(facing.normalized, up.normalized);

                    transform.rotation = rotationSmooth <= 0f
                        ? targetRot
                        : Quaternion.Slerp(transform.rotation, targetRot, 1f - Mathf.Exp(-rotationSmooth * dt));
                }
            }

            // 5. Animator speed — the commanded value is exactly what the NPC travels at.
            if (animator != null && _hasSpeedParam)
            {
                animator.SetFloat(_speedHash, _stopped ? 0f : speed);
            }
        }

        private void ApplyEndBehavior()
        {
            switch (endBehavior)
            {
                case EndBehavior.Loop:
                    if (_progress > 1f) _progress -= 1f;
                    else if (_progress < 0f) _progress += 1f;
                    break;

                case EndBehavior.PingPong:
                    if (_progress > 1f) { _progress = 2f - _progress; _direction = -1; }
                    else if (_progress < 0f) { _progress = -_progress; _direction = 1; }
                    break;

                case EndBehavior.Stop:
                    if (_progress >= 1f) { _progress = 1f; _stopped = true; }
                    else if (_progress <= 0f) { _progress = 0f; _stopped = true; }
                    break;

                case EndBehavior.Disable:
                    if (_progress >= 1f || _progress <= 0f)
                    {
                        _progress = Mathf.Clamp01(_progress);
                        _stopped = true;
                        enabled = false;
                    }
                    break;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos || !SplineIsUsable()) return;

            // Re-cache length in editor so the preview works before Play.
            if (_splineLength <= 0f) _splineLength = spline.CalculateLength();
            if (_splineLength <= 0f) return;

            float gizmoT = Application.isPlaying ? _progress : Mathf.Clamp01(startProgress);
            spline.Evaluate(gizmoT, out float3 posF3, out float3 tangentF3, out float3 upF3);

            Vector3 pos = (Vector3)posF3;
            Vector3 tangent = ((Vector3)tangentF3);
            Vector3 up = ((Vector3)upF3);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(pos, 0.12f);
            if (tangent.sqrMagnitude > 1e-6f)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(pos, pos + tangent.normalized * 0.6f);
            }
            if (up.sqrMagnitude > 1e-6f)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(pos, pos + up.normalized * 0.4f);
            }
        }
#endif
    }
}

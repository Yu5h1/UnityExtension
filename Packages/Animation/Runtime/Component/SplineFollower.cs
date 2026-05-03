using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace Yu5h1Lib
{
    /// <summary>
    /// Drives an <see cref="ILocomotor"/> along a <see cref="SplineContainer"/>
    /// by computing a desired-velocity vector each frame and calling
    /// <see cref="ILocomotor.Drive"/>.
    ///
    /// Two-point lookahead model:
    ///   - Near point (<see cref="lookaheadDistance"/>): heading target. The
    ///     command vector points from current position toward this sample.
    ///   - Far  point (+ <see cref="curveLookahead"/>): tangent here is compared
    ///     against the near tangent. The angle between them = how curvy the
    ///     path is just ahead → speed scales from cruise toward minSpeed.
    ///
    /// This is intentionally a controller (not a position setter). The locomotor
    /// implementation decides what to do with the vector — a momentum-based boat
    /// keeps its inertia, an NPC turns and walks, a CharacterController slides
    /// along terrain. <see cref="SplinePoseFollower"/> is the position-setter
    /// alternative for NPCs on fixed paths.
    ///
    /// Setup:
    ///   1. Drop on the same GameObject as an <see cref="ILocomotor"/> implementation
    ///      (or assign one explicitly via <see cref="locomotorBehaviour"/>).
    ///   2. Reference a SplineContainer. For routes that move with a platform
    ///      (e.g. NPC walking the boat deck), the SplineContainer should be a
    ///      child of that platform — Evaluate respects its transform so the
    ///      sampled point automatically tracks the platform.
    ///   3. Tune <see cref="cruiseSpeed"/> (m/s) and <see cref="lookaheadDistance"/>:
    ///      - Boat (slow to turn): lookahead 10–20 m, cruise 3–6 m/s
    ///      - NPC walking: lookahead 0.5–1.5 m, cruise 1.0–1.5 m/s
    ///
    /// Note: lookaheadDistance smaller than the locomotor's stopping distance
    /// will cause oscillation (overshoot → reverse → overshoot). Err larger.
    /// </summary>
    [DisallowMultipleComponent]
    public class SplineFollower : MonoBehaviour
    {
        public enum EndBehavior { Stop, Loop, PingPong, Disable }

        [Header("References")]
        [Tooltip("Spline to follow. Evaluate respects its transform, so it can be a child of a moving platform.")]
        [SerializeField] private SplineContainer spline;

        [Tooltip("Locomotor that receives Drive(velocity) each frame. Auto-resolved on this GameObject if left empty.")]
        [SerializeField] private MonoBehaviour locomotorBehaviour;

        [Header("Lookahead")]
        [Tooltip("Distance ahead along the spline (m) used as the heading target.")]
        [SerializeField, Min(0.01f)] private float lookaheadDistance = 5f;

        [Tooltip("Additional distance past the heading target whose tangent is sampled to detect upcoming curvature. 0 = disable curve slowdown.")]
        [SerializeField, Min(0f)] private float curveLookahead = 10f;

        [Header("Speed")]
        [Tooltip("Cruise speed (m/s) on straight sections. Magnitude of the velocity vector passed to Drive().")]
        [SerializeField, Min(0f)] private float cruiseSpeed = 2f;

        [Tooltip("Speed (m/s) at maximum curvature. The vector's magnitude lerps from cruise toward this as the curve sharpens.")]
        [SerializeField, Min(0f)] private float minSpeed = 0.3f;

        [Tooltip("Curvature angle (degrees between near & far tangents) that maps to full slowdown (= minSpeed).")]
        [SerializeField, Min(1f)] private float curveAngleThreshold = 30f;

        [Tooltip("Distance from end (m) within which speed ramps to 0. Stop mode only.")]
        [SerializeField, Min(0f)] private float arriveDistance = 1.5f;

        [Header("End behaviour")]
        [SerializeField] private EndBehavior endBehavior = EndBehavior.Loop;

        [Header("Debug")]
        [SerializeField] private bool drawGizmos = true;

        // --- runtime ---
        private ILocomotor _locomotor;
        private float _splineLength;
        private int _direction = 1; // for PingPong
        private float _lastTNow;
        private float _lastTNear;
        private float _lastTFar;

        public SplineContainer Spline
        {
            get => spline;
            set { spline = value; CacheLength(); }
        }

        public ILocomotor Locomotor => _locomotor;

        /// <summary>Normalized progress on the spline (last computed). Read-only.</summary>
        public float CurrentT => _lastTNow;

        private void Reset()
        {
            // Prefer self if this GameObject already implements ILocomotor.
            var self = GetComponent<ILocomotor>() as MonoBehaviour;
            if (self != null) locomotorBehaviour = self;
        }

        private void Awake()
        {
            ResolveLocomotor();
            CacheLength();
        }

        private void OnValidate()
        {
            if (locomotorBehaviour != null && locomotorBehaviour is not ILocomotor)
            {
                Debug.LogWarning(
                    $"{nameof(SplineFollower)}: '{locomotorBehaviour.GetType().Name}' does not implement ILocomotor.",
                    this);
                locomotorBehaviour = null;
            }
        }

        private void ResolveLocomotor()
        {
            if (locomotorBehaviour is ILocomotor explicitLoco)
            {
                _locomotor = explicitLoco;
                return;
            }
            _locomotor = GetComponent<ILocomotor>();
        }

        private void CacheLength()
        {
            _splineLength = (spline != null && spline.Spline != null && spline.Spline.Count > 1)
                ? spline.CalculateLength()
                : 0f;
        }

        private void Update()
        {
            if (_locomotor == null || spline == null || _splineLength <= 0f) return;

            // 1. Project current world position onto the spline (in spline-local space).
            float3 localPos = spline.transform.InverseTransformPoint(transform.position);
            SplineUtility.GetNearestPoint(spline.Spline, localPos, out _, out float tNow);
            _lastTNow = tNow;

            // 2. Compute lookahead t values for near (heading) and far (curvature).
            float dtNear = Mathf.Clamp(lookaheadDistance / _splineLength, 0f, 1f);
            float dtFar  = Mathf.Clamp((lookaheadDistance + curveLookahead) / _splineLength, 0f, 1f);
            float tNear = AdvanceT(tNow, dtNear * _direction);
            float tFar  = AdvanceT(tNow, dtFar  * _direction);
            _lastTNear = tNear;
            _lastTFar  = tFar;

            // 3. Sample near (heading target) + tangents at both points (world space).
            spline.Evaluate(tNear, out float3 nearPosF3, out float3 nearTanF3, out _);
            spline.Evaluate(tFar,  out _,                out float3 farTanF3,  out _);
            Vector3 nearPos = (Vector3)nearPosF3;
            Vector3 nearTan = (Vector3)nearTanF3;
            Vector3 farTan  = (Vector3)farTanF3;

            // 4. Heading vector (raw — Y included; the locomotor decides whether to ignore it).
            Vector3 toNear = nearPos - transform.position;
            if (toNear.sqrMagnitude < 1e-6f)
            {
                _locomotor.Drive(Vector3.zero);
                return;
            }

            // 5. Upcoming curvature → speed ratio. Squared = stay near cruise on
            //    gentle curves, drop quickly as the corner sharpens.
            float curveAngle = (curveLookahead > 0f
                                && nearTan.sqrMagnitude > 1e-6f
                                && farTan.sqrMagnitude  > 1e-6f)
                ? Vector3.Angle(nearTan, farTan)
                : 0f;
            float curveRatio = Mathf.Clamp01(curveAngle / curveAngleThreshold);
            float speed = Mathf.Lerp(cruiseSpeed, minSpeed, curveRatio * curveRatio);

            // 6. End-behaviour adjustments to speed / direction.
            switch (endBehavior)
            {
                case EndBehavior.Stop:
                {
                    float remainingT = _direction > 0 ? (1f - tNow) : tNow;
                    float remaining = remainingT * _splineLength;
                    if (remaining < arriveDistance && arriveDistance > 0f)
                        speed *= Mathf.Clamp01(remaining / arriveDistance);
                    if (remaining <= 0.001f) speed = 0f;
                    break;
                }
                case EndBehavior.Disable:
                    if ((_direction > 0 && tNow >= 1f) || (_direction < 0 && tNow <= 0f))
                    {
                        _locomotor.Drive(Vector3.zero);
                        enabled = false;
                        return;
                    }
                    break;
                case EndBehavior.PingPong:
                    if (tNow >= 0.999f && _direction > 0) _direction = -1;
                    else if (tNow <= 0.001f && _direction < 0) _direction = 1;
                    break;
                // Loop: AdvanceT already wraps.
            }

            // 7. Issue the velocity command.
            _locomotor.Drive(toNear.normalized * speed);
        }

        private float AdvanceT(float t, float delta)
        {
            float result = t + delta;
            if (endBehavior == EndBehavior.Loop)
            {
                if (result > 1f) result -= 1f;
                else if (result < 0f) result += 1f;
            }
            else
            {
                result = Mathf.Clamp01(result);
            }
            return result;
        }

        private void OnDisable()
        {
            _locomotor?.Drive(Vector3.zero);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos || spline == null) return;

            // Re-cache in editor so gizmos show even before play.
            if (_splineLength <= 0f && spline.Spline != null && spline.Spline.Count > 1)
                _splineLength = spline.CalculateLength();
            if (_splineLength <= 0f) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.15f);

            spline.Evaluate(_lastTNear, out float3 nearF3, out _, out _);
            Vector3 nearPos = (Vector3)nearF3;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(nearPos, 0.15f);
            Gizmos.DrawLine(transform.position, nearPos);

            if (curveLookahead > 0f)
            {
                spline.Evaluate(_lastTFar, out float3 farF3, out _, out _);
                Vector3 farPos = (Vector3)farF3;
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(farPos, 0.1f);
                Gizmos.DrawLine(nearPos, farPos);
            }
        }
#endif
    }
}

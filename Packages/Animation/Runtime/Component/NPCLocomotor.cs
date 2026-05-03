using UnityEngine;
using Yu5h1Lib.Runtime;

namespace Yu5h1Lib
{
    /// <summary>
    /// Simple ground-locomotion implementation of <see cref="ILocomotor"/> for NPCs.
    ///
    /// Receives a desired world-space velocity via <see cref="Drive"/>, then each
    /// frame:
    ///   1. Rotates around world up toward the desired horizontal direction
    ///      at <see cref="turnSpeed"/> deg/s.
    ///   2. Translates along the (horizontal) forward vector at the desired
    ///      magnitude (m/s) — i.e. no strafing, the body has to face where it
    ///      walks.
    ///   3. Reports the resulting frame-delta as <see cref="Velocity"/> and
    ///      writes its magnitude into the Animator's speed parameter.
    ///
    /// Disables Animator root motion on Awake — the controller (e.g.
    /// <see cref="SplineFollower"/>) is the single source of locomotion authority.
    /// Letting the animation also push position/rotation creates fight conditions.
    ///
    /// For NPCs that need to stay anchored to a moving platform (e.g. a boat deck),
    /// pair this with a <c>TransformFollower</c> that follows only the platform's
    /// Y position — X/Z are owned by this component. Or use
    /// <see cref="SplinePoseFollower"/> directly if the path is fixed.
    /// </summary>
    [DisallowMultipleComponent]
    public class NPCLocomotor : MonoBehaviour, ILocomotor
    {
        [Header("References")]
        [Tooltip("Animator that receives the speed parameter. Optional — leave null for non-animated movers.")]
        [SerializeField] private Animator animator;

        [Header("Animator")]
        [Tooltip("Float parameter on the Animator that receives |Velocity| (m/s). Leave empty to skip.")]
        [SerializeField] private string speedParameter = "Speed";

        [Header("Motion")]
        [Tooltip("Max yaw rate (deg/s) when turning toward the desired direction.")]
        [SerializeField, Min(0f)] private float turnSpeed = 360f;

        // --- ILocomotor ---
        public Vector3 Velocity { get; private set; }
        public void Drive(Vector3 velocity) => _commanded = velocity;

        // --- runtime ---
        private Vector3 _commanded;
        private Vector3 _lastPosition;
        private int _speedHash;
        private bool _hasSpeedParam;
        public MinMax speedRange = new MinMax(2f, 6f);

        private void Reset()
        {
            animator = GetComponent<Animator>();
        }

        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (animator != null) animator.applyRootMotion = false;

            _hasSpeedParam = !string.IsNullOrEmpty(speedParameter);
            if (_hasSpeedParam) _speedHash = Animator.StringToHash(speedParameter);

            _lastPosition = transform.position;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            // 1. Resolve horizontal command (NPC walks on flat ground; ignore Y).
            Vector3 horiz = _commanded;
            horiz.y = 0f;
            float speed = horiz.magnitude;

            if (speed > 1e-4f)
            {
                Vector3 dir = horiz / speed;

                // Turn toward desired direction around world up.
                Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, targetRot, turnSpeed * dt);

                // Translate along current forward at desired magnitude.
                // (Forward catches up via the rotation step — no strafing.)
                Vector3 fwd = transform.forward;
                fwd.y = 0f;
                if (fwd.sqrMagnitude > 1e-6f)
                {
                    fwd.Normalize();
                    transform.position += fwd * (speed * dt);
                }
            }

            // 2. Report actual velocity from frame displacement.
            Velocity = (transform.position - _lastPosition) / dt;
            _lastPosition = transform.position;

            // 3. Drive Animator with achieved speed magnitude.
            if (animator != null && _hasSpeedParam)
                animator.SetFloat(_speedHash, Velocity.magnitude);
        }
    }
}

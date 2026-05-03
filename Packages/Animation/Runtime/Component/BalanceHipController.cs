using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// Counter-rotates a hip (or spine-root) bone so the upper body stays aligned to a desired up direction,
    /// even when the character root is tilted (e.g. standing on a swaying boat deck).
    ///
    /// Usage:
    ///   1. Place this on the character root (or any GameObject; <see cref="hipBone"/> is the actual target).
    ///   2. Assign <see cref="hipBone"/> to the Hips bone of the rig.
    ///   3. Default <see cref="desiredUp"/> is world Vector3.up (keep vertical).
    ///      Set it externally each frame for custom behavior (e.g. lean into wind, follow a different surface).
    ///
    /// Pair with <see cref="GroundIKTarget"/> on the feet so legs adjust to the deck while
    /// the upper body stays upright. Add SpringBone on chest/head for natural lag.
    /// </summary>
    [DisallowMultipleComponent]
    public class BalanceHipController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The hip / pelvis / spine-root bone to counter-rotate.")]
        [SerializeField] private Transform hipBone;

        [Header("Target Up")]
        [Tooltip("World-space up direction the upper body should align to. Default = Vector3.up (stay vertical).")]
        [SerializeField] private Vector3 desiredUp = Vector3.up;

        [Header("Correction")]
        [Tooltip("0 = no correction (hip follows root fully). 1 = full correction (hip stays vertical).")]
        [Range(0f, 1f)]
        [SerializeField] private float blend = 1f;

        [Tooltip("Cap how much correction can be applied. Beyond this angle, the hip simply tilts with the root.")]
        [Range(0f, 90f)]
        [SerializeField] private float maxCorrectionAngle = 45f;

        [Header("Smoothing")]
        [Tooltip("How quickly the correction follows. 0 = snap. Higher = faster. Lower values give an 'unsteady' feel.")]
        [SerializeField, Min(0f)] private float smoothingSpeed = 8f;

        // --- runtime state ---
        private Quaternion currentCorrection = Quaternion.identity;

        /// <summary>External code can set this per-frame to drive the desired up direction (e.g. read a normal sensor).</summary>
        public Vector3 DesiredUp
        {
            get => desiredUp;
            set => desiredUp = value.sqrMagnitude > 1e-6f ? value : Vector3.up;
        }

        /// <summary>Current blend amount (0..1). Reduce on extreme tilts to simulate "losing balance".</summary>
        public float Blend
        {
            get => blend;
            set => blend = Mathf.Clamp01(value);
        }

        private void LateUpdate()
        {
            if (hipBone == null) return;

            // The hip's current "up" in world space (after animation has been applied).
            Vector3 currentUp = hipBone.up;

            // Compute the rotation that would bring currentUp to desiredUp.
            Quaternion fullCorrection = Quaternion.FromToRotation(currentUp, desiredUp.normalized);

            // Clamp the magnitude of the correction.
            fullCorrection.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f) angle -= 360f; // normalize to [-180, 180]
            angle = Mathf.Clamp(angle, -maxCorrectionAngle, maxCorrectionAngle);
            Quaternion clampedCorrection = Quaternion.AngleAxis(angle * blend, axis);

            // Smooth toward the clamped correction (frame-rate independent exponential smoothing).
            float t = smoothingSpeed <= 0f ? 1f : 1f - Mathf.Exp(-smoothingSpeed * Time.deltaTime);
            currentCorrection = Quaternion.Slerp(currentCorrection, clampedCorrection, t);

            // Apply on top of the animator's pose.
            hipBone.rotation = currentCorrection * hipBone.rotation;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (hipBone == null) return;
            Vector3 origin = hipBone.position;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(origin, origin + hipBone.up * 0.3f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(origin, origin + desiredUp.normalized * 0.3f);
        }
#endif
    }
}

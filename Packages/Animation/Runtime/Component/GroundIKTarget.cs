using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// Drives a Transform (typically used as an Animation Rigging IK target)
    /// by raycasting to the ground beneath a reference point.
    ///
    /// Usage:
    ///   1. Place this on the IK target GameObject (the one referenced by TwoBoneIKConstraint.Target).
    ///   2. Assign <see cref="footReference"/> to the original foot bone (or any anchor).
    ///   3. The target transform is updated each frame to sit on the ground beneath the foot reference.
    ///
    /// One instance per limb. For a biped, attach two: one per foot target.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-50)] // run before Animator's IK pass when possible
    public class GroundIKTarget : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The foot bone (or any transform) used as the ray origin reference.")]
        [SerializeField] private Transform footReference;

        [Header("Raycast")]
        [Tooltip("Direction of the ray in world space. Default = -Vector3.up (straight down).")]
        [SerializeField] private Vector3 rayDirection = Vector3.down;

        [Tooltip("How high above the foot reference to start the ray (avoids starting below the ground).")]
        [SerializeField] private float rayStartOffset = 0.5f;

        [Tooltip("Maximum distance the ray will travel.")]
        [SerializeField] private float rayLength = 2f;

        [SerializeField] private LayerMask groundMask = ~0;

        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

        [Header("Placement")]
        [Tooltip("Local offset added to the hit point (e.g. ankle height above the sole).")]
        [SerializeField] private Vector3 hitOffset = Vector3.zero;

        [Tooltip("If true, target rotation aligns to the ground normal (with the foot's forward preserved).")]
        [SerializeField] private bool alignToNormal = true;

        [Header("Smoothing")]
        [Tooltip("Position lerp speed. 0 = snap instantly. Higher = faster follow.")]
        [SerializeField, Min(0f)] private float positionLerpSpeed = 0f;

        [Tooltip("Rotation lerp speed. 0 = snap instantly. Higher = faster follow.")]
        [SerializeField, Min(0f)] private float rotationLerpSpeed = 0f;

        [Header("Fallback")]
        [Tooltip("If no hit, follow the foot reference directly (no ground correction).")]
        [SerializeField] private bool followReferenceOnMiss = true;

        // --- runtime state ---
        public bool IsGrounded { get; private set; }
        public Vector3 GroundNormal { get; private set; } = Vector3.up;
        public Vector3 GroundPoint { get; private set; }
        public Collider GroundCollider { get; private set; }

        private void Reset()
        {
            // Try to auto-find a reference if user forgot
            if (footReference == null && transform.parent != null)
                footReference = transform.parent;
        }

        private void LateUpdate()
        {
            if (footReference == null)
                return;

            Vector3 origin = footReference.position - rayDirection.normalized * rayStartOffset;

            if (Physics.Raycast(origin, rayDirection, out var hit, rayLength + rayStartOffset, groundMask, triggerInteraction))
            {
                IsGrounded = true;
                GroundNormal = hit.normal;
                GroundPoint = hit.point;
                GroundCollider = hit.collider;

                Vector3 desiredPos = hit.point + (transform.rotation * hitOffset);
                Quaternion desiredRot = alignToNormal
                    ? Quaternion.FromToRotation(Vector3.up, hit.normal) * footReference.rotation
                    : footReference.rotation;

                ApplyTransform(desiredPos, desiredRot);
            }
            else
            {
                IsGrounded = false;
                if (followReferenceOnMiss)
                    ApplyTransform(footReference.position, footReference.rotation);
            }
        }

        private void ApplyTransform(Vector3 desiredPos, Quaternion desiredRot)
        {
            if (positionLerpSpeed <= 0f)
                transform.position = desiredPos;
            else
                transform.position = Vector3.Lerp(transform.position, desiredPos, 1f - Mathf.Exp(-positionLerpSpeed * Time.deltaTime));

            if (rotationLerpSpeed <= 0f)
                transform.rotation = desiredRot;
            else
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, 1f - Mathf.Exp(-rotationLerpSpeed * Time.deltaTime));
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (footReference == null)
                return;
            Vector3 origin = footReference.position - rayDirection.normalized * rayStartOffset;
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Gizmos.DrawLine(origin, origin + rayDirection.normalized * (rayLength + rayStartOffset));
            if (IsGrounded)
                Gizmos.DrawWireSphere(GroundPoint, 0.05f);
        }
#endif
    }
}

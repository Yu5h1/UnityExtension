using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// Makes a Transform follow another Transform with selective inheritance.
    ///
    /// Common use cases:
    ///   - NPC stands on a moving boat: follow position, optionally follow Y rotation (facing),
    ///     ignore X/Z rotation so the body stays world-vertical.
    ///   - Camera follows a target without inheriting roll.
    ///   - Anchor an effect to a moving point without parenting.
    /// </summary>
    [DisallowMultipleComponent]
    public class TransformFollower : MonoBehaviour
    {
        public enum UpdateMode { Update, LateUpdate, FixedUpdate }

        [Header("Target")]
        [SerializeField] private Transform target;

        [Tooltip("Local-space offset applied in the target's coordinate frame. Useful when the anchor isn't exactly where the follower should be.")]
        [SerializeField] private Vector3 localOffset = Vector3.zero;

        [Header("Position")]
        [SerializeField] private bool followPositionX = true;
        [SerializeField] private bool followPositionY = true;
        [SerializeField] private bool followPositionZ = true;

        [Header("Rotation")]
        [Tooltip("Inherit yaw (Y axis) — typical for 'facing direction'.")]
        [SerializeField] private bool followYaw = false;
        [Tooltip("Inherit pitch (X axis) — uncommon; usually false to keep body vertical.")]
        [SerializeField] private bool followPitch = false;
        [Tooltip("Inherit roll (Z axis) — uncommon; usually false to keep body vertical.")]
        [SerializeField] private bool followRoll = false;

        [Header("Smoothing")]
        [Tooltip("Position lerp speed. 0 = snap. Higher = faster follow.")]
        [SerializeField, Min(0f)] private float positionLerpSpeed = 0f;
        [Tooltip("Rotation lerp speed. 0 = snap. Higher = faster follow.")]
        [SerializeField, Min(0f)] private float rotationLerpSpeed = 0f;

        [Header("Update")]
        [SerializeField] private UpdateMode updateMode = UpdateMode.LateUpdate;

        public Transform Target { get => target; set => target = value; }

        private void Update()        { if (updateMode == UpdateMode.Update)      Tick(Time.deltaTime); }
        private void LateUpdate()    { if (updateMode == UpdateMode.LateUpdate)  Tick(Time.deltaTime); }
        private void FixedUpdate()   { if (updateMode == UpdateMode.FixedUpdate) Tick(Time.fixedDeltaTime); }

        private void Tick(float dt)
        {
            if (target == null) return;

            // --- Position ---
            Vector3 targetPos = target.TransformPoint(localOffset);
            Vector3 current = transform.position;
            Vector3 desired = new Vector3(
                followPositionX ? targetPos.x : current.x,
                followPositionY ? targetPos.y : current.y,
                followPositionZ ? targetPos.z : current.z);

            transform.position = positionLerpSpeed <= 0f
                ? desired
                : Vector3.Lerp(current, desired, 1f - Mathf.Exp(-positionLerpSpeed * dt));

            // --- Rotation ---
            if (followYaw || followPitch || followRoll)
            {
                Vector3 targetEuler = target.rotation.eulerAngles;
                Vector3 currentEuler = transform.rotation.eulerAngles;
                Vector3 desiredEuler = new Vector3(
                    followPitch ? targetEuler.x : currentEuler.x,
                    followYaw   ? targetEuler.y : currentEuler.y,
                    followRoll  ? targetEuler.z : currentEuler.z);

                Quaternion desiredRot = Quaternion.Euler(desiredEuler);
                transform.rotation = rotationLerpSpeed <= 0f
                    ? desiredRot
                    : Quaternion.Slerp(transform.rotation, desiredRot, 1f - Mathf.Exp(-rotationLerpSpeed * dt));
            }
        }
    }
}

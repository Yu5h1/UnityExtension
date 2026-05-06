using UnityEngine;
using System.CartesianCoordinate;
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
        [SerializeField] public Transform target;

        [SerializeField] private Optional<float> PositionX;
        [SerializeField] private Optional<float> PositionY;
        [SerializeField] private Optional<float> PositionZ;

        [Header("Look At")]
        [Tooltip("Which face of this object points toward the look target. none = disabled.")]
        public Direction lookatDirection = Direction.none;
        [Tooltip("Offset along the axis from LookTarget toward this object. Positive = move away from LookTarget (larger on screen if LookTarget is camera).")]
        [SerializeField] private float lookatDepthOffset = 0f;
        [Tooltip("The transform to look at. Falls back to target if null.")]
        [SerializeField] private Transform lookTarget;
        public Transform LookTarget => lookTarget != null ? lookTarget : target;

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

            var offset = new Vector3(
                PositionX.enabled ? PositionX.value : 0,
                PositionY.enabled ? PositionY.value : 0,
                PositionZ.enabled ? PositionZ.value : 0);
            
            Vector3 targetPos = target.TransformPoint(offset);
            Vector3 current = transform.position;

            transform.position = targetPos;
            //positionLerpSpeed <= 0f
                //? targetPos
                //: Vector3.Lerp(current, targetPos, 1f - Mathf.Exp(-positionLerpSpeed * dt));

            // --- Rotation: follow target axes ---
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

            // --- Rotation: look at LookTarget with specified face ---
            if (lookatDirection != Direction.none)
            {
                var lt = LookTarget;
                if (lt != null)
                {
                    Vector3 toTarget = lt.position - transform.position;
                    if (toTarget.sqrMagnitude > 0.0001f)
                    {
                        Quaternion desired = Quaternion.LookRotation(toTarget).LookAt(lookatDirection);
                        transform.rotation = rotationLerpSpeed <= 0f
                            ? desired
                            : Quaternion.Slerp(transform.rotation, desired, 1f - Mathf.Exp(-rotationLerpSpeed * dt));

                        // depth offset: move along the axis from LookTarget toward self
                        if (lookatDepthOffset != 0f)
                            transform.position += toTarget.normalized * -lookatDepthOffset;
                    }
                }
            }
        }
    }
}

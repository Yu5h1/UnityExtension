using System;
using UnityEngine;

namespace Yu5h1Lib.CameraControl
{
    /// <summary>
    /// Where the camera's pivot wants to be: a target plus an offset, eased toward it. It never decides which way the
    /// camera looks; that is <see cref="CameraLookDirection"/>. With no target it is idle and the controller's focus
    /// point is the pivot.
    /// </summary>
    [Serializable]
    public class CameraFollow
    {
        public enum OffsetSpace
        {
            /// <summary>The offset is added to the target position as is.</summary>
            World,
            /// <summary>The offset rotates and scales with the target.</summary>
            Target,
        }

        [SerializeField] private Transform _target;
        [SerializeField] private Vector3 _offset;
        [SerializeField] private OffsetSpace _space = OffsetSpace.World;
        [SerializeField, Min(0f)] private float _smoothing;

        public Transform target { get => _target; set => _target = value; }
        public Vector3 offset { get => _offset; set => _offset = value; }
        public OffsetSpace space { get => _space; set => _space = value; }

        /// <summary>
        /// Response speed in 1/second: the remaining distance to the goal shrinks by <c>1 - e^(-smoothing * dt)</c> each
        /// step, so the result does not depend on the frame rate. 0 follows without delay.
        /// </summary>
        public float smoothing { get => _smoothing; set => _smoothing = Mathf.Max(0f, value); }

        public bool isFollowing => _target != null;

        /// <summary>The pivot the target and offset ask for. Only valid while <see cref="isFollowing"/>.</summary>
        public Vector3 goal => _target.position + (_space == OffsetSpace.Target ? _target.TransformVector(_offset) : _offset);

        public Vector3 Step(Vector3 current, float deltaTime)
        {
            if (_smoothing <= 0f)
                return goal;
            return Vector3.Lerp(current, goal, 1f - Mathf.Exp(-_smoothing * Mathf.Max(0f, deltaTime)));
        }

        public void AddWorldOffset(Vector3 worldDelta)
            => _offset += _space == OffsetSpace.Target ? _target.InverseTransformVector(worldDelta) : worldDelta;

        public void SetGoal(Vector3 worldPoint)
            => _offset = _space == OffsetSpace.Target
                ? _target.InverseTransformVector(worldPoint - _target.position)
                : worldPoint - _target.position;
    }
}

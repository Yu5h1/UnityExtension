using System;
using UnityEngine;
using MinMax = Yu5h1Lib.Runtime.MinMax;

namespace Yu5h1Lib.CameraControl
{
    /// <summary>
    /// Which way the camera looks, kept as yaw and pitch in degrees (Unity Euler order, no roll). It is independent of
    /// <see cref="CameraFollow"/>: a locked direction stays put whatever the camera follows, which is how a 2D view
    /// keeps its plane, and an unlocked one changes only through <see cref="Orbit"/> or <see cref="Set"/>.
    /// </summary>
    [Serializable]
    public class CameraLookDirection
    {
        [SerializeField] private float _yaw;
        [SerializeField] private float _pitch;
        [SerializeField] private bool _locked;
        [SerializeField] private MinMax _pitchRange = new MinMax(-89f, 89f);
        [SerializeField] private bool _limitYaw;
        [SerializeField] private MinMax _yawRange = new MinMax(-180f, 180f);

        public float yaw { get => _yaw; set => _yaw = _limitYaw ? _yawRange.Clamp(value) : value; }
        public float pitch { get => _pitch; set => _pitch = _pitchRange.Clamp(value); }

        /// <summary>A locked direction ignores <see cref="Orbit"/>; <see cref="Set"/> still changes it.</summary>
        public bool locked { get => _locked; set => _locked = value; }

        public MinMax pitchRange { get => _pitchRange; set { _pitchRange = value; _pitch = _pitchRange.Clamp(_pitch); } }
        public bool limitYaw { get => _limitYaw; set { _limitYaw = value; if (value) _yaw = _yawRange.Clamp(_yaw); } }
        public MinMax yawRange { get => _yawRange; set { _yawRange = value; if (_limitYaw) _yaw = _yawRange.Clamp(_yaw); } }

        public Quaternion rotation => Quaternion.Euler(_pitch, _yaw, 0f);

        /// <summary>
        /// Turns the view by an amount already converted to degrees: <c>delta.x</c> turns right, <c>delta.y</c> looks up.
        /// Returns false when the direction is locked.
        /// </summary>
        public bool Orbit(Vector2 delta)
        {
            if (_locked)
                return false;
            yaw = _yaw + delta.x;
            pitch = _pitch - delta.y;
            return true;
        }

        public void Set(float yaw, float pitch)
        {
            this.yaw = yaw;
            this.pitch = pitch;
        }

        public void Set(Quaternion rotation)
        {
            var euler = rotation.eulerAngles;
            Set(Mathf.DeltaAngle(0f, euler.y), Mathf.DeltaAngle(0f, euler.x));
        }
    }
}

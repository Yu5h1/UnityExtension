using System;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   One named control interval on the timeline of a motion: a window such as "not controllable" or "cancellable"
    ///   that tells whether the motion accepts input. The bounds are stored in frames, as the animation file has them,
    ///   and in seconds; <see cref="MotionIntervals"/> turns them into normalized time.
    /// </summary>
    [Serializable]
    public struct MotionInterval
    {
        public string name;
        public int startFrame;
        public int endFrame;
        public float startTime;
        public float endTime;
        [Tooltip("The motion accepts operation control in this interval.")]
        public bool underControl;
        [Tooltip("The motion can be cancelled into another in this interval.")]
        public bool cancellable;

        /// <summary>
        ///   True when <paramref name="seconds"/> lies in [<see cref="startTime"/>, <see cref="endTime"/>). An
        ///   interval that ends at the last frame of the motion also holds the end itself.
        /// </summary>
        public bool Contains(float seconds, float duration)
            => seconds >= startTime && (seconds < endTime || (endTime >= duration && seconds <= duration));
    }
}

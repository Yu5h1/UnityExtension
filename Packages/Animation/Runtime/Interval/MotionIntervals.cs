using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    ///   The named control intervals of one humanoid motion clip, read from the <c>animations[].extras.yu5h1.intervals</c>
    ///   of the motion GLB by the humanoid importer. It is a ScriptableObject beside the clip, because an
    ///   <see cref="AnimationClip"/> cannot carry data of its own, and it lives in the runtime assembly so a game reads
    ///   it without the editor. The importer stores it as a sub-asset of the import settings and lists it in the
    ///   settings' <c>intervals</c>; a game can reference the asset directly or find it by <see cref="clip"/>.
    ///   <para>
    ///   Time is given three ways: frames (<see cref="MotionInterval.startFrame"/>), seconds, and normalized time
    ///   0..1 (seconds over <see cref="duration"/>, equal to frame over <see cref="length"/>).
    ///   </para>
    /// </summary>
    public class MotionIntervals : ScriptableObject
    {
        [SerializeField] AnimationClip _clip;
        [SerializeField] float _frameRate;
        [SerializeField] int _length;
        [SerializeField] float _duration;
        [SerializeField] List<MotionInterval> _intervals = new List<MotionInterval>();

        /// <summary>The humanoid clip these intervals belong to.</summary>
        public AnimationClip clip => _clip;
        /// <summary>Frames per second of the animation file the intervals were marked on.</summary>
        public float frameRate => _frameRate;
        /// <summary>The last frame of the animation file.</summary>
        public int length => _length;
        /// <summary>Length of the motion in seconds, <see cref="length"/> over <see cref="frameRate"/>.</summary>
        public float duration => _duration;
        public IReadOnlyList<MotionInterval> intervals => _intervals;

        /// <summary>Fills the asset; used by the importer.</summary>
        public void Set(AnimationClip clip, float frameRate, int length, float duration, IEnumerable<MotionInterval> intervals)
        {
            _clip = clip;
            _frameRate = frameRate;
            _length = length;
            _duration = duration;
            _intervals = new List<MotionInterval>(intervals);
        }

        public float Normalize(float seconds) => _duration > 0f ? seconds / _duration : 0f;

        public float NormalizedStart(MotionInterval interval) => Normalize(interval.startTime);

        public float NormalizedEnd(MotionInterval interval) => Normalize(interval.endTime);

        /// <summary>The first interval with this name, or false when there is none.</summary>
        public bool TryFind(string name, out MotionInterval interval)
        {
            foreach (var candidate in _intervals)
            {
                if (candidate.name == name)
                {
                    interval = candidate;
                    return true;
                }
            }
            interval = default;
            return false;
        }

        /// <summary>Every interval that holds <paramref name="seconds"/>, in file order.</summary>
        public IEnumerable<MotionInterval> At(float seconds)
        {
            foreach (var candidate in _intervals)
                if (candidate.Contains(seconds, _duration))
                    yield return candidate;
        }

        /// <summary>Same as <see cref="At"/> with time normalized 0..1.</summary>
        public IEnumerable<MotionInterval> AtNormalized(float normalizedTime) => At(normalizedTime * _duration);

        /// <summary>True when an interval holding <paramref name="seconds"/> is marked under control.</summary>
        public bool IsUnderControl(float seconds)
        {
            foreach (var candidate in At(seconds))
                if (candidate.underControl)
                    return true;
            return false;
        }

        /// <summary>True when an interval holding <paramref name="seconds"/> is marked cancellable.</summary>
        public bool IsCancellable(float seconds)
        {
            foreach (var candidate in At(seconds))
                if (candidate.cancellable)
                    return true;
            return false;
        }
    }
}

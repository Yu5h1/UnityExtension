using UnityEngine;

namespace Yu5h1Lib
{
    /// <summary>
    /// Turns elapsed time into a looping frame index at a fixed rate.
    /// <para>Composed in as a serialized field rather than inherited from, so an owner that needs frame
    /// stepping keeps its single base class free, and frame stepping stays usable outside the material
    /// drivers that first needed it.</para>
    /// <para>Knows only its rate and the frame count it is handed. Anything about what a frame means —
    /// a sheet cell, a texture in a list — belongs to the caller that owns that concept.</para>
    /// <para>Holds no position: the index is derived from <see cref="Time.time"/>, so every owner sharing
    /// one asset reports the same frame at the same moment. Duplicate the owning asset for independent timing.</para>
    /// </summary>
    [System.Serializable]
    public class FrameStepResolver
    {
        [Tooltip("Frames per second for the time-driven frame index.")]
        [SerializeField, Min(0f)] private float fps = 12f;

        /// <summary>Frame index within <c>[0, frameCount)</c>, or <c>0</c> when there is nothing to step through.</summary>
        public int Resolve(int frameCount)
            => frameCount <= 0 ? 0 : (int)(Time.time * fps) % frameCount;
    }
}

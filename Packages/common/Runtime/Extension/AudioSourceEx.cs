using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace Yu5h1Lib
{

    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
    public static class AudioSourceEx
    {
        public static bool TryStop(this AudioSource audioSource)
        {
            if (audioSource == null || !audioSource.isPlaying)
                return false;
            audioSource.Stop();
            return true;
        }
        public static bool NotPlaying(this AudioSource audioSource) => audioSource?.isPlaying ?? true;
    }
}

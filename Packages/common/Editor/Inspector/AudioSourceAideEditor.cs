using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Yu5h1Lib.EditorExtension;

namespace Yu5h1Lib
{
	[CustomEditor(typeof(AudioSourceAide))]
	public class AudioSourceAideEditor : Editor<AudioSourceAide>
	{
		AudioSource audio => targetObject.component;
        float playbackNormalize => audio.time / audio.clip.length;

        public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			if (targetObject.component && audio.isPlaying && audio.clip != null)
            {
				float n = EditorGUILayout.Slider(playbackNormalize, 0, 1);
				if (n != playbackNormalize)
					audio.time = n * audio.clip.length;

            }
			
		}
	} 
}
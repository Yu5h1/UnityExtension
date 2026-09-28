using UnityEditor;
using UnityEngine;

namespace Yu5h1Lib.EditorExtension
{
    [CustomEditor(typeof(AudioMixerProxy))]
    public class AudioMixerProxyEditor : Editor<AudioMixerProxy>
    {
        private const string VoiceMixerFieldName = "_voiceMixer";

        public override void DrawProperty(SerializedProperty property)
        {
            if (property.name != VoiceMixerFieldName)
            {
                base.DrawProperty(property);
                return;
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(property);
                if (property.objectReferenceValue == null && GUILayout.Button("Create Mixer", GUILayout.Width(100)))
                    CreateMixer(property);
            }
        }

        private void CreateMixer(SerializedProperty property)
        {
            var path = EditorUtility.SaveFilePanelInProject("Create Audio Mixer", $"{targetObject.name} Mixer", "mixer",
                "Choose where to save the new AudioMixer asset.");
            if (string.IsNullOrEmpty(path))
                return;
            if (!AudioMixerBuilder.TryCreate(path, targetObject.defaultGroupNames, out var mixer, out var error))
            {
                error.printError();
                return;
            }
            property.objectReferenceValue = mixer;
        }
    }
}

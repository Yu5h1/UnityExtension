using UnityEngine;
using UnityEngine.Audio;

namespace Yu5h1Lib
{
    [DisallowMultipleComponent]
    public class AudioMixerProxy : MonoBehaviour
    {
        [SerializeField] private AudioMixer _voiceMixer;
        public AudioMixer voiceMixer => _voiceMixer;

        public float sound
        {
            get => GetGroupVolume(nameof(sound));
            set => SetGroupVolume(nameof(sound), value);
        }
        public float bgm
        {
            get => GetGroupVolume(nameof(bgm));
            set => SetGroupVolume(nameof(bgm), value);
        }
        public float voice
        {
            get => GetGroupVolume(nameof(voice));
            set => SetGroupVolume(nameof(voice), value);
        }

        public void ToggleGroupVolume(string groupName)
        {
            if (GetGroupVolume(groupName) > 0f)
                SetGroupVolume(groupName, 0f);
            else
                SetGroupVolume(groupName, 1f);
        }

        public readonly string[] defaultGroupNames = new string[]
        {
            nameof(sound),
            nameof(bgm),
            nameof(voice)
        };

        public void SetGroupVolume(string groupName, float volume)
        {
            float dbVolume = volume > 0 ? Mathf.Log10(volume) * 20f : -80f;
            voiceMixer.SetFloat(groupName, dbVolume);
        }
        public float GetGroupVolume(string groupName)
        {
            voiceMixer.GetFloat(groupName, out float dbValue);
            if (dbValue <= -80f)
                return 0f;
            else
                return Mathf.Pow(10f, dbValue / 20f);
        }
    }
}

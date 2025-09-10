using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[DisallowMultipleComponent]
public class AudioMixerController : MonoBehaviour
{
    [SerializeField] private AudioMixer _voiceMixer;
    public AudioMixer voiceMixer => _voiceMixer;


    public float sfx_Volume
    {
        get => GetGroupVolume(nameof(sfx_Volume));
        set => SetGroupVolume(nameof(sfx_Volume), value);
    }
    public float bgm_Volume
    {
        get => GetGroupVolume(nameof(bgm_Volume));
        set => SetGroupVolume(nameof(bgm_Volume), value);
    }
    public float voice_Volume 
    {
        get => GetGroupVolume(nameof(voice_Volume));
        set => SetGroupVolume(nameof(voice_Volume), value);
    }

    public void ToggleGroupVolume(string groupName)
    {
        if (GetGroupVolume(groupName) > 0f)
            SetGroupVolume(groupName, 0f);
        else
            SetGroupVolume(groupName, 1f);
    }

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

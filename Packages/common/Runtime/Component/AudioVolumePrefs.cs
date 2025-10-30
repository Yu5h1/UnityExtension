using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Yu5h1Lib;

public class AudioVolumePrefs : SingletonBehaviour<AudioVolumePrefs>
{
    [SerializeField] private ObservableFloatPref bgmVolumeInfo = new ObservableFloatPref(){
        key = nameof(bgmVolume),
        defaultValue = 0.5f
    };
    public static float bgmVolume
    {
        get => instance.bgmVolumeInfo.Value;
        set => instance.bgmVolumeInfo.Value = value;
    }

    [SerializeField] private ObservableBoolPref bgmVolume_enabledPref = new ObservableBoolPref()
    {
        key = nameof(bgmVolume_enabled),
        defaultValue = true
    };
    public static bool bgmVolume_enabled
    {
        get => instance.bgmVolume_enabledPref.Value;
        set => instance.bgmVolume_enabledPref.Value = value;
    }

    [SerializeField] private ObservableFloatPref sfxVolumeInfo = new ObservableFloatPref() {
        key = nameof(sfxVolume),
        defaultValue = 0.5f
    };
    public static float sfxVolume
    {
        get => instance.sfxVolumeInfo.Value;
        set => instance.sfxVolumeInfo.Value = value;
    }
    
    [SerializeField] private ObservableFloatPref voiceVolumeInfo = new ObservableFloatPref()
    {
        key = nameof(voiceVolume),
        defaultValue = 0.5f
    };
    public static float voiceVolume
    {
        get => instance.voiceVolumeInfo.Value;
        set => instance.voiceVolumeInfo.Value = value;
    }

    protected override void OnInstantiated() { }

    protected override void OnInitializing() 
    {
        
    }
    private void Start()
    {
        bgmVolumeInfo.Init(false);
        sfxVolumeInfo.Init(false);
        voiceVolumeInfo.Init(false);
        bgmVolume_enabledPref.Init(false);
    }
    //[SerializeField]
    //public void ResetValue()
    //{
    //    PlayerPrefs.DeleteKey(nameof(bgmVolume));
    //    PlayerPrefs.DeleteKey(nameof(sfxVolume));
    //    PlayerPrefs.DeleteKey(nameof(voiceVolume));
    //}
}

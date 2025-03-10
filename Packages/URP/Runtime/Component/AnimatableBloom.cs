using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Yu5h1Lib;

[System.Serializable]
public class AnimatableBloom : VolumeController.Animatable<Bloom>
{
    public float intensity;
    public override void Read()
    {
        intensity = Component.intensity.value;
    }

    public override void Write()
    {
        Component.intensity.value = intensity;
    }
}

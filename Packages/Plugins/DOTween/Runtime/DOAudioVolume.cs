using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yu5h1Lib
{
    public class DOAudioVolume : TweenBehaviour<AudioSource, float, FloatOptions>
    {
        protected override TweenerCore<float, float, FloatOptions> CreateTweenCore()
        {
            throw new System.NotImplementedException();
        }
    }
}

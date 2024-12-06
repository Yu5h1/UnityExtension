using DG.Tweening;
using DG.Tweening.Plugins.Options;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using FloatTweener = DG.Tweening.Core.TweenerCore<float, float, DG.Tweening.Plugins.Options.FloatOptions>;

namespace Yu5h1Lib
{
    public class DOTimeScale : TweenFloat<Transform>
    {
        protected override float GetFloat() => Time.timeScale;
        protected override void SetFloat(float val) => Time.timeScale = val;
    }
}

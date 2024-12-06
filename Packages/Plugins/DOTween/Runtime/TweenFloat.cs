using DG.Tweening;
using DG.Tweening.Plugins.Options;
using UnityEngine;

namespace Yu5h1Lib
{
    using FloatTweener = DG.Tweening.Core.TweenerCore<float, float, DG.Tweening.Plugins.Options.FloatOptions>;
    public abstract class TweenFloat<T> : TweenBehaviour<T, float, float, FloatOptions> where T : Component
    {
        protected abstract float GetFloat();
        protected abstract void SetFloat(float val);
        protected override FloatTweener CreateTweenCore()
        {
            var t = DOTween.To(GetFloat, SetFloat, endValue, Duration);
            t.SetTarget(component);
            return t;
        }
    }
}

using UnityEngine;
using DG.Tweening;
using DG.Tweening.Plugins.Options;
using Yu5h1Lib;

using FloatTweener = DG.Tweening.Core.TweenerCore<float, float, DG.Tweening.Plugins.Options.FloatOptions>;

[RequireComponent(typeof(CanvasGroup))]
public class DOFadeCanvasGroup : TweenBehaviour<CanvasGroup,float,float,FloatOptions>
{
    protected override FloatTweener CreateTweenCore() => component.DOFade(endValue, Duration);
}

using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;

[RequireComponent(typeof(Image))]
public class TweenImage_UI : TweenColorRenderer<Image>
{
    protected override TweenerCore<Color, Color, ColorOptions> CreateTweenCore()
        => component.DOColor(endValue, Duration);
    
}

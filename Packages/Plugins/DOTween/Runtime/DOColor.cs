using UnityEngine;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class DOColor : TweenColorRenderer<Component>
{
    public override Component OverrideGetComponent()
    {
        if (TryGetComponent(out Graphic g))
            return g;
        if (TryGetComponent(out SpriteRenderer s))
            return s;
        
        return base.OverrideGetComponent();
    }

    protected override void ResetStartValue()
    {
        startValue = component switch
        {
            SpriteRenderer renderer => renderer.color,
            Graphic g => g.color,
            _ => Color.white
        };
    }
    protected override void ResetEndValue()
    {
        endValue = component switch
        {
            SpriteRenderer renderer => renderer.color,
            Graphic g => g.color,
            _ => Color.black
        };
    }

    protected override TweenerCore<Color, Color, ColorOptions> CreateTweenCore()
    {
        switch (component)
        {
            case SpriteRenderer renderer: return renderer.DOColor(_endValue, Duration);
            case Graphic g: return g.DOColor(_endValue, Duration);
            default:  throw new System.NotSupportedException("DOColor require SpriteRenderer or Graphic");
        }
        
    } 
}

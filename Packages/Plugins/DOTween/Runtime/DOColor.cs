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
        if (transform is RectTransform m)
            return m.GetComponent<Image>();
        return GetComponent<SpriteRenderer>();
    }
    protected override TweenerCore<Color, Color, ColorOptions> CreateTweenCore()
    {
        switch (component)
        {
            case SpriteRenderer renderer: return renderer.DOColor(_endValue, Duration);
            case Image img: return img.DOColor(_endValue, Duration);
            default:  throw new System.NotSupportedException("DOColor require SpriteRenderer or Image");
        }
        
    } 
}

using UnityEngine;
using DG.Tweening;
using DG.Tweening.Plugins.Options;
using Yu5h1Lib;
using FloatTweener = DG.Tweening.Core.TweenerCore<float, float, DG.Tweening.Plugins.Options.FloatOptions>;
using System;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class DOFade : TweenBehaviour<Component,float,float,FloatOptions>
{
    public override Component OverrideGetComponent()
    {
        if (transform is RectTransform m)
        {
            if (TryGetComponent(out CanvasGroup canvasGroup))
                return canvasGroup;
            return m.GetComponent<Image>();
        }
        return GetComponent<SpriteRenderer>();
    }
    protected override FloatTweener CreateTweenCore() {
        switch (component)
        {
            case CanvasGroup canvasGroup:
                return canvasGroup.DOFade(endValue, Duration);
            case Image img:
            case SpriteRenderer renderer :
                return DOTween.To(GetAlpha, SetAlpha, endValue, Duration);
            default:
                throw new NullReferenceException($"{component} DOFade require CanvasGroup or SpriteRenderer");
        }
        
    }
    private float GetAlpha() {
        switch (component)
        {
            case Image img:
                return img.color.a;
            case SpriteRenderer renderer:
                return renderer.color.a;
        }
        throw new NullReferenceException($"{component} DOFade require CanvasGroup ,Image or SpriteRenderer");
    }
    private void SetAlpha(float alpha)
    {
        Color color = default(Color);
        switch (component)
        {
            case Image img:
                color = img.color;
                color.a = alpha;
                img.color = color;
                break;
            case SpriteRenderer renderer:
                color = renderer.color;
                color.a = alpha;
                renderer.color = color;
                break;
                case CanvasGroup c:
                c.alpha = alpha;
                break;
        }
    }
    [ContextMenu("Rewind")]
    public void Rewind()
    {
        tweener.Rewind();
    }
}

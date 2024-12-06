using DG.Tweening;
using DG.Tweening.Plugins.Options;
using UnityEngine;
using Yu5h1Lib;

public abstract class TweenColorRenderer<T> : TweenBehaviour<T,Color,ColorOptions> where T : Component
{
    public Color color {
        get => component switch
        {
            SpriteRenderer spriteRenderer => spriteRenderer.color,
            _ => default(Color)
        };
        set
        {
            switch (component)
            {
                case SpriteRenderer spriteRenderer:
                    spriteRenderer.color = value;
                    break;
                case UnityEngine.UI.Image image:
                    image.color = value;
                    break;
            }
        }
    }
    private void Reset()
    {
        ChangeStartValue = true;
    }
    public void FadeIn(bool StartwithZeroAlpha = true)
    {
        if (StartwithZeroAlpha)
        {
            var c = color;
            c.a = 0;
            color = c;
        }
        Kill();
        _endValue.a = 1;
        Start();
    }
    public void FadeIn(Color to, bool StartwithzeroAlpha = true)
    {
        _endValue = to;
        FadeIn(StartwithzeroAlpha);
    }
    public void FadeIn(Color from, Color to, bool StartwithzeroAlpha = true)
    {
        color = from;
        FadeIn(to, StartwithzeroAlpha);
    }
    public void FadeOut()
    {
        Kill();
        _endValue = color;
        _endValue.a = 0;
        Start();
    }
    public void FadeOut(Color to)
    {
        _endValue = to;
        _endValue.a = 0;
        FadeOut();
    }
    public void FadeOut(Color from, Color to)
    {
        color = from;
        FadeOut(to);
    }
}

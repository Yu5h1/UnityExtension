using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Unity.VisualScripting;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Yu5h1Lib;

[RequireComponent(typeof(SpriteRenderer))]
public class DOSpriteRendererColor : TweenColorRenderer<SpriteRenderer>
{
    protected override TweenerCore<Color, Color, ColorOptions> CreateTweenCore()
        => component.DOColor(_endValue, Duration);
}

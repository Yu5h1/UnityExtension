using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yu5h1Lib;

public class DORotate2D : DOTransform<Quaternion, Vector3, QuaternionOptions>
{
    public RotateMode RotateMode = RotateMode.Fast;

    protected override TweenerCore<Quaternion, Vector3, QuaternionOptions> CreateTweenCore()
         => local ? transform.DOLocalRotate(endValue, Duration) :
                    transform.DORotate(endValue, Duration);


}

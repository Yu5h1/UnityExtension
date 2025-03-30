using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;
using Yu5h1Lib;

public class DOMove2D : DOTransform<Vector3,VectorOptions>
{
    [SerializeField,ReadOnly]
    public Vector2 velocity ;//{ get; private set; }

    protected override void Awake()
    {
        velocity = (_endValue - _startValue) / Duration;
        _startValue = transform.TransformPoint(_startValue);
        _endValue = transform.TransformPoint(_endValue);
        base.Awake();
    }
    protected override TweenerCore<Vector3, Vector3, VectorOptions> CreateTweenCore()
        => transform.DOMove(endValue, Duration);

    protected override void OnComplete() => OnComplete(velocity);
    protected override void OnRewind() => OnRewind(-velocity);

    protected override void ResetStartValue() => _startValue = Vector3.zero;
    protected override void ResetEndValue() => _endValue = Vector3.zero;

    public override string ToString() => tweener.ToString();

    [ContextMenu("endvalue to local")]
    public void valueToLoacl()
    {
        _endValue = transform.InverseTransformPoint(_endValue);
    }
}

using System;
using UnityEngine;
using Yu5h1Lib.Common;
using Yu5h1Lib.Runtime;

public interface ITextOps : ITextAttribute, IOps
{
    void CrossFadeAlpha(float alpha, float duration, bool ignoreTimeScale);

    void ForceUpdate();
    float GetActualFontSize();
    float GetWrapDistance();
    float GetFirstLineOffsetY();
}
public interface ITextAttribute
{
    string text { get; set; }
    Color color { get; set; }
    float fontSize { get; set; }
    float lineSpacing { get; set; }
    Alignment alignment { get; set; }
    CanvasRenderer canvasRenderer { get; }
}
public abstract class TextOps<T> : OpsBase<T>, ITextOps where T : Component
{
    protected TextOps(T component) : base(component) { }
    public abstract string text { get; set; }
    public abstract Color color { get; set; }
    public abstract float fontSize { get; set; }
    public abstract float lineSpacing { get; set; }
    public abstract Alignment alignment { get; set; }

    public abstract CanvasRenderer canvasRenderer { get; }

    
    public abstract void CrossFadeAlpha(float alpha, float duration, bool ignoreTimeScale);

    public abstract void ForceUpdate();
    public abstract float GetActualFontSize();
    public abstract float GetWrapDistance();
    public abstract float GetFirstLineOffsetY();

    public void Apply(ITextAttribute setting)
    {
        text = setting.text;
        color = setting.color;
        fontSize = setting.fontSize;
        lineSpacing = setting.lineSpacing;
        alignment = setting.alignment;
    }
}

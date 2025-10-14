using System;
using UnityEngine;
using Yu5h1Lib.Common;
using Yu5h1Lib.Runtime;

public interface ITextOps : ITextAttribute, IOps
{
    float preferredWidth { get; }
    float preferredHeight { get; }
    float GetTextWidth(bool forceUpdate);
    float GetLineY(int index,bool local);
    float GetBaseLineHeight();
    int GetLineCount();
    int GetLineIndexByPosition(int pos);

    Vector3 GetCharacterPosition(int pos,bool local);
    void CrossFadeAlpha(float alpha, float duration, bool ignoreTimeScale);
    void ForceUpdate();
    void SetLayoutDirty();
    void SetWrappingOverflowMode(bool wrap);
    
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
    
    public abstract float preferredWidth { get; }
    public abstract float preferredHeight { get; }
    public virtual float GetTextWidth(bool forceUpdate)
    {
        if (forceUpdate)
            ForceUpdate();
        return preferredWidth;
    }
    public abstract Color color { get; set; }
    public abstract float fontSize { get; set; }
    public abstract float lineSpacing { get; set; }    
    public abstract Alignment alignment { get; set; }    
    public abstract CanvasRenderer canvasRenderer { get; }

    public abstract void SetWrappingOverflowMode(bool wrap);
    public abstract void CrossFadeAlpha(float alpha, float duration, bool ignoreTimeScale);

    public abstract void ForceUpdate();
    public abstract float GetLineY(int index,bool local);
    public abstract float GetBaseLineHeight();
    public abstract void SetLayoutDirty();
    public abstract int GetLineCount();
    public abstract int GetLineIndexByPosition(int position);
    public abstract Vector3 GetCharacterPosition(int pos,bool local);

    public void Apply(ITextAttribute setting)
    {
        text = setting.text;
        color = setting.color;
        fontSize = setting.fontSize;
        lineSpacing = setting.lineSpacing;
        alignment = setting.alignment;
    }
}

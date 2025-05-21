using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Yu5h1Lib.UI;
using Yu5h1Lib.Runtime;
using UnityEditor;

public class TMP_TextAdapter : TextAdapter<Text, TMP_Text>
{
    public override string GetText(Text t0) => t0.text;
    public override string GetText(TMP_Text t1) => t1.text;
    public override void SetText(Text t0, string val) => t0.text = val;
    public override void SetText(TMP_Text t1, string val) => t1.text = val;

    public override Color GetColor(Text t) => t.color;
    public override Color GetColor(TMP_Text t) => t.color;
    public override void SetColor(Text t, Color val) => t.color = val;
    public override void SetColor(TMP_Text t, Color val) => t.color = val;

 
    public override void CrossFadeAlpha(Text t0, float alpha, float duration, bool ignoreTimeScale)
        => t0.CrossFadeAlphaFixed(alpha, duration, ignoreTimeScale);

    public override void CrossFadeAlpha(TMP_Text t1, float alpha, float duration, bool ignoreTimeScale)
        => t1.CrossFadeAlpha(alpha, duration, ignoreTimeScale);
    //=> t1.CrossFadeAlphaFixed(alpha, duration, ignoreTimeScale);

    public override CanvasRenderer GetCanvasRenderer(Text t0) => t0.canvasRenderer;
    public override CanvasRenderer GetCanvasRenderer(TMP_Text t1) => t1.canvasRenderer;

    public override Alignment GetAlignment(Text t0) => (Alignment)(int)t0.alignment;
    public override void SetAlignment(Text t0, Alignment val) => t0.alignment = (TextAnchor)(int)val;

    public override Alignment GetAlignment(TMP_Text t) => (Alignment)(int)t.alignment;
    public override void SetAlignment(TMP_Text t, Alignment val)
    {
        switch (val)
        {
            case Alignment.Top:
                t.alignment = TextAlignmentOptions.Top;
                break;
            case Alignment.Bottom:
                t.alignment = TextAlignmentOptions.Bottom;
                break;
            case Alignment.Left:
                break;
            case Alignment.Right:
                break;
            case Alignment.Top_Left:
                break;
            case Alignment.Top_Right:
                break;
            case Alignment.Bottom_Left:
                break;
            case Alignment.Bottom_Right:
                break;
            case Alignment.Center:
                break;
            default:
                break;
        }
        
    }
 
}

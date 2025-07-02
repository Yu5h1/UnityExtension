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

    protected override float GetFontSize(Text t0) => t0.fontSize;
    protected override void SetFontSize(Text t0, float value) => t0.fontSize = (int)value;
    protected override float GetFontSize(TMP_Text t) => t.fontSize;
    protected override void SetFontSize(TMP_Text t, float value) => t.fontSize = value;

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
            case Alignment.TopLeft:
                break;
            case Alignment.TopRight:
                break;
            case Alignment.BottomLeft:
                break;
            case Alignment.BottomRight:
                break;
            case Alignment.Center:
                break;
            case Alignment.Fill:
                break;
            default:
                break;
        }
        
    }


    protected override void Reset()
    {
        base.Reset();
    }

    protected override float GetlineSpacing(Text t)             => t.lineSpacing;
    protected override void SetlineSpacing(Text t, float value) => t.lineSpacing = value;
    protected override float GetlineSpacing(TMP_Text t) => t.lineSpacing;
    protected override void SetlineSpacing(TMP_Text t, float value)  => t.lineSpacing = value;

    public override float GetActualFontSize()
    {
        switch (component)
        {
            case Text text:
                // Best Fit 模式：獲取實際渲染的字體大小
                TextGenerator textGen = text.cachedTextGenerator;

                // 強制更新文字生成器
                var generationSettings = text.GetGenerationSettings(text.rectTransform.rect.size);
                textGen.Populate(text.text, generationSettings);

                // 從 TextGenerator 獲取實際使用的字體大小
                float actualSize = textGen.fontSizeUsedForBestFit;

                // 如果獲取失敗，使用備用方法
                if (actualSize <= 0)
                {
                    actualSize = Mathf.Min(text.resizeTextMaxSize,
                        Mathf.Max(text.resizeTextMinSize, text.fontSize));
                }
                return actualSize;
            case TMP_Text tmp_text:
                tmp_text.ForceMeshUpdate();
                return tmp_text.fontSize;
        }
        return 0f;
    }
    public override float GetWrapDistance()
    {
        switch (component)
        {
            case Text text:
                UILineInfo[] linesInfo = text.cachedTextGenerator.GetLinesArray();
                return linesInfo[0].height; 
            case TMP_Text tmp_text:
                tmp_text.ForceMeshUpdate();

                if (tmp_text.textInfo?.lineInfo != null && tmp_text.textInfo.lineInfo.Length > 0)
                    return tmp_text.textInfo.lineInfo[0].lineHeight;
                break;
        }
        return 0f;
    }
    public override float GetFirstLineOffsetY()
    {
        switch (component)
        {
            case Text text:
                UILineInfo[] linesInfo = text.cachedTextGenerator.GetLinesArray();
                return linesInfo[0].height;
            case TMP_Text tmp_text:
                tmp_text.ForceMeshUpdate();

                if (tmp_text.textInfo?.lineInfo != null && tmp_text.textInfo.lineInfo.Length > 0)                
                return tmp_text.textInfo.lineInfo[0].baseline +( GetActualFontSize() * 0.5f);
                break;
        }
        return 0f;
    }
}

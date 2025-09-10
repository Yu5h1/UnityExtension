using UnityEngine;
using UnityEngine.UI;
using Yu5h1Lib.Common;
using Yu5h1Lib.Runtime;
using Yu5h1Lib.UI;

public sealed class UI_TextOps : TextOps<Text>
{
    public UI_TextOps(Text t) : base(t) { }

    public override string text { get => c.text; set => c.text = value; }
    public override Color color { get => c.color; set => c.color = value; }

    public override float fontSize { get => c.fontSize; set => c.fontSize = (int)value; }
    public override float lineSpacing { get => c.lineSpacing; set => c.lineSpacing = value; }

    public override Alignment alignment
    {
        get => c.alignment.ToAlignment();
        set => c.alignment = value.ToTextAnchor();
    }

    public override CanvasRenderer canvasRenderer => c.canvasRenderer;

    public override void CrossFadeAlpha(float a, float d, bool ig)
        => c.CrossFadeAlphaFixed(a, d, ig);

    public override void ForceUpdate() {}

    public override float GetActualFontSize()
    {
        var gen = c.cachedTextGenerator;
        var gs = c.GetGenerationSettings(c.rectTransform.rect.size);
        gen.Populate(c.text, gs);
        var s = gen.fontSizeUsedForBestFit;
        return (s > 0) ? s : Mathf.Clamp(c.fontSize, c.resizeTextMinSize, c.resizeTextMaxSize);
    }

    public override float GetWrapDistance()
        => c.cachedTextGenerator.GetLinesArray()[0].height;

    public override float GetFirstLineOffsetY() => c.cachedTextGenerator.GetLinesArray()[0].height;
    public override int GetLineCount() => c.cachedTextGenerator.lineCount;




#if UNITY_EDITOR
[UnityEditor.InitializeOnLoadMethod]
#else
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
    private static void RegisterTextOpsConstructor()
    {
        OpsFactory.Register<Text,ITextOps>(t => new UI_TextOps(t));
    }

}

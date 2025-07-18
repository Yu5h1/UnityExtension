using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Yu5h1Lib.Common;
using Yu5h1Lib.Runtime;
using Yu5h1Lib.UI;

public sealed class TMPTextOps : TextOps<TMP_Text>
{
    public TMPTextOps(TMP_Text t) : base(t) {}

    public override string text { get => c.text; set => c.text = value; }
    public override Color color { get => c.color; set => c.color = value; }

    public override float fontSize { get => c.fontSize; set => c.fontSize = value; }
    public override float lineSpacing { get => c.lineSpacing; set => c.lineSpacing = value; }

    public override Alignment alignment
    {
        get => c.alignment.ToAlignment();
        set => c.alignment = value.ToAlignmentOption();
    }
    public override CanvasRenderer canvasRenderer => c.canvasRenderer;

    public override void CrossFadeAlpha(float a, float d, bool ig)
        => c.CrossFadeAlpha(a, d, ig);

    public override float GetActualFontSize()
    {
        c.ForceMeshUpdate();
        return c.fontSize;
    }

    public override float GetWrapDistance()
    {
        c.ForceMeshUpdate();

        if (c.textInfo?.lineInfo != null && c.textInfo.lineInfo.Length > 0)
            return c.textInfo.lineInfo[0].lineHeight;
        return 0;
    }

    public override float GetFirstLineOffsetY()
    {
        if (c.textInfo?.lineInfo != null && c.textInfo.lineInfo.Length > 0)
            return c.textInfo.lineInfo[0].baseline + (GetActualFontSize() * 0.5f);
        return 0;
    }
   

#if UNITY_EDITOR
[UnityEditor.InitializeOnLoadMethod]
#else
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
    private static void RegisterTextOpsConstructor()
    {
        OpsFactory.Register<TMP_Text,ITextOps>(t => new TMPTextOps(t));
    }

}

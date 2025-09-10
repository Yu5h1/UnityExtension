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

    public override void ForceUpdate() => c.ForceMeshUpdate();

    public override float GetActualFontSize()
    {
        ForceUpdate();
        return c.fontSize;
    }

    public override float GetWrapDistance()
    {
        ForceUpdate();
        if (c.textInfo?.lineInfo != null && c.textInfo.lineInfo.Length > 0)
            return c.textInfo.lineInfo[0].lineHeight;
        return 0;
    }

    public override float GetFirstLineOffsetY()
    {

        //c.ForceMeshUpdate();

        //return GetActualFontSize();
        //c.textInfo.lineInfo[0].baseline.print();
        //return ((TMP_Text)RawComponent).textBounds.max.y;

        if (c.textInfo?.characterInfo != null && c.textInfo.characterCount > 0)
        {
            var firstChar = c.textInfo.characterInfo[0];
            if (firstChar.isVisible)
            {
                return firstChar.bottomLeft.y;
            }
        }

        //if (c.textInfo?.lineInfo != null && c.textInfo.lineInfo.Length > 0)
        //    return c.textInfo.lineInfo[0].baseline + (GetActualFontSize() * 0.5f);
        return 0;

        //return 56;

        //c.ForceMeshUpdate();
        //// 檢查文字信息是否有效
        //if (c.textInfo?.lineInfo != null &&
        //    c.textInfo.lineInfo.Length > 0 &&
        //    c.textInfo.characterCount > 0)
        //{
        //    var firstLine = c.textInfo.lineInfo[0];

        //    // 使用 ascender 或 baseline 都可以，看你的需求
        //    return firstLine.ascender;  // 或 firstLine.baseline
        //}

        //return c.fontSize * 0.8f;
    }

    public override int GetLineCount() => c.textInfo == null ? 0 : c.textInfo.lineCount;



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

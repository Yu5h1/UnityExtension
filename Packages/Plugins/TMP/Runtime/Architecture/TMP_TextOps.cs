using TMPro;
using UnityEngine;
using UnityEngine.Scripting;
using Yu5h1Lib;
using Yu5h1Lib.Runtime;
using Yu5h1Lib.UI;

[OpsRegistration(typeof(TMP_Text), typeof(ITextOps))]
public sealed class TMPTextOps : TextOps<TMP_Text>
{
    [Preserve] public TMPTextOps(TMP_Text t) : base(t) {}

    public override string text { get => c.text; set => c.text = value; }
    public override float preferredWidth => c.preferredWidth;
    public override float preferredHeight => c.preferredHeight;
    public override Color color { get => c.color; set => c.color = value; }

    public override float fontSize { get => c.fontSize; set => c.fontSize = value; }
    public override float lineSpacing { get => c.lineSpacing; set => c.lineSpacing = value; }

    public override Alignment alignment
    {
        get => c.alignment.ToAlignment();
        set => c.alignment = value.ToAlignmentOption();
    }

    public override CanvasRenderer canvasRenderer => c.canvasRenderer;


    public override void SetWrappingOverflowMode(bool wrap)
    {
        if (wrap)
        {
            c.textWrappingMode = TextWrappingModes.PreserveWhitespace;
            c.overflowMode = TextOverflowModes.ScrollRect;
        }
        else
        {
            c.textWrappingMode = TextWrappingModes.Normal;
            c.overflowMode = TextOverflowModes.Overflow;
        }        
    }

    public override void CrossFadeAlpha(float a, float d, bool ig)
        => c.CrossFadeAlpha(a, d, ig);

    public override void ForceUpdate() => c.ForceMeshUpdate();

    public override float GetBaseLineHeight()
    {
        ForceUpdate();
        if (c.textInfo?.lineInfo != null && c.textInfo.lineInfo.Length > 0)
            return c.textInfo.lineInfo[0].lineHeight;
        return 0;
    }

    public override float GetLineY(int index, bool local)
    {
        var y = 0f;
        if (c.textInfo?.lineInfo != null && c.textInfo.lineInfo.Length > index && index >= 0)
            y = c.textInfo.lineInfo[index].ascender;
        return local ? y : c.transform.TransformPoint(0,y,0).y;
    }

    public override void SetLayoutDirty() => c.SetLayoutDirty();
    public override int GetLineCount() => c.textInfo == null ? 0 : c.textInfo.lineCount;

    public override int GetLineIndexByPosition(int pos)
    {
        if (c == null )
            return 0;

        c.ForceMeshUpdate();
        
        pos = Mathf.Clamp(pos, 0, c.text.Length);

        var textInfo = c.textInfo;

        if (textInfo.characterCount == 0)
            return 0;

        int validPos = Mathf.Min(pos, textInfo.characterCount - 1);

        if (validPos >= 0 && validPos < textInfo.characterCount)
            return textInfo.characterInfo[validPos].lineNumber;

        return 0;
    }

    public override Vector3 GetCharacterPosition(int pos, bool local)
    {
        var textInfo = c.textInfo;

        if (textInfo.characterCount == 0)
            return c.transform.position;

        //c.ForceMeshUpdate(true);


        pos = Mathf.Clamp(pos, 0, textInfo.characterCount - 1);

        TMP_CharacterInfo charInfo = textInfo.characterInfo[pos];
        var localPos = new Vector3(0, charInfo.baseLine,0) ;// (charInfo.bottomLeft + charInfo.topLeft) * 0.5f;
        return local ? localPos : c.transform.TransformPoint(localPos);
    }
}

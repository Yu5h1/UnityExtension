using UnityEngine;
using UnityEngine.UI;
using Yu5h1Lib;
using Yu5h1Lib.Common;
using Yu5h1Lib.Runtime;
using Yu5h1Lib.UI;

public sealed class UI_TextOps : TextOps<Text>
{
    public UI_TextOps(Text t) : base(t) { }

    public override string text { get => c.text; set => c.text = value; }
    public override float preferredWidth => c.preferredWidth;    
    public override float preferredHeight => c.preferredHeight;

    public override float GetTextWidth(bool forceUpdate)
    {
        if (!forceUpdate)
            return preferredWidth;
        TextGenerator textGenerator = c.cachedTextGeneratorForLayout;
        textGenerator.Populate(c.text, c.GetGenerationSettings(c.rectTransform.rect.size));
        float width = 0;
        for (int i = 0; i < textGenerator.verts.Count; i += 4)
        {
            Vector3 vertex1 = textGenerator.verts[i].position;
            Vector3 vertex2 = textGenerator.verts[i + 2].position;
            width += Mathf.Abs(vertex2.x - vertex1.x);
        }
        return width;
    }
    public override Color color { get => c.color; set => c.color = value; }

    public override float fontSize { get => c.fontSize; set => c.fontSize = (int)value; }
    public override float lineSpacing { get => c.lineSpacing; set => c.lineSpacing = value; }

    public override Alignment alignment
    {
        get => c.alignment.ToAlignment();
        set => c.alignment = value.ToTextAnchor();
    }


    public override CanvasRenderer canvasRenderer => c.canvasRenderer;

    public override void SetWrappingOverflowMode(bool wrap)
    {
        if (wrap)
        {
            c.horizontalOverflow = HorizontalWrapMode.Wrap;
            c.verticalOverflow = VerticalWrapMode.Overflow;
        }
        else
        {
            c.horizontalOverflow = HorizontalWrapMode.Overflow;
            c.verticalOverflow = VerticalWrapMode.Truncate;
        }
    }

    public override void CrossFadeAlpha(float a, float d, bool ig)
        => c.CrossFadeAlphaFixed(a, d, ig);

    public override void ForceUpdate() {}

    public override float GetLineY(int index,bool local)
    {
        var line = c.cachedTextGenerator.GetLinesArray()[0];
        var y = line.topY - line.height;
        return local ? y : c.transform.TransformPoint(0, y, 0).y;
    }

    public override float GetBaseLineHeight()
        => c.cachedTextGenerator.GetLinesArray()[0].height;
    
    public override void SetLayoutDirty() => c.SetLayoutDirty();

    public override int GetLineCount() => c.cachedTextGenerator.lineCount;
    public override int GetLineIndexByPosition(int position)
    {
        if (RawComponent == null || text.IsEmpty())
            return 0;

        var textGen = c.cachedTextGenerator;

        var lineCount = GetLineCount();
        if (GetLineCount() == 0)
        {
            SetLayoutDirty();
            Canvas.ForceUpdateCanvases();
        }

        position = Mathf.Clamp(position, 0, text.Length);

        for (int i = 0; i < lineCount; i++)
        {
            var lineInfo = textGen.lines[i];

            int nextLineStart = (i + 1 < textGen.lineCount) ?
                textGen.lines[i + 1].startCharIdx : text.Length;

            if (position >= lineInfo.startCharIdx && position < nextLineStart)
                return i;
        }
        return Mathf.Max(0, textGen.lineCount - 1);
    }

    public override Vector3 GetCharacterPosition(int pos, bool local)
    {
        var textGen = c.cachedTextGenerator;

        c.SetLayoutDirty();
        Canvas.ForceUpdateCanvases();

        if (textGen.characterCount == 0)
            return c.transform.position;

        pos = Mathf.Clamp(pos, 0, textGen.characterCount);

        Vector2 localPos;

        if (pos >= textGen.characterCount)
        {
            UICharInfo lastChar = textGen.characters[textGen.characterCount - 1];
            localPos = new Vector2(lastChar.cursorPos.x + lastChar.charWidth, lastChar.cursorPos.y);
        }
        else
        {
            UICharInfo charInfo = textGen.characters[pos];
            localPos = charInfo.cursorPos;
        }

        return local ? localPos : c.transform.TransformPoint(localPos);
    }


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

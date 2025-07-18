using UnityEngine;
using Yu5h1Lib.Runtime;

namespace Yu5h1Lib.UI
{
    public class TextAdapter : UI_Adapter<ITextOps>, ITextOps, ITextAttribute
    {
        public string text { get => Ops.text; set => Ops.text = value; }
        public Color color { get => Ops.color; set => Ops.color = value; }
        public float fontSize { get => Ops.fontSize; set => Ops.fontSize = value; }
        public float lineSpacing { get => Ops.lineSpacing; set => Ops.lineSpacing = value; }
        public Alignment alignment { get => Ops.alignment; set => Ops.alignment = value; }

        public CanvasRenderer canvasRenderer => Ops.canvasRenderer;

        public void CrossFadeAlpha(float alpha, float duration, bool ignoreTimeScale) => Ops.CrossFadeAlpha(alpha, duration, ignoreTimeScale);
        public float GetActualFontSize() => Ops.GetActualFontSize();
        public float GetWrapDistance() => Ops.GetWrapDistance();
        public float GetFirstLineOffsetY() => Ops.GetFirstLineOffsetY();

        public void Append(string content) => text += $"\n{content}";
    } 
}

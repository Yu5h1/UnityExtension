using UnityEngine;
using Yu5h1Lib.Runtime;

namespace Yu5h1Lib.UI
{
    public class TextAdapter : UI_Adapter<ITextOps>, ITextOps, ITextAttribute,IBindable
    {
        public string CharactersToTrim;
        public string text { get => Ops.text.Trim(ParseTrimCharacters()); set => Ops.text = value; }
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

        public char[] ParseTrimCharacters()
            => CharactersToTrim.Replace("\\n", "\n")       // 換行
                               .Replace("\\r", "\r")       // 回車
                               .Replace("\\t", "\t")       // Tab
                               .Replace("\\v", "\v")       // 垂直 Tab
                               .Replace("\\f", "\f")       // 換頁
                               .Replace("\\b", "\b")       // 退格
                               .Replace("\\0", "\0")       // 空字元
                               .Replace("\\\"", "\"")      // 雙引號
                               .Replace("\\'", "\'")       // 單引號
                               .Replace("\\\\", "\\")      // 反斜線 (最後處理)
                               .ToCharArray();

        #region IBingable
        public string GetFieldName() => gameObject.name;
        public string GetValue() => text;
        public void SetValue(string value) => text = value;
        #endregion
    }
}

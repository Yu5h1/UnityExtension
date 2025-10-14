using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;
using Yu5h1Lib.Runtime;

namespace Yu5h1Lib.UI
{
    [DisallowMultipleComponent]
    public class TextAdapter : UI_Adapter<ITextOps>, ITextOps, ITextAttribute,IBindable
    {
        public string CharactersToTrim;
        public string text { get => Ops.text.Trim(Regex.Unescape(CharactersToTrim).ToCharArray()); set => Ops.text = value; }
        public Color color { get => Ops.color; set => Ops.color = value; }
        public float fontSize { get => Ops.fontSize; set => Ops.fontSize = value; }
        public float lineSpacing { get => Ops.lineSpacing; set => Ops.lineSpacing = value; }
        public Alignment alignment { get => Ops.alignment; set => Ops.alignment = value; }

        public CanvasRenderer canvasRenderer => Ops.canvasRenderer;

        

        public void CrossFadeAlpha(float alpha, float duration, bool ignoreTimeScale) => Ops.CrossFadeAlpha(alpha, duration, ignoreTimeScale);

        #region Ops
        public float preferredWidth => Ops.preferredWidth;
        public float preferredHeight => Ops.preferredHeight;
        public float GetTextWidth(bool forceUpdate) => Ops.GetTextWidth(forceUpdate);
        public float GetLineY(int index,bool local) => Ops.GetLineY(index,local);
        public float GetBaseLineHeight() => Ops.GetBaseLineHeight();
        public int GetLineCount() => Ops.GetLineCount();
        public int GetLineIndexByPosition(int pos) => Ops.GetLineIndexByPosition(pos);
        public void ForceUpdate() => Ops.ForceUpdate();
        public void SetLayoutDirty() => Ops.SetLayoutDirty();
        public void SetWrappingOverflowMode(bool wrap) => Ops.SetWrappingOverflowMode(wrap);
        public Vector3 GetCharacterPosition(int pos, bool local) => Ops.GetCharacterPosition(pos, local);
        #endregion

        public void Append(string content) => text += $"\n{content}";
        public void Show(string content)
        {
            text = content;
            gameObject.SetActive(true);
        }
        #region IBingable
        public string GetFieldName() => gameObject.name;
        public string GetValue() => text;
        public void SetValue(string value) => text = value;
        public void SetValue(Object bindable)
        {
            if (bindable is IBindable Ibindable)
                SetValue(Ibindable.GetValue());
        }
        #endregion           

        public void Invoke(InlineEvent inlineEvent)
            => inlineEvent?.Invoke();
    }
}
